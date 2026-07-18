using Microsoft.EntityFrameworkCore;
using ProjectHopeLynden.Application.IncomingOrders;
using ProjectHopeLynden.Domain.IncomingOrders;
using ProjectHopeLynden.Domain.Inventory;
using ProjectHopeLynden.Infrastructure.Persistence;

namespace ProjectHopeLynden.Infrastructure.IncomingOrders;

public sealed class IncomingOrderService(ProjectHopeDbContext context) : IIncomingOrderService
{
    public async Task<IReadOnlyList<IncomingOrderListItem>> GetOrdersAsync(
        DateTime operatingDate,
        CancellationToken cancellationToken = default)
    {
        var date = operatingDate.Date;
        var orders = await context.IncomingOrders
            .AsNoTracking()
            .Include(order => order.Lines)
            .OrderBy(order => order.Status == IncomingOrderStatus.Received || order.Status == IncomingOrderStatus.Cancelled)
            .ThenBy(order => order.ExpectedDate)
            .ThenBy(order => order.Vendor)
            .ToListAsync(cancellationToken);

        return orders.Select(order => new IncomingOrderListItem(
                order.Id,
                order.OrderDate,
                order.Vendor,
                order.Status,
                order.ExpectedDate,
                GetDateState(order, date),
                order.ProductSummary,
                order.Lines.Count))
            .ToArray();
    }

    public async Task<IReadOnlyList<IncomingOrderInventoryOption>> GetInventoryOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var entries = await context.InventoryEntries
            .AsNoTracking()
            .Include(entry => entry.Category)
            .Include(entry => entry.Item)
            .Include(entry => entry.Location)
            .OrderBy(entry => entry.Category.Name)
            .ThenBy(entry => entry.Item.Name)
            .ThenBy(entry => entry.Location.Name)
            .ToListAsync(cancellationToken);

