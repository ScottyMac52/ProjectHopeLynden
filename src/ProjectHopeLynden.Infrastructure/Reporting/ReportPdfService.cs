using System.Globalization;
using System.Text;
using ProjectHopeLynden.Application.IncomingOrders;
using ProjectHopeLynden.Application.Inventory;
using ProjectHopeLynden.Application.Reporting;
using ProjectHopeLynden.Domain.IncomingOrders;

namespace ProjectHopeLynden.Infrastructure.Reporting;

public sealed class ReportPdfService(IReportPdfRenderer renderer) : IReportPdfService
{
    public ReportPdfFile CreateCommodityReport(CommodityReportView report)
    {
        var rows = report.Rows.Select(row => Row(
            row.ItemName,
            row.CategoryName,
            Quantity(row.Quantity),
            row.LocationName,
            Date(row.BestByDate),
            DateTimeUtc(row.QuantityAsOfUtc))).ToArray();
        var table = new ReportPdfTable(
            ["Item", "Category", "Quantity", "Location", "BB Date", "Quantity As Of"],
            [1.75, 1.55, 0.75, 1.35, 1.0, 1.45],
            rows,
            Row("Total", string.Empty, Quantity(report.TotalQuantity), string.Empty, string.Empty, string.Empty));
        var definition = new ReportPdfDefinition(
            "Commodity Report", report.GeneratedAtUtc, true,
            [new("Commodity rows", report.Rows.Count.ToString(CultureInfo.InvariantCulture)),
             new("Total quantity", Quantity(report.TotalQuantity))],
            [new("Commodity Inventory", "No inventory rows are currently marked as Commodity.", table)]);
        return Render(definition, "commodity-report");
    }

    public ReportPdfFile CreateTrendsReport(InventoryTrendReportView report, string? categoryName)
    {
        var grouping = report.Request.Grouping == InventoryTrendGrouping.Category ? "Category" : "Item";
        var details = new List<ReportPdfDetail>
        {
            new("Grouping", grouping),
            new("Inventory type", report.Request.IsCommodity switch
            {
                true => "Commodity",
                false => "Non-Commodity",
                null => "All inventory",
            }),
        };
        if (!string.IsNullOrWhiteSpace(report.Request.ItemName))
        {
            details.Add(new("Item contains", report.Request.ItemName.Trim()));
        }
        if (report.Request.CategoryId.HasValue)
        {
            details.Add(new("Category", categoryName ?? $"ID {report.Request.CategoryId.Value}"));
        }

        var snapshotRows = report.InventorySnapshots.Select(point => Row(
            Date(point.CountedOnUtc), point.GroupName, Quantity(point.EndOfDayQuantity),
            point.InventoryEntryCount.ToString(CultureInfo.InvariantCulture))).ToArray();
        var activityRows = report.CountActivity.Select(point => Row(
            Date(point.CountedOnUtc), point.GroupName, SignedQuantity(point.NetQuantityChange),
            point.CountEventCount.ToString(CultureInfo.InvariantCulture))).ToArray();

        var definition = new ReportPdfDefinition(
            "Inventory Trends", report.GeneratedAtUtc, false, details,
            [
                new("Inventory Snapshot", "No end-of-day inventory snapshots match these filters.",
                    new(["Date", grouping, "End-of-Day Quantity", "Entries Included"], [1.25, 2.45, 1.45, 1.2], snapshotRows)),
                new("Count Activity", "No operational count activity matches these filters yet.",
                    new(["Date", grouping, "Net Change", "Count Events"], [1.25, 2.45, 1.45, 1.2], activityRows)),
            ]);
        return Render(definition, "inventory-trends");
    }

    public ReportPdfFile CreateInventorySpreadsheet(
        IReadOnlyList<CategoryInventoryView> inventories,
        DateTime generatedAtUtc)
    {
        var sections = inventories.Select(inventory =>
        {
            var rows = inventory.Entries.Select(entry => Row(
                entry.ItemName, entry.LocationName, Quantity(entry.CurrentQuantity), YesNo(entry.IsCommodity),
                Date(entry.BestByDate), YesNo(entry.IsMenuItem), DateTimeUtc(entry.LastUpdatedAtUtc))).ToArray();
            return new ReportPdfSection(inventory.CategoryName,
                "No inventory entries are recorded for this category.",
                new(["Item", "Location", "Quantity", "Commodity", "BB Date", "Menu Item", "Last Updated"],
                    [1.8, 1.35, 0.7, 0.8, 0.95, 0.75, 1.35], rows));
        }).ToArray();
        if (sections.Length == 0)
        {
            sections = [new("Inventory", "No inventory categories have been added yet.", null)];
        }

        var entryCount = inventories.Sum(inventory => inventory.Entries.Count);
        var definition = new ReportPdfDefinition(
            "Inventory Spreadsheet", generatedAtUtc, true,
            [new("Categories", inventories.Count.ToString(CultureInfo.InvariantCulture)),
             new("Inventory rows", entryCount.ToString(CultureInfo.InvariantCulture))],
            sections);
        return Render(definition, "inventory-spreadsheet");
    }

