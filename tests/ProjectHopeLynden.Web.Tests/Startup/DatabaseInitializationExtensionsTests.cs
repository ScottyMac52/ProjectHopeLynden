using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectHopeLynden.Infrastructure.DependencyInjection;
using ProjectHopeLynden.Infrastructure.Persistence;
using ProjectHopeLynden.Web.Startup;
using Xunit;

namespace ProjectHopeLynden.Web.Tests.Startup;

public sealed class DatabaseInitializationExtensionsTests
{
    [Fact]
    public async Task InitializeProjectHopeDatabaseAsync_MigratesDatabase()
    {
        var databasePath = CreateTemporaryDatabasePath();
        await using var app = CreateApp(databasePath, Environments.Production);

        try
        {
            await app.InitializeProjectHopeDatabaseAsync();

            await using var scope = app.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ProjectHopeDbContext>();

            Assert.True(await context.Database.CanConnectAsync());
            Assert.Contains(
                "Categories",
                await context.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'table';").ToListAsync());
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    [Fact]
    public async Task InitializeProjectHopeDatabaseAsync_SkipsSeedingOutsideDevelopment()
    {
        var databasePath = CreateTemporaryDatabasePath();
        await using var app = CreateApp(databasePath, Environments.Production);

        try
        {
            await app.InitializeProjectHopeDatabaseAsync();

            await using var scope = app.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ProjectHopeDbContext>();

            Assert.False(await context.Categories.AnyAsync());
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    [Fact]
    public async Task InitializeProjectHopeDatabaseAsync_SeedsDataInDevelopment()
    {
        var databasePath = CreateTemporaryDatabasePath();
        await using var app = CreateApp(databasePath, Environments.Development);

        try
        {
            await app.InitializeProjectHopeDatabaseAsync();

            await using var scope = app.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ProjectHopeDbContext>();

            Assert.True(await context.Categories.AnyAsync(category => category.Name == "Canned Vegetables"));
            Assert.True(await context.InventoryEntries.AnyAsync(entry => entry.IsCommodity));
            Assert.True(await context.InventoryCountHistory.AnyAsync());
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    private static WebApplication CreateApp(string databasePath, string environmentName)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environmentName,
        });

        builder.Services.AddProjectHopePersistence($"Data Source={databasePath}");

        return builder.Build();
    }

    private static string CreateTemporaryDatabasePath()
    {
        return Path.Combine(Path.GetTempPath(), $"ProjectHopeLynden-{Guid.NewGuid():N}.db");
    }

    private static void DeleteDatabase(string databasePath)
    {
        foreach (var path in new[] { databasePath, $"{databasePath}-shm", $"{databasePath}-wal" })
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
