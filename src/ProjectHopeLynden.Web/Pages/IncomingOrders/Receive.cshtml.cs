using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using ProjectHopeLynden.Application.IncomingOrders;
using ProjectHopeLynden.Domain.IncomingOrders;
using ProjectHopeLynden.Web.Features;

namespace ProjectHopeLynden.Web.Pages.IncomingOrders;

public sealed class ReceiveModel(
    IIncomingOrderService incomingOrderService,
    IOptions<ProjectHopeFeatureOptions> featureOptions) : PageModel
{
    public IncomingOrderEditView? Order { get; private set; }

    public bool ReceiveFailed { get; private set; }

    public string? ReceiveMessage { get; private set; }

    [BindProperty(SupportsGet = true)]
    public int OrderId { get; set; }

    [BindProperty]
    public List<ReceiptLineInput> Lines { get; set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        if (!featureOptions.Value.IncomingOrders)
        {
            return NotFound();
        }

        Order = await incomingOrderService.GetOrderAsync(OrderId);
        if (Order is null || Order.Status is IncomingOrderStatus.Received or IncomingOrderStatus.Cancelled)
        {
            return NotFound();
        }

        Lines = Order.Lines.Select(line => new ReceiptLineInput
        {
            LineId = line.Id,
            InventoryEntryName = line.InventoryEntryName,
            ExpectedQuantity = line.ExpectedQuantity,
            ReceivedQuantity = line.ExpectedQuantity,
        }).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!featureOptions.Value.IncomingOrders)
        {
            return NotFound();
        }

        var result = await incomingOrderService.ReceiveOrderAsync(
            OrderId,
            Lines.Select(line => new IncomingOrderReceiptLineRequest(line.LineId, line.ReceivedQuantity)).ToArray(),
            DateTime.UtcNow);

        if (result.Succeeded)
        {
            return RedirectToPage("/IncomingOrders/Index");
        }

        ReceiveFailed = true;
        ReceiveMessage = result.ErrorMessage ?? "The order could not be received.";
        Order = await incomingOrderService.GetOrderAsync(OrderId);
        return Order is null ? NotFound() : Page();
    }

    public sealed class ReceiptLineInput
    {
        public int LineId { get; set; }

        public string InventoryEntryName { get; set; } = string.Empty;

        public double ExpectedQuantity { get; set; }

        public double? ReceivedQuantity { get; set; }
    }
}
