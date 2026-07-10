namespace ProjectHopeLynden.Application.Inventory;

public interface IInventoryHistoryService
{
    Task<InventoryEntryHistoryView?> GetHistoryForEntryAsync(
        int inventoryEntryId,
        CancellationToken cancellationToken = default);
}
