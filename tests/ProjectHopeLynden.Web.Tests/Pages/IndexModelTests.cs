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
    public void Summary_ReturnsProjectHopeSummary()
    {
        var model = new IndexModel();

        Assert.Equal("Local inventory management for Project Hope Food Bank of Lynden.", model.Summary);
    }

    [Fact]
    public void OnGet_CompletesWithoutChangingPageContent()
    {
        var model = new IndexModel();

        model.OnGet();

        Assert.Equal("Project Hope Inventory", model.PageTitle);
        Assert.Equal("Local inventory management for Project Hope Food Bank of Lynden.", model.Summary);
    }
}
