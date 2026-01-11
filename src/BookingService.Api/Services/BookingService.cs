using BookingService.Api.Services.Interfaces;
using BookingService.Api.Responses;
using BookingService.Api.Requests;
using BookingService.Api.Events.Booking;
using BookingService.Database.Entities;
using BookingService.Database.Repositories.Interfaces;
using BookingService.Api.Exceptions;
using BookingService.Api.Services.Grpc;
using System.Linq;

namespace BookingService.Api.Services;

public class BookingService(
    IBookingRepository bookingRepository,
    IServiceRepository serviceRepository,
    IAvailabilityGrpcClient availabilityGrpcClient,
    IKafkaProducerService kafkaProducerService,
    ILogger<BookingService> logger): IBookingService
{
    public async Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request, Guid userId, Guid tenantId)
    {
        if (request.StartDateTime <= DateTime.UtcNow)
        {
            throw new ValidationException("Booking start time must be in the future");
        }

        var service = await serviceRepository.GetByIdAsync(request.ServiceId);
        if (service == null)
        {
            throw new NotFoundException("Service", request.ServiceId);
        }

        if (service.TenantId != tenantId)
        {
            throw new AuthorizationException("Service", "access", "You can only book services from your own tenant");
        }

        if (!service.IsActive)
        {
            throw new ConflictException("SERVICE_INACTIVE", "The selected service is not currently available for booking");
        }

        var endDateTime = request.StartDateTime.AddMinutes(service.DurationMinutes);

        var availabilityRequest = new TimeSlotRequest
        {
            TenantId = tenantId.ToString(),
            ServiceId = request.ServiceId.ToString(),
            StartTime = request.StartDateTime,
            EndTime = endDateTime
        };

        logger.LogInformation("Checking availability for booking - Tenant: {TenantId}, Service: {ServiceId}, Start: {StartTime}, End: {EndTime}",
            tenantId, request.ServiceId, request.StartDateTime, endDateTime);

        // Synchronous grpc call - blocks until availability service responds
        var availabilityResponse = await availabilityGrpcClient.CheckAvailabilityAsync(availabilityRequest);

        if (!availabilityResponse.IsAvailable)
        {
            var conflictDescriptions = availabilityResponse.Conflicts
                .Select(c => $"{c.Type}: {c.OverlapStart:HH:mm} - {c.OverlapEnd:HH:mm}");

            logger.LogWarning("Time slot not available for booking. Conflicts: {Conflicts}",
                string.Join(", ", conflictDescriptions));

            var errorMessage = availabilityResponse.Conflicts.Count > 0
                ? $"The requested time slot is not available due to the following conflicts: {string.Join(", ", conflictDescriptions)}"
                : "The requested time slot is not available";

            throw new ConflictException("SLOT_UNAVAILABLE", errorMessage);
        }

        var booking = new Booking
        {
            TenantId = tenantId,
            OwnerId = userId,
            ServiceId = request.ServiceId,
            StartDateTime = request.StartDateTime,
            EndDateTime = endDateTime,
            BookingStatus = BookingStatus.Pending,
            Notes = request.Notes
        };

        var createdBooking = await bookingRepository.CreateAsync(booking);

        var bookingCreatedEvent = new BookingCreatedEvent
        {
            BookingId = createdBooking.Id,
            TenantId = createdBooking.TenantId,
            OwnerId = createdBooking.OwnerId,
            ServiceId = createdBooking.ServiceId,
            StartDateTime = createdBooking.StartDateTime,
            EndDateTime = createdBooking.EndDateTime,
            BookingStatus = createdBooking.BookingStatus,
            Notes = createdBooking.Notes
        };
        await kafkaProducerService.PublishBookingEventAsync(bookingCreatedEvent);

        return MapToResponse(createdBooking);
    }

    public async Task<BookingResponse?> GetBookingByIdAsync(Guid id, Guid userId, Guid tenantId, string userRole)
    {
        var booking = await bookingRepository.GetByIdAsync(id);
        if (booking == null)
        {
            return null;
        }

        if (userRole.Equals("Customer", StringComparison.OrdinalIgnoreCase))
        {
            if (booking.OwnerId != userId)
            {
                throw new AuthorizationException("Booking", "read", "You can only view your own bookings");
            }
        }
        else
        {
            if (booking.TenantId != tenantId)
            {
                throw new AuthorizationException("Booking", "read", "You can only view bookings from your tenant");
            }
        }

        return MapToResponse(booking);
    }

    public async Task<(PaginatedResponse<BookingResponse> Bookings, int TotalCount)> GetBookingsAsync(
        GetBookingsRequest request,
        Guid userId,
        Guid? tenantId,
        string userRole)
    {
        List<Booking> bookings;

        if (userRole.Equals("Customer", StringComparison.OrdinalIgnoreCase))
        {
            bookings = await bookingRepository.GetByOwnerIdAsync(userId);

            if (tenantId.HasValue)
            {
                bookings = bookings.Where(b => b.TenantId == tenantId.Value).ToList();
            }
        }
        else
        {
            if (!tenantId.HasValue)
            {
                throw new AuthorizationException("Booking", "read", "Providers must have a tenant ID.");
            }
            bookings = await bookingRepository.GetByTenantIdAsync(tenantId.Value);
        }

        bookings = ApplyFilters(bookings, request);

        var totalCount = bookings.Count;

        bookings = bookings.OrderBy(b => b.StartDateTime).ToList();

        var paginatedBookings = bookings
            .Skip(request.Offset)
            .Take(request.Limit)
            .ToList();

        var response = new PaginatedResponse<BookingResponse>
        {
            Offset = request.Offset,
            Limit = request.Limit,
            TotalCount = totalCount,
            Data = paginatedBookings.Select(MapToResponse).ToList()
        };

        return (response, totalCount);
    }

    public async Task<BookingResponse> CancelBookingAsync(Guid id, Guid userId)
    {
        var booking = await bookingRepository.GetByIdAsync(id);
        if (booking == null)
        {
            throw new NotFoundException("Booking", id);
        }

        if (booking.OwnerId != userId)
        {
            throw new AuthorizationException("Booking", "cancel", "You can only cancel your own bookings");
        }

        if (booking.BookingStatus == BookingStatus.Cancelled)
        {
            throw new ConflictException("Status", "Booking is already cancelled");
        }

        if (booking.BookingStatus == BookingStatus.Completed)
        {
            throw new ConflictException("Status", "Cannot cancel a completed booking");
        }

        booking.BookingStatus = BookingStatus.Cancelled;
        var updatedBooking = await bookingRepository.UpdateAsync(booking);

        var bookingCancelledEvent = new BookingCancelledEvent
        {
            BookingId = updatedBooking.Id,
            TenantId = updatedBooking.TenantId,
            OwnerId = updatedBooking.OwnerId,
            ServiceId = updatedBooking.ServiceId
        };
        await kafkaProducerService.PublishBookingEventAsync(bookingCancelledEvent);

        return MapToResponse(updatedBooking);
    }

    private static List<Booking> ApplyFilters(List<Booking> bookings, GetBookingsRequest request)
    {
        if (request.StartDate.HasValue)
        {
            bookings = bookings.Where(b => b.StartDateTime >= request.StartDate.Value).ToList();
        }

        if (request.EndDate.HasValue)
        {
            bookings = bookings.Where(b => b.EndDateTime <= request.EndDate.Value).ToList();
        }

        if (request.Status.HasValue)
        {
            bookings = bookings.Where(b => b.BookingStatus == request.Status.Value).ToList();
        }

        return bookings;
    }

    private static BookingResponse MapToResponse(Booking booking)
    {
        return new BookingResponse
        {
            Id = booking.Id,
            TenantId = booking.TenantId,
            OwnerId = booking.OwnerId,
            ServiceId = booking.ServiceId,
            StartDateTime = booking.StartDateTime,
            EndDateTime = booking.EndDateTime,
            Status = booking.BookingStatus,
            Notes = booking.Notes,
            CreatedAt = booking.CreatedAt,
            UpdatedAt = booking.UpdatedAt
        };
    }
}