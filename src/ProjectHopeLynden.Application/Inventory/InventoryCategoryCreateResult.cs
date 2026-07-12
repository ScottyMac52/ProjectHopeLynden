namespace ProjectHopeLynden.Application.Inventory;

public sealed record InventoryCategoryCreateResult(
    bool Succeeded,
    int? CategoryId,
    string? ErrorMessage);
