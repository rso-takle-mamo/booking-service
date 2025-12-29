using Microsoft.EntityFrameworkCore;
using BookingService.Database.Entities;
using BookingService.Database.Repositories.Interfaces;

namespace BookingService.Database.Repositories.Implementation;

public class ServiceRepository(BookingDbContext context) : IServiceRepository
{
    public async Task<Service?> GetByIdAsync(Guid id)
    {
        return await context.Services
            .AsNoTracking()
            .Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<List<Service>> GetByTenantIdAsync(Guid tenantId)
    {
        return await context.Services
            .AsNoTracking()
            .Include(s => s.Category)
            .Where(s => s.TenantId == tenantId)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<List<Service>> GetByCategoryIdAsync(Guid categoryId)
    {
        return await context.Services
            .AsNoTracking()
            .Include(s => s.Category)
            .Where(s => s.CategoryId == categoryId)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<Service> CreateAsync(Service service)
    {
        context.Services.Add(service);
        await context.SaveChangesAsync();
        return service;
    }

    public async Task<Service> UpdateAsync(Service service)
    {
        context.Services.Update(service);
        await context.SaveChangesAsync();
        return service;
    }

    public async Task DeleteAsync(Service service)
    {
        context.Services.Remove(service);
        await context.SaveChangesAsync();
    }
}