using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectHopeLynden.Application.Inventory;
using ProjectHopeLynden.Infrastructure.Inventory;
using ProjectHopeLynden.Infrastructure.Persistence;
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
        services.AddScoped<IInventoryQuantityService, InventoryQuantityService>();
        services.AddScoped<IInventoryHistoryService, InventoryHistoryService>();
        services.AddScoped<InitialInventorySeeder>();

        return services;
    }
}
