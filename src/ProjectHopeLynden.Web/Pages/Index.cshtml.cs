using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ProjectHopeLynden.Web.Pages;

public sealed class IndexModel : PageModel
{
    public string PageTitle { get; } = "Project Hope Inventory";

    public string Summary { get; } = "Local inventory management for Project Hope Food Bank of Lynden.";

    public void OnGet()
    {
    }
}
