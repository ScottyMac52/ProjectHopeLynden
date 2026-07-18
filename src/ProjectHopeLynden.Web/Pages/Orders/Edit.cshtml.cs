using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjectHopeLynden.Application.IncomingOrders;

namespace ProjectHopeLynden.Web.Pages.Orders;

public sealed class EditModel(IIncomingOrderService incomingOrderService) : PageModel
{
    public string PageTitle { get; } = "Edit Incoming Order";

    public IncomingOrderEditView? EditView { get; private set; }

    public bool OperationFailed { get; private set; }

    public string? OperationMessage { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public int IncomingOrderId { get; set; }

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

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var loaded = await LoadAsync(loadFormValues: true, cancellationToken);
        return loaded ? Page() : NotFound();
    }

    public async Task<IActionResult> OnPostSaveAsync(CancellationToken cancellationToken)
    {
        var result = await incomingOrderService.UpdateAsync(
            IncomingOrderId,
            new IncomingOrderSaveRequest(
                InventoryEntryId,
                Quantity,
                ExpectedDate,
                Source,
                Reference),
            DateTime.UtcNow,
            cancellationToken);

        if (!result.Succeeded)
        {
            OperationFailed = true;
            OperationMessage = result.ErrorMessage ?? "Incoming order could not be updated.";

            if (!await LoadAsync(loadFormValues: false, cancellationToken))
            {
                return NotFound();
            }

            return Page();
        }

        StatusMessage = "Incoming order updated.";
        return RedirectToPage("/Orders/Index");
    }

    private async Task<bool> LoadAsync(
        bool loadFormValues,
        CancellationToken cancellationToken)
    {
        EditView = await incomingOrderService.GetForEditAsync(IncomingOrderId, cancellationToken);
        if (EditView is null)
        {
            return false;
        }

        if (loadFormValues)
        {
            InventoryEntryId = EditView.Order.InventoryEntryId;
            Quantity = EditView.Order.Quantity;
            ExpectedDate = EditView.Order.ExpectedDate;
            Source = EditView.Order.Source;
            Reference = EditView.Order.Reference;
        }

        return true;
    }
}
