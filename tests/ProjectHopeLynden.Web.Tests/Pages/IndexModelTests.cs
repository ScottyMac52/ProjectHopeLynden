using ProjectHopeLynden.Web.Pages;

namespace ProjectHopeLynden.Web.Tests.Pages;

public sealed class IndexModelTests
{
    [Fact]
    public void PageTitle_ReturnsApplicationTitle()
    {
        var model = new IndexModel();

        Assert.Equal("Project Hope Inventory", model.PageTitle);
    }

    [Fact]
    public void Summary_ReturnsCompassionateProjectHopeSummary()
    {
        var model = new IndexModel();

        Assert.Equal(
            "Inventory support for serving Lynden families with dignity and compassion.",
            model.Summary);
    }

    [Fact]
    public void ImpactStatement_ReturnsFoodBankServiceContext()
    {
        var model = new IndexModel();

        Assert.Equal(
            "Simple tools for food distribution, Commodity reporting, and inventory history.",
            model.ImpactStatement);
    }

    [Fact]
    public void TaskCards_ReturnExpectedOperationalDestinations()
    {
        var model = new IndexModel();

        Assert.Collection(
            model.TaskCards,
            card => Assert.Equal(
                new HomeTaskCard(
                    "Daily counts",
                    "Inventory by Category",
                    "View and update current counts grouped by category in the familiar spreadsheet layout.",
                    "/Inventory/Index",
                    "Open spreadsheet",
                    true),
                card),
            card => Assert.Equal(
                new HomeTaskCard(
                    "Planning",
                    "Incoming Orders",
                    "Schedule expected inventory and see incoming quantities on the inventory spreadsheet.",
                    "/Orders/Index",
                    "View incoming orders"),
                card),
            card => Assert.Equal(
                new HomeTaskCard(
                    "Find food",
                    "Search Inventory",
                    "Find an item across categories and locations, including Commodity and non-Commodity records.",
                    "/Inventory/Search",
                    "Search inventory"),
                card),
            card => Assert.Equal(
                new HomeTaskCard(
                    "Maintenance",
                    "Manage Inventory",
                    "Select a category, update quantities, add categories, and open item editing or history.",
                    "/Inventory/Manage",
                    "Manage inventory"),
                card),
            card => Assert.Equal(
                new HomeTaskCard(
                    "Reporting",
                    "Commodity Report",
                    "Review and print the current Commodity-only inventory report for food-bank reporting.",
                    "/Reports/Commodity",
                    "Open Commodity report"),
                card),
            card => Assert.Equal(
                new HomeTaskCard(
                    "Planning",
                    "Inventory Trends",
                    "Review recorded count history by item or category and filter by Commodity status.",
                    "/Reports/Trends",
                    "View inventory trends"),
                card),
            card => Assert.Equal(
                new HomeTaskCard(
                    "Stewardship",
                    "Database Backup",
                    "Create a recoverable copy of the inventory database and its count history.",
                    "/Administration/Backup",
                    "Create database backup"),
                card));
    }

    [Fact]
    public void TaskCards_ProvideUniqueCompleteActions()
    {
        var model = new IndexModel();

        Assert.All(model.TaskCards, card =>
        {
            Assert.False(string.IsNullOrWhiteSpace(card.Kicker));
            Assert.False(string.IsNullOrWhiteSpace(card.Title));
            Assert.False(string.IsNullOrWhiteSpace(card.Description));
            Assert.False(string.IsNullOrWhiteSpace(card.Page));
            Assert.False(string.IsNullOrWhiteSpace(card.ActionText));
        });
        Assert.Equal(model.TaskCards.Count, model.TaskCards.Select(card => card.Title).Distinct().Count());
        Assert.Equal(model.TaskCards.Count, model.TaskCards.Select(card => card.Description).Distinct().Count());
        Assert.Equal(model.TaskCards.Count, model.TaskCards.Select(card => card.Page).Distinct().Count());
        Assert.Equal(model.TaskCards.Count, model.TaskCards.Select(card => card.ActionText).Distinct().Count());
        Assert.Single(model.TaskCards, card => card.IsFeatured);
    }

    [Fact]
    public void TaskCards_RemoveLegacyDuplicateCommodityAndHistoryContent()
    {
        var model = new IndexModel();
        var dashboardText = string.Join(
            ' ',
            model.TaskCards.SelectMany(card => new[] { card.Title, card.Description }));

        Assert.DoesNotContain("Commodity Care", dashboardText, StringComparison.Ordinal);
        Assert.DoesNotContain("Commodity Records", dashboardText, StringComparison.Ordinal);
        Assert.DoesNotContain("Inventory History", dashboardText, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "Historical inventory counts will support trend reporting over time.",
            dashboardText,
            StringComparison.Ordinal);
    }

    [Fact]
    public void OnGet_CompletesWithoutChangingPageContent()
    {
        var model = new IndexModel();

        model.OnGet();

        Assert.Equal("Project Hope Inventory", model.PageTitle);
        Assert.Equal(
            "Inventory support for serving Lynden families with dignity and compassion.",
            model.Summary);
        Assert.Equal(
            "Simple tools for food distribution, Commodity reporting, and inventory history.",
            model.ImpactStatement);
        Assert.Equal(7, model.TaskCards.Count);
    }
}
