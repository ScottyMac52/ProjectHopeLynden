using ProjectHopeLynden.Web.Pages;
using Xunit;

namespace ProjectHopeLynden.Web.Tests.Pages;

public sealed class IndexModelTests
{
    [Fact]
    public void PageTitle_ReturnsProjectHopeInventory()
    {
        var model = new IndexModel();

        Assert.Equal("Project Hope Inventory", model.PageTitle);
    }

    [Fact]
    public void Summary_ReturnsLocalInventoryDescription()
    {
        var model = new IndexModel();

        Assert.Equal("Local inventory management for Project Hope Food Bank of Lynden.", model.Summary);
    }

    [Fact]
    public void OnGet_CompletesWithoutChangingDisplayText()
    {
        var model = new IndexModel();

        model.OnGet();

        Assert.Equal("Project Hope Inventory", model.PageTitle);
        Assert.Contains("Project Hope Food Bank of Lynden", model.Summary);
    }
}
