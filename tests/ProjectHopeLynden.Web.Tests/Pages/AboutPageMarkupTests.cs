using Xunit;

namespace ProjectHopeLynden.Web.Tests.Pages;

public sealed class AboutPageMarkupTests
{
    [Fact]
    public void Layout_IncludesAboutNavigationAndCreatorAttribution()
    {
        var layout = ReadAsset("_Layout.cshtml");

        Assert.Contains("asp-page=\"/About\">About</a>", layout);
        Assert.Contains("Created for Project Hope Food Bank of Lynden by", layout);
        Assert.Contains("asp-page=\"/About\">Scott McIntosh</a>", layout);
    }

    [Fact]
    public void AboutPage_DisplaysDynamicFourPartVersionAndCreator()
    {
        var aboutPage = ReadAsset("About.cshtml");

        Assert.Contains("Version: <strong>@Model.Version</strong>", aboutPage);
        Assert.Contains("@Model.ApplicationName", aboutPage);
        Assert.Contains("@Model.OrganizationName", aboutPage);
        Assert.Contains("@Model.CreatorName", aboutPage);
    }

    private static string ReadAsset(string fileName)
    {
        return File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "TestAssets",
            fileName));
    }
}
