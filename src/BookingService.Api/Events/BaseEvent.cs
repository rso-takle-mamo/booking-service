namespace BookingService.Api.Events;

public abstract class BaseEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
}
