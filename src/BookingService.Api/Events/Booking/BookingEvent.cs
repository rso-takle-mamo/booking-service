using BookingService.Database.Entities;

namespace BookingService.Api.Events.Booking;

public class BookingEvent : BaseEvent
{
    public Guid BookingId { get; set; }
    public Guid TenantId { get; set; }
    public Guid OwnerId { get; set; }
    public Guid ServiceId { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public BookingStatus BookingStatus { get; set; }
    public string? Notes { get; set; }
}

public class BookingCreatedEvent : BookingEvent
{
    public BookingCreatedEvent()
    {
        EventType = nameof(BookingCreatedEvent);
    }
}

public class BookingCancelledEvent : BaseEvent
{
    public BookingCancelledEvent()
    {
        EventType = nameof(BookingCancelledEvent);
    }

    public Guid BookingId { get; set; }
    public Guid TenantId { get; set; }
    public Guid OwnerId { get; set; }
    public Guid ServiceId { get; set; }
}
