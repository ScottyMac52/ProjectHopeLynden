using ProjectHopeLynden.Web.Pages;

namespace ProjectHopeLynden.Web.Tests;

public sealed class IndexPageTests
{
    [Fact]
    public void IndexPage_ExposesProjectTitle()
    {
        var model = new IndexModel();

        Assert.Equal("Project Hope Inventory", model.PageTitle);
    }

    [Fact]
    public void IndexPage_ExposesProjectSummary()
    {
        var model = new IndexModel();

        Assert.Contains("Project Hope Food Bank of Lynden", model.Summary);
    }
}
