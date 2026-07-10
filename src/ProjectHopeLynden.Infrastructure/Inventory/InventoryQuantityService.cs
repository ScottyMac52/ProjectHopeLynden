using Microsoft.EntityFrameworkCore;
using ProjectHopeLynden.Application.Inventory;
using ProjectHopeLynden.Domain.Inventory;
using ProjectHopeLynden.Infrastructure.Persistence;

namespace ProjectHopeLynden.Infrastructure.Inventory;

public sealed class InventoryQuantityService(ProjectHopeDbContext context) : IInventoryQuantityService
{
    public async Task<InventoryQuantityUpdateResult> UpdateCurrentQuantityAsync(
        int inventoryEntryId,
        int quantity,
        DateTime countedAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (quantity < 0)
        {
            return new InventoryQuantityUpdateResult(false, "Quantity must be zero or greater.");
        }

        var entry = await context.InventoryEntries
            .SingleOrDefaultAsync(inventoryEntry => inventoryEntry.Id == inventoryEntryId, cancellationToken);

        if (entry is null)
        {
            return new InventoryQuantityUpdateResult(false, "Inventory entry was not found.");
        }

        var previousQuantity = entry.CurrentQuantity;
        entry.CurrentQuantity = quantity;
        entry.LastUpdatedAtUtc = countedAtUtc;

        context.InventoryCountHistory.Add(new InventoryCountHistory
        {
            InventoryEntryId = entry.Id,
            CountedQuantity = quantity,
            CountedAtUtc = countedAtUtc,
            PreviousQuantity = previousQuantity,
            QuantityChange = quantity - previousQuantity,
        });

        await context.SaveChangesAsync(cancellationToken);

        return new InventoryQuantityUpdateResult(true, null);
    }
}
