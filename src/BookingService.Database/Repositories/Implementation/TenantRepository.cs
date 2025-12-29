using Microsoft.EntityFrameworkCore;
using BookingService.Database.Entities;
using BookingService.Database.Repositories.Interfaces;

namespace BookingService.Database.Repositories.Implementation;

public class TenantRepository(BookingDbContext context) : ITenantRepository
{
    public async Task<Tenant?> GetByIdAsync(Guid id)
    {
        return await context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Tenant?> GetByBusinessNameAsync(string businessName)
    {
        return await context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.BusinessName == businessName);
    }

    public async Task<Tenant> CreateAsync(Tenant tenant)
    {
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();
        return tenant;
    }

    public async Task<Tenant> UpdateAsync(Tenant tenant)
    {
        context.Tenants.Update(tenant);
        await context.SaveChangesAsync();
        return tenant;
    }

    public async Task DeleteAsync(Tenant tenant)
    {
        context.Tenants.Remove(tenant);
        await context.SaveChangesAsync();
    }
}