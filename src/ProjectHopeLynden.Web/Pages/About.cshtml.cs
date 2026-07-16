using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjectHopeLynden.Web.Information;

namespace ProjectHopeLynden.Web.Pages;

public sealed class AboutModel : PageModel
{
    public string ApplicationName { get; } = "Project Hope Inventory";

    public string Version { get; } = ApplicationVersionFormatter.Format(
        typeof(AboutModel).Assembly.GetName().Version);

    public string OrganizationName { get; } = "Project Hope Food Bank of Lynden";

    public string CreatorName { get; } = "Scott McIntosh";
}
