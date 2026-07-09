using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProjectHopeLynden.Infrastructure.Persistence;
using ProjectHopeLynden.Infrastructure.Persistence.Seeding;
using Xunit;

namespace ProjectHopeLynden.Infrastructure.Tests.Persistence.Seeding;

public sealed class InitialInventorySeederTests
{
    [Fact]
    public async Task SeedAsync_CreatesSampleCategoriesLocationsInventoryEntriesAndHistory()
    {
        await using var connection = await OpenConnectionAsync();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();
        var seeder = new InitialInventorySeeder(context);

        await seeder.SeedAsync();

        var categoryNames = await context.Categories
            .Select(category => category.Name)
            .ToListAsync();

        var locationNames = await context.Locations
            .Select(location => location.Name)
            .ToListAsync();

        foreach (var categoryName in InitialInventorySeedData.CategoryNames)
        {
            Assert.Contains(categoryName, categoryNames);
        }

        foreach (var locationName in InitialInventorySeedData.LocationNames)
        {
            Assert.Contains(locationName, locationNames);
        }

        Assert.True(await context.Items.AnyAsync());
        Assert.True(await context.InventoryEntries.AnyAsync());
        Assert.True(await context.InventoryCountHistory.AnyAsync());
    }

    [Fact]
    public async Task SeedAsync_IncludesAllObservedSpreadsheetCategories()
    {
        await using var connection = await OpenConnectionAsync();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();
        var seeder = new InitialInventorySeeder(context);

        await seeder.SeedAsync();

        var categoryNames = await context.Categories
            .Select(category => category.Name)
            .ToListAsync();

        Assert.Equal(InitialInventorySeedData.CategoryNames.Length, categoryNames.Count);
        Assert.Contains("Soup is a MESS", categoryNames);
        Assert.Contains("Formula", categoryNames);
        Assert.Contains("Frozen Miscellaneous", categoryNames);
    }

    [Fact]
    public async Task SeedAsync_IncludesSameItemAsCommodityAndNonCommodityInventory()
    {
        await using var connection = await OpenConnectionAsync();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();
        var seeder = new InitialInventorySeeder(context);

        await seeder.SeedAsync();

        var greenBeans = await context.Items.SingleAsync(item => item.Name == "Green Beans");
        var greenBeanEntries = await context.InventoryEntries
            .Where(entry => entry.ItemId == greenBeans.Id)
            .ToListAsync();

        Assert.Equal(2, greenBeanEntries.Count);
        Assert.Contains(greenBeanEntries, entry => entry.IsCommodity);
        Assert.Contains(greenBeanEntries, entry => !entry.IsCommodity);
    }

    [Fact]
    public async Task SeedAsync_IncludesMultipleHistoricalCountRecordsForAtLeastOneEntry()
    {
        await using var connection = await OpenConnectionAsync();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();
        var seeder = new InitialInventorySeeder(context);

        await seeder.SeedAsync();

        var greenBeans = await context.Items.SingleAsync(item => item.Name == "Green Beans");
        var commodityEntry = await context.InventoryEntries.SingleAsync(
            entry => entry.ItemId == greenBeans.Id && entry.IsCommodity);

        var history = await context.InventoryCountHistory
            .Where(record => record.InventoryEntryId == commodityEntry.Id)
            .OrderBy(record => record.CountedAtUtc)
            .ToListAsync();

        Assert.Equal(2, history.Count);
        Assert.Equal(new[] { 20, 24 }, history.Select(record => record.CountedQuantity).ToArray());
        Assert.Equal(4, history[1].QuantityChange);
    }

    [Fact]
    public async Task SeedAsync_IsIdempotent()
    {
        await using var connection = await OpenConnectionAsync();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();
        var seeder = new InitialInventorySeeder(context);

        await seeder.SeedAsync();
        var firstCounts = await ReadTableCountsAsync(context);

        await seeder.SeedAsync();
        var secondCounts = await ReadTableCountsAsync(context);

        Assert.Equal(firstCounts, secondCounts);
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

    private static async Task<SeedTableCounts> ReadTableCountsAsync(ProjectHopeDbContext context)
    {
        return new SeedTableCounts(
            await context.Categories.CountAsync(),
            await context.Items.CountAsync(),
            await context.Locations.CountAsync(),
            await context.InventoryEntries.CountAsync(),
            await context.InventoryCountHistory.CountAsync());
    }

    private sealed record SeedTableCounts(
        int CategoryCount,
        int ItemCount,
        int LocationCount,
        int InventoryEntryCount,
        int InventoryCountHistoryCount);
}
