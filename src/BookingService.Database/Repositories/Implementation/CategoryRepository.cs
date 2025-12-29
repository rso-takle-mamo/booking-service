using Microsoft.EntityFrameworkCore;
using BookingService.Database.Entities;
using BookingService.Database.Repositories.Interfaces;

namespace BookingService.Database.Repositories.Implementation;

public class CategoryRepository(BookingDbContext context) : ICategoryRepository
{
    public async Task<Category?> GetByIdAsync(Guid id)
    {
        return await context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<Category>> GetByTenantIdAsync(Guid tenantId)
    {
        return await context.Categories
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Category> CreateAsync(Category category)
    {
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        return category;
    }

    public async Task<Category> UpdateAsync(Category category)
    {
        context.Categories.Update(category);
        await context.SaveChangesAsync();
        return category;
    }

    public async Task DeleteAsync(Category category)
    {
        context.Categories.Remove(category);
        await context.SaveChangesAsync();
    }
}