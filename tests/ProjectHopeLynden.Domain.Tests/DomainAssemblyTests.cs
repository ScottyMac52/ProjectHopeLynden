using ProjectHopeLynden.Domain;

namespace ProjectHopeLynden.Domain.Tests;

public sealed class DomainAssemblyTests
{
    [Fact]
    public void DomainAssembly_IsDiscoverable()
    {
        Assert.Equal("ProjectHopeLynden.Domain", typeof(DomainAssembly).Namespace);
    }
}
