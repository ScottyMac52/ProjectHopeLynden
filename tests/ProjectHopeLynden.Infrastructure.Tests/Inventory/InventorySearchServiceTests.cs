using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProjectHopeLynden.Domain.Inventory;
using ProjectHopeLynden.Infrastructure.Inventory;
using ProjectHopeLynden.Infrastructure.Persistence;

namespace ProjectHopeLynden.Infrastructure.Tests.Inventory;

public sealed class InventorySearchServiceTests : IAsyncLifetime
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
    public async Task SearchAsync_MatchesPartialItemNamesWithoutCaseSensitivity()
    {
        var dryBeans = new Category { Name = "Dry Beans" };
        var cannedVegetables = new Category { Name = "Canned Vegetables" };
        var tomatoes = new Category { Name = "Tomatoes" };
        var backRoom = new Location { Name = "Back Room" };
        var shelf = new Location { Name = "Shelf" };
        var countedAtUtc = new DateTime(2026, 7, 12, 10, 0, 0, DateTimeKind.Utc);

        context.InventoryEntries.AddRange(
            new InventoryEntry
            {
                Item = new Item { Name = "Black Beans" },
                Category = dryBeans,
                Location = backRoom,
                CurrentQuantity = 12,
                IsCommodity = false,
                LastUpdatedAtUtc = countedAtUtc,
            },
            new InventoryEntry
            {
                Item = new Item { Name = "Green Beans" },
                Category = cannedVegetables,
                Location = shelf,
                CurrentQuantity = 24,
                IsCommodity = true,
                BestByDate = new DateTime(2028, 4, 1),
                IsMenuItem = true,
                LastUpdatedAtUtc = countedAtUtc.AddMinutes(15),
            },
            new InventoryEntry
            {
                Item = new Item { Name = "Tomato Sauce" },
                Category = tomatoes,
                Location = shelf,
                CurrentQuantity = 8,
                IsCommodity = false,
                LastUpdatedAtUtc = countedAtUtc.AddMinutes(30),
            });
        await context.SaveChangesAsync();

        var service = new InventorySearchService(context);

        var result = await service.SearchAsync("  bEaN  ");

        Assert.True(result.HasResults);
        Assert.Equal("bEaN", result.SearchTerm);
        Assert.Equal(["Black Beans", "Green Beans"], result.Rows.Select(row => row.ItemName).ToArray());
        Assert.DoesNotContain(result.Rows, row => row.ItemName == "Tomato Sauce");

        var greenBeans = Assert.Single(result.Rows, row => row.ItemName == "Green Beans");
        Assert.Equal("Canned Vegetables", greenBeans.CategoryName);
        Assert.Equal("Shelf", greenBeans.LocationName);
        Assert.Equal(24, greenBeans.CurrentQuantity);
        Assert.True(greenBeans.IsCommodity);
        Assert.True(greenBeans.IsMenuItem);
        Assert.Equal(new DateTime(2028, 4, 1), greenBeans.BestByDate);
    }

    [Fact]
    public async Task SearchAsync_ReturnsCommodityAndNonCommodityRowsForSameItem()
    {
        var item = new Item { Name = "Green Beans" };
        var cannedVegetables = new Category { Name = "Canned Vegetables" };
        var produce = new Category { Name = "Produce" };
        var shelf = new Location { Name = "Shelf" };
        var backRoom = new Location { Name = "Back Room" };
        var countedAtUtc = new DateTime(2026, 7, 12, 11, 0, 0, DateTimeKind.Utc);

        context.InventoryEntries.AddRange(
            new InventoryEntry
            {
                Item = item,
                Category = cannedVegetables,
                Location = shelf,
                CurrentQuantity = 24,
                IsCommodity = true,
                LastUpdatedAtUtc = countedAtUtc,
            },
            new InventoryEntry
            {
                Item = item,
                Category = produce,
                Location = backRoom,
                CurrentQuantity = 6,
                IsCommodity = false,
                LastUpdatedAtUtc = countedAtUtc.AddMinutes(10),
            });
        await context.SaveChangesAsync();

        var service = new InventorySearchService(context);

        var result = await service.SearchAsync("green");

        Assert.Equal(2, result.Rows.Count);
        Assert.Equal(["Canned Vegetables", "Produce"], result.Rows.Select(row => row.CategoryName).ToArray());
        Assert.Equal(["Shelf", "Back Room"], result.Rows.Select(row => row.LocationName).ToArray());
        Assert.Contains(result.Rows, row => row.IsCommodity);
        Assert.Contains(result.Rows, row => !row.IsCommodity);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchAsync_ReturnsEmptyResultForMissingSearchTerm(string? searchTerm)
    {
        var service = new InventorySearchService(context);

        var result = await service.SearchAsync(searchTerm);

        Assert.False(result.HasResults);
        Assert.Equal(string.Empty, result.SearchTerm);
        Assert.Empty(result.Rows);
    }

    [Fact]
    public async Task SearchAsync_ReturnsClearEmptyResultWhenNothingMatches()
    {
        context.InventoryEntries.Add(new InventoryEntry
        {
            Item = new Item { Name = "Green Beans" },
            Category = new Category { Name = "Canned Vegetables" },
            Location = new Location { Name = "Shelf" },
            CurrentQuantity = 24,
            IsCommodity = true,
            LastUpdatedAtUtc = new DateTime(2026, 7, 12, 12, 0, 0, DateTimeKind.Utc),
        });
        await context.SaveChangesAsync();

        var service = new InventorySearchService(context);

        var result = await service.SearchAsync("coffee");

        Assert.False(result.HasResults);
        Assert.Equal("coffee", result.SearchTerm);
        Assert.Empty(result.Rows);
    }
}
