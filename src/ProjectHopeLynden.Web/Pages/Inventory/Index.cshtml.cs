using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjectHopeLynden.Application.Inventory;

namespace ProjectHopeLynden.Web.Pages.Inventory;

public sealed class IndexModel(IInventoryQueryService inventoryQueryService) : PageModel
{
    public IReadOnlyList<InventoryCategoryListItem> Categories { get; private set; } = [];

    public CategoryInventoryView? Inventory { get; private set; }

    public bool CategoryWasNotFound { get; private set; }

    [BindProperty(SupportsGet = true)]
    public int? CategoryId { get; set; }

    public async Task OnGetAsync()
    {
        Categories = await inventoryQueryService.GetCategoriesAsync();

        var selectedCategoryId = CategoryId ?? Categories.FirstOrDefault()?.Id;
        if (selectedCategoryId is null)
        {
            return;
        }

        CategoryId = selectedCategoryId.Value;
        Inventory = await inventoryQueryService.GetInventoryForCategoryAsync(selectedCategoryId.Value);
        CategoryWasNotFound = Inventory is null;
    }
}
