namespace ProjectHopeLynden.Application.Inventory;

public interface IInventoryQueryService
{
    Task<IReadOnlyList<InventoryCategoryListItem>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    Task<CategoryInventoryView?> GetInventoryForCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
}
