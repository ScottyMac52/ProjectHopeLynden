using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProjectHopeLynden.Domain.Inventory;
using ProjectHopeLynden.Infrastructure.Persistence;
using Xunit;

namespace ProjectHopeLynden.Infrastructure.Tests.Persistence;

public sealed class ProjectHopeDbContextTests
{
    [Fact]
    public async Task Database_CreatesExpectedTables()
    {
        await using var connection = await OpenConnectionAsync();
        await using var context = CreateContext(connection);

        await context.Database.MigrateAsync();

        var tableNames = await ReadTableNamesAsync(connection);

        Assert.Contains("Categories", tableNames);
        Assert.Contains("Items", tableNames);
        Assert.Contains("Locations", tableNames);
        Assert.Contains("InventoryEntries", tableNames);
        Assert.Contains("InventoryCountHistory", tableNames);
    }

    [Fact]
    public async Task InventoryEntries_AllowSameItemAsCommodityAndNonCommodity()
    {
        await using var connection = await OpenConnectionAsync();
        await using var context = CreateContext(connection);
        await context.Database.MigrateAsync();

        var item = new Item { Name = "Green Beans" };
        var category = new Category { Name = "Canned Vegetables" };
        var location = new Location { Name = "Pantry Shelf" };
        context.AddRange(item, category, location);
        await context.SaveChangesAsync();

        context.InventoryEntries.AddRange(
            new InventoryEntry
            {
                ItemId = item.Id,
                CategoryId = category.Id,
                LocationId = location.Id,
                CurrentQuantity = 24,
                IsCommodity = true,
                LastUpdatedAtUtc = new DateTime(2026, 7, 9, 10, 0, 0, DateTimeKind.Utc),
            },
            new InventoryEntry
            {
                ItemId = item.Id,
                CategoryId = category.Id,
                LocationId = location.Id,
                CurrentQuantity = 18,
                IsCommodity = false,
                LastUpdatedAtUtc = new DateTime(2026, 7, 9, 10, 5, 0, DateTimeKind.Utc),
            });

        await context.SaveChangesAsync();

        var entries = await context.InventoryEntries
            .Where(entry => entry.ItemId == item.Id)
            .OrderBy(entry => entry.IsCommodity)
            .ToListAsync();

        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, entry => entry is { IsCommodity: true, CurrentQuantity: 24 });
        Assert.Contains(entries, entry => entry is { IsCommodity: false, CurrentQuantity: 18 });
    }

    [Fact]
    public async Task InventoryEntries_CanHaveHistoricalCountRecords()
    {
        await using var connection = await OpenConnectionAsync();
        await using var context = CreateContext(connection);
        await context.Database.MigrateAsync();

        var entry = await CreateInventoryEntryAsync(context, "Rice", isCommodity: true);

        context.InventoryCountHistory.AddRange(
            new InventoryCountHistory
            {
                InventoryEntryId = entry.Id,
                CountedQuantity = 10,
                CountedAtUtc = new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc),
                PreviousQuantity = null,
                QuantityChange = null,
            },
            new InventoryCountHistory
            {
                InventoryEntryId = entry.Id,
                CountedQuantity = 16,
                CountedAtUtc = new DateTime(2026, 7, 8, 9, 0, 0, DateTimeKind.Utc),
                PreviousQuantity = 10,
                QuantityChange = 6,
            });

        await context.SaveChangesAsync();

        var history = await context.InventoryCountHistory
            .Where(record => record.InventoryEntryId == entry.Id)
            .OrderBy(record => record.CountedAtUtc)
            .ToListAsync();

        Assert.Equal([10, 16], history.Select(record => record.CountedQuantity));
        Assert.Equal(6, history[1].QuantityChange);
    }

    [Fact]
    public async Task HistoricalCounts_RemainSeparateForCommodityAndNonCommodityEntries()
    {
        await using var connection = await OpenConnectionAsync();
        await using var context = CreateContext(connection);
        await context.Database.MigrateAsync();

        var commodityEntry = await CreateInventoryEntryAsync(context, "Tomato Sauce", isCommodity: true);
        var nonCommodityEntry = await CreateInventoryEntryAsync(context, "Tomato Sauce", isCommodity: false);

        context.InventoryCountHistory.AddRange(
            new InventoryCountHistory
            {
                InventoryEntryId = commodityEntry.Id,
                CountedQuantity = 12,
                CountedAtUtc = new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc),
            },
            new InventoryCountHistory
            {
                InventoryEntryId = nonCommodityEntry.Id,
                CountedQuantity = 4,
                CountedAtUtc = new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc),
            });

        await context.SaveChangesAsync();

        var commodityHistory = await context.InventoryCountHistory
            .Where(record => record.InventoryEntryId == commodityEntry.Id)
            .SingleAsync();

        var nonCommodityHistory = await context.InventoryCountHistory
            .Where(record => record.InventoryEntryId == nonCommodityEntry.Id)
            .SingleAsync();

        Assert.Equal(12, commodityHistory.CountedQuantity);
        Assert.Equal(4, nonCommodityHistory.CountedQuantity);
    }

    private static async Task<SqliteConnection> OpenConnectionAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        return connection;
    }

    private static ProjectHopeDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<ProjectHopeDbContext>()
            .UseSqlite(connection)
            .Options;

        return new ProjectHopeDbContext(options);
    }

    private static async Task<HashSet<string>> ReadTableNamesAsync(SqliteConnection connection)
    {
        var tableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table';";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tableNames.Add(reader.GetString(0));
        }

        return tableNames;
    }

    private static async Task<InventoryEntry> CreateInventoryEntryAsync(
        ProjectHopeDbContext context,
        string itemName,
        bool isCommodity)
    {
        var item = await context.Items.SingleOrDefaultAsync(existing => existing.Name == itemName);
        if (item is null)
        {
            item = new Item { Name = itemName };
            context.Items.Add(item);
        }

        var category = await context.Categories.SingleOrDefaultAsync(existing => existing.Name == "Canned Goods");
        if (category is null)
        {
            category = new Category { Name = "Canned Goods" };
            context.Categories.Add(category);
        }

        var location = await context.Locations.SingleOrDefaultAsync(existing => existing.Name == "Pantry Shelf");
        if (location is null)
        {
            location = new Location { Name = "Pantry Shelf" };
            context.Locations.Add(location);
        }

        await context.SaveChangesAsync();

        var entry = new InventoryEntry
        {
            ItemId = item.Id,
            CategoryId = category.Id,
            LocationId = location.Id,
            CurrentQuantity = 0,
            IsCommodity = isCommodity,
            LastUpdatedAtUtc = new DateTime(2026, 7, 9, 10, 0, 0, DateTimeKind.Utc),
        };

        context.InventoryEntries.Add(entry);
        await context.SaveChangesAsync();

        return entry;
    }
}
