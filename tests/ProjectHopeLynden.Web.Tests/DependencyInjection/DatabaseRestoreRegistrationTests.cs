using Microsoft.Extensions.DependencyInjection;
using ProjectHopeLynden.Application.Backup;
using ProjectHopeLynden.Infrastructure.DependencyInjection;
using Xunit;

namespace ProjectHopeLynden.Web.Tests.DependencyInjection;

public sealed class DatabaseRestoreRegistrationTests
{
    [Fact]
    public void AddProjectHopeDatabaseBackup_RegistersRestoreService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProjectHopePersistence("Data Source=:memory:");
        services.AddProjectHopeDatabaseBackup("Backups");

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IDatabaseRestoreService>());
    }
}
