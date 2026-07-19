using Microsoft.EntityFrameworkCore;
using ProjectHopeLynden.Domain.IncomingOrders;
using ProjectHopeLynden.Domain.Inventory;

namespace ProjectHopeLynden.Infrastructure.Persistence;

public sealed class ProjectHopeDbContext(DbContextOptions<ProjectHopeDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Item> Items => Set<Item>();

    public DbSet<Location> Locations => Set<Location>();

    public DbSet<InventoryEntry> InventoryEntries => Set<InventoryEntry>();

    public DbSet<InventoryCountHistory> InventoryCountHistory => Set<InventoryCountHistory>();

    public DbSet<IncomingOrder> IncomingOrders => Set<IncomingOrder>();

    public DbSet<IncomingOrderLine> IncomingOrderLines => Set<IncomingOrderLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureCategories(modelBuilder);
        ConfigureItems(modelBuilder);
        ConfigureLocations(modelBuilder);
        ConfigureInventoryEntries(modelBuilder);
        ConfigureInventoryCountHistory(modelBuilder);
        ConfigureIncomingOrders(modelBuilder);
        ConfigureIncomingOrderLines(modelBuilder);
    }

    private static void ConfigureCategories(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(category => category.Id);

            entity.Property(category => category.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(category => category.Name)
                .IsUnique();
        });
    }

    private static void ConfigureItems(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Item>(entity =>
        {
            entity.ToTable("Items");
            entity.HasKey(item => item.Id);

            entity.Property(item => item.Name)
                .HasMaxLength(150)
                .IsRequired();

            entity.HasIndex(item => item.Name)
                .IsUnique();
        });
    }

    private static void ConfigureLocations(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Location>(entity =>
        {
            entity.ToTable("Locations");
            entity.HasKey(location => location.Id);

            entity.Property(location => location.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(location => location.Name)
                .IsUnique();
        });
    }

    private static void ConfigureInventoryEntries(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryEntry>(entity =>
        {
            entity.ToTable("InventoryEntries", table =>
            {
                table.HasCheckConstraint("CK_InventoryEntries_CurrentQuantity_NonNegative", "CurrentQuantity >= 0");
            });

            entity.HasKey(entry => entry.Id);

            entity.Property(entry => entry.CurrentQuantity)
                .HasColumnType("REAL")
                .IsRequired();

            entity.Property(entry => entry.LastUpdatedAtUtc)
                .IsRequired();

            entity.Property(entry => entry.IsCommodity)
                .IsRequired();

            entity.Property(entry => entry.IsMenuItem)
                .IsRequired();

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
    }

    private static void ConfigureInventoryCountHistory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryCountHistory>(entity =>
        {
            entity.ToTable("InventoryCountHistory", table =>
            {
                table.HasCheckConstraint("CK_InventoryCountHistory_CountedQuantity_NonNegative", "CountedQuantity >= 0");
            });

            entity.HasKey(history => history.Id);

            entity.Property(history => history.CountedQuantity)
                .HasColumnType("REAL")
                .IsRequired();

            entity.Property(history => history.PreviousQuantity)
                .HasColumnType("REAL");

            entity.Property(history => history.QuantityChange)
                .HasColumnType("REAL");

            entity.Property(history => history.CountedAtUtc)
                .IsRequired();

            entity.Property(history => history.ItemNameAtCount)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(history => history.CategoryNameAtCount)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(history => history.LocationNameAtCount)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(history => history.IsCommodityAtCount)
                .IsRequired();

            entity.HasOne(history => history.InventoryEntry)
                .WithMany(entry => entry.CountHistory)
                .HasForeignKey(history => history.InventoryEntryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(history => new { history.InventoryEntryId, history.CountedAtUtc });
        });
    }

    private static void ConfigureIncomingOrders(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IncomingOrder>(entity =>
        {
            entity.ToTable("IncomingOrders", table =>
            {
                table.HasCheckConstraint("CK_IncomingOrders_InvoiceAmount_NonNegative", "InvoiceAmount IS NULL OR InvoiceAmount >= 0");
                table.HasCheckConstraint("CK_IncomingOrders_Weight_NonNegative", "Weight IS NULL OR Weight >= 0");
            });

            entity.HasKey(order => order.Id);
            entity.Property(order => order.Vendor).HasMaxLength(150).IsRequired();
            entity.Property(order => order.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(order => order.InvoiceNumber).HasMaxLength(100);
            entity.Property(order => order.InvoiceAmount).HasColumnType("REAL");
            entity.Property(order => order.SentToPayer).HasMaxLength(100);
            entity.Property(order => order.ChargeTo).HasMaxLength(100);
            entity.Property(order => order.Weight).HasColumnType("REAL");
            entity.Property(order => order.ProductSummary).HasMaxLength(500);
            entity.Property(order => order.Notes).HasMaxLength(2000);
            entity.HasIndex(order => new { order.Status, order.ExpectedDate });
        });
    }

    private static void ConfigureIncomingOrderLines(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IncomingOrderLine>(entity =>
        {
            entity.ToTable("IncomingOrderLines", table =>
            {
                table.HasCheckConstraint("CK_IncomingOrderLines_ExpectedQuantity_Positive", "ExpectedQuantity > 0");
                table.HasCheckConstraint("CK_IncomingOrderLines_ReceivedQuantity_Positive", "ReceivedQuantity IS NULL OR ReceivedQuantity > 0");
            });

            entity.HasKey(line => line.Id);
            entity.Property(line => line.ExpectedQuantity).HasColumnType("REAL").IsRequired();
            entity.Property(line => line.ReceivedQuantity).HasColumnType("REAL");
            entity.HasOne(line => line.IncomingOrder)
                .WithMany(order => order.Lines)
                .HasForeignKey(line => line.IncomingOrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(line => line.InventoryEntry)
                .WithMany()
                .HasForeignKey(line => line.InventoryEntryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(line => line.IncomingOrderId);
            entity.HasIndex(line => line.InventoryEntryId);
            entity.HasIndex(line => new { line.IncomingOrderId, line.InventoryEntryId }).IsUnique();
        });
    }
}
