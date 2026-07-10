using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjectHopeLynden.Application.Inventory;

namespace ProjectHopeLynden.Web.Pages.Inventory;

public sealed class IndexModel(
    IInventoryQueryService inventoryQueryService,
    IInventoryQuantityService inventoryQuantityService) : PageModel
{
    public string PageTitle { get; } = "Inventory Stewardship";

    public string Summary { get; } = "Keep food bank shelves, Commodity records, and guest support work connected in one local view.";

    public IReadOnlyList<InventoryCategoryListItem> Categories { get; private set; } = [];

    public CategoryInventoryView? Inventory { get; private set; }

    public bool CategoryWasNotFound { get; private set; }

    public bool QuantityUpdateFailed { get; private set; }

    public string? QuantityUpdateMessage { get; private set; }

    [BindProperty(SupportsGet = true)]
    public int? CategoryId { get; set; }

    [BindProperty]
    public int? InventoryEntryId { get; set; }

    [BindProperty]
    public int? UpdatedQuantity { get; set; }

    public async Task OnGetAsync()
    {
        await LoadInventoryAsync();
    }

    public async Task<IActionResult> OnPostUpdateQuantityAsync()
    {
        if (InventoryEntryId is null)
        {
            return await ReloadWithQuantityErrorAsync("Inventory entry is required.");
        }

        if (UpdatedQuantity is null)
        {
            return await ReloadWithQuantityErrorAsync("Quantity is required.");
        }

        var result = await inventoryQuantityService.UpdateCurrentQuantityAsync(
            InventoryEntryId.Value,
            UpdatedQuantity.Value,
            DateTime.UtcNow);

        if (!result.Succeeded)
        {
            return await ReloadWithQuantityErrorAsync(result.ErrorMessage ?? "Quantity update failed.");
        }

        return RedirectToPage(new { categoryId = CategoryId });
    }

    private async Task<PageResult> ReloadWithQuantityErrorAsync(string message)
    {
        QuantityUpdateFailed = true;
        QuantityUpdateMessage = message;
        await LoadInventoryAsync();
        return Page();
    }

    private async Task LoadInventoryAsync()
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
