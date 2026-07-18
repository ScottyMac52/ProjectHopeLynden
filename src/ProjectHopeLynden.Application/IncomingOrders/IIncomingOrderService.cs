namespace ProjectHopeLynden.Application.IncomingOrders;

public interface IIncomingOrderService
{
    Task<IncomingOrdersView> GetOrdersAsync(CancellationToken cancellationToken = default);

    Task<IncomingOrderEditView?> GetForEditAsync(
        int incomingOrderId,
        CancellationToken cancellationToken = default);

    Task<IncomingOrderOperationResult> CreateAsync(
        IncomingOrderSaveRequest request,
        DateTime createdAtUtc,
        CancellationToken cancellationToken = default);

    Task<IncomingOrderOperationResult> UpdateAsync(
        int incomingOrderId,
        IncomingOrderSaveRequest request,
        DateTime updatedAtUtc,
        CancellationToken cancellationToken = default);

    Task<IncomingOrderOperationResult> CancelAsync(
        int incomingOrderId,
        DateTime cancelledAtUtc,
        CancellationToken cancellationToken = default);

    Task<IncomingOrderOperationResult> ReceiveAsync(
        int incomingOrderId,
        DateTime receivedAtUtc,
        CancellationToken cancellationToken = default);

    Task<IncomingOrderProcessingResult> ReceiveDueAsync(
        DateOnly dueThroughDate,
        DateTime receivedAtUtc,
        CancellationToken cancellationToken = default);
}
