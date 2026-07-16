using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using ProjectHopeLynden.Web.Hosting;
using Xunit;

namespace ProjectHopeLynden.Web.Tests.Hosting;

public sealed class ProjectHopeWindowsServiceHostTests
{
    [Fact]
    public void ServiceContract_UsesStableNames()
    {
        Assert.Equal("ProjectHopeLynden", ProjectHopeWindowsServiceHost.ServiceName);
        Assert.Equal(
            "Project Hope Lynden Inventory Server",
            ProjectHopeWindowsServiceHost.DisplayName);
    }

    [Fact]
    public void ConfigureWindowsServiceOptions_AssignsServiceName()
    {
        var options = new WindowsServiceLifetimeOptions();

        ProjectHopeWindowsServiceHost.ConfigureWindowsServiceOptions(options);

        Assert.Equal(ProjectHopeWindowsServiceHost.ServiceName, options.ServiceName);
    }

    [Fact]
    public void ConfigureWindowsServiceOptions_RejectsNullOptions()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ProjectHopeWindowsServiceHost.ConfigureWindowsServiceOptions(null!));
    }

    [Fact]
    public void Configure_AcceptsNormalInteractiveHost()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development,
        });

        ProjectHopeWindowsServiceHost.Configure(builder);

        Assert.NotNull(builder);
    }

    [Fact]
    public void Configure_RejectsNullBuilder()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ProjectHopeWindowsServiceHost.Configure(null!));
    }
}
