using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProjectHopeLynden.Application.Inventory;
using ProjectHopeLynden.Domain.Inventory;
using ProjectHopeLynden.Infrastructure.Inventory;
using ProjectHopeLynden.Infrastructure.Persistence;

namespace ProjectHopeLynden.Infrastructure.Tests.Inventory;

public sealed class InventoryEntryMaintenanceServiceTests : IAsyncLifetime
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
    public async Task CreateEntryAsync_SavesValidInventoryEntryInRequestedCategory()
    {
        var category = await AddCategoryAsync("Canned Vegetables");
        var location = await AddLocationAsync("Shelf");
        var savedAtUtc = new DateTime(2026, 7, 13, 9, 0, 0, DateTimeKind.Utc);
        var service = new InventoryEntryMaintenanceService(context);

        var result = await service.CreateEntryAsync(
            new InventoryEntrySaveRequest(
                "Green Beans",
                category.Id,
                location.Id,
                CurrentQuantity: 24,
                BestByDate: new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
                IsCommodity: true,
                IsMenuItem: false),
            savedAtUtc);

        Assert.True(result.Succeeded);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(category.Id, result.CategoryId);

        var entry = await context.InventoryEntries
            .AsNoTracking()
            .Include(inventoryEntry => inventoryEntry.Item)
            .Include(inventoryEntry => inventoryEntry.Category)
            .Include(inventoryEntry => inventoryEntry.Location)
            .SingleAsync();

        Assert.Equal("Green Beans", entry.Item.Name);
        Assert.Equal("Canned Vegetables", entry.Category.Name);
        Assert.Equal("Shelf", entry.Location.Name);
        Assert.Equal(24, entry.CurrentQuantity);
        Assert.True(entry.IsCommodity);
        Assert.False(entry.IsMenuItem);
        Assert.Equal(savedAtUtc, entry.LastUpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateEntryAsync_EditsLocationBestByDateCommodityAndMenuItem()
    {
        var originalCategory = await AddCategoryAsync("Canned Vegetables");
        var shelf = await AddLocationAsync("Shelf");
        var backRoom = await AddLocationAsync("Back Room");
        var entry = await AddInventoryEntryAsync("Green Beans", originalCategory, shelf, quantity: 24, isCommodity: true, isMenuItem: false);
        var savedAtUtc = new DateTime(2026, 7, 14, 10, 30, 0, DateTimeKind.Utc);
        var service = new InventoryEntryMaintenanceService(context);

        var result = await service.UpdateEntryAsync(
            entry.Id,
            new InventoryEntrySaveRequest(
                "Green Beans",
                originalCategory.Id,
                backRoom.Id,
                CurrentQuantity: null,
                BestByDate: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                IsCommodity: false,
                IsMenuItem: true),
            savedAtUtc);

        Assert.True(result.Succeeded);

        var savedEntry = await context.InventoryEntries
            .AsNoTracking()
            .SingleAsync(inventoryEntry => inventoryEntry.Id == entry.Id);

        Assert.Equal(backRoom.Id, savedEntry.LocationId);
        Assert.Equal(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), savedEntry.BestByDate);
        Assert.False(savedEntry.IsCommodity);
        Assert.True(savedEntry.IsMenuItem);
        Assert.Equal(24, savedEntry.CurrentQuantity);
        Assert.Equal(savedAtUtc, savedEntry.LastUpdatedAtUtc);
    }

    [Fact]
    public async Task CreateEntryAsync_RejectsMissingRequiredFieldsWithoutSaving()
    {
        var location = await AddLocationAsync("Shelf");
        var service = new InventoryEntryMaintenanceService(context);

        var result = await service.CreateEntryAsync(
            new InventoryEntrySaveRequest(
                "   ",
                CategoryId: null,
                location.Id,
                CurrentQuantity: 12,
                BestByDate: null,
                IsCommodity: false,
                IsMenuItem: false),
            new DateTime(2026, 7, 13, 9, 0, 0, DateTimeKind.Utc));

        Assert.False(result.Succeeded);
        Assert.Equal("Item name is required.", result.ErrorMessage);
        Assert.False(await context.InventoryEntries.AnyAsync());
    }

    [Fact]
    public async Task UpdateEntryAsync_KeepsCommodityStatusSpecificToInventoryEntry()
    {
        var category = await AddCategoryAsync("Canned Vegetables");
        var shelf = await AddLocationAsync("Shelf");
        var backRoom = await AddLocationAsync("Back Room");
        var commodityEntry = await AddInventoryEntryAsync("Green Beans", category, shelf, quantity: 24, isCommodity: true, isMenuItem: false);
        var nonCommodityEntry = await AddInventoryEntryAsync("Green Beans", category, backRoom, quantity: 6, isCommodity: false, isMenuItem: false);
        var service = new InventoryEntryMaintenanceService(context);

        var result = await service.UpdateEntryAsync(
            nonCommodityEntry.Id,
            new InventoryEntrySaveRequest(
                "Green Beans",
                category.Id,
                backRoom.Id,
                CurrentQuantity: null,
                BestByDate: null,
                IsCommodity: false,
                IsMenuItem: true),
            new DateTime(2026, 7, 14, 10, 30, 0, DateTimeKind.Utc));

        Assert.True(result.Succeeded);

        var savedEntries = await context.InventoryEntries
            .AsNoTracking()
            .OrderBy(inventoryEntry => inventoryEntry.Id)
            .ToListAsync();

        Assert.True(savedEntries.Single(inventoryEntry => inventoryEntry.Id == commodityEntry.Id).IsCommodity);
        Assert.False(savedEntries.Single(inventoryEntry => inventoryEntry.Id == nonCommodityEntry.Id).IsCommodity);
        Assert.True(savedEntries.Single(inventoryEntry => inventoryEntry.Id == nonCommodityEntry.Id).IsMenuItem);
    }

    [Fact]
    public async Task GetFormOptionsAsync_ReturnsCategoriesAndLocationsOrderedByName()
    {
        await AddCategoryAsync("Soup is a MESS");
        await AddCategoryAsync("Canned Vegetables");
        await AddLocationAsync("Shelf");
        await AddLocationAsync("Back Room");
        var service = new InventoryEntryMaintenanceService(context);

        var options = await service.GetFormOptionsAsync();

        Assert.Equal(["Canned Vegetables", "Soup is a MESS"], options.Categories.Select(category => category.Name).ToArray());
        Assert.Equal(["Back Room", "Shelf"], options.Locations.Select(location => location.Name).ToArray());
    }

    private async Task<Category> AddCategoryAsync(string name)
    {
        var category = new Category { Name = name };
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        return category;
    }

    private async Task<Location> AddLocationAsync(string name)
    {
        var location = new Location { Name = name };
        context.Locations.Add(location);
        await context.SaveChangesAsync();
        return location;
    }

    private async Task<InventoryEntry> AddInventoryEntryAsync(
        string itemName,
        Category category,
        Location location,
        int quantity,
        bool isCommodity,
        bool isMenuItem)
    {
        var item = await context.Items.SingleOrDefaultAsync(existingItem => existingItem.Name == itemName)
            ?? new Item { Name = itemName };

        var entry = new InventoryEntry
        {
            Item = item,
            Category = category,
            Location = location,
            CurrentQuantity = quantity,
            IsCommodity = isCommodity,
            IsMenuItem = isMenuItem,
            LastUpdatedAtUtc = new DateTime(2026, 7, 13, 8, 0, 0, DateTimeKind.Utc),
        };

        context.InventoryEntries.Add(entry);
        await context.SaveChangesAsync();
        return entry;
    }
}
