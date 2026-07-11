namespace ProjectHopeLynden.Domain.Inventory;

public sealed class InventoryCountHistory
{
    public int Id { get; set; }

    public int InventoryEntryId { get; set; }

    public InventoryEntry InventoryEntry { get; set; } = null!;

    public double CountedQuantity { get; set; }

    public DateTime CountedAtUtc { get; set; }

    public double? PreviousQuantity { get; set; }

    public double? QuantityChange { get; set; }
}
