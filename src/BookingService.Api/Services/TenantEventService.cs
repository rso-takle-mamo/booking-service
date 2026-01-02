using Microsoft.Extensions.Logging;
using BookingService.Api.Events.Tenant;
using BookingService.Api.Services.Interfaces;
using BookingService.Database.Repositories.Interfaces;

namespace BookingService.Api.Services;

public class TenantEventService(
    ILogger<TenantEventService> logger,
    ITenantRepository tenantRepository) : ITenantEventService
{
    public async Task HandleTenantCreatedEventAsync(TenantCreatedEvent tenantEvent)
    {
        logger.LogInformation("Handling tenant created event for tenant ID: {TenantId}", tenantEvent.TenantId);

        try
        {
            var existingTenant = await tenantRepository.GetByIdAsync(tenantEvent.TenantId);
            if (existingTenant != null)
            {
                logger.LogWarning("Tenant with ID {TenantId} already exists, skipping creation", tenantEvent.TenantId);
                return;
            }

            var tenant = new Database.Entities.Tenant
            {
                Id = tenantEvent.TenantId,
                BusinessName = tenantEvent.BusinessName,
                Email = tenantEvent.BusinessEmail,
                Phone = tenantEvent.BusinessPhone,
                Address = tenantEvent.Address,
                TimeZone = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await tenantRepository.CreateAsync(tenant);
            logger.LogInformation("Successfully created tenant {TenantId} in booking database", tenantEvent.TenantId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling tenant created event for tenant ID: {TenantId}", tenantEvent.TenantId);
            throw;
        }
    }

    public async Task HandleTenantUpdatedEventAsync(TenantUpdatedEvent tenantEvent)
    {
        logger.LogInformation("Handling tenant updated event for tenant ID: {TenantId}", tenantEvent.TenantId);

        try
        {
            var existingTenant = await tenantRepository.GetByIdAsync(tenantEvent.TenantId);
            if (existingTenant == null)
            {
                logger.LogWarning("Tenant with ID {TenantId} not found for update", tenantEvent.TenantId);
                return;
            }

            var updatedTenant = new Database.Entities.Tenant
            {
                Id = tenantEvent.TenantId,
                BusinessName = tenantEvent.BusinessName,
                Email = tenantEvent.BusinessEmail,
                Phone = tenantEvent.BusinessPhone,
                Address = tenantEvent.Address,
                TimeZone = existingTenant.TimeZone,
                CreatedAt = existingTenant.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };

            await tenantRepository.UpdateAsync(updatedTenant);
            logger.LogInformation("Successfully updated tenant {TenantId} in booking database", tenantEvent.TenantId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling tenant updated event for tenant ID: {TenantId}", tenantEvent.TenantId);
            throw;
        }
    }
}
