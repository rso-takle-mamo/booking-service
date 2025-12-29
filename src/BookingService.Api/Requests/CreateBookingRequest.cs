using System.ComponentModel.DataAnnotations;

namespace BookingService.Api.Requests;

public class CreateBookingRequest
{
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    [Required(ErrorMessage = "Service ID is required")]
    public required Guid ServiceId { get; set; }

    /// <example>2026-12-25T10:00:00Z</example>
    [Required(ErrorMessage = "Start date time is required")]
    public required DateTime StartDateTime { get; set; }

    /// <example>Notes for the appointment</example>
    [MaxLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string? Notes { get; set; }

    /// <summary>
    /// Tenant ID - Required for customers (specifies which tenant's service to book),
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid? TenantId { get; set; }
}