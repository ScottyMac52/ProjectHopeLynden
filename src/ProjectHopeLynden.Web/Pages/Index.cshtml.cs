using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ProjectHopeLynden.Web.Pages;

public sealed class IndexModel : PageModel
{
    public string PageTitle { get; } = "Project Hope Inventory";

    public string Summary { get; } = "Inventory support for serving Lynden families with dignity and compassion.";

    public string ImpactStatement { get; } = "Simple tools for food distribution, Commodity reporting, and inventory history.";

    public void OnGet()
    {
    }
}
