using Microsoft.EntityFrameworkCore;
using ProjectHopeLynden.Application.Inventory;
using ProjectHopeLynden.Domain.Inventory;
using ProjectHopeLynden.Infrastructure.Persistence;

namespace ProjectHopeLynden.Infrastructure.Inventory;

public sealed class InventoryCategoryService(ProjectHopeDbContext context) : IInventoryCategoryService
{
    public async Task<InventoryCategoryCreateResult> CreateCategoryAsync(
        string? categoryName,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = categoryName?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return new InventoryCategoryCreateResult(false, null, "Category name is required.");
        }

        var existingCategory = await context.Categories
            .SingleOrDefaultAsync(
                category => category.Name.ToLower() == normalizedName.ToLower(),
                cancellationToken);
        if (existingCategory is not null)
        {
            return new InventoryCategoryCreateResult(false, existingCategory.Id, "That category already exists.");
        }

        var category = new Category { Name = normalizedName };
        context.Categories.Add(category);
        await context.SaveChangesAsync(cancellationToken);

        return new InventoryCategoryCreateResult(true, category.Id, null);
    }
}
