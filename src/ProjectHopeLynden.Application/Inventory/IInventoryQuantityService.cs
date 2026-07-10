namespace ProjectHopeLynden.Application.Inventory;

public interface IInventoryQuantityService
{
    Task<InventoryQuantityUpdateResult> UpdateCurrentQuantityAsync(
        int inventoryEntryId,
        int quantity,
        DateTime countedAtUtc,
        CancellationToken cancellationToken = default);
}
