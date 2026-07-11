using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using ProjectHopeLynden.Domain.Inventory;

namespace ProjectHopeLynden.Infrastructure.Persistence.Seeding;

[ExcludeFromCodeCoverage]
public sealed class InitialInventorySeeder(ProjectHopeDbContext context)
{
    private static readonly DateTime FirstCountDate = new(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime SecondCountDate = new(2026, 7, 8, 9, 0, 0, DateTimeKind.Utc);

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var categories = await EnsureCategoriesAsync(cancellationToken);
        var locations = await EnsureLocationsAsync(cancellationToken);
        var items = await EnsureItemsAsync(cancellationToken);

        foreach (var seedEntry in InitialInventorySeedData.InventoryEntries)
        {
            var inventoryEntry = await EnsureInventoryEntryAsync(
                items[seedEntry.ItemName],
                categories[seedEntry.CategoryName],
                locations[seedEntry.LocationName],
                seedEntry.CurrentQuantity,
                seedEntry.BestByDate,
                seedEntry.IsCommodity,
                seedEntry.IsMenuItem,
                cancellationToken);

            await EnsureHistoryAsync(
                inventoryEntry,
                seedEntry.PreviousQuantity,
                seedEntry.CurrentQuantity,
                cancellationToken);
        }
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
        var seedItemNames = InitialInventorySeedData.InventoryEntries
            .Select(entry => entry.ItemName)
            .Distinct(StringComparer.OrdinalIgnoreCase);

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
        DateTime? bestByDate,
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
            BestByDate = bestByDate,
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
