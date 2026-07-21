using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjectHopeLynden.Application.Inventory;
using ProjectHopeLynden.Application.Reporting;
using ProjectHopeLynden.Web.Reporting;

namespace ProjectHopeLynden.Web.Pages.Reports;

public sealed class CommodityReportModel(
    IInventoryCommodityService inventoryCommodityService,
    IReportPdfService? reportPdfService = null) : PageModel
{
    public string PageTitle { get; } = "Commodity Report";

    public string Summary { get; } = "Commodity-only inventory prepared for discovery and reporting to the Bellingham Food Bank.";

    public CommodityReportView Report { get; private set; } = new(DateTime.MinValue, []);

    public async Task OnGetAsync()
    {
        Report = await inventoryCommodityService.GetCommodityReportAsync(DateTime.UtcNow);
    }

    public async Task<IActionResult> OnGetPdfAsync()
    {
        var report = await inventoryCommodityService.GetCommodityReportAsync(DateTime.UtcNow);
        return new InlinePdfResult(RequirePdfService().CreateCommodityReport(report));
    }

    private IReportPdfService RequirePdfService() =>
        reportPdfService ?? throw new InvalidOperationException("PDF reporting is not configured.");
}
