using Microsoft.EntityFrameworkCore;
using ProjectHopeLynden.Infrastructure.Persistence;
using Xunit;

namespace ProjectHopeLynden.Infrastructure.Tests.Persistence;

public sealed class ProjectHopeDbContextFactoryTests
{
    [Fact]
    public void CreateDbContext_ReturnsProjectHopeDbContext()
    {
        var factory = new ProjectHopeDbContextFactory();

        using var context = factory.CreateDbContext([]);

        Assert.IsType<ProjectHopeDbContext>(context);
    }

    [Fact]
    public void CreateDbContext_ConfiguresSqliteProvider()
    {
        var factory = new ProjectHopeDbContextFactory();

        using var context = factory.CreateDbContext([]);

        Assert.Equal("Microsoft.EntityFrameworkCore.Sqlite", context.Database.ProviderName);
    }

    [Fact]
    public async Task CreateDbContext_CanCreateTheDatabase()
    {
        var factory = new ProjectHopeDbContextFactory();

        await using var context = factory.CreateDbContext([]);
        try
        {
            await context.Database.EnsureCreatedAsync();

            Assert.True(await context.Database.CanConnectAsync());
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }
}
