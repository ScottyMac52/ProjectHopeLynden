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
        "Pull Room",
        "FC",
        "Crypt",
    ];

    public static readonly InventorySeedEntry[] InventoryEntries =
    [
        // Existing synthetic rows retained for Commodity and history workflow coverage.
        new("Green Beans", "Canned Vegetables", "Shelf", 24, 20, null, true, false, At(2026, 7, 8)),
        new("Green Beans", "Canned Vegetables", "Back Room", 18, 14, null, false, false, At(2026, 7, 8)),
        new("Tomato Sauce", "Tomatoes", "Pantry Area", 36, 30, null, true, true, At(2026, 7, 8)),
        new("Oat Cereal", "Cereals", "Shelf", 12, 9, null, false, false, At(2026, 7, 8)),

        // Dry Beans - issue #44 spreadsheet data.
        new("Black American Premium", "Dry Beans", "Back Room", 6, null, null, true, false, At(2026, 6, 10)),
        new("Black, Soranco", "Dry Beans", "Back Room", 2, 5, null, true, false, At(2026, 6, 24)),
        new("Garbanzo", "Dry Beans", "Back Room", 7, null, null, false, true, At(2026, 6, 24)),
        new("Great Northern", "Dry Beans", "Back Room", 1, 6, null, false, false, At(2026, 6, 24)),
        new("Great Northern", "Dry Beans", "Back Room", 18, 21, null, true, true, At(2026, 6, 10)),
        new("Navy Beans", "Dry Beans", "Back Room", 10, null, null, false, false, At(2026, 6, 24)),
        new("Pinto", "Dry Beans", "Back Room", 0, 3, null, true, true, At(2026, 6, 10)),
        new("Pinto", "Dry Beans", "Back Room", 1, 4, null, false, false, At(2026, 6, 10)),
        new("Pinto, Jack's Bean Co", "Dry Beans", "Back Room", 10, null, null, true, false, At(2026, 6, 10)),
        new("Red Kidney, Dark Morrison Farms", "Dry Beans", "Pull Room", 8, 11, null, true, false, At(2026, 6, 10)),
        new("Red Small", "Dry Beans", "Back Room", 1, 4, null, true, false, At(2026, 6, 10)),

        // Noodles - issue #44 spreadsheet data.
        new("Mac & Cheese", "Noodles", "Back Room", 39, null, null, false, false, At(2026, 6, 24)),
        new("Mac & Cheese Kraft", "Noodles", "Back Room", 42, null, BestBy(2026, 4, 1), false, true, At(2026, 6, 24)),
        new("Mac & Cheese Premium Pantry", "Noodles", "Back Room", 13, null, null, true, false, At(2026, 6, 10)),
        new("Penne Chickpea Barilla", "Noodles", "FC", 17, null, null, false, false, At(2026, 6, 10)),
        new("Penne Rigate Dreamfields", "Noodles", "Crypt", 53, null, BestBy(2027, 3, 1), false, false, At(2026, 6, 10)),
        new("Potato Flakes Jack & the Beanstalk", "Noodles", "Crypt", 20, null, BestBy(2028, 10, 1), false, false, At(2026, 6, 10)),
        new("Ramen Misc.", "Noodles", "Back Room", 0, null, null, false, true, At(2026, 6, 10)),
        new("Rice Meal", "Noodles", "Back Room", 4, null, null, false, false, At(2026, 6, 10)),
        new("Rice Misc.", "Noodles", "Back Room", 2, null, null, false, false, At(2026, 6, 10)),
        new("White Rice Long Grain Flickertail, IB", "Noodles", "Back Room", 23, null, null, false, false, At(2026, 6, 10)),
        new("White Rice Long Grain Flickertail, UP", "Noodles", "Back Room", 9, null, null, false, false, At(2026, 6, 24)),

        // Dry Mix - issue #44 spreadsheet data. Holiday rows are menu items until that label is modeled separately.
        new("Cake Mix Misc.", "Dry Mix", "Back Room", 1.5, null, null, false, false, At(2026, 6, 3)),
        new("Cookie Mix Everything Trader Joe's", "Dry Mix", "Back Room", 4, null, BestBy(2026, 3, 1), false, false, At(2026, 6, 3)),
        new("Cookie Mix Misc.", "Dry Mix", "Back Room", 3, null, null, false, false, At(2026, 6, 3)),
        new("Cornbread Mix Jiffy", "Dry Mix", "FC", 20, 24, null, false, true, At(2026, 6, 24)),
        new("Frosting", "Dry Mix", "Back Room", 1, null, null, false, false, At(2026, 6, 3)),
        new("Masa Flour El Maizal", "Dry Mix", "Pull Room", 0.5, null, null, false, false, At(2026, 6, 24)),
        new("Potatoes Instant Misc.", "Dry Mix", "Back Room", 1.5, null, null, false, false, At(2026, 6, 3)),
        new("Stuffing Misc.", "Dry Mix", "Crypt", 7, null, null, false, true, At(2026, 6, 24)),
        new("Stuffing Pepperidge Farm", "Dry Mix", "Crypt", 22, null, null, false, true, At(2026, 6, 24)),
    ];

    private static DateTime At(int year, int month, int day)
    {
        return new DateTime(year, month, day, 9, 0, 0, DateTimeKind.Utc);
    }

    private static DateTime BestBy(int year, int month, int day)
    {
        return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
    }

    public sealed record InventorySeedEntry(
        string ItemName,
        string CategoryName,
        string LocationName,
        double CurrentQuantity,
        double? PreviousQuantity,
        DateTime? BestByDate,
        bool IsCommodity,
        bool IsMenuItem,
        DateTime LastUpdatedAtUtc);
}
