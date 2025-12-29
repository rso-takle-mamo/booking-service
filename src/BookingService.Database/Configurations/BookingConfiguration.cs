using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BookingService.Database.Entities;

namespace BookingService.Database.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.OwnerId)
            .IsRequired();

        builder.Property(e => e.ServiceId)
            .IsRequired();

        builder.Property(e => e.StartDateTime)
            .IsRequired();

        builder.Property(e => e.EndDateTime)
            .IsRequired();

        builder.Property(e => e.BookingStatus)
            .IsRequired();

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Indexes
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.OwnerId);
        builder.HasIndex(e => e.ServiceId);
        builder.HasIndex(e => new { e.TenantId, e.StartDateTime, e.EndDateTime });
        builder.HasIndex(e => e.BookingStatus);

        // Relationship with Tenant - cascade delete
        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationship with Service - restrict delete
        builder.HasOne(e => e.Service)
            .WithMany()
            .HasForeignKey(e => e.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}