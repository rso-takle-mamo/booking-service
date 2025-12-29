using BookingService.Database.Entities;

namespace BookingService.Database.Repositories.Interfaces;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id);
    Task<List<Category>> GetByTenantIdAsync(Guid tenantId);
    Task<Category> CreateAsync(Category category);
    Task<Category> UpdateAsync(Category category);
    Task DeleteAsync(Category category);
}