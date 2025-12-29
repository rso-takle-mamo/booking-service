using Microsoft.Extensions.DependencyInjection;
using BookingService.Database.Repositories.Interfaces;
using BookingService.Database.Repositories.Implementation;

namespace BookingService.Database;

public static class BookingDatabaseExtensions
{
    public static void AddBookingDatabase(this IServiceCollection services)
    {
        services.AddDbContext<BookingDbContext>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
    }
}