using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjectHopeLynden.Application.Inventory;

namespace ProjectHopeLynden.Web.Pages.Reports;

public sealed class TrendsModel(
    IInventoryTrendReportService trendReportService,
    IInventoryQueryService inventoryQueryService) : PageModel
{
    public string PageTitle { get; } = "Inventory Trends";

    public string Summary { get; } = "Review historical inventory counts by item or category and filter Commodity status.";

    public IReadOnlyList<InventoryCategoryListItem> Categories { get; private set; } = [];

    public InventoryTrendReportView Report { get; private set; } = new(
        DateTime.MinValue,
        new InventoryTrendReportRequest(InventoryTrendGrouping.Item),
        []);

    [BindProperty(SupportsGet = true)]
    public string GroupBy { get; set; } = "item";

    [BindProperty(SupportsGet = true)]
    public string? ItemName { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string InventoryType { get; set; } = "all";

    public async Task OnGetAsync()
    {
        Categories = await inventoryQueryService.GetCategoriesAsync();

        var grouping = string.Equals(GroupBy, "category", StringComparison.OrdinalIgnoreCase)
            ? InventoryTrendGrouping.Category
            : InventoryTrendGrouping.Item;

        bool? isCommodity = InventoryType switch
        {
            "commodity" => true,
            "noncommodity" => false,
            _ => null,
        };

        var request = new InventoryTrendReportRequest(
            grouping,
            ItemName,
            CategoryId,
            isCommodity);

        Report = await trendReportService.GetTrendReportAsync(request, DateTime.UtcNow);
    }
}
