using BookingService.Api.Responses;
using BookingService.Api.Requests;

namespace BookingService.Api.Services.Interfaces;

public interface IBookingService
{
    Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request, Guid userId, Guid tenantId);
    Task<BookingResponse?> GetBookingByIdAsync(Guid id, Guid userId, Guid tenantId, string userRole);
    Task<(PaginatedResponse<BookingResponse> Bookings, int TotalCount)> GetBookingsAsync(
        GetBookingsRequest request,
        Guid userId,
        Guid? tenantId,
        string userRole);
    Task<BookingResponse> CancelBookingAsync(Guid id, Guid userId);
}