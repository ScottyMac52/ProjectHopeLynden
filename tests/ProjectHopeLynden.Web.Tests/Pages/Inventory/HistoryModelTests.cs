using ProjectHopeLynden.Application.Inventory;
using ProjectHopeLynden.Web.Pages.Inventory;

namespace ProjectHopeLynden.Web.Tests.Pages.Inventory;

public sealed class HistoryModelTests
{
    [Fact]
    public void PageTitle_ReturnsInventoryHistoryTitle()
    {
        var model = new HistoryModel(new StubInventoryHistoryService());

        Assert.Equal("Inventory Count History", model.PageTitle);
    }

    [Fact]
    public void Summary_ReturnsHistoricalCountMessage()
    {
        var model = new HistoryModel(new StubInventoryHistoryService());

        Assert.Equal("Review previous and current counts for a Project Hope inventory entry.", model.Summary);
    }

    [Fact]
    public async Task OnGetAsync_LoadsHistoryForRequestedInventoryEntry()
    {
        var history = new InventoryEntryHistoryView(
            InventoryEntryId: 14,
            CategoryId: 2,
            ItemName: "Green Beans",
            CategoryName: "Canned Vegetables",
            LocationName: "Shelf",
            IsCommodity: true,
            CurrentQuantity: 24,
            Records:
            [
                new InventoryCountHistoryListItem(
                    CountedAtUtc: new DateTime(2026, 7, 10, 9, 0, 0, DateTimeKind.Utc),
                    PreviousQuantity: 18,
                    CountedQuantity: 24,
                    QuantityChange: 6),
            ]);
        var service = new StubInventoryHistoryService(history);
        var model = new HistoryModel(service) { InventoryEntryId = 14 };

        await model.OnGetAsync();

        Assert.Same(history, model.History);
        Assert.False(model.EntryWasNotFound);
        Assert.Equal(14, service.RequestedInventoryEntryId);
    }

    [Fact]
    public async Task OnGetAsync_SetsNotFoundWhenInventoryEntryDoesNotExist()
    {
        var service = new StubInventoryHistoryService(history: null);
        var model = new HistoryModel(service) { InventoryEntryId = 404 };

        await model.OnGetAsync();

        Assert.Null(model.History);
        Assert.True(model.EntryWasNotFound);
        Assert.Equal(404, service.RequestedInventoryEntryId);
    }

    [Fact]
    public async Task OnGetAsync_SetsNotFoundWhenNoInventoryEntryIsProvided()
    {
        var service = new StubInventoryHistoryService();
        var model = new HistoryModel(service);

        await model.OnGetAsync();

        Assert.Null(model.History);
        Assert.True(model.EntryWasNotFound);
        Assert.Null(service.RequestedInventoryEntryId);
    }

    private sealed class StubInventoryHistoryService(
        InventoryEntryHistoryView? history = null) : IInventoryHistoryService
    {
        public int? RequestedInventoryEntryId { get; private set; }

        public Task<InventoryEntryHistoryView?> GetHistoryForEntryAsync(
            int inventoryEntryId,
            CancellationToken cancellationToken = default)
        {
            RequestedInventoryEntryId = inventoryEntryId;
            return Task.FromResult(history);
        }
    }
}
