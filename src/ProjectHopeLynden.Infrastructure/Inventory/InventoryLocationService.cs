using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using ProjectHopeLynden.Application.Inventory;
using ProjectHopeLynden.Domain.Inventory;
using ProjectHopeLynden.Infrastructure.Persistence;

namespace ProjectHopeLynden.Infrastructure.Inventory;

public sealed class InventoryLocationService(ProjectHopeDbContext context) : IInventoryLocationService
{
    public async Task<IReadOnlyList<InventoryLocationListItem>> GetLocationsAsync(
        CancellationToken cancellationToken = default)
    {
        return await context.Locations
            .AsNoTracking()
            .OrderBy(location => location.Name)
            .ThenBy(location => location.Id)
            .Select(location => new InventoryLocationListItem(location.Id, location.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<InventoryLocationCreateResult> CreateLocationAsync(
        string? locationName,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = locationName?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return Failure(null, null, "Location name is required.");
        }

        if (normalizedName.Length > 100)
        {
            return Failure(null, null, "Location name must be 100 characters or fewer.");
        }

        var normalizedKey = normalizedName.ToUpperInvariant();
        var existingLocation = await FindByNormalizedNameAsync(normalizedKey, cancellationToken);
        if (existingLocation is not null)
        {
            return Duplicate(existingLocation);
        }

        var location = new Location { Name = normalizedName };
        context.Locations.Add(location);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return new InventoryLocationCreateResult(true, location.Id, location.Name, null);
        }
        catch (DbUpdateException)
        {
            context.Entry(location).State = EntityState.Detached;
            try
            {
                existingLocation = await FindByNormalizedNameAsync(normalizedKey, cancellationToken);
                return existingLocation is null
                    ? Failure(null, null, "Location could not be added. Please try again.")
                    : Duplicate(existingLocation);
            }
            catch (DbException)
            {
                return Failure(null, null, "Location could not be added. Please try again.");
            }
        }
    }

    private Task<Location?> FindByNormalizedNameAsync(
        string normalizedName,
        CancellationToken cancellationToken)
    {
        return context.Locations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                location => EF.Property<string>(location, "NormalizedName") == normalizedName,
                cancellationToken);
    }

    private static InventoryLocationCreateResult Duplicate(Location existingLocation) =>
        Failure(existingLocation.Id, existingLocation.Name,
            $"The location '{existingLocation.Name}' already exists.");

    private static InventoryLocationCreateResult Failure(
        int? locationId,
        string? locationName,
        string errorMessage) =>
        new(false, locationId, locationName, errorMessage);
}
