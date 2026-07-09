using ProjectHopeLynden.Domain.Inventory;

namespace ProjectHopeLynden.Domain.Tests.Inventory;

public sealed class InventoryEntityTests
{
    [Fact]
    public void Category_DefaultsToEmptyNameAndInventoryEntries()
    {
        var category = new Category();

        Assert.Equal(string.Empty, category.Name);
        Assert.Empty(category.InventoryEntries);
    }

    [Fact]
    public void Item_DefaultsToEmptyNameAndInventoryEntries()
    {
        var item = new Item();

        Assert.Equal(string.Empty, item.Name);
        Assert.Empty(item.InventoryEntries);
    }

    [Fact]
    public void Location_DefaultsToEmptyNameAndInventoryEntries()
    {
        var location = new Location();

        Assert.Equal(string.Empty, location.Name);
        Assert.Empty(location.InventoryEntries);
    }

    [Fact]
    public void InventoryEntry_StoresSpreadsheetFieldsAndRelationships()
    {
        var category = new Category { Id = 1, Name = "Canned Vegetables" };
        var item = new Item { Id = 2, Name = "Green Beans" };
        var location = new Location { Id = 3, Name = "Shelf" };
        var bestByDate = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastUpdatedAtUtc = new DateTime(2026, 7, 9, 12, 0, 0, DateTimeKind.Utc);

        var entry = new InventoryEntry
        {
            Id = 4,
            CategoryId = category.Id,
            Category = category,
            ItemId = item.Id,
            Item = item,
            LocationId = location.Id,
            Location = location,
            CurrentQuantity = 24,
            BestByDate = bestByDate,
            IsCommodity = true,
            IsMenuItem = false,
            LastUpdatedAtUtc = lastUpdatedAtUtc,
        };

        Assert.Equal(4, entry.Id);
        Assert.Same(category, entry.Category);
        Assert.Same(item, entry.Item);
        Assert.Same(location, entry.Location);
        Assert.Equal(24, entry.CurrentQuantity);
        Assert.Equal(bestByDate, entry.BestByDate);
        Assert.True(entry.IsCommodity);
        Assert.False(entry.IsMenuItem);
        Assert.Equal(lastUpdatedAtUtc, entry.LastUpdatedAtUtc);
        Assert.Empty(entry.CountHistory);
    }

    [Fact]
    public void InventoryCountHistory_StoresHistoricalCountValues()
    {
        var entry = new InventoryEntry { Id = 10, CurrentQuantity = 24 };
        var countedAtUtc = new DateTime(2026, 7, 9, 12, 0, 0, DateTimeKind.Utc);

        var history = new InventoryCountHistory
        {
            Id = 11,
            InventoryEntryId = entry.Id,
            InventoryEntry = entry,
            CountedQuantity = 24,
            CountedAtUtc = countedAtUtc,
            PreviousQuantity = 20,
            QuantityChange = 4,
        };

        Assert.Equal(11, history.Id);
        Assert.Equal(entry.Id, history.InventoryEntryId);
        Assert.Same(entry, history.InventoryEntry);
        Assert.Equal(24, history.CountedQuantity);
        Assert.Equal(countedAtUtc, history.CountedAtUtc);
        Assert.Equal(20, history.PreviousQuantity);
        Assert.Equal(4, history.QuantityChange);
    }
}
