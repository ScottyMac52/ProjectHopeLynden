using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProjectHopeLynden.Domain.Inventory;
using ProjectHopeLynden.Infrastructure.Inventory;
using ProjectHopeLynden.Infrastructure.Persistence;

namespace ProjectHopeLynden.Infrastructure.Tests.Inventory;

public sealed class InventoryHistoryServiceTests : IAsyncLifetime
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
    public async Task GetHistoryForEntryAsync_ReturnsHistoryInCountedDateOrder()
    {
        var entry = await AddInventoryEntryAsync(
            itemName: "Green Beans",
            categoryName: "Canned Vegetables",
            locationName: "Shelf",
            currentQuantity: 19,
            isCommodity: true);

        context.InventoryCountHistory.AddRange(
            CreateHistory(entry, countedQuantity: 19, previousQuantity: 14, countedAtUtc: new DateTime(2026, 7, 12, 9, 0, 0, DateTimeKind.Utc)),
            CreateHistory(entry, countedQuantity: 12, previousQuantity: 10, countedAtUtc: new DateTime(2026, 7, 10, 9, 0, 0, DateTimeKind.Utc)),
            CreateHistory(entry, countedQuantity: 14, previousQuantity: 12, countedAtUtc: new DateTime(2026, 7, 11, 9, 0, 0, DateTimeKind.Utc)));
        await context.SaveChangesAsync();

        var service = new InventoryHistoryService(context);

        var history = await service.GetHistoryForEntryAsync(entry.Id);

        Assert.NotNull(history);
        Assert.Equal(entry.Id, history.InventoryEntryId);
        Assert.Equal(entry.CategoryId, history.CategoryId);
        Assert.Equal("Green Beans", history.ItemName);
        Assert.Equal("Canned Vegetables", history.CategoryName);
        Assert.Equal("Shelf", history.LocationName);
        Assert.True(history.IsCommodity);
        Assert.Equal(19, history.CurrentQuantity);
        Assert.True(history.HasHistory);
        Assert.Equal(new[] { 12, 14, 19 }, history.Records.Select(record => record.CountedQuantity).ToArray());
    }

    [Fact]
    public async Task GetHistoryForEntryAsync_ReturnsEmptyHistoryViewWhenEntryHasNoHistory()
    {
        var entry = await AddInventoryEntryAsync(
            itemName: "Oat Cereal",
            categoryName: "Cereals",
            locationName: "Shelf",
            currentQuantity: 8,
            isCommodity: false);
        var service = new InventoryHistoryService(context);

        var history = await service.GetHistoryForEntryAsync(entry.Id);

        Assert.NotNull(history);
        Assert.Equal("Oat Cereal", history.ItemName);
        Assert.False(history.HasHistory);
        Assert.Empty(history.Records);
    }

    [Fact]
    public async Task GetHistoryForEntryAsync_ReturnsNullForUnknownInventoryEntry()
    {
        var service = new InventoryHistoryService(context);

        var history = await service.GetHistoryForEntryAsync(404);

        Assert.Null(history);
    }

    [Fact]
    public async Task GetHistoryForEntryAsync_KeepsCommodityAndNonCommodityHistoriesSeparate()
    {
        var category = new Category { Name = "Canned Vegetables" };
        var item = new Item { Name = "Green Beans" };
        var shelf = new Location { Name = "Shelf" };
        var backRoom = new Location { Name = "Back Room" };
        var commodityEntry = new InventoryEntry
        {
            Category = category,
            Item = item,
            Location = shelf,
            CurrentQuantity = 24,
            IsCommodity = true,
            LastUpdatedAtUtc = new DateTime(2026, 7, 12, 9, 0, 0, DateTimeKind.Utc),
        };
        var nonCommodityEntry = new InventoryEntry
        {
            Category = category,
            Item = item,
            Location = backRoom,
            CurrentQuantity = 6,
            IsCommodity = false,
            LastUpdatedAtUtc = new DateTime(2026, 7, 12, 9, 15, 0, DateTimeKind.Utc),
        };

        context.InventoryEntries.AddRange(commodityEntry, nonCommodityEntry);
        await context.SaveChangesAsync();

        context.InventoryCountHistory.AddRange(
            CreateHistory(commodityEntry, countedQuantity: 24, previousQuantity: 18, countedAtUtc: new DateTime(2026, 7, 12, 9, 0, 0, DateTimeKind.Utc)),
            CreateHistory(nonCommodityEntry, countedQuantity: 6, previousQuantity: 8, countedAtUtc: new DateTime(2026, 7, 12, 9, 15, 0, DateTimeKind.Utc)));
        await context.SaveChangesAsync();

        var service = new InventoryHistoryService(context);

        var commodityHistory = await service.GetHistoryForEntryAsync(commodityEntry.Id);
        var nonCommodityHistory = await service.GetHistoryForEntryAsync(nonCommodityEntry.Id);

        Assert.NotNull(commodityHistory);
        Assert.True(commodityHistory.IsCommodity);
        var commodityRecord = Assert.Single(commodityHistory.Records);
        Assert.Equal(24, commodityRecord.CountedQuantity);

        Assert.NotNull(nonCommodityHistory);
        Assert.False(nonCommodityHistory.IsCommodity);
        var nonCommodityRecord = Assert.Single(nonCommodityHistory.Records);
        Assert.Equal(6, nonCommodityRecord.CountedQuantity);
    }

    private async Task<InventoryEntry> AddInventoryEntryAsync(
        string itemName,
        string categoryName,
        string locationName,
        int currentQuantity,
        bool isCommodity)
    {
        var entry = new InventoryEntry
        {
            Category = new Category { Name = categoryName },
            Item = new Item { Name = itemName },
            Location = new Location { Name = locationName },
            CurrentQuantity = currentQuantity,
            IsCommodity = isCommodity,
            LastUpdatedAtUtc = new DateTime(2026, 7, 12, 9, 0, 0, DateTimeKind.Utc),
        };

        context.InventoryEntries.Add(entry);
        await context.SaveChangesAsync();
        return entry;
    }

    private static InventoryCountHistory CreateHistory(
        InventoryEntry entry,
        int countedQuantity,
        int previousQuantity,
        DateTime countedAtUtc)
    {
        return new InventoryCountHistory
        {
            InventoryEntryId = entry.Id,
            CountedQuantity = countedQuantity,
            PreviousQuantity = previousQuantity,
            QuantityChange = countedQuantity - previousQuantity,
            CountedAtUtc = countedAtUtc,
        };
    }
}
