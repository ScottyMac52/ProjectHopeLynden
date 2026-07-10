using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProjectHopeLynden.Domain.Inventory;
using ProjectHopeLynden.Infrastructure.Inventory;
using ProjectHopeLynden.Infrastructure.Persistence;

namespace ProjectHopeLynden.Infrastructure.Tests.Inventory;

public sealed class InventoryQueryServiceTests : IAsyncLifetime
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
    public async Task GetCategoriesAsync_ReturnsCategoriesOrderedByName()
    {
        context.Categories.AddRange(
            new Category { Name = "Soup is a MESS" },
            new Category { Name = "Canned Vegetables" },
            new Category { Name = "Dry Beans" });
        await context.SaveChangesAsync();

        var service = new InventoryQueryService(context);

        var categories = await service.GetCategoriesAsync();

        Assert.Equal(
            ["Canned Vegetables", "Dry Beans", "Soup is a MESS"],
            categories.Select(category => category.Name).ToArray());
    }

    [Fact]
    public async Task GetInventoryForCategoryAsync_ReturnsOnlyEntriesForRequestedCategory()
    {
        var lastUpdatedAtUtc = new DateTime(2026, 7, 9, 12, 30, 0, DateTimeKind.Utc);
        var bestByDate = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var cannedVegetables = new Category { Name = "Canned Vegetables" };
        var cereal = new Category { Name = "Cereals" };
        var shelf = new Location { Name = "Shelf" };
        var backRoom = new Location { Name = "Back Room" };
        var greenBeans = new Item { Name = "Green Beans" };
        var oatCereal = new Item { Name = "Oat Cereal" };

        context.InventoryEntries.AddRange(
            new InventoryEntry
            {
                Category = cannedVegetables,
                Item = greenBeans,
                Location = shelf,
                CurrentQuantity = 24,
                IsCommodity = true,
                BestByDate = bestByDate,
                IsMenuItem = false,
                LastUpdatedAtUtc = lastUpdatedAtUtc,
            },
            new InventoryEntry
            {
                Category = cereal,
                Item = oatCereal,
                Location = backRoom,
                CurrentQuantity = 12,
                IsCommodity = false,
                IsMenuItem = true,
                LastUpdatedAtUtc = lastUpdatedAtUtc.AddHours(1),
            });
        await context.SaveChangesAsync();

        var service = new InventoryQueryService(context);

        var inventory = await service.GetInventoryForCategoryAsync(cannedVegetables.Id);

        Assert.NotNull(inventory);
        Assert.True(inventory.HasEntries);
        Assert.Equal(cannedVegetables.Id, inventory.CategoryId);
        Assert.Equal("Canned Vegetables", inventory.CategoryName);

        var entry = Assert.Single(inventory.Entries);
        Assert.Equal("Green Beans", entry.ItemName);
        Assert.Equal("Shelf", entry.LocationName);
        Assert.Equal(24, entry.CurrentQuantity);
        Assert.True(entry.IsCommodity);
        Assert.Equal(bestByDate, entry.BestByDate);
        Assert.False(entry.IsMenuItem);
        Assert.Equal(lastUpdatedAtUtc, entry.LastUpdatedAtUtc);
    }

    [Fact]
    public async Task GetInventoryForCategoryAsync_ReturnsEmptyViewForCategoryWithoutEntries()
    {
        var category = new Category { Name = "Dry Beans" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var service = new InventoryQueryService(context);

        var inventory = await service.GetInventoryForCategoryAsync(category.Id);

        Assert.NotNull(inventory);
        Assert.Equal("Dry Beans", inventory.CategoryName);
        Assert.False(inventory.HasEntries);
        Assert.Empty(inventory.Entries);
    }

    [Fact]
    public async Task GetInventoryForCategoryAsync_ReturnsNullForUnknownCategory()
    {
        var service = new InventoryQueryService(context);

        var inventory = await service.GetInventoryForCategoryAsync(42);

        Assert.Null(inventory);
    }
}
