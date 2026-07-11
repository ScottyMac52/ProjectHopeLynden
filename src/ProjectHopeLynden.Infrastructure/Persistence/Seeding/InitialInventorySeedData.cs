using System.Diagnostics.CodeAnalysis;

namespace ProjectHopeLynden.Infrastructure.Persistence.Seeding;

[ExcludeFromCodeCoverage]
public static class InitialInventorySeedData
{
    public static readonly string[] CategoryNames =
    [
        "Dry Beans",
        "Noodles",
        "Dry Mix",
        "Condiments",
        "Snacks",
        "Cereals",
        "Produce",
        "Eggs",
        "Frozen Meat",
        "Frozen Miscellaneous",
        "Canned Vegetables",
        "Canned Fruit",
        "Soup is a MESS",
        "Canned Beans",
        "Tomatoes",
        "Canned Meat",
        "Diapers",
        "Wipes",
        "Formula",
    ];

    public static readonly string[] LocationNames =
    [
        "Shelf",
        "Back Room",
        "Freezer",
        "Pantry Area",
    ];

    public static readonly InventorySeedEntry[] InventoryEntries =
    [
        new("Pinto Beans", "Dry Beans", "Pantry Area", 16, 12, BestBy(2027, 6, 30), false, false),
        new("Spaghetti", "Noodles", "Shelf", 20, 18, BestBy(2027, 4, 30), false, true),
        new("Pancake Mix", "Dry Mix", "Pantry Area", 8, 10, BestBy(2027, 2, 28), false, true),
        new("Ketchup", "Condiments", "Shelf", 14, 12, BestBy(2027, 8, 31), false, false),
        new("Granola Bars", "Snacks", "Shelf", 30, 24, BestBy(2027, 3, 31), false, false),
        new("Oat Cereal", "Cereals", "Shelf", 12, 9, BestBy(2027, 5, 31), false, false),
        new("Potatoes", "Produce", "Back Room", 40, 32, BestBy(2026, 7, 22), false, true),
        new("Dozen Eggs", "Eggs", "Back Room", 10, 8, BestBy(2026, 7, 25), false, true),
        new("Ground Beef", "Frozen Meat", "Freezer", 15, 12, BestBy(2026, 10, 31), false, true),
        new("Frozen Mixed Vegetables", "Frozen Miscellaneous", "Freezer", 18, 20, BestBy(2027, 1, 31), false, false),
        new("Green Beans", "Canned Vegetables", "Shelf", 24, 20, BestBy(2028, 1, 31), true, false),
        new("Green Beans", "Canned Vegetables", "Back Room", 18, 14, BestBy(2028, 1, 31), false, false),
        new("Canned Peaches", "Canned Fruit", "Pantry Area", 22, 18, BestBy(2027, 12, 31), false, false),
        new("Chicken Noodle Soup", "Soup is a MESS", "Shelf", 25, 20, BestBy(2027, 11, 30), false, true),
        new("Black Beans", "Canned Beans", "Pantry Area", 28, 24, BestBy(2028, 2, 29), true, false),
        new("Tomato Sauce", "Tomatoes", "Pantry Area", 36, 30, BestBy(2028, 3, 31), true, true),
        new("Canned Tuna", "Canned Meat", "Shelf", 24, 20, BestBy(2028, 1, 31), false, true),
        new("Size 4 Diapers", "Diapers", "Back Room", 6, 5, null, false, false),
        new("Baby Wipes", "Wipes", "Back Room", 9, 7, null, false, false),
        new("Infant Formula", "Formula", "Pantry Area", 4, 6, BestBy(2027, 1, 31), false, false),
    ];

    private static DateTime BestBy(int year, int month, int day)
    {
        return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
    }

    public sealed record InventorySeedEntry(
        string ItemName,
        string CategoryName,
        string LocationName,
        int CurrentQuantity,
        int PreviousQuantity,
        DateTime? BestByDate,
        bool IsCommodity,
        bool IsMenuItem);
}
