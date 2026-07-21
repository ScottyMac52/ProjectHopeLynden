using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ProjectHopeLynden.Application.IncomingOrders;
using ProjectHopeLynden.Application.Inventory;
using ProjectHopeLynden.Application.Reporting;
using ProjectHopeLynden.Domain.IncomingOrders;
using ProjectHopeLynden.Web.Features;
using ProjectHopeLynden.Web.Pages.IncomingOrders;
using ProjectHopeLynden.Web.Pages.Inventory;
using ProjectHopeLynden.Web.Pages.Reports;
using ProjectHopeLynden.Web.Reporting;
using IncomingOrdersIndexModel = ProjectHopeLynden.Web.Pages.IncomingOrders.IndexModel;
using InventoryIndexModel = ProjectHopeLynden.Web.Pages.Inventory.IndexModel;

namespace ProjectHopeLynden.Web.Tests.Pages.Reports;

public sealed class ReportPdfPageModelTests
{
    private static readonly ReportPdfFile ExpectedPdf = new([37, 80, 68, 70], "report-2026-07-21.pdf");

    [Fact]
    public async Task CommodityPdf_UsesCurrentCommodityReportAndReturnsInlinePdf()
    {
        var report = new CommodityReportView(DateTime.UtcNow, []);
        var pdfService = new StubReportPdfService();
        var model = new CommodityReportModel(new StubCommodityService(report), pdfService);

        var result = await model.OnGetPdfAsync();

        Assert.Same(report, pdfService.CommodityReport);
        AssertInlinePdf(result);
    }

    [Fact]
    public async Task TrendsPdf_PreservesFiltersAndResolvedCategoryName()
    {
        var pdfService = new StubReportPdfService();
        var model = new TrendsModel(
            new StubTrendService(),
            new StubQueryService([new InventoryCategoryListItem(7, "Canned Vegetables")]),
            pdfService)
        {
            GroupBy = "category",
            ItemName = "Beans",
            CategoryId = 7,
            InventoryType = "commodity",
        };

        var result = await model.OnGetPdfAsync();

        Assert.Equal(InventoryTrendGrouping.Category, pdfService.TrendsReport?.Request.Grouping);
        Assert.Equal("Beans", pdfService.TrendsReport?.Request.ItemName);
        Assert.Equal(true, pdfService.TrendsReport?.Request.IsCommodity);
        Assert.Equal("Canned Vegetables", pdfService.TrendsCategoryName);
        AssertInlinePdf(result);
    }

    [Fact]
    public async Task SpreadsheetPdf_LoadsEveryCategory()
    {
        var inventory = new CategoryInventoryView(2, "Canned Vegetables", []);
        var pdfService = new StubReportPdfService();
        var model = new InventoryIndexModel(
            new StubQueryService([new InventoryCategoryListItem(2, "Canned Vegetables")], inventory),
            new StubQuantityService(),
            reportPdfService: pdfService);

        var result = await model.OnGetPdfAsync();

        Assert.Same(inventory, Assert.Single(pdfService.Spreadsheet!));
        AssertInlinePdf(result);
    }

    [Fact]
    public async Task SearchPdf_PreservesNormalizedSearchTerm()
    {
        var searchResult = new InventorySearchResult("beans", []);
        var pdfService = new StubReportPdfService();
        var searchService = new StubSearchService(searchResult);
        var model = new SearchModel(searchService, pdfService) { SearchTerm = "  beans " };

        var result = await model.OnGetPdfAsync();

        Assert.Same(searchResult, pdfService.SearchResult);
        Assert.Equal("  beans ", searchService.RequestedTerm);
        AssertInlinePdf(result);
    }

    [Fact]
    public async Task SearchPdf_RejectsBlankSearchWithoutGeneratingPdf()
    {
        var pdfService = new StubReportPdfService();
        var model = new SearchModel(new StubSearchService(new InventorySearchResult(string.Empty, [])), pdfService);

        var result = await model.OnGetPdfAsync();

        Assert.IsType<BadRequestResult>(result);
        Assert.Null(pdfService.SearchResult);
    }

