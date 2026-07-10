namespace ProjectHopeLynden.Application.Inventory;

public interface IInventoryEntryMaintenanceService
{
    Task<InventoryEntryFormOptions> GetFormOptionsAsync(CancellationToken cancellationToken = default);

    Task<InventoryEntryEditView?> GetEntryForEditAsync(
        int inventoryEntryId,
        CancellationToken cancellationToken = default);

    Task<InventoryEntrySaveResult> CreateEntryAsync(
        InventoryEntrySaveRequest request,
        DateTime savedAtUtc,
        CancellationToken cancellationToken = default);

    Task<InventoryEntrySaveResult> UpdateEntryAsync(
        int inventoryEntryId,
        InventoryEntrySaveRequest request,
        DateTime savedAtUtc,
        CancellationToken cancellationToken = default);
}
