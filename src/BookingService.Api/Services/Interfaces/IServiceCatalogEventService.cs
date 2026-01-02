using BookingService.Api.Events.Service;
using BookingService.Api.Events.Category;

namespace BookingService.Api.Services.Interfaces;

public interface IServiceCatalogEventService
{
    Task HandleServiceCreatedEventAsync(ServiceCreatedEvent serviceEvent);
    Task HandleServiceEditedEventAsync(ServiceEditedEvent serviceEvent);
    Task HandleServiceDeletedEventAsync(ServiceDeletedEvent serviceEvent);
    Task HandleCategoryCreatedEventAsync(CategoryCreatedEvent categoryEvent);
    Task HandleCategoryEditedEventAsync(CategoryEditedEvent categoryEvent);
    Task HandleCategoryDeletedEventAsync(CategoryDeletedEvent categoryEvent);
}
