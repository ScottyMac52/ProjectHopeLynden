using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjectHopeLynden.Application.Inventory;
using ProjectHopeLynden.Application.Reporting;
using ProjectHopeLynden.Web.Reporting;

namespace ProjectHopeLynden.Web.Pages.Inventory;

public sealed class IndexModel(
    IInventoryQueryService inventoryQueryService,
    IInventoryQuantityService inventoryQuantityService,
    IInventoryCategoryService? inventoryCategoryService = null,
    IReportPdfService? reportPdfService = null) : PageModel
{
    public string PageTitle { get; } = "Inventory Spreadsheet";

    public string Summary { get; } = "Review and update inventory in one continuous spreadsheet-style grid, or use the original edit form for full row maintenance.";

    public IReadOnlyList<CategoryInventoryView> CategoryInventories { get; private set; } = [];

    public bool QuantityUpdateFailed { get; private set; }
    public string? QuantityUpdateMessage { get; private set; }
    public bool CategoryCreateFailed { get; private set; }
    public string? CategoryCreateMessage { get; private set; }

    [BindProperty] public int? CategoryId { get; set; }
    [BindProperty] public int? InventoryEntryId { get; set; }
    [BindProperty] public double? UpdatedQuantity { get; set; }
    [BindProperty] public string? NewCategoryName { get; set; }

    public async Task OnGetAsync() => await LoadInventoryAsync();

    public async Task<IActionResult> OnGetPdfAsync()
    {
        await LoadInventoryAsync();
        return new InlinePdfResult(RequirePdfService().CreateInventorySpreadsheet(CategoryInventories, DateTime.UtcNow));
    }

    public async Task<IActionResult> OnPostCreateCategoryAsync()
    {
        if (inventoryCategoryService is null)
        {
            CategoryCreateFailed = true;
            CategoryCreateMessage = "Category creation is unavailable.";
            await LoadInventoryAsync();
            return Page();
        }

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

    private IReportPdfService RequirePdfService() =>
        reportPdfService ?? throw new InvalidOperationException("PDF reporting is not configured.");
}
