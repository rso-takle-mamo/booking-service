namespace BookingService.Database.Entities;

public class Booking
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid OwnerId { get; set; }
    public Guid ServiceId { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public BookingStatus BookingStatus { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Service Service { get; set; } = null!;
}