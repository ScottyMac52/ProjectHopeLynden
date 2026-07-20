namespace ProjectHopeLynden.Web.Tests.Pages.Inventory;

public sealed class InventoryEditLinkMarkupTests
{
    [Fact]
    public void ManagePage_UsesItemNamesAsEditLinksWithoutDetailsColumn()
    {
        var markup = ReadAsset("InventoryManage.cshtml");

        Assert.Contains("asp-route-returnUrl", markup);
        Assert.Contains("<span class=\"visually-hidden\"> (edit)</span>", markup);
        Assert.DoesNotContain("<th scope=\"col\">Details</th>", markup);
        Assert.DoesNotContain(">Edit details</a>", markup);
        Assert.Contains(">View history</a>", markup);
    }

    [Fact]
    public void SpreadsheetPage_UsesItemNamesAsEditLinksAndConsolidatesAddRowAction()
    {
        var markup = ReadAsset("InventoryIndex.cshtml");

        Assert.Contains("asp-route-returnUrl", markup);
        Assert.Contains("<span class=\"visually-hidden\"> (edit)</span>", markup);
        Assert.DoesNotContain("<th scope=\"col\">Edit</th>", markup);
        Assert.DoesNotContain(">Edit</a>", markup);
        Assert.DoesNotContain(">Add inventory row</a>", markup);
        Assert.Contains("<div class=\"inventory-toolbar__primary\">", markup);
        Assert.Contains(">Add Row</a>", markup);
        Assert.Contains(">Add row</a>", markup);
        Assert.Contains(">History</a>", markup);
        Assert.Contains("<th colspan=\"7\" scope=\"rowgroup\">", markup);
        Assert.Contains("<td colspan=\"8\">No inventory entries", markup);
        Assert.DoesNotContain("colspan=\"9\"", markup);
    }

    [Fact]
    public void SearchPage_UsesItemNamesAsEditLinksAndKeepsHistoryAction()
    {
        var markup = ReadAsset("InventorySearch.cshtml");

        Assert.Contains("asp-route-returnUrl", markup);
        Assert.Contains("<span class=\"visually-hidden\"> (edit)</span>", markup);
        Assert.DoesNotContain(">Edit</a>", markup);
        Assert.Contains(">History</a>", markup);
    }

    [Fact]
    public void MaintainPage_PreservesReturnUrlAndUsesItForCancel()
    {
        var markup = ReadAsset("InventoryMaintain.cshtml");

        Assert.Contains("<input type=\"hidden\" asp-for=\"ReturnUrl\" />", markup);
        Assert.Contains("href=\"@Model.CancelUrl\">Cancel</a>", markup);
    }

    private static string ReadAsset(string fileName) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestAssets", fileName));
}
