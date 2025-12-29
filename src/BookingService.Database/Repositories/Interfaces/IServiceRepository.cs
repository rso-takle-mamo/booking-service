using BookingService.Database.Entities;

namespace BookingService.Database.Repositories.Interfaces;

public interface IServiceRepository
{
    Task<Service?> GetByIdAsync(Guid id);
    Task<List<Service>> GetByTenantIdAsync(Guid tenantId);
    Task<List<Service>> GetByCategoryIdAsync(Guid categoryId);
    Task<Service> CreateAsync(Service service);
    Task<Service> UpdateAsync(Service service);
    Task DeleteAsync(Service service);
}