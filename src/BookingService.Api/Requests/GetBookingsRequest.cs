using BookingService.Database.Entities;

namespace BookingService.Api.Requests;

public class GetBookingsRequest : PaginationRequest
{
    /// <example>2024-12-01T00:00:00Z</example>
    public DateTime? StartDate { get; set; }

    /// <example>2026-12-31T23:59:59Z</example>
    public DateTime? EndDate { get; set; }

    /// <example>Pending</example>
    public BookingStatus? Status { get; set; }

    /// <summary>
    /// Tenant ID - Required for customers (specifies which tenant's bookings to retrieve),
    /// forbidden for providers (automatically uses their own tenant)
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid? TenantId { get; set; }
}