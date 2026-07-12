using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectHopeLynden.Application.Backup;
using ProjectHopeLynden.Application.Inventory;
using ProjectHopeLynden.Infrastructure.Inventory;
using ProjectHopeLynden.Infrastructure.Persistence;
using ProjectHopeLynden.Infrastructure.Persistence.Backup;
using ProjectHopeLynden.Infrastructure.Persistence.Seeding;

namespace ProjectHopeLynden.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProjectHopePersistence(
        this IServiceCollection services,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("A ProjectHope persistence connection string is required.", nameof(connectionString));
        }

        services.AddDbContext<ProjectHopeDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IInventoryQueryService, InventoryQueryService>();
        services.AddScoped<IInventorySearchService, InventorySearchService>();
        services.AddScoped<IInventoryQuantityService, InventoryQuantityService>();
        services.AddScoped<IInventoryHistoryService, InventoryHistoryService>();
        services.AddScoped<IInventoryCommodityService, InventoryCommodityService>();
        services.AddScoped<IInventoryTrendReportService, InventoryTrendReportService>();
        services.AddScoped<IInventoryEntryMaintenanceService, InventoryEntryMaintenanceService>();
        services.AddScoped<IInventoryCategoryService, InventoryCategoryService>();
        services.AddScoped<InitialInventorySeeder>();

        return services;
    }

    public static IServiceCollection AddProjectHopeDatabaseBackup(
        this IServiceCollection services,
        string backupFolder)
    {
        if (string.IsNullOrWhiteSpace(backupFolder))
        {
            throw new ArgumentException("A Project Hope database backup folder is required.", nameof(backupFolder));
        }

        services.AddSingleton(new DatabaseBackupOptions(backupFolder));
        services.AddScoped<IDatabaseBackupService, DatabaseBackupService>();

        return services;
    }
}
