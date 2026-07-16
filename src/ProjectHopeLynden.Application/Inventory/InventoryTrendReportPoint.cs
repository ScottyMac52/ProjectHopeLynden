namespace ProjectHopeLynden.Application.Inventory;

public sealed record InventoryTrendReportPoint(
    string GroupName,
    DateTime CountedOnUtc,
    double EndOfDayQuantity,
    int InventoryEntryCount);
