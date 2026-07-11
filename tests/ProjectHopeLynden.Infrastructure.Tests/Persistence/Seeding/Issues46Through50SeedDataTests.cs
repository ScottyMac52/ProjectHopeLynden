using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProjectHopeLynden.Infrastructure.Persistence;
using ProjectHopeLynden.Infrastructure.Persistence.Seeding;
using Xunit;

namespace ProjectHopeLynden.Infrastructure.Tests.Persistence.Seeding;

public sealed class Issues46Through50SeedDataTests
{
    [Fact]
    public async Task SeedAsync_IncludesExpectedInventoryCountsForIssues46Through50()
    {
        await using var connection = await OpenConnectionAsync();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();
        var seeder = new InitialInventorySeeder(context);

        await seeder.SeedAsync();

        var categoryCounts = await context.InventoryEntries
            .GroupBy(entry => entry.Category.Name)
            .ToDictionaryAsync(group => group.Key, group => group.Count());

        var expectedCounts = new Dictionary<string, int>
        {
            ["Condiments"] = 8,
            ["Snacks"] = 3,
            ["Cereals"] = 1,
            ["Produce"] = 2,
            ["Eggs"] = 1,
            ["Frozen Meat"] = 2,
            ["Frozen Miscellaneous"] = 2,
            ["Canned Vegetables"] = 14,
            ["Canned Fruit"] = 8,
            ["Soup is a MESS"] = 9,
            ["Canned Beans"] = 8,
            ["Tomatoes"] = 9,
            ["Canned Meat"] = 8,
            ["Diapers"] = 15,
            ["Wipes"] = 2,
            ["Formula"] = 8,
        };

        foreach (var expectedCount in expectedCounts)
        {
            Assert.Equal(expectedCount.Value, categoryCounts[expectedCount.Key]);
        }

        Assert.Equal(131, await context.InventoryEntries.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_IncludesAdditionalSpreadsheetLocations()
    {
        await using var connection = await OpenConnectionAsync();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();
        var seeder = new InitialInventorySeeder(context);

        await seeder.SeedAsync();

        var locations = await context.Locations
            .Select(location => location.Name)
            .ToArrayAsync();

        Assert.Contains("WIC", locations);
        Assert.Contains("WIF", locations);
        Assert.Contains("Front", locations);
        Assert.Contains("Kitchen", locations);
    }

    [Fact]
    public async Task SeedAsync_PreservesRepresentativeRowsAndHandwrittenCorrections()
    {
        await using var connection = await OpenConnectionAsync();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();
        var seeder = new InitialInventorySeeder(context);

        await seeder.SeedAsync();

        await AssertEntryAsync(context, "Jelly Misc.", "Condiments", "Back Room", 2, 4);
        await AssertEntryAsync(context, "Popcorn Misc.", "Snacks", "Back Room", 1, 3.5);
        await AssertEntryAsync(context, "Apples Pink Lady", "Produce", "WIC", 9, null);
        await AssertEntryAsync(context, "Corn North Pride", "Canned Vegetables", "Crypt", 8, 82);
        await AssertEntryAsync(context, "Chicken Crider", "Canned Meat", "Back Room", 7, null);
        await AssertEntryAsync(context, "Chicken Crider", "Canned Meat", "Crypt", 30, 55);
        await AssertEntryAsync(context, "Size 2", "Diapers", "Kitchen", 0, 8);
        await AssertEntryAsync(context, "Sensitive", "Formula", "Front", 7, 5);
        await AssertEntryAsync(context, "Total Comfort", "Formula", "Front", 12, 13);
    }

    [Fact]
    public async Task SeedAsync_DoesNotIncludeCrossedOutSpreadsheetRows()
    {
        await using var connection = await OpenConnectionAsync();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();
        var seeder = new InitialInventorySeeder(context);

        await seeder.SeedAsync();

        Assert.False(await context.InventoryEntries.AnyAsync(
            entry => entry.Item.Name == "Chicken Breast" && entry.Category.Name == "Frozen Meat"));
        Assert.False(await context.InventoryEntries.AnyAsync(
            entry => entry.Item.Name == "Peaches, Sliced Mission Pride" && entry.Category.Name == "Canned Fruit"));
        Assert.False(await context.InventoryEntries.AnyAsync(
            entry => entry.Item.Name == "Cream of Ch. Campbell's" && entry.Category.Name == "Soup is a MESS"));
    }

    private static async Task AssertEntryAsync(
        ProjectHopeDbContext context,
        string itemName,
        string categoryName,
        string locationName,
        double currentQuantity,
        double? previousQuantity)
    {
        var entry = await context.InventoryEntries
            .Include(inventoryEntry => inventoryEntry.Item)
            .Include(inventoryEntry => inventoryEntry.Category)
            .Include(inventoryEntry => inventoryEntry.Location)
            .SingleAsync(inventoryEntry => inventoryEntry.Item.Name == itemName
                && inventoryEntry.Category.Name == categoryName
                && inventoryEntry.Location.Name == locationName);

        Assert.Equal(currentQuantity, entry.CurrentQuantity);

        var history = await context.InventoryCountHistory
            .Where(record => record.InventoryEntryId == entry.Id)
            .OrderBy(record => record.CountedAtUtc)
            .Select(record => record.CountedQuantity)
            .ToArrayAsync();

        var expectedHistory = previousQuantity.HasValue
            ? new[] { previousQuantity.Value, currentQuantity }
            : new[] { currentQuantity };

        Assert.Equal(expectedHistory, history);
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
}
