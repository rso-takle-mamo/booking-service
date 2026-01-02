namespace BookingService.Api.Events.Category;

public class CategoryEvent : BaseEvent
{
    public Guid CategoryId { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CategoryCreatedEvent : CategoryEvent
{
    public CategoryCreatedEvent()
    {
        EventType = nameof(CategoryCreatedEvent);
    }
}

public class CategoryEditedEvent : CategoryEvent
{
    public CategoryEditedEvent()
    {
        EventType = nameof(CategoryEditedEvent);
    }
}

public class CategoryDeletedEvent : BaseEvent
{
    public CategoryDeletedEvent()
    {
        EventType = nameof(CategoryDeletedEvent);
    }

    public Guid CategoryId { get; set; }
    public Guid TenantId { get; set; }
}
