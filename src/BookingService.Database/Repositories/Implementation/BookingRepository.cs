using Microsoft.EntityFrameworkCore;
using BookingService.Database.Entities;
using BookingService.Database.Repositories.Interfaces;

namespace BookingService.Database.Repositories.Implementation;

public class BookingRepository(BookingDbContext context) : IBookingRepository
{
    public async Task<Booking?> GetByIdAsync(Guid id)
    {
        return await context.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<List<Booking>> GetByTenantIdAsync(Guid tenantId)
    {
        return await context.Bookings
            .AsNoTracking()
            .Where(b => b.TenantId == tenantId)
            .OrderBy(b => b.StartDateTime)
            .ToListAsync();
    }

    public async Task<List<Booking>> GetByOwnerIdAsync(Guid ownerId)
    {
        return await context.Bookings
            .AsNoTracking()
            .Where(b => b.OwnerId == ownerId)
            .OrderBy(b => b.StartDateTime)
            .ToListAsync();
    }

    public async Task<List<Booking>> GetByServiceIdAsync(Guid serviceId)
    {
        return await context.Bookings
            .AsNoTracking()
            .Where(b => b.ServiceId == serviceId)
            .OrderBy(b => b.StartDateTime)
            .ToListAsync();
    }

    public async Task<List<Booking>> GetBookingsByTenantAndDateRangeAsync(Guid tenantId, DateTime startDate, DateTime endDate)
    {
        return await context.Bookings
            .AsNoTracking()
            .Where(b => b.TenantId == tenantId &&
                        b.StartDateTime < endDate &&
                        b.EndDateTime > startDate)
            .OrderBy(b => b.StartDateTime)
            .ToListAsync();
    }

    public async Task<List<Booking>> GetBookingsByStatusAsync(Guid tenantId, BookingStatus status)
    {
        return await context.Bookings
            .AsNoTracking()
            .Where(b => b.TenantId == tenantId && b.BookingStatus == status)
            .OrderBy(b => b.StartDateTime)
            .ToListAsync();
    }

    public async Task<bool> IsTimeSlotAvailableAsync(Guid tenantId, Guid? excludeBookingId, DateTime startDateTime, DateTime endDateTime)
    {
        var query = context.Bookings
            .Where(b => b.TenantId == tenantId &&
                        b.BookingStatus != BookingStatus.Cancelled &&
                        b.StartDateTime < endDateTime &&
                        b.EndDateTime > startDateTime);

        if (excludeBookingId.HasValue)
        {
            query = query.Where(b => b.Id != excludeBookingId.Value);
        }

        return !await query.AnyAsync();
    }

    public async Task<Booking> CreateAsync(Booking booking)
    {
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        return booking;
    }

    public async Task<Booking> UpdateAsync(Booking booking)
    {
        context.Bookings.Update(booking);
        await context.SaveChangesAsync();
        return booking;
    }

    public async Task DeleteAsync(Booking booking)
    {
        context.Bookings.Remove(booking);
        await context.SaveChangesAsync();
    }
}