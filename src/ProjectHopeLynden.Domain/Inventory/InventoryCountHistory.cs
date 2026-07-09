namespace ProjectHopeLynden.Domain.Inventory;

public sealed class InventoryCountHistory
{
    public int Id { get; set; }

    public int InventoryEntryId { get; set; }

    public InventoryEntry InventoryEntry { get; set; } = null!;

    public int CountedQuantity { get; set; }

    public DateTime CountedAtUtc { get; set; }

    public int? PreviousQuantity { get; set; }

    public int? QuantityChange { get; set; }
}
