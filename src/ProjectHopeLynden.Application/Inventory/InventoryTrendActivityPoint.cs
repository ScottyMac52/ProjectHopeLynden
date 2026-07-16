namespace ProjectHopeLynden.Application.Inventory;

public sealed record InventoryTrendActivityPoint(
    string GroupName,
    DateTime CountedOnUtc,
    double? NetQuantityChange,
    int CountEventCount);
