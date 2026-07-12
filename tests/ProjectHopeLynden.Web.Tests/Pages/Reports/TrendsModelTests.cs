using ProjectHopeLynden.Application.Inventory;
using ProjectHopeLynden.Web.Pages.Reports;

namespace ProjectHopeLynden.Web.Tests.Pages.Reports;

public sealed class TrendsModelTests
{
    [Fact]
    public void PageCopy_DescribesHistoricalTrendFiltering()
    {
        var model = CreateModel(new StubTrendReportService());

        Assert.Equal("Inventory Trends", model.PageTitle);
        Assert.Contains("historical inventory counts", model.Summary);
        Assert.Contains("Commodity", model.Summary);
    }

    [Fact]
    public async Task OnGetAsync_LoadsDefaultItemReportAndCategories()
    {
        var trendService = new StubTrendReportService();
        var categories = new[] { new InventoryCategoryListItem(3, "Canned Vegetables") };
        var model = CreateModel(trendService, new StubInventoryQueryService(categories));

        await model.OnGetAsync();

        Assert.Equal(categories, model.Categories);
        Assert.NotNull(trendService.RequestedReport);
        Assert.Equal(InventoryTrendGrouping.Item, trendService.RequestedReport.Grouping);
        Assert.Null(trendService.RequestedReport.ItemName);
        Assert.Null(trendService.RequestedReport.CategoryId);
        Assert.Null(trendService.RequestedReport.IsCommodity);
        Assert.Same(trendService.ReturnedReport, model.Report);
    }

    [Fact]
    public async Task OnGetAsync_BuildsCategoryCommodityFilterRequest()
    {
        var trendService = new StubTrendReportService();
        var model = CreateModel(trendService);
        model.GroupBy = "category";
        model.ItemName = "Green Beans";
        model.CategoryId = 7;
        model.InventoryType = "commodity";

        await model.OnGetAsync();

        Assert.NotNull(trendService.RequestedReport);
        Assert.Equal(InventoryTrendGrouping.Category, trendService.RequestedReport.Grouping);
        Assert.Equal("Green Beans", trendService.RequestedReport.ItemName);
        Assert.Equal(7, trendService.RequestedReport.CategoryId);
        Assert.Equal(true, trendService.RequestedReport.IsCommodity);
    }

    [Fact]
    public async Task OnGetAsync_BuildsNonCommodityFilterRequest()
    {
        var trendService = new StubTrendReportService();
        var model = CreateModel(trendService);
        model.InventoryType = "noncommodity";

        await model.OnGetAsync();

        Assert.NotNull(trendService.RequestedReport);
        Assert.Equal(false, trendService.RequestedReport.IsCommodity);
    }

    private static TrendsModel CreateModel(
        StubTrendReportService trendService,
        IInventoryQueryService? queryService = null)
    {
        return new TrendsModel(
            trendService,
            queryService ?? new StubInventoryQueryService([]));
    }

    private sealed class StubTrendReportService : IInventoryTrendReportService
    {
        public InventoryTrendReportRequest? RequestedReport { get; private set; }

        public InventoryTrendReportView? ReturnedReport { get; private set; }

        public Task<InventoryTrendReportView> GetTrendReportAsync(
            InventoryTrendReportRequest request,
            DateTime generatedAtUtc,
            CancellationToken cancellationToken = default)
        {
            RequestedReport = request;
            ReturnedReport = new InventoryTrendReportView(generatedAtUtc, request, []);
            return Task.FromResult(ReturnedReport);
        }
    }

    private sealed class StubInventoryQueryService(
        IReadOnlyList<InventoryCategoryListItem> categories) : IInventoryQueryService
    {
        public Task<IReadOnlyList<InventoryCategoryListItem>> GetCategoriesAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(categories);
        }

        public Task<CategoryInventoryView?> GetInventoryForCategoryAsync(
            int categoryId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<CategoryInventoryView?>(null);
        }
    }
}
