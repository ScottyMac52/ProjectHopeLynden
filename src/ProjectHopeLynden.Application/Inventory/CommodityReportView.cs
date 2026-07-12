namespace ProjectHopeLynden.Application.Inventory;

public sealed record CommodityReportView(
    DateTime GeneratedAtUtc,
    IReadOnlyList<CommodityReportRow> Rows)
{
    public bool HasRows => Rows.Count > 0;

    public double TotalQuantity => Rows.Sum(row => row.Quantity);
}
