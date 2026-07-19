namespace ProjectHopeLynden.Domain.IncomingOrders;

public sealed class IncomingOrder
{
    public int Id { get; set; }

    public DateTime OrderDate { get; set; }

    public string Vendor { get; set; } = string.Empty;

    public IncomingOrderStatus Status { get; set; }

    public DateTime? InvoiceDate { get; set; }

    public string? InvoiceNumber { get; set; }

    public double? InvoiceAmount { get; set; }

    public DateTime? DueDate { get; set; }

    public string? SentToPayer { get; set; }

    public string? ChargeTo { get; set; }

    public DateTime ExpectedDate { get; set; }

    public double? Weight { get; set; }

    public string? ProductSummary { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ReceivedAtUtc { get; set; }

    public List<IncomingOrderLine> Lines { get; set; } = [];
}