        return entries.Select(entry => new IncomingOrderInventoryOption(
                entry.Id,
                $"{entry.Category.Name} — {entry.Item.Name} — {entry.Location.Name}{(entry.IsCommodity ? " (Commodity)" : string.Empty)}"))
            .ToArray();
    }

    public async Task<IncomingOrderEditView?> GetOrderAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await context.IncomingOrders
            .AsNoTracking()
            .Include(incomingOrder => incomingOrder.Lines)
                .ThenInclude(line => line.InventoryEntry)
                    .ThenInclude(entry => entry.Category)
            .Include(incomingOrder => incomingOrder.Lines)
                .ThenInclude(line => line.InventoryEntry)
                    .ThenInclude(entry => entry.Item)
            .Include(incomingOrder => incomingOrder.Lines)
                .ThenInclude(line => line.InventoryEntry)
                    .ThenInclude(entry => entry.Location)
            .SingleOrDefaultAsync(incomingOrder => incomingOrder.Id == orderId, cancellationToken);

        return order is null ? null : ToEditView(order);
    }

    public async Task<IncomingOrderSaveResult> SaveOrderAsync(
        int? orderId,
        IncomingOrderSaveRequest request,
        DateTime createdAtUtc,
        CancellationToken cancellationToken = default)
    {
        var validationMessage = await ValidateSaveRequestAsync(request, cancellationToken);
        if (validationMessage is not null)
        {
            return new IncomingOrderSaveResult(false, validationMessage, orderId);
        }

        IncomingOrder order;
        if (orderId is null)
        {
            order = new IncomingOrder { CreatedAtUtc = createdAtUtc };
            context.IncomingOrders.Add(order);
        }
        else
        {
            order = await context.IncomingOrders
                .Include(incomingOrder => incomingOrder.Lines)
                .SingleOrDefaultAsync(incomingOrder => incomingOrder.Id == orderId.Value, cancellationToken)
                ?? throw new InvalidOperationException("Incoming order was not found.");

            if (order.Status == IncomingOrderStatus.Received)
            {
                return new IncomingOrderSaveResult(false, "A received order cannot be edited.", order.Id);
            }

            context.IncomingOrderLines.RemoveRange(order.Lines);
            order.Lines.Clear();
        }

        ApplyRequest(order, request);
        await context.SaveChangesAsync(cancellationToken);

        return new IncomingOrderSaveResult(true, null, order.Id);
    }

    public async Task<IncomingOrderReceiptResult> ReceiveOrderAsync(
        int orderId,
        IReadOnlyList<IncomingOrderReceiptLineRequest> lines,
        DateTime receivedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var order = await context.IncomingOrders
            .Include(incomingOrder => incomingOrder.Lines)
                .ThenInclude(line => line.InventoryEntry)
                    .ThenInclude(entry => entry.Item)
            .Include(incomingOrder => incomingOrder.Lines)
                .ThenInclude(line => line.InventoryEntry)
                    .ThenInclude(entry => entry.Category)
            .Include(incomingOrder => incomingOrder.Lines)
                .ThenInclude(line => line.InventoryEntry)
                    .ThenInclude(entry => entry.Location)
            .SingleOrDefaultAsync(incomingOrder => incomingOrder.Id == orderId, cancellationToken);

        if (order is null)
        {
            return new IncomingOrderReceiptResult(false, "Incoming order was not found.");
        }

        if (order.Status == IncomingOrderStatus.Received)
        {
            return new IncomingOrderReceiptResult(false, "This order has already been received.");
        }

        if (order.Status == IncomingOrderStatus.Cancelled)
        {
            return new IncomingOrderReceiptResult(false, "A cancelled order cannot be received.");
        }

        if (lines.Count != order.Lines.Count || lines.Select(line => line.LineId).Distinct().Count() != lines.Count)
        {
            return new IncomingOrderReceiptResult(false, "A received quantity is required for every order line.");
        }

        var receiptByLineId = lines.ToDictionary(line => line.LineId);
        if (order.Lines.Any(line =>
                !receiptByLineId.TryGetValue(line.Id, out var receipt) ||
                receipt.ReceivedQuantity is null or <= 0))
        {
            return new IncomingOrderReceiptResult(false, "Every received quantity must be greater than zero.");
        }

        foreach (var line in order.Lines)
        {
            var receivedQuantity = receiptByLineId[line.Id].ReceivedQuantity!.Value;
            var entry = line.InventoryEntry;
            var previousQuantity = entry.CurrentQuantity;
            var newQuantity = previousQuantity + receivedQuantity;

            entry.CurrentQuantity = newQuantity;
            entry.LastUpdatedAtUtc = receivedAtUtc;
            line.ReceivedQuantity = receivedQuantity;

            context.InventoryCountHistory.Add(new InventoryCountHistory
            {
                InventoryEntryId = entry.Id,
                CountedQuantity = newQuantity,
                CountedAtUtc = receivedAtUtc,
                PreviousQuantity = previousQuantity,
                QuantityChange = receivedQuantity,
                ItemIdAtCount = entry.ItemId,
                ItemNameAtCount = entry.Item.Name,
                CategoryIdAtCount = entry.CategoryId,
                CategoryNameAtCount = entry.Category.Name,
                LocationIdAtCount = entry.LocationId,
                LocationNameAtCount = entry.Location.Name,
                IsCommodityAtCount = entry.IsCommodity,
            });
        }

        order.Status = IncomingOrderStatus.Received;
        order.ReceivedAtUtc = receivedAtUtc;

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new IncomingOrderReceiptResult(true, null);
    }

    private async Task<string?> ValidateSaveRequestAsync(
        IncomingOrderSaveRequest request,
        CancellationToken cancellationToken)
    {
        if (request.OrderDate is null)
        {
            return "Order date is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Vendor))
        {
            return "Vendor is required.";
        }

        if (request.ExpectedDate is null)
        {
            return "Expected pickup or delivery date is required.";
        }

        if (!Enum.IsDefined(request.Status) || request.Status == IncomingOrderStatus.Received)
        {
            return "Choose Pending, Ordered, or Cancelled. Use Receive Order to mark an order received.";
        }

        if (request.InvoiceAmount is < 0 || request.Weight is < 0)
        {
            return "Invoice amount and weight cannot be negative.";
        }

        if (request.Lines.Count == 0 || request.Lines.Any(line => line.InventoryEntryId is null || line.ExpectedQuantity is null or <= 0))
        {
            return "At least one inventory line with a quantity greater than zero is required.";
        }

        var inventoryEntryIds = request.Lines.Select(line => line.InventoryEntryId!.Value).ToArray();
        if (inventoryEntryIds.Distinct().Count() != inventoryEntryIds.Length)
        {
            return "The same inventory row cannot be added to an order more than once.";
        }

        var existingEntryCount = await context.InventoryEntries
            .CountAsync(entry => inventoryEntryIds.Contains(entry.Id), cancellationToken);

        return existingEntryCount == inventoryEntryIds.Length
            ? null
            : "One or more selected inventory rows were not found.";
    }

    private static IncomingOrderDateState GetDateState(IncomingOrder order, DateTime operatingDate)
    {
        if (order.Status is IncomingOrderStatus.Received or IncomingOrderStatus.Cancelled)
        {
            return IncomingOrderDateState.Complete;
        }

        var expectedDate = order.ExpectedDate.Date;
        return expectedDate < operatingDate
            ? IncomingOrderDateState.Overdue
            : expectedDate == operatingDate
                ? IncomingOrderDateState.DueToday
                : IncomingOrderDateState.Upcoming;
    }

    private static void ApplyRequest(IncomingOrder order, IncomingOrderSaveRequest request)
    {
        order.OrderDate = request.OrderDate!.Value.Date;
        order.Vendor = request.Vendor!.Trim();
        order.Status = request.Status;
        order.InvoiceDate = request.InvoiceDate?.Date;
        order.InvoiceNumber = Normalize(request.InvoiceNumber);
        order.InvoiceAmount = request.InvoiceAmount;
        order.DueDate = request.DueDate?.Date;
        order.SentToPayer = Normalize(request.SentToPayer);
        order.ChargeTo = Normalize(request.ChargeTo);
        order.ExpectedDate = request.ExpectedDate!.Value.Date;
        order.Weight = request.Weight;
        order.ProductSummary = Normalize(request.ProductSummary);
        order.Notes = Normalize(request.Notes);
        order.Lines = request.Lines.Select(line => new IncomingOrderLine
        {
            InventoryEntryId = line.InventoryEntryId!.Value,
            ExpectedQuantity = line.ExpectedQuantity!.Value,
        }).ToList();
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IncomingOrderEditView ToEditView(IncomingOrder order)
    {
        return new IncomingOrderEditView(
            order.Id,
            order.OrderDate,
            order.Vendor,
            order.Status,
            order.InvoiceDate,
            order.InvoiceNumber,
            order.InvoiceAmount,
            order.DueDate,
            order.SentToPayer,
            order.ChargeTo,
            order.ExpectedDate,
            order.Weight,
            order.ProductSummary,
            order.Notes,
            order.ReceivedAtUtc,
            order.Lines.Select(line => new IncomingOrderLineEditView(
                    line.Id,
                    line.InventoryEntryId,
                    $"{line.InventoryEntry.Category.Name} — {line.InventoryEntry.Item.Name} — {line.InventoryEntry.Location.Name}{(line.InventoryEntry.IsCommodity ? " (Commodity)" : string.Empty)}",
                    line.ExpectedQuantity,
                    line.ReceivedQuantity))
                .ToArray());
    }
}
