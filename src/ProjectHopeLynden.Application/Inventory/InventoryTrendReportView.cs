namespace ProjectHopeLynden.Application.Inventory;

public sealed record InventoryTrendReportView(
    DateTime GeneratedAtUtc,
    InventoryTrendReportRequest Request,
    IReadOnlyList<InventoryTrendReportPoint> InventorySnapshots,
    IReadOnlyList<InventoryTrendActivityPoint> CountActivity)
{
    public bool HasInventorySnapshots => InventorySnapshots.Count > 0;

    public bool HasCountActivity => CountActivity.Count > 0;

    public bool HasData => HasInventorySnapshots || HasCountActivity;
}
