using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using ProjectHopeLynden.Application.IncomingOrders;
using ProjectHopeLynden.Web.Features;

namespace ProjectHopeLynden.Web.Pages.IncomingOrders;

public sealed class IndexModel(
    IIncomingOrderService incomingOrderService,
    IOptions<ProjectHopeFeatureOptions> featureOptions) : PageModel
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
}
