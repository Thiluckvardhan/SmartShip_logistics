using Microsoft.EntityFrameworkCore;
using SmartShip.ShipmentService.Models;

namespace SmartShip.ShipmentService.Data;

public class ShipmentDbContext(DbContextOptions<ShipmentDbContext> options) : DbContext(options)
{
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Package> Packages => Set<Package>();
    public DbSet<PickupSchedule> PickupSchedules => Set<PickupSchedule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.HasKey(x => x.ShipmentId);

            entity.Property(x => x.TrackingNumber)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(x => x.TrackingNumber)
                .IsUnique();

            entity.Property(x => x.TotalWeight)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(x => x.EstimatedRate)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.Property(x => x.UpdatedAt)
                .IsRequired();

            entity.HasOne(x => x.SenderAddress)
                .WithMany(x => x.SenderShipments)
                .HasForeignKey(x => x.SenderAddressId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ReceiverAddress)
                .WithMany(x => x.ReceiverShipments)
                .HasForeignKey(x => x.ReceiverAddressId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasKey(x => x.AddressId);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.Phone)
                .IsRequired()
                .HasMaxLength(25);

            entity.Property(x => x.Street)
                .IsRequired()
                .HasMaxLength(300);

            entity.Property(x => x.City)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.State)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Country)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.PostalCode)
                .IsRequired()
                .HasMaxLength(20);
        });

        modelBuilder.Entity<Package>(entity =>
        {
            entity.HasKey(x => x.PackageId);

            entity.Property(x => x.ItemName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Quantity)
                .IsRequired();

            entity.Property(x => x.Weight)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasMaxLength(500);

            entity.HasOne(x => x.Shipment)
                .WithMany(x => x.Packages)
                .HasForeignKey(x => x.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PickupSchedule>(entity =>
        {
            entity.HasKey(x => x.PickupScheduleId);

            entity.Property(x => x.PickupDate)
                .IsRequired();

            entity.Property(x => x.Notes)
                .HasMaxLength(1000);

            entity.HasOne(x => x.Shipment)
                .WithMany(x => x.PickupSchedules)
                .HasForeignKey(x => x.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        base.OnModelCreating(modelBuilder);
    }
}
