namespace BookingService.Api.Events.Tenant;

public class TenantEvent : BaseEvent
{
    public Guid TenantId { get; set; }
    public Guid OwnerId { get; set; }
    public string VatNumber { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string? BusinessEmail { get; set; }
    public string? BusinessPhone { get; set; }
    public string? Address { get; set; }
    public string? Description { get; set; }
}

public class TenantCreatedEvent : TenantEvent
{
    public TenantCreatedEvent()
    {
        EventType = nameof(TenantCreatedEvent);
    }
}

public class TenantUpdatedEvent : TenantEvent
{
    public TenantUpdatedEvent()
    {
        EventType = nameof(TenantUpdatedEvent);
    }
}
