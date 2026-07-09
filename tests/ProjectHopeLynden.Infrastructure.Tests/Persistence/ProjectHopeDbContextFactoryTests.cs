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

        Assert.NotNull(context);
        Assert.True(context.Database.IsSqlite());
        Assert.Contains(
            context.Database.GetConnectionString(),
            connectionString => connectionString.Contains("ProjectHopeLynden.db", StringComparison.Ordinal));
    }
}
