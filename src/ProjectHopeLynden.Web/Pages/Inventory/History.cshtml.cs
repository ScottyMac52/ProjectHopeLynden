using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjectHopeLynden.Application.Inventory;

namespace ProjectHopeLynden.Web.Pages.Inventory;

public sealed class HistoryModel(IInventoryHistoryService inventoryHistoryService) : PageModel
{
    public string PageTitle { get; } = "Inventory Count History";

    public string Summary { get; } = "Review previous and current counts for a Project Hope inventory entry.";

    public InventoryEntryHistoryView? History { get; private set; }

    public bool EntryWasNotFound { get; private set; }

    [BindProperty(SupportsGet = true)]
    public int InventoryEntryId { get; set; }

    public async Task OnGetAsync()
    {
        if (InventoryEntryId <= 0)
        {
            EntryWasNotFound = true;
            return;
        }

        History = await inventoryHistoryService.GetHistoryForEntryAsync(InventoryEntryId);
        EntryWasNotFound = History is null;
    }
}
