namespace ProjectHopeLynden.Application.Inventory;

/// <summary>
/// Summarizes operational count updates that have a known previous quantity.
/// Imported baseline counts are intentionally excluded.
/// </summary>
public sealed record InventoryTrendActivityPoint(
    string GroupName,
    DateTime CountedOnUtc,
    double? NetQuantityChange,
    int CountEventCount);
