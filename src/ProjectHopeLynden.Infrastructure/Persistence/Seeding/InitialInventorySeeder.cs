using Microsoft.EntityFrameworkCore;
using ProjectHopeLynden.Domain.Inventory;

namespace ProjectHopeLynden.Infrastructure.Persistence.Seeding;

public sealed class InitialInventorySeeder(ProjectHopeDbContext context)
{
    private static readonly DateTime FirstCountDate = new(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime SecondCountDate = new(2026, 7, 8, 9, 0, 0, DateTimeKind.Utc);

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var categories = await EnsureCategoriesAsync(cancellationToken);
        var locations = await EnsureLocationsAsync(cancellationToken);
        var items = await EnsureItemsAsync(cancellationToken);

        var greenBeansCommodity = await EnsureInventoryEntryAsync(
            items["Green Beans"],
            categories["Canned Vegetables"],
            locations["Shelf"],
            currentQuantity: 24,
            isCommodity: true,
            isMenuItem: false,
            cancellationToken);

        var greenBeansNonCommodity = await EnsureInventoryEntryAsync(
            items["Green Beans"],
            categories["Canned Vegetables"],
            locations["Back Room"],
            currentQuantity: 18,
            isCommodity: false,
            isMenuItem: false,
            cancellationToken);

        var tomatoSauceCommodity = await EnsureInventoryEntryAsync(
            items["Tomato Sauce"],
            categories["Tomatoes"],
            locations["Pantry Area"],
            currentQuantity: 36,
            isCommodity: true,
            isMenuItem: true,
            cancellationToken);

        var cerealNonCommodity = await EnsureInventoryEntryAsync(
            items["Oat Cereal"],
            categories["Cereals"],
            locations["Shelf"],
            currentQuantity: 12,
            isCommodity: false,
            isMenuItem: false,
            cancellationToken);

        await EnsureHistoryAsync(greenBeansCommodity, previousQuantity: 20, currentQuantity: 24, cancellationToken);
        await EnsureHistoryAsync(greenBeansNonCommodity, previousQuantity: 14, currentQuantity: 18, cancellationToken);
        await EnsureHistoryAsync(tomatoSauceCommodity, previousQuantity: 30, currentQuantity: 36, cancellationToken);
        await EnsureHistoryAsync(cerealNonCommodity, previousQuantity: 9, currentQuantity: 12, cancellationToken);
    }

    private async Task<Dictionary<string, Category>> EnsureCategoriesAsync(CancellationToken cancellationToken)
    {
        var categories = await context.Categories.ToListAsync(cancellationToken);
        var categoryLookup = categories.ToDictionary(category => category.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var categoryName in InitialInventorySeedData.CategoryNames)
        {
            if (categoryLookup.ContainsKey(categoryName))
            {
                continue;
            }

            var category = new Category { Name = categoryName };
            context.Categories.Add(category);
            categoryLookup[categoryName] = category;
        }

        await context.SaveChangesAsync(cancellationToken);
        return categoryLookup;
    }

    private async Task<Dictionary<string, Location>> EnsureLocationsAsync(CancellationToken cancellationToken)
    {
        var locations = await context.Locations.ToListAsync(cancellationToken);
        var locationLookup = locations.ToDictionary(location => location.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var locationName in InitialInventorySeedData.LocationNames)
        {
            if (locationLookup.ContainsKey(locationName))
            {
                continue;
            }

            var location = new Location { Name = locationName };
            context.Locations.Add(location);
            locationLookup[locationName] = location;
        }

        await context.SaveChangesAsync(cancellationToken);
        return locationLookup;
    }

    private async Task<Dictionary<string, Item>> EnsureItemsAsync(CancellationToken cancellationToken)
    {
        var seedItemNames = new[]
        {
            "Green Beans",
            "Tomato Sauce",
            "Oat Cereal",
        };

        var items = await context.Items.ToListAsync(cancellationToken);
        var itemLookup = items.ToDictionary(item => item.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var itemName in seedItemNames)
        {
            if (itemLookup.ContainsKey(itemName))
            {
                continue;
            }

            var item = new Item { Name = itemName };
            context.Items.Add(item);
            itemLookup[itemName] = item;
        }

        await context.SaveChangesAsync(cancellationToken);
        return itemLookup;
    }

    private async Task<InventoryEntry> EnsureInventoryEntryAsync(
        Item item,
        Category category,
        Location location,
        int currentQuantity,
        bool isCommodity,
        bool isMenuItem,
        CancellationToken cancellationToken)
    {
        var existingEntry = await context.InventoryEntries.SingleOrDefaultAsync(
            entry => entry.ItemId == item.Id
                && entry.CategoryId == category.Id
                && entry.LocationId == location.Id
                && entry.IsCommodity == isCommodity,
            cancellationToken);

        if (existingEntry is not null)
        {
            return existingEntry;
        }

        var entry = new InventoryEntry
        {
            ItemId = item.Id,
            CategoryId = category.Id,
            LocationId = location.Id,
            CurrentQuantity = currentQuantity,
            IsCommodity = isCommodity,
            IsMenuItem = isMenuItem,
            LastUpdatedAtUtc = SecondCountDate,
        };

        context.InventoryEntries.Add(entry);
        await context.SaveChangesAsync(cancellationToken);

        return entry;
    }

    private async Task EnsureHistoryAsync(
        InventoryEntry inventoryEntry,
        int previousQuantity,
        int currentQuantity,
        CancellationToken cancellationToken)
    {
        var hasHistory = await context.InventoryCountHistory.AnyAsync(
            history => history.InventoryEntryId == inventoryEntry.Id,
            cancellationToken);

        if (hasHistory)
        {
            return;
        }

        context.InventoryCountHistory.AddRange(
            new InventoryCountHistory
            {
                InventoryEntryId = inventoryEntry.Id,
                CountedQuantity = previousQuantity,
                CountedAtUtc = FirstCountDate,
                PreviousQuantity = null,
                QuantityChange = null,
            },
            new InventoryCountHistory
            {
                InventoryEntryId = inventoryEntry.Id,
                CountedQuantity = currentQuantity,
                CountedAtUtc = SecondCountDate,
                PreviousQuantity = previousQuantity,
                QuantityChange = currentQuantity - previousQuantity,
            });

        await context.SaveChangesAsync(cancellationToken);
    }
}
