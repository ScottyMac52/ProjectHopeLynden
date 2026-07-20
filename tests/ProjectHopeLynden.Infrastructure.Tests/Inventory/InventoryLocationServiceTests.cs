using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProjectHopeLynden.Domain.Inventory;
using ProjectHopeLynden.Infrastructure.Inventory;
using ProjectHopeLynden.Infrastructure.Persistence;

namespace ProjectHopeLynden.Infrastructure.Tests.Inventory;

public sealed class InventoryLocationServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private ProjectHopeDbContext context = null!;

    public async Task InitializeAsync()
    {
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ProjectHopeDbContext>()
            .UseSqlite(connection)
            .Options;
        context = new ProjectHopeDbContext(options);
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await context.DisposeAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task GetLocationsAsync_ReturnsStableNameOrder()
    {
        context.Locations.AddRange(
            new Location { Name = "Shelf" },
            new Location { Name = "Back Room" },
            new Location { Name = "Cooler" });
        await context.SaveChangesAsync();
        var service = new InventoryLocationService(context);

        var locations = await service.GetLocationsAsync();

        Assert.Equal(["Back Room", "Cooler", "Shelf"], locations.Select(location => location.Name));
    }

    [Fact]
    public async Task CreateLocationAsync_TrimsAndSavesValidName()
    {
        var service = new InventoryLocationService(context);

        var result = await service.CreateLocationAsync("  Loading Dock  ");

        Assert.True(result.Succeeded);
        Assert.Equal("Loading Dock", result.LocationName);
        Assert.Equal(result.LocationId, (await context.Locations.AsNoTracking().SingleAsync()).Id);
    }

    [Theory]
    [InlineData(null, "Location name is required.")]
    [InlineData("", "Location name is required.")]
    [InlineData("   ", "Location name is required.")]
    public async Task CreateLocationAsync_RejectsBlankName(string? name, string expectedMessage)
    {
        var result = await new InventoryLocationService(context).CreateLocationAsync(name);

        Assert.False(result.Succeeded);
        Assert.Equal(expectedMessage, result.ErrorMessage);
        Assert.False(await context.Locations.AnyAsync());
    }

    [Fact]
    public async Task CreateLocationAsync_RejectsNameOverMaximumLength()
    {
        var result = await new InventoryLocationService(context).CreateLocationAsync(new string('L', 101));

        Assert.False(result.Succeeded);
        Assert.Contains("100 characters", result.ErrorMessage);
        Assert.False(await context.Locations.AnyAsync());
    }

    [Fact]
    public async Task CreateLocationAsync_RejectsCaseAndWhitespaceDuplicateAndIdentifiesExistingLocation()
    {
        var existing = new Location { Name = "Back Room" };
        context.Locations.Add(existing);
        await context.SaveChangesAsync();
        var service = new InventoryLocationService(context);

        var result = await service.CreateLocationAsync("  back room  ");

        Assert.False(result.Succeeded);
        Assert.Equal(existing.Id, result.LocationId);
        Assert.Equal("Back Room", result.LocationName);
        Assert.Equal("The location 'Back Room' already exists.", result.ErrorMessage);
        Assert.Equal(1, await context.Locations.CountAsync());
    }

    [Fact]
    public async Task Database_PreventsNormalizedDuplicateLocations()
    {
        context.Locations.Add(new Location { Name = "Shelf" });
        await context.SaveChangesAsync();
        context.Locations.Add(new Location { Name = " shelf " });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        context.ChangeTracker.Clear();
        Assert.Equal(1, await context.Locations.CountAsync());
    }

    [Fact]
    public async Task CreateLocationAsync_ReturnsActionableMessageWhenPersistenceFails()
    {
        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE TRIGGER RejectBlockedLocation
            BEFORE INSERT ON Locations
            WHEN NEW.Name = 'Blocked Location'
            BEGIN
                SELECT RAISE(ABORT, 'blocked for test');
            END;
            """);
        var service = new InventoryLocationService(context);

        var result = await service.CreateLocationAsync("Blocked Location");

        Assert.False(result.Succeeded);
        Assert.Equal("Location could not be added. Please try again.", result.ErrorMessage);
        Assert.False(await context.Locations.AnyAsync());
    }
}
