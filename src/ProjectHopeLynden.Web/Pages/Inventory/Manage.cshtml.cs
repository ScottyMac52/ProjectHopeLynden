using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjectHopeLynden.Application.Inventory;

namespace ProjectHopeLynden.Web.Pages.Inventory;

public sealed class ManageModel(
    IInventoryQueryService inventoryQueryService,
    IInventoryQuantityService inventoryQuantityService,
    IInventoryCategoryService inventoryCategoryService) : PageModel
{
    public string PageTitle { get; } = "Manage Inventory";

    public string Summary { get; } = "Use the category-focused editor to add rows, update counts, and maintain inventory details.";

    public IReadOnlyList<InventoryCategoryListItem> Categories { get; private set; } = [];

    public CategoryInventoryView? Inventory { get; private set; }

    public bool CategoryWasNotFound { get; private set; }

    public bool QuantityUpdateFailed { get; private set; }

    public string? QuantityUpdateMessage { get; private set; }

    public bool CategoryCreateFailed { get; private set; }

    public string? CategoryCreateMessage { get; private set; }

    [BindProperty(SupportsGet = true)]
    public int? CategoryId { get; set; }

    [BindProperty]
    public int? InventoryEntryId { get; set; }

    [BindProperty]
    public double? UpdatedQuantity { get; set; }

    [BindProperty]
    public string? NewCategoryName { get; set; }

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

    public async Task<IActionResult> OnPostCreateCategoryAsync()
    {
        var result = await inventoryCategoryService.CreateCategoryAsync(NewCategoryName);
        if (!result.Succeeded)
        {
            CategoryCreateFailed = true;
            CategoryCreateMessage = result.ErrorMessage ?? "Category creation failed.";
            await LoadInventoryAsync();
            return Page();
        }

        return RedirectToPage(new { categoryId = result.CategoryId });
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
