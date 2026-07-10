namespace ProjectHopeLynden.Application.Inventory;

public sealed record InventoryEntrySaveResult(
    bool Succeeded,
    string? ErrorMessage,
    int? InventoryEntryId,
    int? CategoryId);
