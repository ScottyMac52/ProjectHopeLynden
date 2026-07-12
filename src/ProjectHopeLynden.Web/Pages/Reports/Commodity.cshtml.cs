using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjectHopeLynden.Application.Inventory;

namespace ProjectHopeLynden.Web.Pages.Reports;

public sealed class CommodityReportModel(IInventoryCommodityService inventoryCommodityService) : PageModel
{
    public string PageTitle { get; } = "Commodity Report";

    public string Summary { get; } = "Commodity-only inventory prepared for discovery and reporting to the Bellingham Food Bank.";

    public CommodityReportView Report { get; private set; } = new(DateTime.MinValue, []);

    public async Task OnGetAsync()
    {
        Report = await inventoryCommodityService.GetCommodityReportAsync(DateTime.UtcNow);
    }
}
