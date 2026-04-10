using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.TransactionDb;

public class TransactionDbContext(DbContextOptions<TransactionDbContext> options) : DbContext(options)
{
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TransactionKey> TransactionKeys => Set<TransactionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(x => x.Id);
            
            entity.Property(x => x.ExternalId).IsRequired().ValueGeneratedOnAdd();
            entity.Property(x => x.SourceId).IsRequired().ValueGeneratedOnAdd();
            entity.Property(x => x.Status).IsRequired().ValueGeneratedOnAdd();
            entity.Property(x => x.Category).HasConversion<string>();

            // Owned Money value object — flattened to Amount + Currency columns
            entity.OwnsOne(x => x.Amount, money =>
            {
                money.Property(m => m.Amount).HasColumnName("Amount").HasPrecision(18, 4);
                money.Property(m => m.Currency).HasColumnName("Currency").HasPrecision(18, 4);
            });
            
            //Deduplication constraint enforced at DB level
            entity.HasIndex(x => new { x.ExternalId, x.SourceId }).IsUnique();

            entity.HasIndex(x => x.OccurredAt);
            entity.HasIndex(x => x.SourceId);
            entity.HasIndex(x => x.Status);
            
            entity.Property(x => x.Metadata).HasColumnType("jsonb");
        });

        modelBuilder.Entity<TransactionKey>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ExternalId).IsRequired().ValueGeneratedOnAdd();
            entity.Property(x => x.SourceId).IsRequired().ValueGeneratedOnAdd();
            
            //Duplicate pairs (externalId, sourceId) handled at db level
            entity.HasIndex(x => new { x.ExternalId, x.SourceId }).IsUnique();
            
            entity.HasIndex(x => x.SeenAt);
        });
    }
}