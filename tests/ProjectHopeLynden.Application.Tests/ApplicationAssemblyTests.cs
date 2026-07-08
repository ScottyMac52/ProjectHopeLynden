using ProjectHopeLynden.Application;
using ProjectHopeLynden.Domain;

namespace ProjectHopeLynden.Application.Tests;

public sealed class ApplicationAssemblyTests
{
    [Fact]
    public void ApplicationAssembly_ReferencesDomainAssembly()
    {
        Assert.Equal(typeof(DomainAssembly), ApplicationAssembly.DomainMarkerType);
    }
}
