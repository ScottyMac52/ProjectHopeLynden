using ProjectHopeLynden.Web.Information;
using Xunit;

namespace ProjectHopeLynden.Web.Tests.Information;

public sealed class ApplicationVersionFormatterTests
{
    [Fact]
    public void Format_PreservesFourPartVersion()
    {
        var result = ApplicationVersionFormatter.Format(new Version(1, 2, 3, 4));

        Assert.Equal("1.2.3.4", result);
    }

    [Fact]
    public void Format_FillsMissingBuildAndRevisionWithZero()
    {
        var result = ApplicationVersionFormatter.Format(new Version(2, 5));

        Assert.Equal("2.5.0.0", result);
    }

    [Fact]
    public void Format_ReturnsZeroVersionWhenAssemblyVersionIsUnavailable()
    {
        var result = ApplicationVersionFormatter.Format(null);

        Assert.Equal("0.0.0.0", result);
    }
}
