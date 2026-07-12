using Microsoft.EntityFrameworkCore;
using ProjectHopeLynden.Application.Inventory;
using ProjectHopeLynden.Infrastructure.Persistence;

namespace ProjectHopeLynden.Infrastructure.Inventory;

public sealed class InventoryCommodityService(ProjectHopeDbContext context) : IInventoryCommodityService
{
    public async Task<IReadOnlyList<CommodityInventoryEntryListItem>> GetCommodityInventoryAsync(
        CancellationToken cancellationToken = default)
    {
        return await context.InventoryEntries
            .AsNoTracking()
            .Where(entry => entry.IsCommodity)
            .OrderBy(entry => entry.Item.Name)
            .ThenBy(entry => entry.Category.Name)
            .ThenBy(entry => entry.Location.Name)
            .ThenBy(entry => entry.Id)
            .Select(entry => new CommodityInventoryEntryListItem(
                entry.Id,
                entry.Item.Name,
                entry.Category.Name,
                entry.Location.Name,
                entry.CurrentQuantity,
                entry.BestByDate,
                entry.IsMenuItem,
                entry.LastUpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<InventoryItemTotalView?> GetItemTotalAsync(
        string itemName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(itemName))
        {
            return null;
        }

        var entries = await context.InventoryEntries
            .AsNoTracking()
            .Where(entry => entry.Item.Name == itemName)
            .OrderBy(entry => entry.Category.Name)
            .ThenBy(entry => entry.Location.Name)
            .ThenBy(entry => entry.IsCommodity)
            .ThenBy(entry => entry.Id)
            .Select(entry => new InventoryItemTotalEntry(
                entry.Id,
                entry.Item.Name,
                entry.Category.Name,
                entry.Location.Name,
                entry.CurrentQuantity,
                entry.IsCommodity,
                entry.LastUpdatedAtUtc))
            .ToListAsync(cancellationToken);

        if (entries.Count == 0)
        {
            return null;
        }

        var commodityQuantity = entries
            .Where(entry => entry.IsCommodity)
            .Sum(entry => entry.CurrentQuantity);
        var nonCommodityQuantity = entries
            .Where(entry => !entry.IsCommodity)
            .Sum(entry => entry.CurrentQuantity);

        return new InventoryItemTotalView(
            entries[0].ItemName,
            commodityQuantity + nonCommodityQuantity,
            commodityQuantity,
            nonCommodityQuantity,
            entries);
    }

    public async Task<CommodityReportView> GetCommodityReportAsync(
        DateTime generatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var entries = await GetCommodityInventoryAsync(cancellationToken);
        var rows = entries
            .Select(entry => new CommodityReportRow(
                entry.InventoryEntryId,
                entry.ItemName,
                entry.CategoryName,
                entry.LocationName,
                entry.CurrentQuantity,
                entry.BestByDate,
                entry.LastUpdatedAtUtc))
            .ToList();

        return new CommodityReportView(generatedAtUtc, rows);
    }
}
