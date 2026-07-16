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

    public int ItemIdAtCount { get; set; }

    public string ItemNameAtCount { get; set; } = string.Empty;

    public int CategoryIdAtCount { get; set; }

    public string CategoryNameAtCount { get; set; } = string.Empty;

    public int LocationIdAtCount { get; set; }

    public string LocationNameAtCount { get; set; } = string.Empty;

    public bool IsCommodityAtCount { get; set; }
}
