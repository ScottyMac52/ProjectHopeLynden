using Microsoft.Extensions.Hosting;

namespace ProjectHopeLynden.Web.Hosting;

public static class ProjectHopeWindowsServiceHost
{
    public const string ServiceName = "ProjectHopeLynden";

    public const string DisplayName = "Project Hope Lynden Inventory Server";

    public static void Configure(WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Host.UseWindowsService(ConfigureWindowsServiceOptions);
    }

    public static void ConfigureWindowsServiceOptions(WindowsServiceLifetimeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.ServiceName = ServiceName;
    }
}
