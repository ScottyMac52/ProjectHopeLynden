using ProjectHopeLynden.Application.IncomingOrders;
using ProjectHopeLynden.Application.Inventory;

namespace ProjectHopeLynden.Application.Reporting;

public interface IReportPdfService
{
    ReportPdfFile CreateCommodityReport(CommodityReportView report);

    ReportPdfFile CreateTrendsReport(InventoryTrendReportView report, string? categoryName);

    ReportPdfFile CreateInventorySpreadsheet(
        IReadOnlyList<CategoryInventoryView> inventories,
        DateTime generatedAtUtc);

    ReportPdfFile CreateInventorySearch(InventorySearchResult result, DateTime generatedAtUtc);

    ReportPdfFile CreateInventoryHistory(InventoryEntryHistoryView history, DateTime generatedAtUtc);

    ReportPdfFile CreateIncomingOrders(
        IReadOnlyList<IncomingOrderListItem> orders,
        DateTime operatingDate,
        DateTime generatedAtUtc);
}
