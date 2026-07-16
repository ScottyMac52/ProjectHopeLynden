using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProjectHopeLynden.Domain.Inventory;
using ProjectHopeLynden.Infrastructure.Inventory;
using ProjectHopeLynden.Infrastructure.Persistence;

namespace ProjectHopeLynden.Infrastructure.Tests.Inventory;

public sealed class InventoryQuantityServiceTests : IAsyncLifetime
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
    public async Task UpdateCurrentQuantityAsync_UpdatesQuantityLastUpdatedAndStoresHistory()
    {
        var originalUpdatedAtUtc = new DateTime(2026, 7, 9, 8, 0, 0, DateTimeKind.Utc);
        var countedAtUtc = new DateTime(2026, 7, 10, 9, 30, 0, DateTimeKind.Utc);
        var entry = await AddInventoryEntryAsync(1.5, originalUpdatedAtUtc);
        var service = new InventoryQuantityService(context);

        var result = await service.UpdateCurrentQuantityAsync(entry.Id, 0.5, countedAtUtc);

        Assert.True(result.Succeeded);
        Assert.Null(result.ErrorMessage);

        var savedEntry = await context.InventoryEntries
            .AsNoTracking()
            .SingleAsync(inventoryEntry => inventoryEntry.Id == entry.Id);

        Assert.Equal(0.5, savedEntry.CurrentQuantity);
        Assert.Equal(countedAtUtc, savedEntry.LastUpdatedAtUtc);

        var history = await context.InventoryCountHistory
            .AsNoTracking()
            .SingleAsync(record => record.InventoryEntryId == entry.Id);

        Assert.Equal(1.5, history.PreviousQuantity);
        Assert.Equal(0.5, history.CountedQuantity);
        Assert.Equal(-1d, history.QuantityChange);
        Assert.Equal(countedAtUtc, history.CountedAtUtc);
        Assert.Equal(entry.ItemId, history.ItemIdAtCount);
        Assert.Equal("Green Beans", history.ItemNameAtCount);
        Assert.Equal(entry.CategoryId, history.CategoryIdAtCount);
        Assert.Equal("Canned Vegetables", history.CategoryNameAtCount);
        Assert.Equal(entry.LocationId, history.LocationIdAtCount);
        Assert.Equal("Shelf", history.LocationNameAtCount);
        Assert.True(history.IsCommodityAtCount);
    }

    [Fact]
    public async Task UpdateCurrentQuantityAsync_RejectsNegativeQuantityWithoutChangingInventoryOrHistory()
    {
        var originalUpdatedAtUtc = new DateTime(2026, 7, 9, 8, 0, 0, DateTimeKind.Utc);
        var countedAtUtc = new DateTime(2026, 7, 10, 9, 30, 0, DateTimeKind.Utc);
        var entry = await AddInventoryEntryAsync(12, originalUpdatedAtUtc);
        var service = new InventoryQuantityService(context);

        var result = await service.UpdateCurrentQuantityAsync(entry.Id, -1, countedAtUtc);

        Assert.False(result.Succeeded);
        Assert.Equal("Quantity must be zero or greater.", result.ErrorMessage);

        var savedEntry = await context.InventoryEntries
            .AsNoTracking()
            .SingleAsync(inventoryEntry => inventoryEntry.Id == entry.Id);

        Assert.Equal(12d, savedEntry.CurrentQuantity);
        Assert.Equal(originalUpdatedAtUtc, savedEntry.LastUpdatedAtUtc);
        Assert.False(await context.InventoryCountHistory.AnyAsync());
    }

    [Fact]
    public async Task UpdateCurrentQuantityAsync_ReturnsFailureWhenInventoryEntryDoesNotExist()
    {
        var countedAtUtc = new DateTime(2026, 7, 10, 9, 30, 0, DateTimeKind.Utc);
        var service = new InventoryQuantityService(context);

        var result = await service.UpdateCurrentQuantityAsync(404, 8, countedAtUtc);

        Assert.False(result.Succeeded);
        Assert.Equal("Inventory entry was not found.", result.ErrorMessage);
        Assert.False(await context.InventoryCountHistory.AnyAsync());
    }

    private async Task<InventoryEntry> AddInventoryEntryAsync(double quantity, DateTime lastUpdatedAtUtc)
    {
        var entry = new InventoryEntry
        {
            Category = new Category { Name = "Canned Vegetables" },
            Item = new Item { Name = "Green Beans" },
            Location = new Location { Name = "Shelf" },
            CurrentQuantity = quantity,
            IsCommodity = true,
            IsMenuItem = false,
            LastUpdatedAtUtc = lastUpdatedAtUtc,
        };

        context.InventoryEntries.Add(entry);
        await context.SaveChangesAsync();
        return entry;
    }
}
