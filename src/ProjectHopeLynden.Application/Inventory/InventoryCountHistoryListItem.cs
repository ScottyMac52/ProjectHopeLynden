namespace ProjectHopeLynden.Application.Inventory;

public sealed record InventoryCountHistoryListItem(
    DateTime CountedAtUtc,
    int? PreviousQuantity,
    int CountedQuantity,
    int? QuantityChange);
