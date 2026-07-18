using ProjectHopeLynden.Domain.Inventory;

namespace ProjectHopeLynden.Domain.IncomingOrders;

public sealed class IncomingOrderLine
{
    public int Id { get; set; }

    public int IncomingOrderId { get; set; }

    public IncomingOrder IncomingOrder { get; set; } = null!;

    public int InventoryEntryId { get; set; }

    public InventoryEntry InventoryEntry { get; set; } = null!;

    public double ExpectedQuantity { get; set; }

    public double? ReceivedQuantity { get; set; }
}
