using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjectHopeLynden.Application.Inventory;

namespace ProjectHopeLynden.Web.Pages.Inventory;

public sealed class MaintainModel(
    IInventoryEntryMaintenanceService entryMaintenanceService,
    IInventoryLocationService locationService) : PageModel
{
    public string PageTitle => IsEditing ? "Edit Inventory Row" : "Add Inventory Row";

    public string Summary { get; } = "Capture new rows and correct row details without editing spreadsheet files directly.";

    public InventoryEntryFormOptions Options { get; private set; } = new([], []);

    public bool EntryWasNotFound { get; private set; }

    public bool SaveFailed { get; private set; }

    public string? SaveMessage { get; private set; }

    public bool LocationCreateFailed { get; private set; }

    public string? LocationCreateMessage { get; private set; }

    public string? LocationCreateSuccessMessage { get; private set; }

    public bool IsEditing => InventoryEntryId.HasValue;

    [BindProperty(SupportsGet = true)]
    public int? InventoryEntryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? CategoryId { get; set; }

    [BindProperty]
    public string? ItemName { get; set; }

    [BindProperty]
    public int? SelectedCategoryId { get; set; }

    [BindProperty]
    public int? LocationId { get; set; }

    [BindProperty]
    public double? CurrentQuantity { get; set; }

    [BindProperty]
    public DateTime? BestByDate { get; set; }

    [BindProperty]
    public bool IsCommodity { get; set; }

    [BindProperty]
    public bool IsMenuItem { get; set; }

    [BindProperty]
    public string? NewLocationName { get; set; }

    public async Task OnGetAsync()
    {
        Options = await entryMaintenanceService.GetFormOptionsAsync();

        if (InventoryEntryId is null)
        {
            SelectedCategoryId = CategoryId ?? Options.Categories.FirstOrDefault()?.Id;
            LocationId = Options.Locations.FirstOrDefault()?.Id;
            CurrentQuantity = 0;
            return;
        }

        var editView = await entryMaintenanceService.GetEntryForEditAsync(InventoryEntryId.Value);
        if (editView is null)
        {
            EntryWasNotFound = true;
            return;
        }

        ApplyEditView(editView);
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        var request = new InventoryEntrySaveRequest(
            ItemName,
            SelectedCategoryId,
            LocationId,
            CurrentQuantity,
            BestByDate,
            IsCommodity,
            IsMenuItem);

        var result = InventoryEntryId is null
            ? await entryMaintenanceService.CreateEntryAsync(request, DateTime.UtcNow)
            : await entryMaintenanceService.UpdateEntryAsync(InventoryEntryId.Value, request, DateTime.UtcNow);

        if (!result.Succeeded)
        {
            SaveFailed = true;
            SaveMessage = result.ErrorMessage ?? "Inventory row could not be saved.";
            Options = await entryMaintenanceService.GetFormOptionsAsync();
            return Page();
        }

        return RedirectToPage("/Inventory/Manage", new { categoryId = result.CategoryId ?? SelectedCategoryId });
    }

    public async Task<IActionResult> OnPostCreateLocationAsync()
    {
        var result = await locationService.CreateLocationAsync(NewLocationName);
        Options = await entryMaintenanceService.GetFormOptionsAsync();

        if (!result.Succeeded)
        {
            LocationCreateFailed = true;
            LocationCreateMessage = result.ErrorMessage ?? "Location could not be added.";
            return Page();
        }

        LocationId = result.LocationId;
        NewLocationName = null;
        ModelState.Remove(nameof(LocationId));
        ModelState.Remove(nameof(NewLocationName));
        LocationCreateSuccessMessage =
            $"Location '{result.LocationName}' was added and selected. Your inventory row has not been saved yet.";
        return Page();
    }

    private void ApplyEditView(InventoryEntryEditView editView)
    {
        ItemName = editView.ItemName;
        SelectedCategoryId = editView.CategoryId;
        CategoryId = editView.CategoryId;
        LocationId = editView.LocationId;
        CurrentQuantity = editView.CurrentQuantity;
        BestByDate = editView.BestByDate;
        IsCommodity = editView.IsCommodity;
        IsMenuItem = editView.IsMenuItem;
    }
}
