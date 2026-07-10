using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProjectHopeLynden.Domain.Inventory;
using ProjectHopeLynden.Infrastructure.Inventory;
using ProjectHopeLynden.Infrastructure.Persistence;

namespace ProjectHopeLynden.Infrastructure.Tests.Inventory;

public sealed class InventoryCommodityServiceTests : IAsyncLifetime
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
    public async Task GetCommodityInventoryAsync_ReturnsOnlyCommodityEntries()
    {
        var cannedVegetables = new Category { Name = "Canned Vegetables" };
        var tomatoes = new Category { Name = "Tomatoes" };
        var shelf = new Location { Name = "Shelf" };
        var backRoom = new Location { Name = "Back Room" };
        var pantry = new Location { Name = "Pantry Area" };
        var greenBeans = new Item { Name = "Green Beans" };
        var tomatoSauce = new Item { Name = "Tomato Sauce" };
        var countedAtUtc = new DateTime(2026, 7, 12, 9, 0, 0, DateTimeKind.Utc);

        context.InventoryEntries.AddRange(
            new InventoryEntry
            {
                Category = cannedVegetables,
                Item = greenBeans,
                Location = shelf,
                CurrentQuantity = 24,
                IsCommodity = true,
                BestByDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                IsMenuItem = false,
                LastUpdatedAtUtc = countedAtUtc,
            },
            new InventoryEntry
            {
                Category = cannedVegetables,
                Item = greenBeans,
                Location = backRoom,
                CurrentQuantity = 6,
                IsCommodity = false,
                IsMenuItem = false,
                LastUpdatedAtUtc = countedAtUtc.AddMinutes(15),
            },
            new InventoryEntry
            {
                Category = tomatoes,
                Item = tomatoSauce,
                Location = pantry,
                CurrentQuantity = 18,
                IsCommodity = true,
                IsMenuItem = true,
                LastUpdatedAtUtc = countedAtUtc.AddMinutes(30),
            });
        await context.SaveChangesAsync();

        var service = new InventoryCommodityService(context);

        var commodityEntries = await service.GetCommodityInventoryAsync();

        Assert.Equal(2, commodityEntries.Count);
        Assert.DoesNotContain(commodityEntries, entry => entry.LocationName == "Back Room");
        Assert.All(commodityEntries, entry => Assert.True(entry.CurrentQuantity > 0));
        Assert.Equal(["Green Beans", "Tomato Sauce"], commodityEntries.Select(entry => entry.ItemName).ToArray());
    }

    [Fact]
    public async Task GetItemTotalAsync_CountsCommodityAndNonCommodityRowsInOperationalTotal()
    {
        var category = new Category { Name = "Canned Vegetables" };
        var shelf = new Location { Name = "Shelf" };
        var backRoom = new Location { Name = "Back Room" };
        var pantry = new Location { Name = "Pantry Area" };
        var greenBeans = new Item { Name = "Green Beans" };
        var tomatoSauce = new Item { Name = "Tomato Sauce" };
        var countedAtUtc = new DateTime(2026, 7, 12, 9, 0, 0, DateTimeKind.Utc);

        context.InventoryEntries.AddRange(
            new InventoryEntry
            {
                Category = category,
                Item = greenBeans,
                Location = shelf,
                CurrentQuantity = 24,
                IsCommodity = true,
                LastUpdatedAtUtc = countedAtUtc,
            },
            new InventoryEntry
            {
                Category = category,
                Item = greenBeans,
                Location = backRoom,
                CurrentQuantity = 6,
                IsCommodity = false,
                LastUpdatedAtUtc = countedAtUtc.AddMinutes(15),
            },
            new InventoryEntry
            {
                Category = category,
                Item = tomatoSauce,
                Location = pantry,
                CurrentQuantity = 40,
                IsCommodity = true,
                LastUpdatedAtUtc = countedAtUtc.AddMinutes(30),
            });
        await context.SaveChangesAsync();

        var service = new InventoryCommodityService(context);

        var total = await service.GetItemTotalAsync("Green Beans");

        Assert.NotNull(total);
        Assert.Equal("Green Beans", total.ItemName);
        Assert.Equal(30, total.OperationalTotalQuantity);
        Assert.Equal(24, total.CommodityQuantity);
        Assert.Equal(6, total.NonCommodityQuantity);
        Assert.True(total.HasCommodityInventory);
        Assert.True(total.HasNonCommodityInventory);
        Assert.True(total.HasMixedCommodityStatus);
        Assert.Equal([true, false], total.Entries.Select(entry => entry.IsCommodity).OrderDescending().ToArray());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Missing Item")]
    public async Task GetItemTotalAsync_ReturnsNullWhenItemNameDoesNotResolve(string? itemName)
    {
        var service = new InventoryCommodityService(context);

        var total = await service.GetItemTotalAsync(itemName!);

        Assert.Null(total);
    }
}
