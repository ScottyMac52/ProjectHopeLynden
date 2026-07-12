namespace ProjectHopeLynden.Application.Inventory;

public sealed record InventoryTrendReportRequest(
    InventoryTrendGrouping Grouping,
    string? ItemName = null,
    int? CategoryId = null,
    bool? IsCommodity = null);
