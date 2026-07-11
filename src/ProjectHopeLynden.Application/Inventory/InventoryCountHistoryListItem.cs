namespace ProjectHopeLynden.Application.Inventory;

public sealed record InventoryCountHistoryListItem(
    DateTime CountedAtUtc,
    double? PreviousQuantity,
    double CountedQuantity,
    double? QuantityChange);
