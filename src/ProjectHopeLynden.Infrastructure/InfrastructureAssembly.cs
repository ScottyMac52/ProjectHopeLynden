using ProjectHopeLynden.Application;
using ProjectHopeLynden.Domain;

namespace ProjectHopeLynden.Infrastructure;

public static class InfrastructureAssembly
{
    public static Type ApplicationMarkerType => typeof(ApplicationAssembly);

    public static Type DomainMarkerType => typeof(DomainAssembly);
}
