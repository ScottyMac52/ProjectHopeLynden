namespace ProjectHopeLynden.Domain.Inventory;

public sealed class IncomingOrderLine
{
    public int Id { get; set; }

    public int InventoryEntryId { get; set; }

    public InventoryEntry InventoryEntry { get; set; } = null!;

    public double Quantity { get; set; }

    public DateOnly ExpectedDate { get; set; }

    public string? Source { get; set; }

    public string? Reference { get; set; }

    public IncomingOrderStatus Status { get; set; } = IncomingOrderStatus.Scheduled;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public DateTime? ReceivedAtUtc { get; set; }

    public DateTime? CancelledAtUtc { get; set; }
}
