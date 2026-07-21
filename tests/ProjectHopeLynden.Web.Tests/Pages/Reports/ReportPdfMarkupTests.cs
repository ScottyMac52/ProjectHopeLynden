namespace ProjectHopeLynden.Web.Tests.Pages.Reports;

public sealed class ReportPdfMarkupTests
{
    [Theory]
    [InlineData("CommodityReport.cshtml")]
    [InlineData("TrendsReport.cshtml")]
    [InlineData("InventoryIndex.cshtml")]
    [InlineData("InventorySearch.cshtml")]
    [InlineData("InventoryHistory.cshtml")]
    [InlineData("IncomingOrdersIndex.cshtml")]
    public void ReportView_OpensPdfForReviewInNewTab(string fileName)
    {
        var markup = ReadAsset(fileName);

        Assert.Contains("asp-page-handler=\"Pdf\"", markup);
        Assert.Contains("target=\"_blank\"", markup);
        Assert.Contains("rel=\"noopener\"", markup);
        Assert.Contains("Generate PDF", markup);
    }

    [Fact]
    public void TrendsPdfLink_PreservesEveryActiveFilter()
    {
        var markup = ReadAsset("TrendsReport.cshtml");

        Assert.Contains("asp-route-groupBy=\"@Model.GroupBy\"", markup);
        Assert.Contains("asp-route-itemName=\"@Model.ItemName\"", markup);
        Assert.Contains("asp-route-categoryId=\"@Model.CategoryId\"", markup);
        Assert.Contains("asp-route-inventoryType=\"@Model.InventoryType\"", markup);
    }

    [Fact]
    public void SearchAndHistoryPdfLinks_PreserveTheirContext()
    {
        var search = ReadAsset("InventorySearch.cshtml");
        var history = ReadAsset("InventoryHistory.cshtml");

        Assert.Contains("asp-route-searchTerm=\"@Model.SearchTerm\"", search);
        Assert.Contains("asp-route-inventoryEntryId=\"@Model.InventoryEntryId\"", history);
    }

    private static string ReadAsset(string fileName) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestAssets", fileName));
}
