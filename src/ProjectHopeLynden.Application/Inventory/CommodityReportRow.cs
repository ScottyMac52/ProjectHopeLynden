namespace ProjectHopeLynden.Application.Inventory;

public sealed record CommodityReportRow(
    int InventoryEntryId,
    string ItemName,
    string CategoryName,
    string LocationName,
    double Quantity,
    DateTime? BestByDate,
    DateTime QuantityAsOfUtc);
