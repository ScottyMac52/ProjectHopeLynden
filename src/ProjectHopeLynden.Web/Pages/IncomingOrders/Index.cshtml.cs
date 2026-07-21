using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using ProjectHopeLynden.Application.IncomingOrders;
using ProjectHopeLynden.Application.Reporting;
using ProjectHopeLynden.Web.Features;
using ProjectHopeLynden.Web.Reporting;

namespace ProjectHopeLynden.Web.Pages.IncomingOrders;

public sealed class IndexModel(
    IIncomingOrderService incomingOrderService,
    IOptions<ProjectHopeFeatureOptions> featureOptions,
    IReportPdfService? reportPdfService = null) : PageModel
{
    public IReadOnlyList<IncomingOrderListItem> Orders { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        if (!featureOptions.Value.IncomingOrders)
        {
            return NotFound();
        }

        Orders = await incomingOrderService.GetOrdersAsync(DateTime.Today);
        return Page();
    }

    public async Task<IActionResult> OnGetPdfAsync()
    {
        if (!featureOptions.Value.IncomingOrders)
        {
            return NotFound();
        }

        var operatingDate = DateTime.Today;
        var orders = await incomingOrderService.GetOrdersAsync(operatingDate);
        return new InlinePdfResult(RequirePdfService().CreateIncomingOrders(orders, operatingDate, DateTime.UtcNow));
    }

    private IReportPdfService RequirePdfService() =>
        reportPdfService ?? throw new InvalidOperationException("PDF reporting is not configured.");
}