    [Fact]
    public async Task HistoryPdf_UsesSelectedEntryHistory()
    {
        var history = new InventoryEntryHistoryView(14, 2, "Green Beans", "Canned Vegetables", "Shelf", true, 24, []);
        var pdfService = new StubReportPdfService();
        var model = new HistoryModel(new StubHistoryService(history), pdfService) { InventoryEntryId = 14 };

        var result = await model.OnGetPdfAsync();

        Assert.Same(history, pdfService.History);
        AssertInlinePdf(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(404)]
    public async Task HistoryPdf_ReturnsNotFoundForInvalidOrMissingEntry(int inventoryEntryId)
    {
        var pdfService = new StubReportPdfService();
        var model = new HistoryModel(new StubHistoryService(null), pdfService) { InventoryEntryId = inventoryEntryId };

        var result = await model.OnGetPdfAsync();

        Assert.IsType<NotFoundResult>(result);
        Assert.Null(pdfService.History);
    }

    [Fact]
    public async Task IncomingOrdersPdf_UsesCurrentOrdersWhenEnabled()
    {
        var order = new IncomingOrderListItem(7, DateTime.Today, "Food Lifeline", IncomingOrderStatus.Pending,
            DateTime.Today, IncomingOrderDateState.DueToday, "Milk", 1);
        var pdfService = new StubReportPdfService();
        var model = new IncomingOrdersIndexModel(
            new StubIncomingOrderService([order]),
            Options.Create(new ProjectHopeFeatureOptions { IncomingOrders = true }),
            pdfService);

        var result = await model.OnGetPdfAsync();

        Assert.Same(order, Assert.Single(pdfService.IncomingOrders!));
        AssertInlinePdf(result);
    }

    [Fact]
    public async Task IncomingOrdersPdf_ReturnsNotFoundWhenFeatureDisabled()
    {
        var pdfService = new StubReportPdfService();
        var model = new IncomingOrdersIndexModel(
            new StubIncomingOrderService([]),
            Options.Create(new ProjectHopeFeatureOptions()),
            pdfService);

        var result = await model.OnGetPdfAsync();

        Assert.IsType<NotFoundResult>(result);
        Assert.Null(pdfService.IncomingOrders);
    }

    private static void AssertInlinePdf(IActionResult result)
    {
        var pdf = Assert.IsType<InlinePdfResult>(result);
        Assert.Equal("application/pdf", pdf.ContentType);
        Assert.Equal(ExpectedPdf.Content, pdf.FileContents);
        Assert.Equal(ExpectedPdf.FileName, pdf.InlineFileName);
    }

    private sealed class StubReportPdfService : IReportPdfService
    {
        public CommodityReportView? CommodityReport { get; private set; }
        public InventoryTrendReportView? TrendsReport { get; private set; }
        public string? TrendsCategoryName { get; private set; }
        public IReadOnlyList<CategoryInventoryView>? Spreadsheet { get; private set; }
        public InventorySearchResult? SearchResult { get; private set; }
        public InventoryEntryHistoryView? History { get; private set; }
        public IReadOnlyList<IncomingOrderListItem>? IncomingOrders { get; private set; }

        public ReportPdfFile CreateCommodityReport(CommodityReportView report)
        {
            CommodityReport = report;
            return ExpectedPdf;
        }

        public ReportPdfFile CreateTrendsReport(InventoryTrendReportView report, string? categoryName)
        {
            TrendsReport = report;
            TrendsCategoryName = categoryName;
            return ExpectedPdf;
        }

        public ReportPdfFile CreateInventorySpreadsheet(IReadOnlyList<CategoryInventoryView> inventories, DateTime generatedAtUtc)
        {
            Spreadsheet = inventories;
            return ExpectedPdf;
        }

        public ReportPdfFile CreateInventorySearch(InventorySearchResult result, DateTime generatedAtUtc)
        {
            SearchResult = result;
            return ExpectedPdf;
        }

        public ReportPdfFile CreateInventoryHistory(InventoryEntryHistoryView history, DateTime generatedAtUtc)
        {
            History = history;
            return ExpectedPdf;
        }

        public ReportPdfFile CreateIncomingOrders(IReadOnlyList<IncomingOrderListItem> orders, DateTime operatingDate, DateTime generatedAtUtc)
        {
            IncomingOrders = orders;
            return ExpectedPdf;
        }
    }

    private sealed class StubCommodityService(CommodityReportView report) : IInventoryCommodityService
    {
        public Task<IReadOnlyList<CommodityInventoryEntryListItem>> GetCommodityInventoryAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CommodityInventoryEntryListItem>>([]);
        public Task<InventoryItemTotalView?> GetItemTotalAsync(string itemName, CancellationToken cancellationToken = default) =>
            Task.FromResult<InventoryItemTotalView?>(null);
        public Task<CommodityReportView> GetCommodityReportAsync(DateTime generatedAtUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(report);
    }

    private sealed class StubTrendService : IInventoryTrendReportService
    {
        public Task<InventoryTrendReportView> GetTrendReportAsync(InventoryTrendReportRequest request, DateTime generatedAtUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new InventoryTrendReportView(generatedAtUtc, request, [], []));
    }

    private sealed class StubQueryService(
        IReadOnlyList<InventoryCategoryListItem> categories,
        CategoryInventoryView? inventory = null) : IInventoryQueryService
    {
        public Task<IReadOnlyList<InventoryCategoryListItem>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(categories);
        public Task<CategoryInventoryView?> GetInventoryForCategoryAsync(int categoryId, CancellationToken cancellationToken = default) =>
            Task.FromResult(inventory);
    }

    private sealed class StubQuantityService : IInventoryQuantityService
    {
        public Task<InventoryQuantityUpdateResult> UpdateCurrentQuantityAsync(int inventoryEntryId, double quantity,
            DateTime countedAtUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(new InventoryQuantityUpdateResult(true, null));
    }

    private sealed class StubSearchService(InventorySearchResult result) : IInventorySearchService
    {
        public string? RequestedTerm { get; private set; }
        public Task<InventorySearchResult> SearchAsync(string? searchTerm, CancellationToken cancellationToken = default)
        {
            RequestedTerm = searchTerm;
            return Task.FromResult(result);
        }
    }

    private sealed class StubHistoryService(InventoryEntryHistoryView? history) : IInventoryHistoryService
    {
        public Task<InventoryEntryHistoryView?> GetHistoryForEntryAsync(int inventoryEntryId,
            CancellationToken cancellationToken = default) => Task.FromResult(history);
    }

    private sealed class StubIncomingOrderService(IReadOnlyList<IncomingOrderListItem> orders) : IIncomingOrderService
    {
        public Task<IReadOnlyList<IncomingOrderListItem>> GetOrdersAsync(DateTime operatingDate,
            CancellationToken cancellationToken = default) => Task.FromResult(orders);
        public Task<IReadOnlyList<IncomingOrderInventoryOption>> GetInventoryOptionsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<IncomingOrderInventoryOption>>([]);
        public Task<IncomingOrderEditView?> GetOrderAsync(int orderId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IncomingOrderEditView?>(null);
        public Task<IncomingOrderSaveResult> SaveOrderAsync(int? orderId, IncomingOrderSaveRequest request,
            DateTime createdAtUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(new IncomingOrderSaveResult(true, null, orderId));
        public Task<IncomingOrderReceiptResult> ReceiveOrderAsync(int orderId,
            IReadOnlyList<IncomingOrderReceiptLineRequest> lines, DateTime receivedAtUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new IncomingOrderReceiptResult(true, null));
    }
}
