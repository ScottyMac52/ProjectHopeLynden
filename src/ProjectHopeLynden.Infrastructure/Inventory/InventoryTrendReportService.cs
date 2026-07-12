using Microsoft.EntityFrameworkCore;
using ProjectHopeLynden.Application.Inventory;
using ProjectHopeLynden.Infrastructure.Persistence;

namespace ProjectHopeLynden.Infrastructure.Inventory;

public sealed class InventoryTrendReportService(ProjectHopeDbContext context) : IInventoryTrendReportService
{
    public async Task<InventoryTrendReportView> GetTrendReportAsync(
        InventoryTrendReportRequest request,
        DateTime generatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var query = context.InventoryCountHistory.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.ItemName))
        {
            query = query.Where(record => record.InventoryEntry.Item.Name == request.ItemName);
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(record => record.InventoryEntry.CategoryId == request.CategoryId.Value);
        }

        if (request.IsCommodity.HasValue)
        {
            query = query.Where(record => record.InventoryEntry.IsCommodity == request.IsCommodity.Value);
        }

        var records = await query
            .Select(record => new TrendRecord(
                record.InventoryEntry.Item.Name,
                record.InventoryEntry.Category.Name,
                record.CountedAtUtc,
                record.CountedQuantity,
                record.QuantityChange))
            .ToListAsync(cancellationToken);

        var points = records
            .GroupBy(record => new TrendKey(
                request.Grouping == InventoryTrendGrouping.Item
                    ? record.ItemName
                    : record.CategoryName,
                record.CountedAtUtc.Date))
            .Select(group => new InventoryTrendReportPoint(
                group.Key.GroupName,
                group.Key.CountedOnUtc,
                group.Sum(record => record.CountedQuantity),
                group.Any(record => record.QuantityChange.HasValue)
                    ? group.Sum(record => record.QuantityChange ?? 0)
                    : null,
                group.Count()))
            .OrderBy(point => point.CountedOnUtc)
            .ThenBy(point => point.GroupName)
            .ToArray();

        return new InventoryTrendReportView(generatedAtUtc, request, points);
    }

    private sealed record TrendRecord(
        string ItemName,
        string CategoryName,
        DateTime CountedAtUtc,
        double CountedQuantity,
        double? QuantityChange);

    private sealed record TrendKey(string GroupName, DateTime CountedOnUtc);
}
