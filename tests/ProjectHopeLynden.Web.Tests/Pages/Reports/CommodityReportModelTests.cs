using ProjectHopeLynden.Application.Inventory;
using ProjectHopeLynden.Web.Pages.Reports;

namespace ProjectHopeLynden.Web.Tests.Pages.Reports;

public sealed class CommodityReportModelTests
{
    [Fact]
    public void PageCopy_DescribesBellinghamCommodityReport()
    {
        var model = new CommodityReportModel(new StubInventoryCommodityService(CreateReport()));

        Assert.Equal("Commodity Report", model.PageTitle);
        Assert.Contains("Bellingham Food Bank", model.Summary);
    }

    [Fact]
    public async Task OnGetAsync_LoadsCurrentCommodityReport()
    {
        var report = CreateReport();
        var service = new StubInventoryCommodityService(report);
        var model = new CommodityReportModel(service);
        var beforeUtc = DateTime.UtcNow;

        await model.OnGetAsync();

        var afterUtc = DateTime.UtcNow;
        Assert.Same(report, model.Report);
        Assert.InRange(service.RequestedGeneratedAtUtc, beforeUtc, afterUtc);
    }

    private static CommodityReportView CreateReport()
    {
        var generatedAtUtc = new DateTime(2026, 7, 12, 12, 0, 0, DateTimeKind.Utc);
        return new CommodityReportView(
            generatedAtUtc,
            [
                new CommodityReportRow(
                    InventoryEntryId: 14,
                    ItemName: "Green Beans",
                    CategoryName: "Canned Vegetables",
                    LocationName: "Shelf",
                    Quantity: 24,
                    BestByDate: new DateTime(2028, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                    QuantityAsOfUtc: generatedAtUtc.AddHours(-1)),
            ]);
    }

    private sealed class StubInventoryCommodityService(CommodityReportView report) : IInventoryCommodityService
    {
        public DateTime RequestedGeneratedAtUtc { get; private set; }

        public Task<IReadOnlyList<CommodityInventoryEntryListItem>> GetCommodityInventoryAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<CommodityInventoryEntryListItem>>([]);
        }

        public Task<InventoryItemTotalView?> GetItemTotalAsync(
            string itemName,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<InventoryItemTotalView?>(null);
        }

        public Task<CommodityReportView> GetCommodityReportAsync(
            DateTime generatedAtUtc,
            CancellationToken cancellationToken = default)
        {
            RequestedGeneratedAtUtc = generatedAtUtc;
            return Task.FromResult(report);
        }
    }
}
