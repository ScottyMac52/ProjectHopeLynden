using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjectHopeLynden.Application.Inventory;
using ProjectHopeLynden.Application.Reporting;
using ProjectHopeLynden.Web.Reporting;

namespace ProjectHopeLynden.Web.Pages.Inventory;

public sealed class SearchModel(
    IInventorySearchService inventorySearchService,
    IReportPdfService? reportPdfService = null) : PageModel
{
    public string PageTitle { get; } = "Search Inventory";

    public string Summary { get; } = "Find inventory by entering all or part of an item name, without knowing its category.";

    public InventorySearchResult Results { get; private set; } = new(string.Empty, []);

    public bool HasSearched => !string.IsNullOrWhiteSpace(SearchTerm);

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public async Task OnGetAsync()
    {
        Results = await inventorySearchService.SearchAsync(SearchTerm);
        SearchTerm = Results.SearchTerm;
    }

    public async Task<IActionResult> OnGetPdfAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchTerm))
        {
            return BadRequest();
        }

        var result = await inventorySearchService.SearchAsync(SearchTerm);
        return new InlinePdfResult(RequirePdfService().CreateInventorySearch(result, DateTime.UtcNow));
    }

    private IReportPdfService RequirePdfService() =>
        reportPdfService ?? throw new InvalidOperationException("PDF reporting is not configured.");
}
