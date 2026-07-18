using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjectHopeLynden.Application.IncomingOrders;

namespace ProjectHopeLynden.Web.Pages.Orders;

public sealed class IndexModel(IIncomingOrderService incomingOrderService) : PageModel
{
    public string PageTitle { get; } = "Incoming Orders";

    public string Summary { get; } = "Track food expected to arrive and automatically add scheduled quantities to inventory on their expected date.";

    public IncomingOrdersView Orders { get; private set; } = new([], [], []);

    public bool OperationFailed { get; private set; }

    public string? OperationMessage { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty]
    public int? InventoryEntryId { get; set; }

    [BindProperty]
    public double? Quantity { get; set; }

    [BindProperty]
    public DateOnly? ExpectedDate { get; set; }

    [BindProperty]
    public string? Source { get; set; }

    [BindProperty]
    public string? Reference { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        ExpectedDate ??= DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken)
    {
        var result = await incomingOrderService.CreateAsync(
            BuildRequest(),
            DateTime.UtcNow,
            cancellationToken);

        if (!result.Succeeded)
        {
            return await ReloadWithErrorAsync(
                result.ErrorMessage ?? "Incoming order could not be scheduled.",
                cancellationToken);
        }

        StatusMessage = "Incoming order scheduled.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCancelAsync(
        int incomingOrderId,
        CancellationToken cancellationToken)
    {
        var result = await incomingOrderService.CancelAsync(
            incomingOrderId,
            DateTime.UtcNow,
            cancellationToken);

        if (!result.Succeeded)
        {
            return await ReloadWithErrorAsync(
                result.ErrorMessage ?? "Incoming order could not be cancelled.",
                cancellationToken);
        }

        StatusMessage = "Incoming order cancelled.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostReceiveAsync(
        int incomingOrderId,
        CancellationToken cancellationToken)
    {
        var result = await incomingOrderService.ReceiveAsync(
            incomingOrderId,
            DateTime.UtcNow,
            cancellationToken);

        if (!result.Succeeded)
        {
            return await ReloadWithErrorAsync(
                result.ErrorMessage ?? "Incoming order could not be received.",
                cancellationToken);
        }

        StatusMessage = "Incoming order received and added to inventory.";
        return RedirectToPage();
    }

    private IncomingOrderSaveRequest BuildRequest()
    {
        return new IncomingOrderSaveRequest(
            InventoryEntryId,
            Quantity,
            ExpectedDate,
            Source,
            Reference);
    }

    private async Task<PageResult> ReloadWithErrorAsync(
        string message,
        CancellationToken cancellationToken)
    {
        OperationFailed = true;
        OperationMessage = message;
        await LoadAsync(cancellationToken);
        return Page();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Orders = await incomingOrderService.GetOrdersAsync(cancellationToken);
    }
}
