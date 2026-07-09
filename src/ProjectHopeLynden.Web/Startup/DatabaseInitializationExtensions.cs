using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using ProjectHopeLynden.Infrastructure.Persistence;
using ProjectHopeLynden.Infrastructure.Persistence.Seeding;

namespace ProjectHopeLynden.Web.Startup;

[ExcludeFromCodeCoverage]
public static class DatabaseInitializationExtensions
{
    public static async Task InitializeProjectHopeDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ProjectHopeDbContext>();

        await context.Database.MigrateAsync();

        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        var seeder = scope.ServiceProvider.GetRequiredService<InitialInventorySeeder>();
        await seeder.SeedAsync();
    }
}
