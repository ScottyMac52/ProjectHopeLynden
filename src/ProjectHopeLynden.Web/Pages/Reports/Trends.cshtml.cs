using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjectHopeLynden.Application.Inventory;
using ProjectHopeLynden.Application.Reporting;
using ProjectHopeLynden.Web.Reporting;

namespace ProjectHopeLynden.Web.Pages.Reports;

public sealed class TrendsModel(
    IInventoryTrendReportService trendReportService,
    IInventoryQueryService inventoryQueryService,
    IReportPdfService? reportPdfService = null) : PageModel
{
    public string PageTitle { get; } = "Inventory Trends";

    public string Summary { get; } = "Compare end-of-day inventory snapshots with operational quantity changes by item, category, and Commodity status.";

    public IReadOnlyList<InventoryCategoryListItem> Categories { get; private set; } = [];

    public InventoryTrendReportView Report { get; private set; } = new(
        DateTime.MinValue,
        new InventoryTrendReportRequest(InventoryTrendGrouping.Item),
        [],
        []);

    [BindProperty(SupportsGet = true)]
    public string GroupBy { get; set; } = "item";

    [BindProperty(SupportsGet = true)]
    public string? ItemName { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string InventoryType { get; set; } = "all";

    public Task OnGetAsync() => LoadReportAsync();

    public async Task<IActionResult> OnGetPdfAsync()
    {
        await LoadReportAsync();
        var categoryName = Categories.SingleOrDefault(category => category.Id == CategoryId)?.Name;
        return new InlinePdfResult(RequirePdfService().CreateTrendsReport(Report, categoryName));
    }

    private async Task LoadReportAsync()
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

    private IReportPdfService RequirePdfService() =>
        reportPdfService ?? throw new InvalidOperationException("PDF reporting is not configured.");
}
