using Microsoft.EntityFrameworkCore;
using ProjectHopeLynden.Application.Inventory;
using ProjectHopeLynden.Infrastructure.Persistence;

namespace ProjectHopeLynden.Infrastructure.Inventory;

public sealed class InventorySearchService(ProjectHopeDbContext context) : IInventorySearchService
{
    public async Task<InventorySearchResult> SearchAsync(
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        var normalizedTerm = searchTerm?.Trim() ?? string.Empty;
        if (normalizedTerm.Length == 0)
        {
            return new InventorySearchResult(normalizedTerm, []);
        }

        var pattern = $"%{normalizedTerm}%";
        var rows = await context.InventoryEntries
            .AsNoTracking()
            .Where(entry => EF.Functions.Like(entry.Item.Name, pattern))
            .OrderBy(entry => entry.Item.Name)
            .ThenBy(entry => entry.Category.Name)
            .ThenBy(entry => entry.Location.Name)
            .ThenByDescending(entry => entry.IsCommodity)
            .ThenBy(entry => entry.Id)
            .Select(entry => new InventorySearchRow(
                entry.Id,
                entry.Item.Name,
                entry.Category.Name,
                entry.Location.Name,
                entry.CurrentQuantity,
                entry.IsCommodity,
                entry.BestByDate,
                entry.IsMenuItem,
                entry.LastUpdatedAtUtc))
            .ToArrayAsync(cancellationToken);

        return new InventorySearchResult(normalizedTerm, rows);
    }
}
