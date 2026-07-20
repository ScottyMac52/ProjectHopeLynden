namespace ProjectHopeLynden.Application.Inventory;

public sealed record InventoryLocationCreateResult(
    bool Succeeded,
    int? LocationId,
    string? LocationName,
    string? ErrorMessage);
