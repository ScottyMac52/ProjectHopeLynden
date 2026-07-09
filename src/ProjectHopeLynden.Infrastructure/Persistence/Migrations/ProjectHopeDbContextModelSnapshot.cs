using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ProjectHopeLynden.Domain.Inventory;
using ProjectHopeLynden.Infrastructure.Persistence;

#nullable disable

namespace ProjectHopeLynden.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ProjectHopeDbContext))]
public sealed class ProjectHopeDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.0");

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(category => category.Id);
            entity.Property(category => category.Id).ValueGeneratedOnAdd();
            entity.Property(category => category.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(category => category.Name).IsUnique();
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.ToTable("Items");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).ValueGeneratedOnAdd();
            entity.Property(item => item.Name).HasMaxLength(150).IsRequired();
            entity.HasIndex(item => item.Name).IsUnique();
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.ToTable("Locations");
            entity.HasKey(location => location.Id);
            entity.Property(location => location.Id).ValueGeneratedOnAdd();
            entity.Property(location => location.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(location => location.Name).IsUnique();
        });

        modelBuilder.Entity<InventoryEntry>(entity =>
        {
            entity.ToTable("InventoryEntries", table =>
            {
                table.HasCheckConstraint("CK_InventoryEntries_CurrentQuantity_NonNegative", "CurrentQuantity >= 0");
            });

            entity.HasKey(entry => entry.Id);
            entity.Property(entry => entry.Id).ValueGeneratedOnAdd();
            entity.Property(entry => entry.BestByDate);
            entity.Property(entry => entry.CurrentQuantity).IsRequired();
            entity.Property(entry => entry.LastUpdatedAtUtc).IsRequired();
            entity.Property(entry => entry.IsCommodity).IsRequired();
            entity.Property(entry => entry.IsMenuItem).IsRequired();

            entity.HasOne(entry => entry.Item)
                .WithMany(item => item.InventoryEntries)
                .HasForeignKey(entry => entry.ItemId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(entry => entry.Category)
                .WithMany(category => category.InventoryEntries)
                .HasForeignKey(entry => entry.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(entry => entry.Location)
                .WithMany(location => location.InventoryEntries)
                .HasForeignKey(entry => entry.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(entry => entry.CategoryId);
            entity.HasIndex(entry => entry.LocationId);
            entity.HasIndex(entry => new { entry.ItemId, entry.IsCommodity });
        });

        modelBuilder.Entity<InventoryCountHistory>(entity =>
        {
            entity.ToTable("InventoryCountHistory", table =>
            {
                table.HasCheckConstraint("CK_InventoryCountHistory_CountedQuantity_NonNegative", "CountedQuantity >= 0");
            });

            entity.HasKey(history => history.Id);
            entity.Property(history => history.Id).ValueGeneratedOnAdd();
            entity.Property(history => history.CountedQuantity).IsRequired();
            entity.Property(history => history.CountedAtUtc).IsRequired();

            entity.HasOne(history => history.InventoryEntry)
                .WithMany(entry => entry.CountHistory)
                .HasForeignKey(history => history.InventoryEntryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(history => new { history.InventoryEntryId, history.CountedAtUtc });
        });
    }
}