    public ReportPdfFile CreateInventorySearch(InventorySearchResult result, DateTime generatedAtUtc)
    {
        var rows = result.Rows.Select(row => Row(
            row.ItemName, row.CategoryName, row.LocationName, Quantity(row.CurrentQuantity),
            row.IsCommodity ? "Commodity" : "Non-Commodity", Date(row.BestByDate), YesNo(row.IsMenuItem),
            DateTimeUtc(row.LastUpdatedAtUtc))).ToArray();
        var definition = new ReportPdfDefinition(
            "Inventory Search Results", generatedAtUtc, true,
            [new("Search term", result.SearchTerm),
             new("Results", result.Rows.Count.ToString(CultureInfo.InvariantCulture))],
            [new("Matching Inventory", $"No item names contain '{result.SearchTerm}'.",
                new(["Item", "Category", "Location", "Quantity", "Inventory Type", "BB Date", "Menu Item", "Last Updated"],
                    [1.45, 1.35, 1.15, 0.65, 1.05, 0.85, 0.65, 1.3], rows))]);
        return Render(definition, "inventory-search");
    }

    public ReportPdfFile CreateInventoryHistory(InventoryEntryHistoryView history, DateTime generatedAtUtc)
    {
        var rows = history.Records.Select(record => Row(
            DateTimeUtc(record.CountedAtUtc), Quantity(record.PreviousQuantity), Quantity(record.CountedQuantity),
            SignedQuantity(record.QuantityChange))).ToArray();
        var definition = new ReportPdfDefinition(
            $"Inventory History - {history.ItemName}", generatedAtUtc, false,
            [new("Category", history.CategoryName), new("Location", history.LocationName),
             new("Commodity", YesNo(history.IsCommodity)), new("Current quantity", Quantity(history.CurrentQuantity))],
            [new("Stored Inventory Counts", "No historical count records have been stored for this inventory entry yet.",
                new(["Counted At", "Previous Quantity", "Counted Quantity", "Change"],
                    [1.8, 1.45, 1.45, 1.2], rows))]);
        return Render(definition, $"inventory-history-{Slug(history.ItemName)}");
    }

    public ReportPdfFile CreateIncomingOrders(
        IReadOnlyList<IncomingOrderListItem> orders,
        DateTime operatingDate,
        DateTime generatedAtUtc)
    {
        var rows = orders.Select(order => Row(
            order.Vendor, Date(order.OrderDate), Date(order.ExpectedDate), DisplayState(order),
            order.ProductSummary ?? string.Empty, order.LineCount.ToString(CultureInfo.InvariantCulture))).ToArray();
        var definition = new ReportPdfDefinition(
            "Incoming Orders", generatedAtUtc, true,
            [new("Operating date", Date(operatingDate)),
             new("Orders", orders.Count.ToString(CultureInfo.InvariantCulture))],
            [new("Expected Deliveries", "No incoming orders are currently recorded.",
                new(["Vendor", "Ordered", "Expected", "Status", "Products", "Lines"],
                    [1.55, 0.95, 0.95, 1.05, 3.1, 0.5], rows))]);
        return Render(definition, "incoming-orders");
    }

    private ReportPdfFile Render(ReportPdfDefinition definition, string reportSlug) =>
        new(renderer.Render(definition), $"project-hope-{reportSlug}-{definition.GeneratedAtUtc:yyyy-MM-dd}.pdf");

    private static IReadOnlyList<string> Row(params string[] values) => values;

    private static string Quantity(double? quantity) =>
        quantity?.ToString("0.##", CultureInfo.InvariantCulture) ?? string.Empty;

    private static string SignedQuantity(double? quantity) =>
        quantity?.ToString("+0.##;-0.##;0", CultureInfo.InvariantCulture) ?? string.Empty;

    private static string Date(DateTime? date) => date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;

    private static string DateTimeUtc(DateTime date) => date.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture);

    private static string YesNo(bool value) => value ? "Yes" : "No";

    private static string DisplayState(IncomingOrderListItem order) => order.DateState switch
    {
        IncomingOrderDateState.DueToday => "Due Today",
        IncomingOrderDateState.Overdue => "Overdue",
        IncomingOrderDateState.Upcoming => "Upcoming",
        _ => order.Status.ToString(),
    };

    private static string Slug(string value)
    {
        var builder = new StringBuilder(value.Length);
        var lastWasSeparator = false;
        foreach (var character in value.Trim().ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                builder.Append(character);
                lastWasSeparator = false;
            }
            else if (!lastWasSeparator && builder.Length > 0)
            {
                builder.Append('-');
                lastWasSeparator = true;
            }
        }
        return builder.ToString().Trim('-');
    }
}
