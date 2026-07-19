using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectHopeLynden.Application.Backup;
using ProjectHopeLynden.Application.Inventory;
using ProjectHopeLynden.Application.IncomingOrders;
using ProjectHopeLynden.Infrastructure.DependencyInjection;
using ProjectHopeLynden.Infrastructure.Persistence;
using ProjectHopeLynden.Infrastructure.Persistence.Seeding;

namespace ProjectHopeLynden.Infrastructure.Tests.DependencyInjection;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddProjectHopePersistence_RegistersPersistenceServices()
    {
        var services = new ServiceCollection();

        services.AddProjectHopePersistence("Data Source=:memory:");

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<ProjectHopeDbContext>());
        Assert.NotNull(provider.GetRequiredService<IInventoryQueryService>());
        Assert.NotNull(provider.GetRequiredService<IInventorySearchService>());
        Assert.NotNull(provider.GetRequiredService<IInventoryQuantityService>());
        Assert.NotNull(provider.GetRequiredService<IInventoryHistoryService>());
        Assert.NotNull(provider.GetRequiredService<IInventoryCommodityService>());
        Assert.NotNull(provider.GetRequiredService<IInventoryTrendReportService>());
        Assert.NotNull(provider.GetRequiredService<IInventoryEntryMaintenanceService>());
        Assert.NotNull(provider.GetRequiredService<IIncomingOrderService>());
        Assert.NotNull(provider.GetRequiredService<InitialInventorySeeder>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddProjectHopePersistence_RejectsMissingConnectionString(string? connectionString)
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() => services.AddProjectHopePersistence(connectionString!));
    }

    [Fact]
    public void AddProjectHopePersistence_ConfiguresSqliteDbContext()
    {
        var services = new ServiceCollection();

        services.AddProjectHopePersistence("Data Source=:memory:");

        var descriptor = Assert.Single(services, service => service.ServiceType == typeof(DbContextOptions<ProjectHopeDbContext>));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddProjectHopeDatabaseBackup_RegistersConfiguredBackupService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProjectHopePersistence("Data Source=:memory:");

        services.AddProjectHopeDatabaseBackup("ConfiguredBackups");

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DatabaseBackupOptions>();
        var service = scope.ServiceProvider.GetRequiredService<IDatabaseBackupService>();

        Assert.Equal("ConfiguredBackups", options.Folder);
        Assert.Equal("ConfiguredBackups", service.BackupFolder);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddProjectHopeDatabaseBackup_RejectsMissingFolder(string? backupFolder)
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() => services.AddProjectHopeDatabaseBackup(backupFolder!));
    }
}
