using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjectHopeLynden.Application.Inventory;

namespace ProjectHopeLynden.Web.Pages.Inventory;

public sealed class LocationsModel(IInventoryLocationService locationService) : PageModel
{
    public string PageTitle { get; } = "Manage Locations";

    public IReadOnlyList<InventoryLocationListItem> Locations { get; private set; } = [];

    public bool CreateFailed { get; private set; }

    public string? CreateMessage { get; private set; }

    [TempData]
    public string? AddedLocationName { get; set; }

    [BindProperty]
    public string? NewLocationName { get; set; }

    public async Task OnGetAsync()
    {
        await LoadLocationsAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        var result = await locationService.CreateLocationAsync(NewLocationName);
        if (!result.Succeeded)
        {
            CreateFailed = true;
            CreateMessage = result.ErrorMessage ?? "Location could not be added.";
            await LoadLocationsAsync();
            return Page();
        }

        AddedLocationName = result.LocationName;
        return RedirectToPage();
    }

    private async Task LoadLocationsAsync()
    {
        Locations = await locationService.GetLocationsAsync();
    }
}
