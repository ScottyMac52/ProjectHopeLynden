namespace ProjectHopeLynden.Application.Inventory;

public interface IInventoryTrendReportService
{
    Task<InventoryTrendReportView> GetTrendReportAsync(
        InventoryTrendReportRequest request,
        DateTime generatedAtUtc,
        CancellationToken cancellationToken = default);
}
