using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProjectHopeLynden.Infrastructure.DependencyInjection;
using ProjectHopeLynden.Infrastructure.Persistence;
using ProjectHopeLynden.Web.Startup;

namespace ProjectHopeLynden.Web.Tests.Startup;

public sealed class DatabaseInitializationExtensionsTests
{
    [Fact]
    public async Task InitializeProjectHopeDatabaseAsync_SeedsFreshProductionDatabase()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"ProjectHopeLynden-{Guid.NewGuid():N}.db");

        try
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = Environments.Production,
            });
            builder.Services.AddProjectHopePersistence($"Data Source={databasePath}");

            await using var app = builder.Build();

            await app.InitializeProjectHopeDatabaseAsync();

            await using var scope = app.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ProjectHopeDbContext>();

            Assert.True(await context.Categories.AnyAsync());
            Assert.True(await context.InventoryEntries.AnyAsync());
            Assert.True(await context.InventoryCountHistory.AnyAsync());
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }
}