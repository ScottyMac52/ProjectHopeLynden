namespace ProjectHopeLynden.Domain.Inventory;

public sealed class Location
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public List<InventoryEntry> InventoryEntries { get; set; } = [];
}
