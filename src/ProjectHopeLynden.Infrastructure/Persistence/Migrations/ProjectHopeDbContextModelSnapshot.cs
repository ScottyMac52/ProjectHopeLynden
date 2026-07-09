using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ProjectHopeLynden.Infrastructure.Persistence;

#nullable disable

namespace ProjectHopeLynden.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ProjectHopeDbContext))]
public sealed class ProjectHopeDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.0");

        modelBuilder.Entity("ProjectHopeLynden.Domain.Inventory.Category", entity =>
        {
            entity.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER")
                .HasAnnotation("Sqlite:Autoincrement", true);

            entity.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("TEXT");

            entity.HasKey("Id");

            entity.HasIndex("Name")
                .IsUnique();

            entity.ToTable("Categories");
        });

        modelBuilder.Entity("ProjectHopeLynden.Domain.Inventory.InventoryCountHistory", entity =>
        {
            entity.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER")
                .HasAnnotation("Sqlite:Autoincrement", true);

            entity.Property<DateTime>("CountedAtUtc")
                .HasColumnType("TEXT");

            entity.Property<int>("CountedQuantity")
                .HasColumnType("INTEGER");

            entity.Property<int>("InventoryEntryId")
                .HasColumnType("INTEGER");

            entity.Property<int?>("PreviousQuantity")
                .HasColumnType("INTEGER");

            entity.Property<int?>("QuantityChange")
                .HasColumnType("INTEGER");

            entity.HasKey("Id");

            entity.HasIndex("InventoryEntryId", "CountedAtUtc");

            entity.ToTable("InventoryCountHistory", table =>
            {
                table.HasCheckConstraint("CK_InventoryCountHistory_CountedQuantity_NonNegative", "CountedQuantity >= 0");
            });
        });

        modelBuilder.Entity("ProjectHopeLynden.Domain.Inventory.InventoryEntry", entity =>
        {
            entity.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER")
                .HasAnnotation("Sqlite:Autoincrement", true);

            entity.Property<DateTime?>("BestByDate")
                .HasColumnType("TEXT");

            entity.Property<int>("CategoryId")
                .HasColumnType("INTEGER");

            entity.Property<int>("CurrentQuantity")
                .HasColumnType("INTEGER");

            entity.Property<bool>("IsCommodity")
                .HasColumnType("INTEGER");

            entity.Property<bool>("IsMenuItem")
                .HasColumnType("INTEGER");

            entity.Property<int>("ItemId")
                .HasColumnType("INTEGER");

            entity.Property<DateTime>("LastUpdatedAtUtc")
                .HasColumnType("TEXT");

            entity.Property<int>("LocationId")
                .HasColumnType("INTEGER");

            entity.HasKey("Id");

            entity.HasIndex("CategoryId");

            entity.HasIndex("ItemId", "IsCommodity");

            entity.HasIndex("LocationId");

            entity.ToTable("InventoryEntries", table =>
            {
                table.HasCheckConstraint("CK_InventoryEntries_CurrentQuantity_NonNegative", "CurrentQuantity >= 0");
            });
        });

        modelBuilder.Entity("ProjectHopeLynden.Domain.Inventory.Item", entity =>
        {
            entity.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER")
                .HasAnnotation("Sqlite:Autoincrement", true);

            entity.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(150)
                .HasColumnType("TEXT");

            entity.HasKey("Id");

            entity.HasIndex("Name")
                .IsUnique();

            entity.ToTable("Items");
        });

        modelBuilder.Entity("ProjectHopeLynden.Domain.Inventory.Location", entity =>
        {
            entity.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER")
                .HasAnnotation("Sqlite:Autoincrement", true);

            entity.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("TEXT");

            entity.HasKey("Id");

            entity.HasIndex("Name")
                .IsUnique();

            entity.ToTable("Locations");
        });

        modelBuilder.Entity("ProjectHopeLynden.Domain.Inventory.InventoryCountHistory", entity =>
        {
            entity.HasOne("ProjectHopeLynden.Domain.Inventory.InventoryEntry", "InventoryEntry")
                .WithMany("CountHistory")
                .HasForeignKey("InventoryEntryId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            entity.Navigation("InventoryEntry");
        });

        modelBuilder.Entity("ProjectHopeLynden.Domain.Inventory.InventoryEntry", entity =>
        {
            entity.HasOne("ProjectHopeLynden.Domain.Inventory.Category", "Category")
                .WithMany("InventoryEntries")
                .HasForeignKey("CategoryId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            entity.HasOne("ProjectHopeLynden.Domain.Inventory.Item", "Item")
                .WithMany("InventoryEntries")
                .HasForeignKey("ItemId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            entity.HasOne("ProjectHopeLynden.Domain.Inventory.Location", "Location")
                .WithMany("InventoryEntries")
                .HasForeignKey("LocationId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            entity.Navigation("Category");
            entity.Navigation("Item");
            entity.Navigation("Location");
        });

        modelBuilder.Entity("ProjectHopeLynden.Domain.Inventory.Category", entity =>
        {
            entity.Navigation("InventoryEntries");
        });

        modelBuilder.Entity("ProjectHopeLynden.Domain.Inventory.InventoryEntry", entity =>
        {
            entity.Navigation("CountHistory");
        });

        modelBuilder.Entity("ProjectHopeLynden.Domain.Inventory.Item", entity =>
        {
            entity.Navigation("InventoryEntries");
        });

        modelBuilder.Entity("ProjectHopeLynden.Domain.Inventory.Location", entity =>
        {
            entity.Navigation("InventoryEntries");
        });
    }
}
