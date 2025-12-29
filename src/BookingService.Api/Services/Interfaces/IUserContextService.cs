namespace BookingService.Api.Services.Interfaces;

public interface IUserContextService
{
    Guid? GetTenantId();
    Guid GetUserId();
    string GetRole();
    bool IsCustomer();
    void ValidateCustomerAccess();
    void ValidateTenantAccess(Guid tenantId, string resource);
}