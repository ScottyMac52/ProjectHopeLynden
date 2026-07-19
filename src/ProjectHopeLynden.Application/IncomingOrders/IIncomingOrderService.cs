namespace ProjectHopeLynden.Application.IncomingOrders;

public interface IIncomingOrderService
{
    Task<IReadOnlyList<IncomingOrderListItem>> GetOrdersAsync(
        DateTime operatingDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IncomingOrderInventoryOption>> GetInventoryOptionsAsync(
        CancellationToken cancellationToken = default);

    Task<IncomingOrderEditView?> GetOrderAsync(
        int orderId,
        CancellationToken cancellationToken = default);

    Task<IncomingOrderSaveResult> SaveOrderAsync(
        int? orderId,
        IncomingOrderSaveRequest request,
        DateTime createdAtUtc,
        CancellationToken cancellationToken = default);

    Task<IncomingOrderReceiptResult> ReceiveOrderAsync(
        int orderId,
        IReadOnlyList<IncomingOrderReceiptLineRequest> lines,
        DateTime receivedAtUtc,
        CancellationToken cancellationToken = default);
}
