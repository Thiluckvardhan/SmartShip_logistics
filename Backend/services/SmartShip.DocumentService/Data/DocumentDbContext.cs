using Microsoft.EntityFrameworkCore;
using SmartShip.DocumentService.Models;

namespace SmartShip.DocumentService.Data;

public class DocumentDbContext(DbContextOptions<DocumentDbContext> options) : DbContext(options)
{
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DeliveryProof> DeliveryProofs => Set<DeliveryProof>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(x => x.DocumentId);

            entity.Property(x => x.FileName)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(x => x.FilePath)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(x => x.DocumentType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.ContentType)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.UploadedAt)
                .IsRequired();
        });

        modelBuilder.Entity<DeliveryProof>(entity =>
        {
            entity.HasKey(x => x.ProofId);

            entity.Property(x => x.FilePath)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(x => x.SignerName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Notes)
                .HasMaxLength(1000);

            entity.Property(x => x.Timestamp)
                .IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}
