using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ProjectHopeLynden.Web.Pages;

public sealed class IndexModel : PageModel
{
    public string PageTitle { get; } = "Project Hope Inventory";

    public string Summary { get; } = "Inventory support for serving Lynden families with dignity and compassion.";

    public string ImpactStatement { get; } = "Simple tools for food distribution, Commodity reporting, and inventory history.";

    public IReadOnlyList<HomeTaskCard> TaskCards { get; } =
    [
        new(
            "Daily counts",
            "Inventory by Category",
            "View and update current counts grouped by category in the familiar spreadsheet layout.",
            "/Inventory/Index",
            "Open spreadsheet",
            true),
        new(
            "Planning",
            "Incoming Orders",
            "Schedule expected inventory and see incoming quantities on the inventory spreadsheet.",
            "/Orders/Index",
            "View incoming orders"),
        new(
            "Find food",
            "Search Inventory",
            "Find an item across categories and locations, including Commodity and non-Commodity records.",
            "/Inventory/Search",
            "Search inventory"),
        new(
            "Maintenance",
            "Manage Inventory",
            "Select a category, update quantities, add categories, and open item editing or history.",
            "/Inventory/Manage",
            "Manage inventory"),
        new(
            "Reporting",
            "Commodity Report",
            "Review and print the current Commodity-only inventory report for food-bank reporting.",
            "/Reports/Commodity",
            "Open Commodity report"),
        new(
            "Planning",
            "Inventory Trends",
            "Review recorded count history by item or category and filter by Commodity status.",
            "/Reports/Trends",
            "View inventory trends"),
        new(
            "Stewardship",
            "Database Backup",
            "Create a recoverable copy of the inventory database and its count history.",
            "/Administration/Backup",
            "Create database backup"),
    ];

    public void OnGet()
    {
    }
}

public sealed record HomeTaskCard(
    string Kicker,
    string Title,
    string Description,
    string Page,
    string ActionText,
    bool IsFeatured = false);
