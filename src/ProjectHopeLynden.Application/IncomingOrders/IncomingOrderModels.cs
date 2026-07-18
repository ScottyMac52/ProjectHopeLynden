using ProjectHopeLynden.Domain.IncomingOrders;

namespace ProjectHopeLynden.Application.IncomingOrders;

public sealed record IncomingOrderListItem(
    int Id,
    DateTime OrderDate,
    string Vendor,
    IncomingOrderStatus Status,
    DateTime ExpectedDate,
    IncomingOrderDateState DateState,
    string? ProductSummary,
    int LineCount);

public sealed record IncomingOrderInventoryOption(int Id, string DisplayName);

public sealed record IncomingOrderLineRequest(int? Id, int? InventoryEntryId, double? ExpectedQuantity);

public sealed record IncomingOrderSaveRequest(
    DateTime? OrderDate,
    string? Vendor,
    IncomingOrderStatus Status,
    DateTime? InvoiceDate,
    string? InvoiceNumber,
    double? InvoiceAmount,
    DateTime? DueDate,
    string? SentToPayer,
    string? ChargeTo,
    DateTime? ExpectedDate,
    double? Weight,
    string? ProductSummary,
    string? Notes,
    IReadOnlyList<IncomingOrderLineRequest> Lines);

public sealed record IncomingOrderSaveResult(bool Succeeded, string? ErrorMessage, int? OrderId);

public sealed record IncomingOrderLineEditView(
    int Id,
    int InventoryEntryId,
    string InventoryEntryName,
    double ExpectedQuantity,
    double? ReceivedQuantity);

public sealed record IncomingOrderEditView(
    int Id,
    DateTime OrderDate,
    string Vendor,
    IncomingOrderStatus Status,
    DateTime? InvoiceDate,
    string? InvoiceNumber,
    double? InvoiceAmount,
    DateTime? DueDate,
    string? SentToPayer,
    string? ChargeTo,
    DateTime ExpectedDate,
    double? Weight,
    string? ProductSummary,
    string? Notes,
    DateTime? ReceivedAtUtc,
    IReadOnlyList<IncomingOrderLineEditView> Lines);

public sealed record IncomingOrderReceiptLineRequest(int LineId, double? ReceivedQuantity);

public sealed record IncomingOrderReceiptResult(bool Succeeded, string? ErrorMessage);
