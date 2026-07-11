namespace ProjectHopeLynden.Application.Inventory;

public interface IInventoryQuantityService
{
    Task<InventoryQuantityUpdateResult> UpdateCurrentQuantityAsync(
        int inventoryEntryId,
        double quantity,
        DateTime countedAtUtc,
        CancellationToken cancellationToken = default);
}
