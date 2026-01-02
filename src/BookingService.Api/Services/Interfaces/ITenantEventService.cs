using BookingService.Api.Events.Tenant;

namespace BookingService.Api.Services.Interfaces;

public interface ITenantEventService
{
    Task HandleTenantCreatedEventAsync(TenantCreatedEvent tenantEvent);
    Task HandleTenantUpdatedEventAsync(TenantUpdatedEvent tenantEvent);
}
