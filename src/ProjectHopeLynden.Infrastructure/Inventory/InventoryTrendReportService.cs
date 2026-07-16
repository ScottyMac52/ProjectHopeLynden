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

        var reportDates = records
            .Select(record => record.CountedAtUtc.Date)
            .Distinct()
            .OrderBy(date => date)
            .ToArray();

        var inventorySnapshots = reportDates
            .SelectMany(date => BuildInventorySnapshots(
                records,
                date,
                request,
                normalizedItemName))
            .OrderBy(point => point.CountedOnUtc)
            .ThenBy(point => point.GroupName)
            .ToArray();

        var countActivity = ApplyFilters(records, request, normalizedItemName)
            .GroupBy(record => new TrendKey(
                GetGroupName(record, request.Grouping),
                record.CountedAtUtc.Date))
            .Select(group => new InventoryTrendActivityPoint(
                group.Key.GroupName,
                group.Key.CountedOnUtc,
                group.All(record => record.QuantityChange.HasValue)
                    ? group.Sum(record => record.QuantityChange!.Value)
                    : null,
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

    private static IEnumerable<InventoryTrendReportPoint> BuildInventorySnapshots(
        IReadOnlyList<TrendRecord> records,
        DateTime countedOnUtc,
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

        return ApplyFilters(latestRecords, request, normalizedItemName)
            .GroupBy(record => GetGroupName(record, request.Grouping))
            .Select(group => new InventoryTrendReportPoint(
                group.Key,
                countedOnUtc,
                group.Sum(record => record.CountedQuantity),
                group.Count()));
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
