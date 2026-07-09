using ProjectHopeLynden.Application;
using ProjectHopeLynden.Domain;
using ProjectHopeLynden.Infrastructure;
using Xunit;

namespace ProjectHopeLynden.Infrastructure.Tests;

public sealed class InfrastructureAssemblyTests
{
    [Fact]
    public void InfrastructureAssembly_ReferencesApplicationAndDomainAssemblies()
    {
        Assert.Equal(typeof(ApplicationAssembly), InfrastructureAssembly.ApplicationMarkerType);
        Assert.Equal(typeof(DomainAssembly), InfrastructureAssembly.DomainMarkerType);
    }
}
