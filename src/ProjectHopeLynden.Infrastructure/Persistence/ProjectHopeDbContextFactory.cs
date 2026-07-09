using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProjectHopeLynden.Infrastructure.Persistence;

public sealed class ProjectHopeDbContextFactory : IDesignTimeDbContextFactory<ProjectHopeDbContext>
{
    public ProjectHopeDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProjectHopeDbContext>();
        optionsBuilder.UseSqlite("Data Source=ProjectHopeLynden.db");

        return new ProjectHopeDbContext(optionsBuilder.Options);
    }
}
