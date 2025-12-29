using Microsoft.AspNetCore.Mvc;
using BookingService.Api.Services.Interfaces;
using BookingService.Api.Requests;
using BookingService.Api.Responses;
using BookingService.Api.Exceptions;

namespace BookingService.Api.Controllers;

[Route("api/bookings")]
public class BookingsController(
    IBookingService bookingService,
    ILogger<BookingsController> logger,
    IUserContextService userContextService)
    : BaseApiController(userContextService)
{
    /// <summary>
    /// Create a new booking (Customers only)
    /// </summary>
    /// <remarks>
    /// Creates a new booking for the specified service and time.
    /// The end time is automatically calculated based on the service duration.
    ///
    /// Only customers can create bookings.
    /// </remarks>
    /// <param name="request">The booking creation request</param>
    /// <returns>The created booking details</returns>
    [HttpPost]
    public async Task<ActionResult<BookingResponse>> CreateBooking([FromBody] CreateBookingRequest request)
    {
        ValidateCustomerAccess();

        var userId = GetUserId();

        if (!request.TenantId.HasValue)
        {
            throw new AuthorizationException("Booking", "create", "Tenant ID is required.");
        }

        var tenantId = request.TenantId.Value;

        logger.LogInformation("Creating booking for user {UserId}, service {ServiceId}, tenant {TenantId}, at {StartTime}",
            userId, request.ServiceId, tenantId, request.StartDateTime);

        var booking = await bookingService.CreateBookingAsync(request, userId, tenantId);

        return CreatedAtAction(
            nameof(GetBookingById),
            new { id = booking.Id },
            booking);
    }

    /// <summary>
    /// Get a booking by ID
    /// </summary>
    /// <remarks>
    /// **CUSTOMERS:**
    /// Can only retrieve their own bookings
    ///
    /// **PROVIDERS:**
    /// Can retrieve any booking within their tenant
    /// </remarks>
    /// <param name="id">The booking ID</param>
    /// <returns>The booking details</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingResponse>> GetBookingById(Guid id)
    {
        var userId = GetUserId();
        var isCustomer = IsCustomer();

        // For customers, don't pass tenantId - the service should validate they own the booking
        // For providers, use their tenantId to validate access
        var tenantId = isCustomer ? Guid.Empty : (GetTenantId() ?? Guid.Empty);
        var userRole = GetUserRole();

        var booking = await bookingService.GetBookingByIdAsync(id, userId, tenantId, userRole);

        if (booking == null)
        {
            return NotFound();
        }

        return Ok(booking);
    }

    /// <summary>
    /// Get bookings with pagination and filtering
    /// </summary>
    /// <remarks>
    /// **CUSTOMERS:**
    /// Can retrieve their own bookings
    /// Must provide tenantId query parameter to specify which tenant's bookings to view
    ///
    /// **PROVIDERS:**
    /// Can retrieve any bookings within their tenant
    /// tenantId parameter is forbidden (automatically uses their own tenant)
    ///
    /// Filter options:
    /// - tenantId: Required for customers, forbidden for providers
    /// - startDate: Filter bookings from this date onwards (UTC)
    /// - endDate: Filter bookings up to this date (UTC)
    /// - status: Filter by booking status (Pending, Confirmed, Completed, Cancelled)
    /// </remarks>
    /// <param name="request">The filter and pagination parameters</param>
    /// <returns>Paginated list of bookings</returns>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<BookingResponse>>> GetBookings([FromQuery] GetBookingsRequest request)
    {
        var userId = GetUserId();
        var userTenantId = GetTenantId();
        var userRole = GetUserRole();
        var isCustomer = IsCustomer();

        
        Guid tenantId;
        if (isCustomer)
        {
            // Customers must provide tenantId in query parameters
            if (!request.TenantId.HasValue)
            {
                throw new AuthorizationException("Booking", "read", "Tenant ID is required for customers to retrieve bookings.");
            }
            tenantId = request.TenantId.Value;
        }
        else
        {
            // Providers cannot provide tenantId - they only see their own tenant
            if (request.TenantId.HasValue)
            {
                throw new AuthorizationException("Booking", "read", "Providers cannot specify tenant ID. They can only view their own tenant's bookings.");
            }
            if (!userTenantId.HasValue)
            {
                throw new AuthorizationException("Booking", "read", "Providers must have a tenant ID.");
            }
            tenantId = userTenantId.Value;
        }

        logger.LogInformation("Retrieving bookings for user {UserId}, role {Role}, tenant {TenantId}, with filters: {@Request}",
            userId, userRole, tenantId, request);

        var (bookings, totalCount) = await bookingService.GetBookingsAsync(request, userId, tenantId, userRole);

        return Ok(bookings);
    }

    /// <summary>
    /// Cancel a booking (Customers only)
    /// </summary>
    /// <remarks>
    /// Cancels a booking and changes its status to Cancelled.
    /// Only customers can cancel their own bookings.
    ///
    /// Bookings can only be cancelled if they are not already cancelled or completed.
    /// </remarks>
    /// <param name="id">The booking ID to cancel</param>
    /// <returns>The updated booking details</returns>
    [HttpPut("{id:guid}/cancel")]
    public async Task<ActionResult<BookingResponse>> CancelBooking(Guid id)
    {
        ValidateCustomerAccess();

        var userId = GetUserId();

        logger.LogInformation("Cancelling booking {BookingId} for user {UserId}", id, userId);

        var booking = await bookingService.CancelBookingAsync(id, userId);
        return Ok(booking);
    }
}