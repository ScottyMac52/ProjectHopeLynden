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

        modelBuilder.Entity("ProjectHopeLynden.Domain.IncomingOrders.IncomingOrder", entity =>
        {
            entity.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER")
                .HasAnnotation("Sqlite:Autoincrement", true);
            entity.Property<string>("ChargeTo").HasMaxLength(100).HasColumnType("TEXT");
            entity.Property<DateTime>("CreatedAtUtc").HasColumnType("TEXT");
            entity.Property<DateTime?>("DueDate").HasColumnType("TEXT");
            entity.Property<DateTime>("ExpectedDate").HasColumnType("TEXT");
            entity.Property<double?>("InvoiceAmount").HasColumnType("REAL");
            entity.Property<DateTime?>("InvoiceDate").HasColumnType("TEXT");
            entity.Property<string>("InvoiceNumber").HasMaxLength(100).HasColumnType("TEXT");
            entity.Property<string>("Notes").HasMaxLength(2000).HasColumnType("TEXT");
            entity.Property<DateTime>("OrderDate").HasColumnType("TEXT");
            entity.Property<string>("ProductSummary").HasMaxLength(500).HasColumnType("TEXT");
            entity.Property<DateTime?>("ReceivedAtUtc").HasColumnType("TEXT");
            entity.Property<string>("SentToPayer").HasMaxLength(100).HasColumnType("TEXT");
            entity.Property<string>("Status").IsRequired().HasMaxLength(20).HasColumnType("TEXT");
            entity.Property<string>("Vendor").IsRequired().HasMaxLength(150).HasColumnType("TEXT");
            entity.Property<double?>("Weight").HasColumnType("REAL");
            entity.HasKey("Id");
            entity.HasIndex("Status", "ExpectedDate");
            entity.ToTable("IncomingOrders", table =>
            {
                table.HasCheckConstraint("CK_IncomingOrders_InvoiceAmount_NonNegative", "InvoiceAmount IS NULL OR InvoiceAmount >= 0");
                table.HasCheckConstraint("CK_IncomingOrders_Weight_NonNegative", "Weight IS NULL OR Weight >= 0");
            });
        });

        modelBuilder.Entity("ProjectHopeLynden.Domain.IncomingOrders.IncomingOrderLine", entity =>
        {
            entity.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER")
                .HasAnnotation("Sqlite:Autoincrement", true);
            entity.Property<double>("ExpectedQuantity").HasColumnType("REAL");
            entity.Property<int>("IncomingOrderId").HasColumnType("INTEGER");
            entity.Property<int>("InventoryEntryId").HasColumnType("INTEGER");
            entity.Property<double?>("ReceivedQuantity").HasColumnType("REAL");
            entity.HasKey("Id");
            entity.HasIndex("IncomingOrderId");
            entity.HasIndex("InventoryEntryId");
            entity.HasIndex("IncomingOrderId", "InventoryEntryId").IsUnique();
            entity.ToTable("IncomingOrderLines", table =>
            {
                table.HasCheckConstraint("CK_IncomingOrderLines_ExpectedQuantity_Positive", "ExpectedQuantity > 0");
                table.HasCheckConstraint("CK_IncomingOrderLines_ReceivedQuantity_Positive", "ReceivedQuantity IS NULL OR ReceivedQuantity > 0");
            });
        });

        modelBuilder.Entity("ProjectHopeLynden.Domain.Inventory.InventoryCountHistory", entity =>
        {
            entity.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER")
                .HasAnnotation("Sqlite:Autoincrement", true);

            entity.Property<int>("CategoryIdAtCount")
                .HasColumnType("INTEGER");

            entity.Property<string>("CategoryNameAtCount")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("TEXT");

            entity.Property<DateTime>("CountedAtUtc")
                .HasColumnType("TEXT");

            entity.Property<double>("CountedQuantity")
                .HasColumnType("REAL");

            entity.Property<int>("InventoryEntryId")
                .HasColumnType("INTEGER");

            entity.Property<bool>("IsCommodityAtCount")
                .HasColumnType("INTEGER");

            entity.Property<int>("ItemIdAtCount")
                .HasColumnType("INTEGER");

            entity.Property<string>("ItemNameAtCount")
                .IsRequired()
                .HasMaxLength(150)
                .HasColumnType("TEXT");

            entity.Property<int>("LocationIdAtCount")
                .HasColumnType("INTEGER");

            entity.Property<string>("LocationNameAtCount")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("TEXT");

            entity.Property<double?>("PreviousQuantity")
                .HasColumnType("REAL");

            entity.Property<double?>("QuantityChange")
                .HasColumnType("REAL");

            entity.HasKey("Id");

            entity.HasIndex("InventoryEntryId", "CountedAtUtc");

            entity.ToTable("InventoryCountHistory", table =>
            {
                table.HasCheckConstraint("CK_InventoryCountHistory_CountedQuantity_NonNegative", "CountedQuantity >= 0");
            });
        });

        modelBuilder.Entity("ProjectHopeLynden.Domain.IncomingOrders.IncomingOrderLine", entity =>
        {
            entity.HasOne("ProjectHopeLynden.Domain.IncomingOrders.IncomingOrder", "IncomingOrder")
                .WithMany("Lines")
                .HasForeignKey("IncomingOrderId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            entity.HasOne("ProjectHopeLynden.Domain.Inventory.InventoryEntry", "InventoryEntry")
                .WithMany()
                .HasForeignKey("InventoryEntryId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
            entity.Navigation("IncomingOrder");
            entity.Navigation("InventoryEntry");
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

            entity.Property<double>("CurrentQuantity")
                .HasColumnType("REAL");

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

            entity.Property<string>("NormalizedName")
                .ValueGeneratedOnAddOrUpdate()
                .HasColumnType("TEXT")
                .HasComputedColumnSql("upper(trim(Name))", false);

            entity.HasKey("Id");

            entity.HasIndex("Name")
                .IsUnique();

            entity.HasIndex("NormalizedName")
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

        modelBuilder.Entity("ProjectHopeLynden.Domain.IncomingOrders.IncomingOrder", entity =>
        {
            entity.Navigation("Lines");
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
