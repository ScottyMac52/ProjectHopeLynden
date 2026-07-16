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
        var normalizedItemName = NormalizeItemName(request.ItemName);
        var records = await context.InventoryCountHistory
            .AsNoTracking()
            .Select(record => new TrendRecord(
                record.Id,
                record.InventoryEntryId,
                record.ItemIdAtCount,
                record.ItemNameAtCount,
                record.CategoryIdAtCount,
                record.CategoryNameAtCount,
                record.LocationIdAtCount,
                record.LocationNameAtCount,
                record.IsCommodityAtCount,
                record.CountedAtUtc,
                record.CountedQuantity,
                record.QuantityChange))
            .ToListAsync(cancellationToken);

        var matchingRecords = ApplyFilters(records, request, normalizedItemName).ToArray();

        var inventorySnapshots = matchingRecords
            .GroupBy(record => GetGroupName(record, request.Grouping))
            .SelectMany(group => group
                .Select(record => record.CountedAtUtc.Date)
                .Distinct()
                .OrderBy(date => date)
                .Select(date => BuildInventorySnapshot(
                    records,
                    date,
                    group.Key,
                    request,
                    normalizedItemName))
                .Where(point => point is not null)
                .Select(point => point!))
            .OrderBy(point => point.CountedOnUtc)
            .ThenBy(point => point.GroupName)
            .ToArray();

        var countActivity = matchingRecords
            .Where(record => record.QuantityChange.HasValue)
            .GroupBy(record => new TrendKey(
                GetGroupName(record, request.Grouping),
                record.CountedAtUtc.Date))
            .Select(group => new InventoryTrendActivityPoint(
                group.Key.GroupName,
                group.Key.CountedOnUtc,
                group.Sum(record => record.QuantityChange!.Value),
                group.Count()))
            .OrderBy(point => point.CountedOnUtc)
            .ThenBy(point => point.GroupName)
            .ToArray();

        return new InventoryTrendReportView(
            generatedAtUtc,
            request,
            inventorySnapshots,
            countActivity);
    }

    private static InventoryTrendReportPoint? BuildInventorySnapshot(
        IReadOnlyList<TrendRecord> records,
        DateTime countedOnUtc,
        string groupName,
        InventoryTrendReportRequest request,
        string? normalizedItemName)
    {
        var latestRecords = records
            .Where(record => record.CountedAtUtc.Date <= countedOnUtc)
            .GroupBy(record => record.InventoryEntryId)
            .Select(group => group
                .OrderByDescending(record => record.CountedAtUtc)
                .ThenByDescending(record => record.Id)
                .First());

        var groupRecords = ApplyFilters(latestRecords, request, normalizedItemName)
            .Where(record => string.Equals(
                GetGroupName(record, request.Grouping),
                groupName,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (groupRecords.Length == 0)
        {
            return null;
        }

        return new InventoryTrendReportPoint(
            groupName,
            countedOnUtc,
            groupRecords.Sum(record => record.CountedQuantity),
            groupRecords.Length);
    }

    private static IEnumerable<TrendRecord> ApplyFilters(
        IEnumerable<TrendRecord> records,
        InventoryTrendReportRequest request,
        string? normalizedItemName)
    {
        if (normalizedItemName is not null)
        {
            records = records.Where(record => string.Equals(
                record.ItemName,
                normalizedItemName,
                StringComparison.OrdinalIgnoreCase));
        }

        if (request.CategoryId.HasValue)
        {
            records = records.Where(record => record.CategoryId == request.CategoryId.Value);
        }

        if (request.IsCommodity.HasValue)
        {
            records = records.Where(record => record.IsCommodity == request.IsCommodity.Value);
        }

        return records;
    }

    private static string GetGroupName(TrendRecord record, InventoryTrendGrouping grouping)
    {
        return grouping == InventoryTrendGrouping.Item
            ? record.ItemName
            : record.CategoryName;
    }

    private static string? NormalizeItemName(string? itemName)
    {
        var trimmedItemName = itemName?.Trim();
        return string.IsNullOrWhiteSpace(trimmedItemName) ? null : trimmedItemName;
    }

    private sealed record TrendRecord(
        int Id,
        int InventoryEntryId,
        int ItemId,
        string ItemName,
        int CategoryId,
        string CategoryName,
        int LocationId,
        string LocationName,
        bool IsCommodity,
        DateTime CountedAtUtc,
        double CountedQuantity,
        double? QuantityChange);

    private sealed record TrendKey(string GroupName, DateTime CountedOnUtc);
}
