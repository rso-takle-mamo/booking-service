using BookingService.Database.Entities;

namespace BookingService.Database.Repositories.Interfaces;

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id);
    Task<Tenant?> GetByBusinessNameAsync(string businessName);
    Task<Tenant> CreateAsync(Tenant tenant);
    Task<Tenant> UpdateAsync(Tenant tenant);
    Task DeleteAsync(Tenant tenant);
}