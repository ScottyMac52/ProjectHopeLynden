using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProjectHopeLynden.Domain.Inventory;
using ProjectHopeLynden.Infrastructure.Inventory;
using ProjectHopeLynden.Infrastructure.Persistence;

namespace ProjectHopeLynden.Infrastructure.Tests.Inventory;

public sealed class InventoryQueryIncomingOrdersTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private ProjectHopeDbContext context = null!;

    public async Task InitializeAsync()
    {
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ProjectHopeDbContext>()
            .UseSqlite(connection)
            .Options;

        context = new ProjectHopeDbContext(options);
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await context.DisposeAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task GetInventoryForCategoryAsync_SumsOnlyScheduledOrdersAndReturnsNextDate()
    {
        var entry = new InventoryEntry
        {
            Category = new Category { Name = "Dry Beans" },
            Item = new Item { Name = "Pinto Beans" },
            Location = new Location { Name = "Shelf" },
            CurrentQuantity = 10,
            IsCommodity = false,
            LastUpdatedAtUtc = DateTime.UtcNow,
        };
        context.InventoryEntries.Add(entry);
        await context.SaveChangesAsync();

        context.IncomingOrderLines.AddRange(
            CreateOrder(entry.Id, 3, new DateOnly(2026, 7, 25), IncomingOrderStatus.Scheduled),
            CreateOrder(entry.Id, 4, new DateOnly(2026, 7, 22), IncomingOrderStatus.Scheduled),
            CreateOrder(entry.Id, 100, new DateOnly(2026, 7, 20), IncomingOrderStatus.Cancelled),
            CreateOrder(entry.Id, 200, new DateOnly(2026, 7, 19), IncomingOrderStatus.Received));
        await context.SaveChangesAsync();

        var service = new InventoryQueryService(context);

        var inventory = await service.GetInventoryForCategoryAsync(entry.CategoryId);

        Assert.NotNull(inventory);
        var row = Assert.Single(inventory.Entries);
        Assert.Equal(7, row.IncomingQuantity);
        Assert.Equal(new DateOnly(2026, 7, 22), row.NextExpectedDate);
    }

    [Fact]
    public async Task GetInventoryForCategoryAsync_ReturnsZeroAndNoDateWithoutScheduledOrders()
    {
        var entry = new InventoryEntry
        {
            Category = new Category { Name = "Dry Beans" },
            Item = new Item { Name = "Pinto Beans" },
            Location = new Location { Name = "Shelf" },
            CurrentQuantity = 10,
            IsCommodity = false,
            LastUpdatedAtUtc = DateTime.UtcNow,
        };
        context.InventoryEntries.Add(entry);
        await context.SaveChangesAsync();

        var service = new InventoryQueryService(context);
        var inventory = await service.GetInventoryForCategoryAsync(entry.CategoryId);

        Assert.NotNull(inventory);
        var row = Assert.Single(inventory.Entries);
        Assert.Equal(0, row.IncomingQuantity);
        Assert.Null(row.NextExpectedDate);
    }

    private static IncomingOrderLine CreateOrder(
        int inventoryEntryId,
        double quantity,
        DateOnly expectedDate,
        IncomingOrderStatus status)
    {
        var timestamp = new DateTime(2026, 7, 18, 8, 0, 0, DateTimeKind.Utc);
        return new IncomingOrderLine
        {
            InventoryEntryId = inventoryEntryId,
            Quantity = quantity,
            ExpectedDate = expectedDate,
            Status = status,
            CreatedAtUtc = timestamp,
            UpdatedAtUtc = timestamp,
        };
    }
}
