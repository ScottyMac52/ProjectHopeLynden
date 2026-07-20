namespace ProjectHopeLynden.Web.Tests.Pages.Inventory;

public sealed class LocationPageMarkupTests
{
    [Fact]
    public void MaintainPage_ProvidesInlineLocationCreationWithoutDiscardingForm()
    {
        var markup = ReadAsset("InventoryMaintain.cshtml");

        Assert.Contains("asp-page-handler=\"CreateLocation\"", markup);
        Assert.Contains("asp-for=\"NewLocationName\"", markup);
        Assert.Contains("asp-for=\"LocationId\"", markup);
        Assert.Contains("View all locations in a new tab", markup);
    }

    [Fact]
    public void ManagePage_LinksToLocationManagement()
    {
        var markup = ReadAsset("InventoryManage.cshtml");

        Assert.Contains("asp-page=\"/Inventory/Locations\"", markup);
        Assert.Contains("Manage locations", markup);
    }

    [Fact]
    public void LocationsPage_ListsAndAddsOnly()
    {
        var markup = ReadAsset("InventoryLocations.cshtml");

        Assert.Contains("Current locations", markup);
        Assert.Contains("asp-page-handler=\"Create\"", markup);
        Assert.Contains("was added and is ready to use", markup);
        Assert.DoesNotContain("Delete location", markup);
        Assert.DoesNotContain("Rename location", markup);
    }

    private static string ReadAsset(string fileName) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestAssets", fileName));
}
