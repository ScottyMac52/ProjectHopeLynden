using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ProjectHopeLynden.Web.Tests;

public sealed class WebApplicationSmokeTests
{
    [Fact]
    public async Task Root_ReturnsProjectHopeInventoryLandingPage()
    {
        var databasePath = CreateTemporaryDatabasePath();

        try
        {
            await using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment(Environments.Development);
                    builder.ConfigureAppConfiguration((_, configurationBuilder) =>
                    {
                        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["ConnectionStrings:ProjectHopeDatabase"] = $"Data Source={databasePath}",
                        });
                    });
                });

            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
            });

            var response = await client.GetAsync("/");
            var body = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode);
            Assert.Contains("Project Hope Inventory", body);
            Assert.Contains("Project Hope Food Bank of Lynden", body);
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    private static string CreateTemporaryDatabasePath()
    {
        return Path.Combine(Path.GetTempPath(), $"ProjectHopeLynden-Web-{Guid.NewGuid():N}.db");
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
