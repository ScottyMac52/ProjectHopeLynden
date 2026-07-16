using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using ProjectHopeLynden.Domain.Inventory;

namespace ProjectHopeLynden.Infrastructure.Persistence.Seeding;

[ExcludeFromCodeCoverage]
public sealed class InitialInventorySeeder(ProjectHopeDbContext context)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var categories = await EnsureCategoriesAsync(cancellationToken);
        var locations = await EnsureLocationsAsync(cancellationToken);
        var items = await EnsureItemsAsync(cancellationToken);

        foreach (var seedEntry in InitialInventorySeedData.InventoryEntries)
        {
            var item = items[seedEntry.ItemName];
            var category = categories[seedEntry.CategoryName];
            var location = locations[seedEntry.LocationName];
            var inventoryEntry = await EnsureInventoryEntryAsync(
                item,
                category,
                location,
                seedEntry.CurrentQuantity,
                seedEntry.BestByDate,
                seedEntry.IsCommodity,
                seedEntry.IsMenuItem,
                seedEntry.LastUpdatedAtUtc,
                cancellationToken);

            await EnsureHistoryAsync(
                inventoryEntry,
                item,
                category,
                location,
                seedEntry.PreviousQuantity,
                seedEntry.CurrentQuantity,
                seedEntry.LastUpdatedAtUtc,
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
        double currentQuantity,
        DateTime? bestByDate,
        bool isCommodity,
        bool isMenuItem,
        DateTime lastUpdatedAtUtc,
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
            LastUpdatedAtUtc = lastUpdatedAtUtc,
        };

        context.InventoryEntries.Add(entry);
        await context.SaveChangesAsync(cancellationToken);

        return entry;
    }

    private async Task EnsureHistoryAsync(
        InventoryEntry inventoryEntry,
        Item item,
        Category category,
        Location location,
        double? previousQuantity,
        double currentQuantity,
        DateTime countedAtUtc,
        CancellationToken cancellationToken)
    {
        var history = await context.InventoryCountHistory
            .Where(record => record.InventoryEntryId == inventoryEntry.Id)
            .OrderBy(record => record.CountedAtUtc)
            .ThenBy(record => record.Id)
            .ToListAsync(cancellationToken);

        if (history.Count == 0)
        {
            context.InventoryCountHistory.Add(CreateHistory(
                inventoryEntry,
                item,
                category,
                location,
                currentQuantity,
                countedAtUtc,
                null,
                null));

            await context.SaveChangesAsync(cancellationToken);
            return;
        }

        if (!previousQuantity.HasValue)
        {
            return;
        }

        var expectedChange = currentQuantity - previousQuantity.Value;
        var syntheticPrior = history.SingleOrDefault(record =>
            record.CountedAtUtc == countedAtUtc.AddDays(-7)
            && record.CountedQuantity == previousQuantity.Value
            && !record.PreviousQuantity.HasValue
            && !record.QuantityChange.HasValue);
        var importedCurrent = history.SingleOrDefault(record =>
            record.CountedAtUtc == countedAtUtc
            && record.CountedQuantity == currentQuantity
            && record.PreviousQuantity == previousQuantity.Value
            && record.QuantityChange == expectedChange);

        if (syntheticPrior is null || importedCurrent is null)
        {
            return;
        }

        context.InventoryCountHistory.Remove(syntheticPrior);
        importedCurrent.PreviousQuantity = null;
        importedCurrent.QuantityChange = null;
        importedCurrent.ItemIdAtCount = item.Id;
        importedCurrent.ItemNameAtCount = item.Name;
        importedCurrent.CategoryIdAtCount = category.Id;
        importedCurrent.CategoryNameAtCount = category.Name;
        importedCurrent.LocationIdAtCount = location.Id;
        importedCurrent.LocationNameAtCount = location.Name;
        importedCurrent.IsCommodityAtCount = inventoryEntry.IsCommodity;

        await context.SaveChangesAsync(cancellationToken);
    }

    private static InventoryCountHistory CreateHistory(
        InventoryEntry inventoryEntry,
        Item item,
        Category category,
        Location location,
        double countedQuantity,
        DateTime countedAtUtc,
        double? previousQuantity,
        double? quantityChange)
    {
        return new InventoryCountHistory
        {
            InventoryEntryId = inventoryEntry.Id,
            CountedQuantity = countedQuantity,
            CountedAtUtc = countedAtUtc,
            PreviousQuantity = previousQuantity,
            QuantityChange = quantityChange,
            ItemIdAtCount = item.Id,
            ItemNameAtCount = item.Name,
            CategoryIdAtCount = category.Id,
            CategoryNameAtCount = category.Name,
            LocationIdAtCount = location.Id,
            LocationNameAtCount = location.Name,
            IsCommodityAtCount = inventoryEntry.IsCommodity,
        };
    }
}
