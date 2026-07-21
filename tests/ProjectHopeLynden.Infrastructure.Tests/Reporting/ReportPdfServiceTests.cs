using System.Text;
using PdfSharp.Pdf.IO;
using ProjectHopeLynden.Application.IncomingOrders;
using ProjectHopeLynden.Application.Inventory;
using ProjectHopeLynden.Domain.IncomingOrders;
using ProjectHopeLynden.Infrastructure.Reporting;

namespace ProjectHopeLynden.Infrastructure.Tests.Reporting;

public sealed class ReportPdfServiceTests
{
    private static readonly DateTime GeneratedAtUtc =
        new(2026, 7, 21, 12, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void CreateCommodityReport_IncludesSummaryRowsAndTotal()
    {
        var renderer = new CapturingRenderer();
        var service = new ReportPdfService(renderer);
        var report = new CommodityReportView(GeneratedAtUtc,
        [
            new CommodityReportRow(14, "Green Beans", "Canned Vegetables", "Shelf", 24,
                new DateTime(2028, 4, 1), GeneratedAtUtc.AddHours(-1)),
        ]);

        var file = service.CreateCommodityReport(report);

        Assert.Equal("project-hope-commodity-report-2026-07-21.pdf", file.FileName);
        Assert.Equal("Commodity Report", renderer.Definition?.Title);
        Assert.Contains(renderer.Definition!.Details, detail => detail.Label == "Total quantity" && detail.Value == "24");
        Assert.Contains(renderer.Definition.Sections.Single().Table!.Rows, row => row.Contains("Green Beans"));
        Assert.Contains("Total", renderer.Definition.Sections.Single().Table!.FooterRow!);
    }

    [Fact]
    public void CreateTrendsReport_PreservesFiltersAndBothDatasets()
    {
        var renderer = new CapturingRenderer();
        var service = new ReportPdfService(renderer);
        var request = new InventoryTrendReportRequest(InventoryTrendGrouping.Category, "Beans", 7, true);
        var report = new InventoryTrendReportView(GeneratedAtUtc, request,
            [new InventoryTrendReportPoint("Canned Vegetables", GeneratedAtUtc.Date, 24, 2)],
            [new InventoryTrendActivityPoint("Canned Vegetables", GeneratedAtUtc.Date, 6, 1)]);

        service.CreateTrendsReport(report, "Canned Vegetables");

        Assert.Contains(renderer.Definition!.Details, detail => detail.Label == "Grouping" && detail.Value == "Category");
        Assert.Contains(renderer.Definition.Details, detail => detail.Label == "Item contains" && detail.Value == "Beans");
        Assert.Contains(renderer.Definition.Details, detail => detail.Label == "Category" && detail.Value == "Canned Vegetables");
        Assert.Contains(renderer.Definition.Details, detail => detail.Label == "Inventory type" && detail.Value == "Commodity");
        Assert.Equal(["Inventory Snapshot", "Count Activity"], renderer.Definition.Sections.Select(section => section.Heading));
    }

    [Fact]
    public void CreateInventorySpreadsheet_UsesOneSectionPerCategory()
    {
        var renderer = new CapturingRenderer();
        var service = new ReportPdfService(renderer);
        var entry = new InventoryEntryListItem(14, "Green Beans", "Shelf", 24, true,
            new DateTime(2028, 4, 1), false, GeneratedAtUtc);

        service.CreateInventorySpreadsheet(
            [new CategoryInventoryView(2, "Canned Vegetables", [entry])], GeneratedAtUtc);

        Assert.True(renderer.Definition!.Landscape);
        var section = Assert.Single(renderer.Definition.Sections);
        Assert.Equal("Canned Vegetables", section.Heading);
        Assert.Contains(section.Table!.Rows, row => row.Contains("Green Beans") && row.Contains("Yes"));
    }

    [Fact]
    public void CreateInventorySearch_IncludesNormalizedTermAndResults()
    {
        var renderer = new CapturingRenderer();
        var service = new ReportPdfService(renderer);
        var result = new InventorySearchResult("beans",
        [
            new InventorySearchRow(14, "Green Beans", "Canned Vegetables", "Shelf", 24,
                true, null, false, GeneratedAtUtc),
        ]);

        service.CreateInventorySearch(result, GeneratedAtUtc);

        Assert.Contains(renderer.Definition!.Details, detail => detail.Label == "Search term" && detail.Value == "beans");
        Assert.Contains(renderer.Definition.Sections.Single().Table!.Rows, row => row.Contains("Green Beans"));
    }

    [Fact]
    public void CreateInventoryHistory_IncludesRowContextAndChronologicalCounts()
    {
        var renderer = new CapturingRenderer();
        var service = new ReportPdfService(renderer);
        var history = new InventoryEntryHistoryView(14, 2, "Green Beans", "Canned Vegetables", "Shelf", true, 24,
        [
            new InventoryCountHistoryListItem(GeneratedAtUtc.AddDays(-1), 18, 24, 6),
        ]);

        var file = service.CreateInventoryHistory(history, GeneratedAtUtc);

        Assert.Equal("project-hope-inventory-history-green-beans-2026-07-21.pdf", file.FileName);
        Assert.Contains(renderer.Definition!.Details, detail => detail.Label == "Current quantity" && detail.Value == "24");
        Assert.Contains(renderer.Definition.Sections.Single().Table!.Rows, row => row.Contains("+6"));
    }

    [Fact]
    public void CreateIncomingOrders_UsesDisplayedDateStateAndStatus()
    {
        var renderer = new CapturingRenderer();
        var service = new ReportPdfService(renderer);
        var order = new IncomingOrderListItem(7, GeneratedAtUtc.Date, "Food Lifeline",
            IncomingOrderStatus.Pending, GeneratedAtUtc.Date, IncomingOrderDateState.DueToday, "Milk", 1);

        service.CreateIncomingOrders([order], GeneratedAtUtc.Date, GeneratedAtUtc);

        Assert.Contains(renderer.Definition!.Sections.Single().Table!.Rows,
            row => row.Contains("Food Lifeline") && row.Contains("Due Today"));
    }

    [Fact]
    public void Renderer_CreatesMultiPageLandscapePdfWithRepeatedRows()
    {
        var rows = Enumerable.Range(1, 160)
            .Select(index => (IReadOnlyList<string>)[index.ToString(), $"Inventory item {index}"])
            .ToArray();
        var definition = new ReportPdfDefinition(
            "Inventory Spreadsheet", GeneratedAtUtc, true,
            [new ReportPdfDetail("Rows", rows.Length.ToString())],
            [new ReportPdfSection("Inventory", null, new ReportPdfTable(["#", "Item"], [1.0, 5.0], rows))]);

        var bytes = new MigraDocReportPdfRenderer().Render(definition);

        Assert.Equal("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
        using var stream = new MemoryStream(bytes);
        using var pdf = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        Assert.True(pdf.PageCount > 1);
        Assert.True(pdf.Pages[0].Width.Point > pdf.Pages[0].Height.Point);
    }

    private sealed class CapturingRenderer : IReportPdfRenderer
    {
        public ReportPdfDefinition? Definition { get; private set; }

        public byte[] Render(ReportPdfDefinition definition)
        {
            Definition = definition;
            return "%PDF-test"u8.ToArray();
        }
    }
}
