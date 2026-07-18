using ProjectHopeLynden.Domain.Inventory;

namespace ProjectHopeLynden.Application.IncomingOrders;

public sealed record IncomingOrderInventoryOption(
    int InventoryEntryId,
    string ItemName,
    string CategoryName,
    string LocationName,
    bool IsCommodity)
{
    public string DisplayName =>
        $"{ItemName} — {CategoryName} — {LocationName}{(IsCommodity ? " — Commodity" : string.Empty)}";
}

public sealed record IncomingOrderListItem(
    int Id,
    int InventoryEntryId,
    string ItemName,
    string CategoryName,
    string LocationName,
    bool IsCommodity,
    double Quantity,
    DateOnly ExpectedDate,
    string? Source,
    string? Reference,
    IncomingOrderStatus Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? ReceivedAtUtc,
    DateTime? CancelledAtUtc)
{
    public bool CanChange => Status == IncomingOrderStatus.Scheduled;
}

public sealed record IncomingOrdersView(
    IReadOnlyList<IncomingOrderInventoryOption> InventoryOptions,
    IReadOnlyList<IncomingOrderListItem> ScheduledOrders,
    IReadOnlyList<IncomingOrderListItem> CompletedOrders);

public sealed record IncomingOrderSaveRequest(
    int? InventoryEntryId,
    double? Quantity,
    DateOnly? ExpectedDate,
    string? Source,
    string? Reference);

public sealed record IncomingOrderOperationResult(
    bool Succeeded,
    string? ErrorMessage,
    int? IncomingOrderId = null);

public sealed record IncomingOrderProcessingResult(
    int ReceivedOrderCount,
    double AddedQuantity);
