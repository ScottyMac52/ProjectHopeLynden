using System.Text.RegularExpressions;
using ProjectHopeLynden.Web.Pages;
using Xunit;

namespace ProjectHopeLynden.Web.Tests.Pages;

public sealed class AboutModelTests
{
    [Fact]
    public void ApplicationInformation_UsesRequestedNamesAndAttribution()
    {
        var model = new AboutModel();

        Assert.Equal("Project Hope Inventory", model.ApplicationName);
        Assert.Equal("Project Hope Food Bank of Lynden", model.OrganizationName);
        Assert.Equal("Scott McIntosh", model.CreatorName);
    }

    [Fact]
    public void Version_UsesFourPartInstalledApplicationFormat()
    {
        var model = new AboutModel();

        Assert.Matches(new Regex(@"^\d+\.\d+\.\d+\.\d+$"), model.Version);
    }
}
