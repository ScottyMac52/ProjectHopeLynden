namespace ProjectHopeLynden.Application.Inventory;

public interface IInventoryLocationService
{
    Task<IReadOnlyList<InventoryLocationListItem>> GetLocationsAsync(
        CancellationToken cancellationToken = default);

    Task<InventoryLocationCreateResult> CreateLocationAsync(
        string? locationName,
        CancellationToken cancellationToken = default);
}
