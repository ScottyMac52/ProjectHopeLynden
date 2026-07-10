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
    }
}
