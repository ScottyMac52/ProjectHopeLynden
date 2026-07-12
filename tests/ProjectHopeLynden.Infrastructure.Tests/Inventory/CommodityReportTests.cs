using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProjectHopeLynden.Domain.Inventory;
using ProjectHopeLynden.Infrastructure.Inventory;
using ProjectHopeLynden.Infrastructure.Persistence;

namespace ProjectHopeLynden.Infrastructure.Tests.Inventory;

public sealed class CommodityReportTests : IAsyncLifetime
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
    public async Task GetCommodityReportAsync_IncludesOnlyCommodityRowsAndTotals()
    {
        var category = new Category { Name = "Canned Vegetables" };
        var shelf = new Location { Name = "Shelf" };
        var backRoom = new Location { Name = "Back Room" };
        var pantry = new Location { Name = "Pantry Area" };
        var greenBeans = new Item { Name = "Green Beans" };
        var tomatoSauce = new Item { Name = "Tomato Sauce" };
        var greenBeansCountedAtUtc = new DateTime(2026, 7, 12, 9, 0, 0, DateTimeKind.Utc);
        var tomatoSauceCountedAtUtc = greenBeansCountedAtUtc.AddMinutes(30);
        var generatedAtUtc = greenBeansCountedAtUtc.AddHours(2);
        var bestByDate = new DateTime(2028, 4, 1, 0, 0, 0, DateTimeKind.Utc);

        context.InventoryEntries.AddRange(
            new InventoryEntry
            {
                Category = category,
                Item = greenBeans,
                Location = shelf,
                CurrentQuantity = 24,
                IsCommodity = true,
                BestByDate = bestByDate,
                LastUpdatedAtUtc = greenBeansCountedAtUtc,
            },
            new InventoryEntry
            {
                Category = category,
                Item = greenBeans,
                Location = backRoom,
                CurrentQuantity = 6,
                IsCommodity = false,
                LastUpdatedAtUtc = greenBeansCountedAtUtc.AddMinutes(15),
            },
            new InventoryEntry
            {
                Category = category,
                Item = tomatoSauce,
                Location = pantry,
                CurrentQuantity = 18,
                IsCommodity = true,
                LastUpdatedAtUtc = tomatoSauceCountedAtUtc,
            });
        await context.SaveChangesAsync();

        var service = new InventoryCommodityService(context);

        var report = await service.GetCommodityReportAsync(generatedAtUtc);

        Assert.True(report.HasRows);
        Assert.Equal(generatedAtUtc, report.GeneratedAtUtc);
        Assert.Equal(42, report.TotalQuantity);
        Assert.Equal(2, report.Rows.Count);
        Assert.DoesNotContain(report.Rows, row => row.LocationName == "Back Room");

        var greenBeansRow = Assert.Single(report.Rows, row => row.ItemName == "Green Beans");
        Assert.Equal("Canned Vegetables", greenBeansRow.CategoryName);
        Assert.Equal("Shelf", greenBeansRow.LocationName);
        Assert.Equal(24, greenBeansRow.Quantity);
        Assert.Equal(bestByDate, greenBeansRow.BestByDate);
        Assert.Equal(greenBeansCountedAtUtc, greenBeansRow.QuantityAsOfUtc);
        Assert.True(greenBeansRow.InventoryEntryId > 0);
    }

    [Fact]
    public async Task GetCommodityReportAsync_ReturnsEmptyFlexibleReportWhenNoCommodityInventoryExists()
    {
        context.InventoryEntries.Add(new InventoryEntry
        {
            Category = new Category { Name = "Cereals" },
            Item = new Item { Name = "Oat Cereal" },
            Location = new Location { Name = "Shelf" },
            CurrentQuantity = 12,
            IsCommodity = false,
            LastUpdatedAtUtc = new DateTime(2026, 7, 12, 9, 0, 0, DateTimeKind.Utc),
        });
        await context.SaveChangesAsync();

        var generatedAtUtc = new DateTime(2026, 7, 12, 12, 0, 0, DateTimeKind.Utc);
        var service = new InventoryCommodityService(context);

        var report = await service.GetCommodityReportAsync(generatedAtUtc);

        Assert.False(report.HasRows);
        Assert.Empty(report.Rows);
        Assert.Equal(0, report.TotalQuantity);
        Assert.Equal(generatedAtUtc, report.GeneratedAtUtc);
    }
}
