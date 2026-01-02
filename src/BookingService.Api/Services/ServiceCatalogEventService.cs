using Microsoft.Extensions.Logging;
using BookingService.Api.Events.Service;
using BookingService.Api.Events.Category;
using BookingService.Api.Services.Interfaces;
using BookingService.Database.Repositories.Interfaces;
using BookingService.Database.Entities;

namespace BookingService.Api.Services;

public class ServiceCatalogEventService(
    ILogger<ServiceCatalogEventService> logger,
    IServiceRepository serviceRepository,
    ICategoryRepository categoryRepository) : IServiceCatalogEventService
{
    // Service event handlers
    public async Task HandleServiceCreatedEventAsync(ServiceCreatedEvent serviceEvent)
    {
        logger.LogInformation("Handling service created event for service ID: {ServiceId}", serviceEvent.ServiceId);

        try
        {
            var existingService = await serviceRepository.GetByIdAsync(serviceEvent.ServiceId);
            if (existingService != null)
            {
                logger.LogWarning("Service with ID {ServiceId} already exists, skipping creation", serviceEvent.ServiceId);
                return;
            }

            var service = new Service
            {
                Id = serviceEvent.ServiceId,
                TenantId = serviceEvent.TenantId,
                Name = serviceEvent.Name,
                Description = serviceEvent.Description,
                Price = serviceEvent.Price,
                DurationMinutes = serviceEvent.DurationMinutes,
                CategoryId = serviceEvent.CategoryId,
                IsActive = serviceEvent.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await serviceRepository.CreateAsync(service);
            logger.LogInformation("Successfully created service {ServiceId} in booking database", serviceEvent.ServiceId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling service created event for service ID: {ServiceId}", serviceEvent.ServiceId);
            throw;
        }
    }

    public async Task HandleServiceEditedEventAsync(ServiceEditedEvent serviceEvent)
    {
        logger.LogInformation("Handling service edited event for service ID: {ServiceId}", serviceEvent.ServiceId);

        try
        {
            var existingService = await serviceRepository.GetByIdAsync(serviceEvent.ServiceId);
            if (existingService == null)
            {
                logger.LogWarning("Service with ID {ServiceId} not found for update", serviceEvent.ServiceId);
                return;
            }

            var updatedService = new Service
            {
                Id = serviceEvent.ServiceId,
                TenantId = serviceEvent.TenantId,
                Name = serviceEvent.Name,
                Description = serviceEvent.Description,
                Price = serviceEvent.Price,
                DurationMinutes = serviceEvent.DurationMinutes,
                CategoryId = serviceEvent.CategoryId,
                IsActive = serviceEvent.IsActive,
                CreatedAt = existingService.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };

            await serviceRepository.UpdateAsync(updatedService);
            logger.LogInformation("Successfully updated service {ServiceId} in booking database", serviceEvent.ServiceId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling service edited event for service ID: {ServiceId}", serviceEvent.ServiceId);
            throw;
        }
    }

    public async Task HandleServiceDeletedEventAsync(ServiceDeletedEvent serviceEvent)
    {
        logger.LogInformation("Handling service deleted event for service ID: {ServiceId}", serviceEvent.ServiceId);

        try
        {
            var existingService = await serviceRepository.GetByIdAsync(serviceEvent.ServiceId);
            if (existingService == null)
            {
                logger.LogWarning("Service with ID {ServiceId} not found for deletion", serviceEvent.ServiceId);
                return;
            }

            await serviceRepository.DeleteAsync(existingService);
            logger.LogInformation("Successfully deleted service {ServiceId} from booking database", serviceEvent.ServiceId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling service deleted event for service ID: {ServiceId}", serviceEvent.ServiceId);
            throw;
        }
    }

    // Category event handlers
    public async Task HandleCategoryCreatedEventAsync(CategoryCreatedEvent categoryEvent)
    {
        logger.LogInformation("Handling category created event for category ID: {CategoryId}", categoryEvent.CategoryId);

        try
        {
            var existingCategory = await categoryRepository.GetByIdAsync(categoryEvent.CategoryId);
            if (existingCategory != null)
            {
                logger.LogWarning("Category with ID {CategoryId} already exists, skipping creation", categoryEvent.CategoryId);
                return;
            }

            var category = new Category
            {
                Id = categoryEvent.CategoryId,
                TenantId = categoryEvent.TenantId,
                Name = categoryEvent.Name,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await categoryRepository.CreateAsync(category);
            logger.LogInformation("Successfully created category {CategoryId} in booking database", categoryEvent.CategoryId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling category created event for category ID: {CategoryId}", categoryEvent.CategoryId);
            throw;
        }
    }

    public async Task HandleCategoryEditedEventAsync(CategoryEditedEvent categoryEvent)
    {
        logger.LogInformation("Handling category edited event for category ID: {CategoryId}", categoryEvent.CategoryId);

        try
        {
            var existingCategory = await categoryRepository.GetByIdAsync(categoryEvent.CategoryId);
            if (existingCategory == null)
            {
                logger.LogWarning("Category with ID {CategoryId} not found for update", categoryEvent.CategoryId);
                return;
            }

            var updatedCategory = new Category
            {
                Id = categoryEvent.CategoryId,
                TenantId = categoryEvent.TenantId,
                Name = categoryEvent.Name,
                CreatedAt = existingCategory.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };

            await categoryRepository.UpdateAsync(updatedCategory);
            logger.LogInformation("Successfully updated category {CategoryId} in booking database", categoryEvent.CategoryId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling category edited event for category ID: {CategoryId}", categoryEvent.CategoryId);
            throw;
        }
    }

    public async Task HandleCategoryDeletedEventAsync(CategoryDeletedEvent categoryEvent)
    {
        logger.LogInformation("Handling category deleted event for category ID: {CategoryId}", categoryEvent.CategoryId);

        try
        {
            var existingCategory = await categoryRepository.GetByIdAsync(categoryEvent.CategoryId);
            if (existingCategory == null)
            {
                logger.LogWarning("Category with ID {CategoryId} not found for deletion", categoryEvent.CategoryId);
                return;
            }

            await categoryRepository.DeleteAsync(existingCategory);
            logger.LogInformation("Successfully deleted category {CategoryId} from booking database", categoryEvent.CategoryId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling category deleted event for category ID: {CategoryId}", categoryEvent.CategoryId);
            throw;
        }
    }
}
