using Microsoft.EntityFrameworkCore;
using ProjectHopeLynden.Infrastructure.Persistence;

namespace ProjectHopeLynden.Infrastructure.Tests.Persistence;

public sealed class ProjectHopeDbContextFactoryTests
{
    [Fact]
    public void CreateDbContext_ReturnsConfiguredProjectHopeDbContext()
    {
        var factory = new ProjectHopeDbContextFactory();

        using var context = factory.CreateDbContext([]);

        var connectionString = context.Database.GetConnectionString();

        Assert.NotNull(context);
        Assert.True(context.Database.IsSqlite());
        Assert.NotNull(connectionString);
        Assert.Contains("ProjectHopeLynden.db", connectionString, StringComparison.Ordinal);
    }
}
