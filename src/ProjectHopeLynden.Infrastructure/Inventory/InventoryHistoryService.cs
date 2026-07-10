using Microsoft.EntityFrameworkCore;
using ProjectHopeLynden.Application.Inventory;
using ProjectHopeLynden.Infrastructure.Persistence;

namespace ProjectHopeLynden.Infrastructure.Inventory;

public sealed class InventoryHistoryService(ProjectHopeDbContext context) : IInventoryHistoryService
{
    public async Task<InventoryEntryHistoryView?> GetHistoryForEntryAsync(
        int inventoryEntryId,
        CancellationToken cancellationToken = default)
    {
        var entry = await context.InventoryEntries
            .AsNoTracking()
            .Where(inventoryEntry => inventoryEntry.Id == inventoryEntryId)
            .Select(inventoryEntry => new
            {
                inventoryEntry.Id,
                inventoryEntry.CategoryId,
                ItemName = inventoryEntry.Item.Name,
                CategoryName = inventoryEntry.Category.Name,
                LocationName = inventoryEntry.Location.Name,
                inventoryEntry.IsCommodity,
                inventoryEntry.CurrentQuantity,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (entry is null)
        {
            return null;
        }

        var records = await context.InventoryCountHistory
            .AsNoTracking()
            .Where(record => record.InventoryEntryId == inventoryEntryId)
            .OrderBy(record => record.CountedAtUtc)
            .ThenBy(record => record.Id)
            .Select(record => new InventoryCountHistoryListItem(
                record.CountedAtUtc,
                record.PreviousQuantity,
                record.CountedQuantity,
                record.QuantityChange))
            .ToListAsync(cancellationToken);

        return new InventoryEntryHistoryView(
            entry.Id,
            entry.CategoryId,
            entry.ItemName,
            entry.CategoryName,
            entry.LocationName,
            entry.IsCommodity,
            entry.CurrentQuantity,
            records);
    }
}
