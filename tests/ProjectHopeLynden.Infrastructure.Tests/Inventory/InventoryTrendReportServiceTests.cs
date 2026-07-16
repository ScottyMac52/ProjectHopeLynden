using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProjectHopeLynden.Application.Inventory;
using ProjectHopeLynden.Domain.Inventory;
using ProjectHopeLynden.Infrastructure.Inventory;
using ProjectHopeLynden.Infrastructure.Persistence;

namespace ProjectHopeLynden.Infrastructure.Tests.Inventory;

public sealed class InventoryTrendReportServiceTests : IAsyncLifetime
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
    public async Task GetTrendReportAsync_UsesFinalSameDayCountAndSeparatesCountActivity()
    {
        var entry = await AddEntryAsync("Green Beans", "Canned Vegetables", "Shelf", true);
        var firstDay = At(2026, 6, 1, 8);
        var secondDay = At(2026, 6, 2, 9);

        context.InventoryCountHistory.AddRange(
            CreateHistory(entry, firstDay, 12, null),
            CreateHistory(entry, secondDay, 10, -2),
            CreateHistory(entry, secondDay.AddHours(3), 8, -2));
        await context.SaveChangesAsync();

        var report = await CreateService().GetTrendReportAsync(
            new InventoryTrendReportRequest(InventoryTrendGrouping.Item, ItemName: "Green Beans"),
            At(2026, 7, 12));

        Assert.Equal([12d, 8d], report.InventorySnapshots
            .Select(point => point.EndOfDayQuantity)
            .ToArray());
        Assert.All(report.InventorySnapshots, point => Assert.Equal(1, point.InventoryEntryCount));

        var secondDayActivity = Assert.Single(
            report.CountActivity,
            point => point.CountedOnUtc == secondDay.Date);
        Assert.Equal(-4, secondDayActivity.NetQuantityChange);
        Assert.Equal(2, secondDayActivity.CountEventCount);
    }

    [Fact]
    public async Task GetTrendReportAsync_CarriesForwardLatestEntryCountsForCategorySnapshot()
    {
        var greenBeans = await AddEntryAsync("Green Beans", "Canned Vegetables", "Shelf", true);
        var peas = await AddEntryAsync("Peas", "Canned Vegetables", "Back Room", false);
        var firstDay = At(2026, 6, 1, 8);
        var secondDay = At(2026, 6, 8, 8);

        context.InventoryCountHistory.AddRange(
            CreateHistory(greenBeans, firstDay, 10, null),
            CreateHistory(peas, firstDay.AddHours(1), 5, null),
            CreateHistory(greenBeans, secondDay, 8, -2));
        await context.SaveChangesAsync();

        var report = await CreateService().GetTrendReportAsync(
            new InventoryTrendReportRequest(
                InventoryTrendGrouping.Category,
                CategoryId: greenBeans.CategoryId),
            At(2026, 7, 12));

        Assert.Equal(2, report.InventorySnapshots.Count);
        Assert.Equal(15, report.InventorySnapshots[0].EndOfDayQuantity);
        Assert.Equal(13, report.InventorySnapshots[1].EndOfDayQuantity);
        Assert.Equal(2, report.InventorySnapshots[1].InventoryEntryCount);
    }

    [Fact]
    public async Task GetTrendReportAsync_UsesCountTimeClassificationAfterEntryIsEdited()
    {
        var entry = await AddEntryAsync("Green Beans", "Canned Vegetables", "Shelf", false);
        var oldCategoryId = entry.CategoryId;
        var firstCount = At(2026, 6, 1, 8);

        context.InventoryCountHistory.Add(CreateHistory(entry, firstCount, 10, null));
        await context.SaveChangesAsync();

        var newItem = new Item { Name = "Canned Beans" };
        var newCategory = new Category { Name = "Meal Ingredients" };
        var newLocation = new Location { Name = "Pantry" };
        context.AddRange(newItem, newCategory, newLocation);
        await context.SaveChangesAsync();

        entry.Item = newItem;
        entry.Category = newCategory;
        entry.Location = newLocation;
        entry.IsCommodity = true;
        await context.SaveChangesAsync();

        context.InventoryCountHistory.Add(CreateHistory(
            entry,
            At(2026, 6, 8, 8),
            12,
            2));
        await context.SaveChangesAsync();

        var oldReport = await CreateService().GetTrendReportAsync(
            new InventoryTrendReportRequest(
                InventoryTrendGrouping.Category,
                CategoryId: oldCategoryId,
                IsCommodity: false),
            At(2026, 7, 12));

        var oldSnapshot = Assert.Single(oldReport.InventorySnapshots);
        Assert.Equal("Canned Vegetables", oldSnapshot.GroupName);
        Assert.Equal(firstCount.Date, oldSnapshot.CountedOnUtc);
        Assert.Equal(10, oldSnapshot.EndOfDayQuantity);

        var newReport = await CreateService().GetTrendReportAsync(
            new InventoryTrendReportRequest(
                InventoryTrendGrouping.Category,
                CategoryId: newCategory.Id,
                IsCommodity: true),
            At(2026, 7, 12));

        var newSnapshot = Assert.Single(newReport.InventorySnapshots);
        Assert.Equal("Meal Ingredients", newSnapshot.GroupName);
        Assert.Equal(12, newSnapshot.EndOfDayQuantity);
    }

    [Theory]
    [InlineData(true, 24)]
    [InlineData(false, 6)]
    public async Task GetTrendReportAsync_FiltersLatestSnapshotsByCommodityStatus(
        bool isCommodity,
        double expectedQuantity)
    {
        var commodityEntry = await AddEntryAsync("Green Beans", "Canned Vegetables", "Shelf", true);
        var nonCommodityEntry = await AddEntryAsync("Green Beans", "Canned Vegetables", "Back Room", false);
        var countedOn = At(2026, 6, 24, 8);

        context.InventoryCountHistory.AddRange(
            CreateHistory(commodityEntry, countedOn, 24, 4),
            CreateHistory(nonCommodityEntry, countedOn.AddHours(1), 6, -2));
        await context.SaveChangesAsync();

        var report = await CreateService().GetTrendReportAsync(
            new InventoryTrendReportRequest(
                InventoryTrendGrouping.Item,
                ItemName: "Green Beans",
                IsCommodity: isCommodity),
            At(2026, 7, 12));

        var point = Assert.Single(report.InventorySnapshots);
        Assert.Equal(expectedQuantity, point.EndOfDayQuantity);
        Assert.Equal(1, point.InventoryEntryCount);
    }

    [Fact]
    public async Task GetTrendReportAsync_ReportsUnknownMovementWhenAnyChangeIsUnknown()
    {
        var greenBeans = await AddEntryAsync("Green Beans", "Canned Vegetables", "Shelf", true);
        var peas = await AddEntryAsync("Peas", "Canned Vegetables", "Shelf", true);
        var countedOn = At(2026, 6, 24, 8);

        context.InventoryCountHistory.AddRange(
            CreateHistory(greenBeans, countedOn, 10, null),
            CreateHistory(peas, countedOn.AddHours(1), 5, 2));
        await context.SaveChangesAsync();

        var report = await CreateService().GetTrendReportAsync(
            new InventoryTrendReportRequest(InventoryTrendGrouping.Category),
            At(2026, 7, 12));

        var activity = Assert.Single(report.CountActivity);
        Assert.Null(activity.NetQuantityChange);
        Assert.Equal(2, activity.CountEventCount);
    }

    [Fact]
    public async Task GetTrendReportAsync_TrimsAndMatchesItemNameIgnoringCase()
    {
        var entry = await AddEntryAsync("Green Beans", "Canned Vegetables", "Shelf", true);
        context.InventoryCountHistory.Add(CreateHistory(entry, At(2026, 6, 24, 8), 14, null));
        await context.SaveChangesAsync();

        var report = await CreateService().GetTrendReportAsync(
            new InventoryTrendReportRequest(
                InventoryTrendGrouping.Item,
                ItemName: "  green beans  "),
            At(2026, 7, 12));

        Assert.Single(report.InventorySnapshots);
        Assert.Single(report.CountActivity);
        Assert.Equal("Green Beans", report.InventorySnapshots[0].GroupName);
    }

    [Fact]
    public async Task GetTrendReportAsync_ReturnsEmptyDatasetsWhenNoHistoryMatches()
    {
        var entry = await AddEntryAsync("Green Beans", "Canned Vegetables", "Shelf", true);
        context.InventoryCountHistory.Add(CreateHistory(entry, At(2026, 6, 24, 8), 14, null));
        await context.SaveChangesAsync();

        var request = new InventoryTrendReportRequest(
            InventoryTrendGrouping.Item,
            ItemName: "Missing Item");

        var report = await CreateService().GetTrendReportAsync(request, At(2026, 7, 12));

        Assert.False(report.HasData);
        Assert.False(report.HasInventorySnapshots);
        Assert.False(report.HasCountActivity);
        Assert.Empty(report.InventorySnapshots);
        Assert.Empty(report.CountActivity);
        Assert.Same(request, report.Request);
    }

    private InventoryTrendReportService CreateService()
    {
        return new InventoryTrendReportService(context);
    }

    private async Task<InventoryEntry> AddEntryAsync(
        string itemName,
        string categoryName,
        string locationName,
        bool isCommodity)
    {
        var item = await context.Items.SingleOrDefaultAsync(existing => existing.Name == itemName)
            ?? new Item { Name = itemName };
        var category = await context.Categories.SingleOrDefaultAsync(existing => existing.Name == categoryName)
            ?? new Category { Name = categoryName };
        var location = await context.Locations.SingleOrDefaultAsync(existing => existing.Name == locationName)
            ?? new Location { Name = locationName };

        var entry = new InventoryEntry
        {
            Item = item,
            Category = category,
            Location = location,
            CurrentQuantity = 0,
            IsCommodity = isCommodity,
            LastUpdatedAtUtc = At(2026, 6, 1),
        };

        context.InventoryEntries.Add(entry);
        await context.SaveChangesAsync();
        return entry;
    }

    private static InventoryCountHistory CreateHistory(
        InventoryEntry entry,
        DateTime countedAtUtc,
        double quantity,
        double? change)
    {
        return new InventoryCountHistory
        {
            InventoryEntryId = entry.Id,
            CountedAtUtc = countedAtUtc,
            CountedQuantity = quantity,
            PreviousQuantity = change.HasValue ? quantity - change.Value : null,
            QuantityChange = change,
            ItemIdAtCount = entry.ItemId,
            ItemNameAtCount = entry.Item.Name,
            CategoryIdAtCount = entry.CategoryId,
            CategoryNameAtCount = entry.Category.Name,
            LocationIdAtCount = entry.LocationId,
            LocationNameAtCount = entry.Location.Name,
            IsCommodityAtCount = entry.IsCommodity,
        };
    }

    private static DateTime At(int year, int month, int day, int hour = 0)
    {
        return new DateTime(year, month, day, hour, 0, 0, DateTimeKind.Utc);
    }
}
