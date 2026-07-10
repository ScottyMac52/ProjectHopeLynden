namespace ProjectHopeLynden.Application.Inventory;

public sealed record InventoryEntrySaveRequest(
    string? ItemName,
    int? CategoryId,
    int? LocationId,
    int? CurrentQuantity,
    DateTime? BestByDate,
    bool IsCommodity,
    bool IsMenuItem);
