namespace ProjectHopeLynden.Application.Inventory;

public sealed record InventoryEntryFormOptions(
    IReadOnlyList<InventoryCategoryListItem> Categories,
    IReadOnlyList<InventoryLocationListItem> Locations);
