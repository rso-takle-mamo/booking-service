using Microsoft.EntityFrameworkCore;
using BookingService.Database.Entities;
using BookingService.Database.Configurations;

namespace BookingService.Database;

public class BookingDbContext : DbContext
{
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Booking> Bookings { get; set; }

    public BookingDbContext() { }

    public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured) return;

        optionsBuilder.UseNpgsql(EnvironmentVariables.GetRequiredVariable("DATABASE_CONNECTION_STRING"));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfiguration(new BookingConfiguration());

        // replicated tables
        modelBuilder.ApplyConfiguration(new TenantConfiguration());
        modelBuilder.ApplyConfiguration(new ServiceConfiguration());
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is Tenant || e.Entity is Service || e.Entity is Category || e.Entity is Booking
                && (e.State is EntityState.Added or EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            switch (entityEntry.State)
            {
                case EntityState.Added:
                    if (((dynamic)entityEntry.Entity).CreatedAt == default(DateTime))
                        ((dynamic)entityEntry.Entity).CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    ((dynamic)entityEntry.Entity).UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}