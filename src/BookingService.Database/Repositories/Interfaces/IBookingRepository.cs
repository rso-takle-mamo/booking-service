using BookingService.Database.Entities;

namespace BookingService.Database.Repositories.Interfaces;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id);
    Task<List<Booking>> GetByTenantIdAsync(Guid tenantId);
    Task<List<Booking>> GetByOwnerIdAsync(Guid ownerId);
    Task<List<Booking>> GetByServiceIdAsync(Guid serviceId);
    Task<List<Booking>> GetBookingsByTenantAndDateRangeAsync(Guid tenantId, DateTime startDate, DateTime endDate);
    Task<List<Booking>> GetBookingsByStatusAsync(Guid tenantId, BookingStatus status);
    Task<bool> IsTimeSlotAvailableAsync(Guid tenantId, Guid? excludeBookingId, DateTime startDateTime, DateTime endDateTime);
    Task<Booking> CreateAsync(Booking booking);
    Task<Booking> UpdateAsync(Booking booking);
    Task DeleteAsync(Booking booking);
}