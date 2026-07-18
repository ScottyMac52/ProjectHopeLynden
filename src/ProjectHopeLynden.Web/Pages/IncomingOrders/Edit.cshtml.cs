using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using ProjectHopeLynden.Application.IncomingOrders;
using ProjectHopeLynden.Domain.IncomingOrders;
using ProjectHopeLynden.Web.Features;

namespace ProjectHopeLynden.Web.Pages.IncomingOrders;

public sealed class EditModel(
    IIncomingOrderService incomingOrderService,
    IOptions<ProjectHopeFeatureOptions> featureOptions) : PageModel
{
    public IReadOnlyList<IncomingOrderInventoryOption> InventoryOptions { get; private set; } = [];

    public bool SaveFailed { get; private set; }

    public string? SaveMessage { get; private set; }

    public bool OrderNotFound { get; private set; }

    public bool IsEditing => OrderId.HasValue;

    [BindProperty(SupportsGet = true)]
    public int? OrderId { get; set; }

    [BindProperty]
    public DateTime? OrderDate { get; set; }

    [BindProperty]
    public string? Vendor { get; set; }

    [BindProperty]
    public IncomingOrderStatus Status { get; set; }

    [BindProperty]
    public DateTime? InvoiceDate { get; set; }

    [BindProperty]
    public string? InvoiceNumber { get; set; }

    [BindProperty]
    public double? InvoiceAmount { get; set; }

    [BindProperty]
    public DateTime? DueDate { get; set; }

    [BindProperty]
    public string? SentToPayer { get; set; }

    [BindProperty]
    public string? ChargeTo { get; set; }

    [BindProperty]
    public DateTime? ExpectedDate { get; set; }

    [BindProperty]
    public double? Weight { get; set; }

    [BindProperty]
    public string? ProductSummary { get; set; }

    [BindProperty]
    public string? Notes { get; set; }

    [BindProperty]
    public List<IncomingOrderLineInput> Lines { get; set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        if (!featureOptions.Value.IncomingOrders)
        {
            return NotFound();
        }

        InventoryOptions = await incomingOrderService.GetInventoryOptionsAsync();
        if (OrderId is null)
        {
            OrderDate = DateTime.Today;
            ExpectedDate = DateTime.Today;
            Status = IncomingOrderStatus.Pending;
            Lines = [new IncomingOrderLineInput()];
            return Page();
        }

        var order = await incomingOrderService.GetOrderAsync(OrderId.Value);
        if (order is null || order.Status == IncomingOrderStatus.Received)
        {
            OrderNotFound = true;
            return Page();
        }

        Apply(order);
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!featureOptions.Value.IncomingOrders)
        {
            return NotFound();
        }

        var request = new IncomingOrderSaveRequest(
            OrderDate,
            Vendor,
            Status,
            InvoiceDate,
            InvoiceNumber,
            InvoiceAmount,
            DueDate,
            SentToPayer,
            ChargeTo,
            ExpectedDate,
            Weight,
            ProductSummary,
            Notes,
            Lines.Select(line => new IncomingOrderLineRequest(line.Id, line.InventoryEntryId, line.ExpectedQuantity)).ToArray());

        IncomingOrderSaveResult result;
        try
        {
            result = await incomingOrderService.SaveOrderAsync(OrderId, request, DateTime.UtcNow);
        }
        catch (InvalidOperationException)
        {
            OrderNotFound = true;
            InventoryOptions = await incomingOrderService.GetInventoryOptionsAsync();
            return Page();
        }

        if (!result.Succeeded)
        {
            SaveFailed = true;
            SaveMessage = result.ErrorMessage ?? "Incoming order could not be saved.";
            InventoryOptions = await incomingOrderService.GetInventoryOptionsAsync();
            if (Lines.Count == 0)
            {
                Lines.Add(new IncomingOrderLineInput());
            }

            return Page();
        }

        return RedirectToPage("/IncomingOrders/Index");
    }

    private void Apply(IncomingOrderEditView order)
    {
        OrderDate = order.OrderDate;
        Vendor = order.Vendor;
        Status = order.Status;
        InvoiceDate = order.InvoiceDate;
        InvoiceNumber = order.InvoiceNumber;
        InvoiceAmount = order.InvoiceAmount;
        DueDate = order.DueDate;
        SentToPayer = order.SentToPayer;
        ChargeTo = order.ChargeTo;
        ExpectedDate = order.ExpectedDate;
        Weight = order.Weight;
        ProductSummary = order.ProductSummary;
        Notes = order.Notes;
        Lines = order.Lines.Select(line => new IncomingOrderLineInput
        {
            Id = line.Id,
            InventoryEntryId = line.InventoryEntryId,
            ExpectedQuantity = line.ExpectedQuantity,
        }).ToList();
    }

    public sealed class IncomingOrderLineInput
    {
        public int? Id { get; set; }

        public int? InventoryEntryId { get; set; }

        public double? ExpectedQuantity { get; set; }
    }
}
