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
    public async Task GetTrendReportAsync_AggregatesRequestedItemCountsOverTime()
    {
        var category = new Category { Name = "Canned Vegetables" };
        var location = new Location { Name = "Shelf" };
        var greenBeans = new Item { Name = "Green Beans" };
        var corn = new Item { Name = "Corn" };
        var greenBeanEntry = CreateEntry(greenBeans, category, location, true);
        var cornEntry = CreateEntry(corn, category, location, true);
        context.InventoryEntries.AddRange(greenBeanEntry, cornEntry);
        await context.SaveChangesAsync();

        var firstCount = At(2026, 6, 1);
        var secondCount = At(2026, 6, 8);
        context.InventoryCountHistory.AddRange(
            CreateHistory(greenBeanEntry, firstCount, 18, null),
            CreateHistory(greenBeanEntry, secondCount, 24, 6),
            CreateHistory(cornEntry, firstCount, 40, null));
        await context.SaveChangesAsync();

        var service = new InventoryTrendReportService(context);
        var generatedAtUtc = At(2026, 7, 12);

        var report = await service.GetTrendReportAsync(
            new InventoryTrendReportRequest(InventoryTrendGrouping.Item, ItemName: "Green Beans"),
            generatedAtUtc);

        Assert.True(report.HasPoints);
        Assert.Equal(generatedAtUtc, report.GeneratedAtUtc);
        Assert.Equal(2, report.Points.Count);
        Assert.All(report.Points, point => Assert.Equal("Green Beans", point.GroupName));
        Assert.Equal([18d, 24d], report.Points.Select(point => point.RecordedQuantity).ToArray());
        Assert.Null(report.Points[0].NetQuantityChange);
        Assert.Equal(6, report.Points[1].NetQuantityChange);
    }

    [Fact]
    public async Task GetTrendReportAsync_SummarizesMovementByCategoryAndDate()
    {
        var cannedVegetables = new Category { Name = "Canned Vegetables" };
        var cannedFruit = new Category { Name = "Canned Fruit" };
        var shelf = new Location { Name = "Shelf" };
        var countedOn = At(2026, 6, 24);
        var greenBeans = CreateEntry(new Item { Name = "Green Beans" }, cannedVegetables, shelf, true);
        var peas = CreateEntry(new Item { Name = "Peas" }, cannedVegetables, shelf, false);
        var peaches = CreateEntry(new Item { Name = "Peaches" }, cannedFruit, shelf, false);
        context.InventoryEntries.AddRange(greenBeans, peas, peaches);
        await context.SaveChangesAsync();

        context.InventoryCountHistory.AddRange(
            CreateHistory(greenBeans, countedOn, 10, 2),
            CreateHistory(peas, countedOn.AddHours(2), 5, -1),
            CreateHistory(peaches, countedOn, 30, 4));
        await context.SaveChangesAsync();

        var service = new InventoryTrendReportService(context);

        var report = await service.GetTrendReportAsync(
            new InventoryTrendReportRequest(
                InventoryTrendGrouping.Category,
                CategoryId: cannedVegetables.Id),
            At(2026, 7, 12));

        var point = Assert.Single(report.Points);
        Assert.Equal("Canned Vegetables", point.GroupName);
        Assert.Equal(countedOn.Date, point.CountedOnUtc);
        Assert.Equal(15, point.RecordedQuantity);
        Assert.Equal(1, point.NetQuantityChange);
        Assert.Equal(2, point.RecordCount);
    }

    [Theory]
    [InlineData(true, 24)]
    [InlineData(false, 6)]
    public async Task GetTrendReportAsync_FiltersSameItemByCommodityStatus(
        bool isCommodity,
        double expectedQuantity)
    {
        var category = new Category { Name = "Canned Vegetables" };
        var shelf = new Location { Name = "Shelf" };
        var backRoom = new Location { Name = "Back Room" };
        var item = new Item { Name = "Green Beans" };
        var commodityEntry = CreateEntry(item, category, shelf, true);
        var nonCommodityEntry = CreateEntry(item, category, backRoom, false);
        context.InventoryEntries.AddRange(commodityEntry, nonCommodityEntry);
        await context.SaveChangesAsync();

        var countedOn = At(2026, 6, 24);
        context.InventoryCountHistory.AddRange(
            CreateHistory(commodityEntry, countedOn, 24, 4),
            CreateHistory(nonCommodityEntry, countedOn, 6, -2));
        await context.SaveChangesAsync();

        var service = new InventoryTrendReportService(context);

        var report = await service.GetTrendReportAsync(
            new InventoryTrendReportRequest(
                InventoryTrendGrouping.Item,
                ItemName: "Green Beans",
                IsCommodity: isCommodity),
            At(2026, 7, 12));

        var point = Assert.Single(report.Points);
        Assert.Equal(expectedQuantity, point.RecordedQuantity);
        Assert.Equal(1, point.RecordCount);
    }

    [Fact]
    public async Task GetTrendReportAsync_ReturnsEmptyReportWhenNoHistoryMatches()
    {
        var service = new InventoryTrendReportService(context);
        var request = new InventoryTrendReportRequest(
            InventoryTrendGrouping.Item,
            ItemName: "Missing Item");

        var report = await service.GetTrendReportAsync(request, At(2026, 7, 12));

        Assert.False(report.HasPoints);
        Assert.Empty(report.Points);
        Assert.Same(request, report.Request);
    }

    private static InventoryEntry CreateEntry(
        Item item,
        Category category,
        Location location,
        bool isCommodity)
    {
        return new InventoryEntry
        {
            Item = item,
            Category = category,
            Location = location,
            CurrentQuantity = 0,
            IsCommodity = isCommodity,
            LastUpdatedAtUtc = At(2026, 6, 24),
        };
    }

    private static InventoryCountHistory CreateHistory(
        InventoryEntry entry,
        DateTime countedAtUtc,
        double quantity,
        double? change)
    {
        return new InventoryCountHistory
        {
            InventoryEntry = entry,
            CountedAtUtc = countedAtUtc,
            CountedQuantity = quantity,
            PreviousQuantity = change.HasValue ? quantity - change.Value : null,
            QuantityChange = change,
        };
    }

    private static DateTime At(int year, int month, int day)
    {
        return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
    }
}
