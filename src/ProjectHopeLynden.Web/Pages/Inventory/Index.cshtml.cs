using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjectHopeLynden.Application.Inventory;

namespace ProjectHopeLynden.Web.Pages.Inventory;

public sealed class IndexModel(
    IInventoryQueryService inventoryQueryService,
    IInventoryQuantityService inventoryQuantityService) : PageModel
{
    public string PageTitle { get; } = "Inventory Overview";

    public string Summary { get; } = "Review every Project Hope inventory category in one familiar spreadsheet-style view.";

    public IReadOnlyList<CategoryInventoryView> CategoryInventories { get; private set; } = [];

    public bool QuantityUpdateFailed { get; private set; }

    public string? QuantityUpdateMessage { get; private set; }

    [BindProperty]
    public int? CategoryId { get; set; }

    [BindProperty]
    public int? InventoryEntryId { get; set; }

    [BindProperty]
    public double? UpdatedQuantity { get; set; }

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

        return RedirectToPage();
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
        var categories = await inventoryQueryService.GetCategoriesAsync();
        var inventories = new List<CategoryInventoryView>(categories.Count);

        foreach (var category in categories)
        {
            var inventory = await inventoryQueryService.GetInventoryForCategoryAsync(category.Id);
            if (inventory is not null)
            {
                inventories.Add(inventory);
            }
        }

        CategoryInventories = inventories;
    }
}
