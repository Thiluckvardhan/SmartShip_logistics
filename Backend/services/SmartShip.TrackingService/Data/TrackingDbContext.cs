using Microsoft.EntityFrameworkCore;
using SmartShip.TrackingService.Models;

namespace SmartShip.TrackingService.Data;

public class TrackingDbContext(DbContextOptions<TrackingDbContext> options) : DbContext(options)
{
    public DbSet<TrackingEvent> TrackingEvents => Set<TrackingEvent>();
    public DbSet<TrackingLocation> TrackingLocations => Set<TrackingLocation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TrackingEvent>(entity =>
        {
            entity.HasKey(x => x.EventId);

            entity.Property(x => x.TrackingNumber)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(x => x.Location)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Description)
                .HasMaxLength(1000);

            entity.Property(x => x.Timestamp)
                .IsRequired();
        });

        modelBuilder.Entity<TrackingLocation>(entity =>
        {
            entity.HasKey(x => x.LocationId);

            entity.Property(x => x.TrackingNumber)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Latitude)
                .IsRequired();

            entity.Property(x => x.Longitude)
                .IsRequired();

            entity.Property(x => x.Timestamp)
                .IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}
