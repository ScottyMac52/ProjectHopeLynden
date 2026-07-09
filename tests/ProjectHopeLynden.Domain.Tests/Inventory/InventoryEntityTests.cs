using ProjectHopeLynden.Domain.Inventory;
using Xunit;

namespace ProjectHopeLynden.Domain.Tests.Inventory;

public sealed class InventoryEntityTests
{
    [Fact]
    public void Category_DefaultsToEmptyNameAndEmptyInventoryEntries()
    {
        var category = new Category();

        Assert.Equal(0, category.Id);
        Assert.Equal(string.Empty, category.Name);
        Assert.Empty(category.InventoryEntries);
    }

    [Fact]
    public void Category_AllowsPropertiesToBeSet()
    {
        var entry = new InventoryEntry { Id = 10 };
        var category = new Category
        {
            Id = 1,
            Name = "Canned Vegetables",
            InventoryEntries = [entry],
        };

        Assert.Equal(1, category.Id);
        Assert.Equal("Canned Vegetables", category.Name);
        Assert.Same(entry, category.InventoryEntries.Single());
    }

    [Fact]
    public void Item_DefaultsToEmptyNameAndEmptyInventoryEntries()
    {
        var item = new Item();

        Assert.Equal(0, item.Id);
        Assert.Equal(string.Empty, item.Name);
        Assert.Empty(item.InventoryEntries);
    }

    [Fact]
    public void Item_AllowsPropertiesToBeSet()
    {
        var entry = new InventoryEntry { Id = 11 };
        var item = new Item
        {
            Id = 2,
            Name = "Green Beans",
            InventoryEntries = [entry],
        };

        Assert.Equal(2, item.Id);
        Assert.Equal("Green Beans", item.Name);
        Assert.Same(entry, item.InventoryEntries.Single());
    }

    [Fact]
    public void Location_DefaultsToEmptyNameAndEmptyInventoryEntries()
    {
        var location = new Location();

        Assert.Equal(0, location.Id);
        Assert.Equal(string.Empty, location.Name);
        Assert.Empty(location.InventoryEntries);
    }

    [Fact]
    public void Location_AllowsPropertiesToBeSet()
    {
        var entry = new InventoryEntry { Id = 12 };
        var location = new Location
        {
            Id = 3,
            Name = "Shelf",
            InventoryEntries = [entry],
        };

        Assert.Equal(3, location.Id);
        Assert.Equal("Shelf", location.Name);
        Assert.Same(entry, location.InventoryEntries.Single());
    }

    [Fact]
    public void InventoryEntry_DefaultsExpectedValues()
    {
        var entry = new InventoryEntry();

        Assert.Equal(0, entry.Id);
        Assert.Equal(0, entry.ItemId);
        Assert.Equal(0, entry.CategoryId);
        Assert.Equal(0, entry.LocationId);
        Assert.Equal(0, entry.CurrentQuantity);
        Assert.Null(entry.BestByDate);
        Assert.False(entry.IsCommodity);
        Assert.False(entry.IsMenuItem);
        Assert.Equal(default(DateTime), entry.LastUpdatedAtUtc);
        Assert.Empty(entry.CountHistory);
    }

    [Fact]
    public void InventoryEntry_AllowsPropertiesAndRelationshipsToBeSet()
    {
        var item = new Item { Id = 1, Name = "Green Beans" };
        var category = new Category { Id = 2, Name = "Canned Vegetables" };
        var location = new Location { Id = 3, Name = "Shelf" };
        var history = new InventoryCountHistory { Id = 4 };
        var lastUpdated = new DateTime(2026, 7, 9, 10, 0, 0, DateTimeKind.Utc);
        var bestByDate = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var entry = new InventoryEntry
        {
            Id = 5,
            ItemId = item.Id,
            Item = item,
            CategoryId = category.Id,
            Category = category,
            LocationId = location.Id,
            Location = location,
            CurrentQuantity = 24,
            BestByDate = bestByDate,
            IsCommodity = true,
            IsMenuItem = true,
            LastUpdatedAtUtc = lastUpdated,
            CountHistory = [history],
        };

        Assert.Equal(5, entry.Id);
        Assert.Equal(1, entry.ItemId);
        Assert.Same(item, entry.Item);
        Assert.Equal(2, entry.CategoryId);
        Assert.Same(category, entry.Category);
        Assert.Equal(3, entry.LocationId);
        Assert.Same(location, entry.Location);
        Assert.Equal(24, entry.CurrentQuantity);
        Assert.Equal(bestByDate, entry.BestByDate);
        Assert.True(entry.IsCommodity);
        Assert.True(entry.IsMenuItem);
        Assert.Equal(lastUpdated, entry.LastUpdatedAtUtc);
        Assert.Same(history, entry.CountHistory.Single());
    }

    [Fact]
    public void InventoryCountHistory_DefaultsExpectedValues()
    {
        var history = new InventoryCountHistory();

        Assert.Equal(0, history.Id);
        Assert.Equal(0, history.InventoryEntryId);
        Assert.Equal(0, history.CountedQuantity);
        Assert.Equal(default(DateTime), history.CountedAtUtc);
        Assert.Null(history.PreviousQuantity);
        Assert.Null(history.QuantityChange);
    }

    [Fact]
    public void InventoryCountHistory_AllowsPropertiesAndRelationshipToBeSet()
    {
        var entry = new InventoryEntry { Id = 7 };
        var countedAt = new DateTime(2026, 7, 8, 9, 0, 0, DateTimeKind.Utc);

        var history = new InventoryCountHistory
        {
            Id = 8,
            InventoryEntryId = entry.Id,
            InventoryEntry = entry,
            CountedQuantity = 24,
            CountedAtUtc = countedAt,
            PreviousQuantity = 20,
            QuantityChange = 4,
        };

        Assert.Equal(8, history.Id);
        Assert.Equal(7, history.InventoryEntryId);
        Assert.Same(entry, history.InventoryEntry);
        Assert.Equal(24, history.CountedQuantity);
        Assert.Equal(countedAt, history.CountedAtUtc);
        Assert.Equal(20, history.PreviousQuantity);
        Assert.Equal(4, history.QuantityChange);
    }
}
