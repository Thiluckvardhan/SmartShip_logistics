using Microsoft.EntityFrameworkCore;
using SmartShip.AdminService.Models;

namespace SmartShip.AdminService.Data;

public class AdminDbContext(DbContextOptions<AdminDbContext> options) : DbContext(options)
{
    public DbSet<ExceptionRecord> ExceptionRecords => Set<ExceptionRecord>();
    public DbSet<Hub> Hubs => Set<Hub>();
    public DbSet<ServiceLocation> ServiceLocations => Set<ServiceLocation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExceptionRecord>(entity =>
        {
            entity.HasKey(x => x.ExceptionId);

            entity.Property(x => x.ExceptionType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Description)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(x => x.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(x => x.CreatedAt)
                .IsRequired();
        });

        modelBuilder.Entity<Hub>(entity =>
        {
            entity.HasKey(x => x.HubId);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Address)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(x => x.ContactNumber)
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(x => x.ManagerName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.Property(x => x.IsActive)
                .IsRequired();
        });

        modelBuilder.Entity<ServiceLocation>(entity =>
        {
            entity.HasKey(x => x.LocationId);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.ZipCode)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(x => x.IsActive)
                .IsRequired();

            entity.HasOne(x => x.Hub)
                .WithMany(x => x.ServiceLocations)
                .HasForeignKey(x => x.HubId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        base.OnModelCreating(modelBuilder);
    }
}
