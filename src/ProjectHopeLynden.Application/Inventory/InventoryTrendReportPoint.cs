namespace ProjectHopeLynden.Application.Inventory;

public sealed record InventoryTrendReportPoint(
    string GroupName,
    DateTime CountedOnUtc,
    double RecordedQuantity,
    double? NetQuantityChange,
    int RecordCount);
