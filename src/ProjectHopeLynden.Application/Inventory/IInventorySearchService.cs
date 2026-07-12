namespace ProjectHopeLynden.Application.Inventory;

public interface IInventorySearchService
{
    Task<InventorySearchResult> SearchAsync(
        string? searchTerm,
        CancellationToken cancellationToken = default);
}
