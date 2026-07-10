using Microsoft.EntityFrameworkCore;
using ProjectHopeLynden.Application.Inventory;
using ProjectHopeLynden.Infrastructure.Persistence;

namespace ProjectHopeLynden.Infrastructure.Inventory;

public sealed class InventoryQueryService(ProjectHopeDbContext context) : IInventoryQueryService
{
    public async Task<IReadOnlyList<InventoryCategoryListItem>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        return await context.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => new InventoryCategoryListItem(category.Id, category.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoryInventoryView?> GetInventoryForCategoryAsync(
        int categoryId,
        CancellationToken cancellationToken = default)
    {
        var category = await context.Categories
            .AsNoTracking()
            .Where(category => category.Id == categoryId)
            .Select(category => new InventoryCategoryListItem(category.Id, category.Name))
            .SingleOrDefaultAsync(cancellationToken);

        if (category is null)
        {
            return null;
        }

        var entries = await context.InventoryEntries
            .AsNoTracking()
            .Where(entry => entry.CategoryId == categoryId)
            .OrderBy(entry => entry.Item.Name)
            .ThenBy(entry => entry.Location.Name)
            .ThenBy(entry => entry.IsCommodity)
            .ThenBy(entry => entry.Id)
            .Select(entry => new InventoryEntryListItem(
                entry.Id,
                entry.Item.Name,
                entry.Location.Name,
                entry.CurrentQuantity,
                entry.IsCommodity,
                entry.BestByDate,
                entry.IsMenuItem,
                entry.LastUpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return new CategoryInventoryView(category.Id, category.Name, entries);
    }
}
