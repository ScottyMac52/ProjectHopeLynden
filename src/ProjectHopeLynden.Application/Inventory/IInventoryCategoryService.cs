namespace ProjectHopeLynden.Application.Inventory;

public interface IInventoryCategoryService
{
    Task<InventoryCategoryCreateResult> CreateCategoryAsync(
        string? categoryName,
        CancellationToken cancellationToken = default);
}
