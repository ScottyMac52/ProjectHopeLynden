namespace ProjectHopeLynden.Domain.Inventory;

public sealed class InventoryEntry
{
    public int Id { get; set; }

    public int ItemId { get; set; }

    public Item Item { get; set; } = null!;

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public int LocationId { get; set; }

    public Location Location { get; set; } = null!;

    public double CurrentQuantity { get; set; }

    public DateTime? BestByDate { get; set; }

    public bool IsCommodity { get; set; }

    public bool IsMenuItem { get; set; }

    public DateTime LastUpdatedAtUtc { get; set; }

    public List<InventoryCountHistory> CountHistory { get; set; } = [];
}
