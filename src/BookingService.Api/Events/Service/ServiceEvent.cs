namespace BookingService.Api.Events.Service;

public class ServiceEvent : BaseEvent
{
    public Guid ServiceId { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public Guid? CategoryId { get; set; }
    public bool IsActive { get; set; }
}

public class ServiceCreatedEvent : ServiceEvent
{
    public ServiceCreatedEvent()
    {
        EventType = nameof(ServiceCreatedEvent);
    }
}

public class ServiceEditedEvent : ServiceEvent
{
    public ServiceEditedEvent()
    {
        EventType = nameof(ServiceEditedEvent);
    }
}

public class ServiceDeletedEvent : BaseEvent
{
    public ServiceDeletedEvent()
    {
        EventType = nameof(ServiceDeletedEvent);
    }

    public Guid ServiceId { get; set; }
    public Guid TenantId { get; set; }
}
