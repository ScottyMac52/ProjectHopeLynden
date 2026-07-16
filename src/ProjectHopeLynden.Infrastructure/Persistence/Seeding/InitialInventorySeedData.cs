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
        "WIC",
        "WIF",
        "Front",
        "Kitchen",
    ];

    public static readonly InventorySeedEntry[] InventoryEntries =
    [
        // Existing sample rows retained for Commodity distinction and baseline inventory coverage.
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

        // Condiments - issue #46 spreadsheet data.
        new("Jelly Misc.", "Condiments", "Back Room", 2, 4, null, false, false, At(2026, 6, 3)),
        new("Mayo Misc.", "Condiments", "Back Room", 1.5, null, null, false, false, At(2026, 6, 3)),
        new("Peanut Butter Hamilton Farms", "Condiments", "Back Room", 10, null, BestBy(2027, 3, 23), false, false, At(2026, 6, 24)),
        new("Peanut Butter Nut'n Better", "Condiments", "Back Room", 13, 18, BestBy(2027, 1, 1), false, false, At(2026, 6, 24)),
        new("Peanut Butter Nut'n Better", "Condiments", "Crypt", 1, null, BestBy(2027, 1, 1), false, false, At(2026, 6, 3)),
        new("Peanut Butter Peter Pan", "Condiments", "Crypt", 32, null, BestBy(2027, 3, 1), false, false, At(2026, 6, 10)),
        new("Vegetable Oil", "Condiments", "Pull Room", 10, null, BestBy(2027, 4, 14), false, false, At(2026, 6, 24)),
        new("Pickles Misc.", "Condiments", "Back Room", 1, null, null, false, false, At(2026, 6, 3)),

        // Snacks - issue #46 spreadsheet data.
        new("Assort. Snacks", "Snacks", "Back Room", 4, 0, null, false, true, At(2026, 6, 10)),
        new("Popcorn Misc.", "Snacks", "Back Room", 1, 3.5, null, false, false, At(2026, 6, 3)),
        new("Raisins", "Snacks", "FC", 5, null, null, true, false, At(2026, 6, 24)),

        // Produce - issue #47 spreadsheet data.
        new("Apples Pink Lady", "Produce", "WIC", 9, null, null, false, false, At(2026, 6, 24)),
        new("Tomato", "Produce", "WIC", 1, null, null, false, false, At(2026, 6, 24)),

        // Eggs - issue #47 spreadsheet data.
        new("Eggs", "Eggs", "WIC", 0, null, null, false, true, At(2026, 6, 10)),

        // Frozen Meat - issue #47 spreadsheet data.
        new("Chicken Drumstick", "Frozen Meat", "WIF", 10, null, null, false, false, At(2026, 6, 24)),
        new("Pork Ground", "Frozen Meat", "WIF", 32, null, null, false, false, At(2026, 6, 24)),

        // Frozen Miscellaneous - issue #47 spreadsheet data.
        new("Assorted", "Frozen Miscellaneous", "WIF", 3, null, null, false, false, At(2026, 6, 24)),
        new("Berries", "Frozen Miscellaneous", "WIF", 39, null, null, false, false, At(2026, 6, 24)),

        // Canned Vegetables - issue #48 spreadsheet data. The crossed-out Harvest Valley label is stored as North Pride.
        new("Carrots Michigan Made", "Canned Vegetables", "Back Room", 16, null, BestBy(2028, 12, 1), true, false, At(2026, 6, 24)),
        new("Corn Good Smart", "Canned Vegetables", "Crypt", 28, null, BestBy(2029, 4, 1), false, false, At(2026, 6, 24)),
        new("Corn Misc.", "Canned Vegetables", "Back Room", 27, null, null, false, false, At(2026, 6, 24)),
        new("Corn North Pride", "Canned Vegetables", "Back Room", 4, 12, BestBy(2028, 12, 1), false, true, At(2026, 6, 24)),
        new("Corn North Pride", "Canned Vegetables", "Crypt", 8, 82, BestBy(2028, 12, 1), false, false, At(2026, 6, 24)),
        new("Green Beans North Pride", "Canned Vegetables", "Back Room", 4, null, null, false, false, At(2026, 6, 24)),
        new("Green Beans Harvest Valley", "Canned Vegetables", "Crypt", 17, 27, null, false, false, At(2026, 6, 24)),
        new("Green Beans Misc.", "Canned Vegetables", "Back Room", 51, 55, null, false, false, At(2026, 6, 24)),
        new("Green Beans North Pride", "Canned Vegetables", "Crypt", 8, 32, BestBy(2028, 4, 1), false, true, At(2026, 6, 24)),
        new("Peas Duchess", "Canned Vegetables", "Crypt", 16, null, BestBy(2029, 3, 1), false, false, At(2026, 6, 24)),
        new("Peas North Pride", "Canned Vegetables", "Crypt", 28, null, BestBy(2029, 2, 1), false, false, At(2026, 6, 24)),
        new("Veg. Misc.", "Canned Vegetables", "Back Room", 65, null, null, false, false, At(2026, 6, 24)),

        // Canned Fruit - issue #48 spreadsheet data.
        new("Applesauce Mother's Maid", "Canned Fruit", "Back Room", 32, null, BestBy(2027, 12, 1), false, false, At(2026, 6, 24)),
        new("Applesauce Mother's Maid", "Canned Fruit", "Crypt", 56, null, BestBy(2027, 12, 1), false, false, At(2026, 6, 24)),
        new("Fruit Misc.", "Canned Fruit", "Back Room", 117, null, null, false, false, At(2026, 6, 24)),
        new("Mangos Diced Del Monte", "Canned Fruit", "Back Room", 42, null, null, false, false, At(2026, 6, 24)),
        new("Mixed Fruit Del Monte", "Canned Fruit", "Crypt", 170, null, BestBy(2026, 8, 1), false, false, At(2026, 6, 24)),
        new("Peaches (Large) Argo", "Canned Fruit", "Back Room", 98, null, null, false, false, At(2026, 6, 24)),
        new("Peaches, Sliced Del Monte", "Canned Fruit", "Back Room", 30, null, null, false, false, At(2026, 6, 24)),
        new("Pineapple", "Canned Fruit", "Back Room", 23, null, null, false, false, At(2026, 6, 24)),

        // Soup is a MESS - issue #48 spreadsheet data.
        new("Chicken & Rice Soup Tasty Kitchen", "Soup is a MESS", "Back Room", 7, 0, BestBy(2027, 3, 1), true, true, At(2026, 6, 24)),
        new("Chicken & Rice Soup Tasty Kitchen", "Soup is a MESS", "Crypt", 20, 30, BestBy(2027, 3, 1), true, true, At(2026, 6, 24)),
        new("Chicken Noodle Soup Tasty Kitchen", "Soup is a MESS", "Crypt", 40, null, BestBy(2027, 3, 1), true, false, At(2026, 6, 24)),
        new("Chili Misc.", "Soup is a MESS", "Back Room", 33, null, null, false, false, At(2026, 6, 24)),
        new("Cream of Bacon Campbell's", "Soup is a MESS", "Crypt", 120, 80, BestBy(2027, 4, 28), false, false, At(2026, 6, 24)),
        new("Cream of Mush. Tasty Kitchen", "Soup is a MESS", "Back Room", 88, null, BestBy(2024, 11, 1), false, false, At(2026, 6, 24)),
        new("Spaghetti Misc.", "Soup is a MESS", "Back Room", 33, null, null, false, false, At(2026, 6, 24)),
        new("Tomato Soup American Beauty", "Soup is a MESS", "Back Room", 2, null, BestBy(2027, 7, 1), true, true, At(2026, 6, 24)),
        new("Tomato Soup American Beauty", "Soup is a MESS", "Crypt", 16, null, BestBy(2027, 7, 1), true, true, At(2026, 6, 24)),

        // Canned Beans - issue #49 spreadsheet data.
        new("Baked Beans Finest Original", "Canned Beans", "Crypt", 28, null, BestBy(2029, 2, 1), false, false, At(2026, 6, 3)),
        new("Baked Beans Misc.", "Canned Beans", "Back Room", 19, null, null, false, false, At(2026, 6, 3)),
        new("Beans Misc.", "Canned Beans", "Back Room", 129, null, null, false, true, At(2026, 6, 24)),
        new("Black Beans, American Beauty", "Canned Beans", "Back Room", 24, null, null, true, false, At(2026, 6, 10)),
        new("Pinto Beans, American Beauty", "Canned Beans", "Back Room", 24, null, BestBy(2028, 3, 1), true, false, At(2026, 6, 10)),
        new("Refried Beans Misc.", "Canned Beans", "Back Room", 23, null, null, false, false, At(2026, 6, 3)),
        new("Stew, Beef Southgate", "Canned Beans", "Crypt", 90, null, BestBy(2028, 3, 1), false, false, At(2026, 6, 3)),
        new("Stew, Vegetable Southgate", "Canned Beans", "Crypt", 100, null, BestBy(2028, 1, 1), false, false, At(2026, 6, 3)),

        // Tomatoes - issue #49 spreadsheet data.
        new("Diced Tom. Misc.", "Tomatoes", "Back Room", 17, null, null, false, false, At(2026, 6, 24)),
        new("Diced Tom. Vine Ripe", "Tomatoes", "Back Room", 6, null, BestBy(2027, 8, 1), false, true, At(2026, 6, 24)),
        new("Diced Tom. Vine Ripe", "Tomatoes", "Crypt", 136, null, BestBy(2027, 8, 1), false, true, At(2026, 6, 16)),
        new("Pasta Sauce Misc.", "Tomatoes", "Back Room", 15, null, null, false, false, At(2026, 6, 24)),
        new("Pasta Sauce, Traditional Richfield Farms", "Tomatoes", "Back Room", 12, null, null, false, false, At(2026, 6, 24)),
        new("Tomato Sauce Misc.", "Tomatoes", "Back Room", 14, null, null, false, false, At(2026, 6, 24)),
        new("Tomato Sauce Vine Ripe", "Tomatoes", "Crypt", 174, 182, BestBy(2026, 2, 1), false, false, At(2026, 6, 24)),
        new("Tomato Sauce Vine Ripe", "Tomatoes", "Back Room", 10, null, BestBy(2026, 2, 1), false, false, At(2026, 6, 24)),

        // Canned Meat - issue #49 spreadsheet data. The handwritten Chicken Crider Back Room count is a separate location row.
        new("Chicken Crider", "Canned Meat", "Back Room", 7, null, BestBy(2028, 6, 1), false, false, At(2026, 6, 24)),
        new("Chicken Crider", "Canned Meat", "Crypt", 30, 55, BestBy(2028, 6, 1), false, false, At(2026, 6, 24)),
        new("Salmon Misc.", "Canned Meat", "Back Room", 5, null, null, false, false, At(2026, 6, 24)),
        new("Salmon Food Lifeline", "Canned Meat", "Back Room", 7, null, null, false, true, At(2026, 6, 24)),
        new("Spam", "Canned Meat", "Back Room", 48, null, null, false, false, At(2026, 6, 24)),
        new("Tuna Duchess", "Canned Meat", "Back Room", 5, null, BestBy(2027, 11, 1), false, true, At(2026, 6, 24)),
        new("Tuna Duchess", "Canned Meat", "Crypt", 56, 60, BestBy(2027, 11, 1), false, false, At(2026, 6, 10)),
        new("Tuna Misc.", "Canned Meat", "Back Room", 30, null, null, false, true, At(2026, 6, 24)),

        // Diapers - issue #50 spreadsheet data.
        new("Size 1", "Diapers", "Front", 22, 15, null, false, false, At(2026, 6, 24)),
        new("Size 1", "Diapers", "Kitchen", 16, 24, null, false, false, At(2026, 6, 24)),
        new("Size 2", "Diapers", "Front", 22, 16, null, false, false, At(2026, 6, 24)),
        new("Size 2", "Diapers", "Kitchen", 0, 8, null, false, false, At(2026, 6, 24)),
        new("Size 3", "Diapers", "Front", 23, 14, null, false, false, At(2026, 6, 24)),
        new("Size 3", "Diapers", "Kitchen", 72, 88, null, false, false, At(2026, 6, 24)),
        new("Size 3", "Diapers", "Crypt", 32, null, null, false, false, At(2026, 6, 24)),
        new("Size 4", "Diapers", "Front", 25, 24, null, false, false, At(2026, 6, 24)),
        new("Size 4", "Diapers", "Kitchen", 8, 16, null, false, false, At(2026, 6, 24)),
        new("Size 5", "Diapers", "Front", 28, 13, null, false, false, At(2026, 6, 24)),
        new("Size 5", "Diapers", "Kitchen", 12, 136, null, false, false, At(2026, 6, 24)),
        new("Size 5", "Diapers", "Crypt", 112, null, null, false, false, At(2026, 6, 24)),
        new("Size 6", "Diapers", "Front", 12, null, null, false, false, At(2026, 6, 24)),
        new("Size 6", "Diapers", "Kitchen", 24, 32, null, false, false, At(2026, 6, 24)),
        new("Size 6", "Diapers", "Crypt", 160, null, null, false, false, At(2026, 6, 24)),

        // Wipes - issue #50 spreadsheet data.
        new("Cuties", "Wipes", "Kitchen", 3, 1, null, false, false, At(2026, 6, 10)),
        new("Cuties", "Wipes", "Crypt", 61, null, null, false, false, At(2026, 6, 10)),

        // Formula - issue #50 spreadsheet data.
        new("Advanced", "Formula", "Front", 8, 7, null, false, false, At(2026, 6, 24)),
        new("Advanced", "Formula", "Kitchen", 72, 84, null, false, false, At(2026, 6, 24)),
        new("Sensitive", "Formula", "Front", 7, 5, null, false, false, At(2026, 6, 24)),
        new("Sensitive", "Formula", "Kitchen", 42, null, null, false, false, At(2026, 6, 24)),
        new("Soy", "Formula", "Front", 31, null, null, false, false, At(2026, 6, 24)),
        new("Soy", "Formula", "Kitchen", 30, null, null, false, false, At(2026, 6, 24)),
        new("Total Comfort", "Formula", "Front", 12, 13, null, false, false, At(2026, 6, 24)),
        new("Total Comfort", "Formula", "Kitchen", 12, null, null, false, false, At(2026, 6, 24)),
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
