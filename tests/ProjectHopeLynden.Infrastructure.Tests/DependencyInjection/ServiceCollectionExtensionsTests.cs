using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectHopeLynden.Infrastructure.DependencyInjection;
using ProjectHopeLynden.Infrastructure.Persistence;
using ProjectHopeLynden.Infrastructure.Persistence.Seeding;
using Xunit;

namespace ProjectHopeLynden.Infrastructure.Tests.DependencyInjection;

public sealed class ServiceCollectionExtensionsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddProjectHopePersistence_RejectsMissingConnectionString(string? connectionString)
    {
        var services = new ServiceCollection();

        Assert.ThrowsAny<ArgumentException>(() => services.AddProjectHopePersistence(connectionString!));
    }

    [Fact]
    public void AddProjectHopePersistence_RegistersDbContextAndSeeder()
    {
        var services = new ServiceCollection();

        services.AddProjectHopePersistence("Data Source=:memory:");

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ProjectHopeDbContext>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<InitialInventorySeeder>());
    }

    [Fact]
    public void AddProjectHopePersistence_ConfiguresSqliteProvider()
    {
        var services = new ServiceCollection();

        services.AddProjectHopePersistence("Data Source=:memory:");

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ProjectHopeDbContext>();

        Assert.Equal("Microsoft.EntityFrameworkCore.Sqlite", context.Database.ProviderName);
    }
}
