using BookingService.Database.Entities;

namespace BookingService.Api.Responses;

public class BookingResponse
{
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; set; }

    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid TenantId { get; set; }

    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid OwnerId { get; set; }

    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid ServiceId { get; set; }

    /// <example>2026-12-25T10:00:00Z</example>
    public DateTime StartDateTime { get; set; }

    /// <example>2026-12-25T10:30:00Z</example>
    public DateTime EndDateTime { get; set; }

    /// <example>Pending</example>
    public BookingStatus Status { get; set; }

    /// <example>Customer notes for the appointment</example>
    public string? Notes { get; set; }

    /// <example>2024-12-20T09:00:00Z</example>
    public DateTime CreatedAt { get; set; }

    /// <example>2024-12-20T09:00:00Z</example>
    public DateTime? UpdatedAt { get; set; }

    // ServiceName to be added later when needed
}