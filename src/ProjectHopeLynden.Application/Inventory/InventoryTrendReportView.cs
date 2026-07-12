namespace ProjectHopeLynden.Application.Inventory;

public sealed record InventoryTrendReportView(
    DateTime GeneratedAtUtc,
    InventoryTrendReportRequest Request,
    IReadOnlyList<InventoryTrendReportPoint> Points)
{
    public bool HasPoints => Points.Count > 0;
}
