using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjectHopeLynden.Application.Inventory;
using ProjectHopeLynden.Web.Pages.Inventory;

namespace ProjectHopeLynden.Web.Tests.Pages.Inventory;

public sealed class IndexModelTests
{
    [Fact]
    public void PageCopy_DescribesSpreadsheetOverview()
    {
        var model = new IndexModel(new StubInventoryQueryService([], []), new StubInventoryQuantityService());

        Assert.Equal("Inventory Overview", model.PageTitle);
        Assert.Contains("every Project Hope inventory category", model.Summary);
    }

    [Fact]
    public async Task OnGetAsync_LoadsEveryCategoryInCategoryOrder()
    {
        var categories = new[]
        {
            new InventoryCategoryListItem(1, "Canned Vegetables"),
            new InventoryCategoryListItem(2, "Cereals"),
        };
        var canned = new CategoryInventoryView(1, "Canned Vegetables", []);
        var cereals = new CategoryInventoryView(2, "Cereals", []);
        var service = new StubInventoryQueryService(categories, [canned, cereals]);
        var model = new IndexModel(service, new StubInventoryQuantityService());

        await model.OnGetAsync();

        Assert.Equal(["Canned Vegetables", "Cereals"], model.CategoryInventories.Select(view => view.CategoryName).ToArray());
        Assert.Equal([1, 2], service.RequestedCategoryIds);
    }

    [Fact]
    public async Task OnGetAsync_SkipsCategoryThatDisappearsDuringLoad()
    {
        var categories = new[]
        {
            new InventoryCategoryListItem(1, "Canned Vegetables"),
            new InventoryCategoryListItem(2, "Cereals"),
        };
        var canned = new CategoryInventoryView(1, "Canned Vegetables", []);
        var service = new StubInventoryQueryService(categories, [canned, null]);
        var model = new IndexModel(service, new StubInventoryQuantityService());

        await model.OnGetAsync();

        Assert.Same(canned, Assert.Single(model.CategoryInventories));
    }

    [Fact]
    public async Task OnGetAsync_LeavesOverviewEmptyWhenNoCategoriesExist()
    {
        var service = new StubInventoryQueryService([], []);
        var model = new IndexModel(service, new StubInventoryQuantityService());

        await model.OnGetAsync();

        Assert.Empty(model.CategoryInventories);
        Assert.Empty(service.RequestedCategoryIds);
    }

    [Fact]
    public async Task OnPostUpdateQuantityAsync_UpdatesQuantityAndRedirectsToOverview()
    {
        var quantityService = new StubInventoryQuantityService();
        var model = new IndexModel(new StubInventoryQueryService([], []), quantityService)
        {
            InventoryEntryId = 14,
            UpdatedQuantity = 1.5,
        };

        var result = await model.OnPostUpdateQuantityAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Null(redirect.RouteValues);
        Assert.Equal(14, quantityService.RequestedInventoryEntryId);
        Assert.Equal(1.5, quantityService.RequestedQuantity);
    }

    [Theory]
    [InlineData(null, 25.0, "Inventory entry is required.")]
    [InlineData(14, null, "Quantity is required.")]
    public async Task OnPostUpdateQuantityAsync_ReloadsOverviewForMissingInput(
        int? inventoryEntryId,
        double? updatedQuantity,
        string expectedMessage)
    {
        var inventory = new CategoryInventoryView(2, "Cereals", []);
        var service = new StubInventoryQueryService(
            [new InventoryCategoryListItem(2, "Cereals")],
            [inventory]);
        var quantityService = new StubInventoryQuantityService();
        var model = new IndexModel(service, quantityService)
        {
            InventoryEntryId = inventoryEntryId,
            UpdatedQuantity = updatedQuantity,
        };

        var result = await model.OnPostUpdateQuantityAsync();

        Assert.IsType<PageResult>(result);
        Assert.True(model.QuantityUpdateFailed);
        Assert.Equal(expectedMessage, model.QuantityUpdateMessage);
        Assert.Same(inventory, Assert.Single(model.CategoryInventories));
        Assert.Null(quantityService.RequestedInventoryEntryId);
    }

    [Fact]
    public async Task OnPostUpdateQuantityAsync_ReloadsOverviewWhenQuantityServiceFails()
    {
        var inventory = new CategoryInventoryView(2, "Cereals", []);
        var service = new StubInventoryQueryService(
            [new InventoryCategoryListItem(2, "Cereals")],
            [inventory]);
        var quantityService = new StubInventoryQuantityService(
            new InventoryQuantityUpdateResult(false, "Quantity must be zero or greater."));
        var model = new IndexModel(service, quantityService)
        {
            InventoryEntryId = 14,
            UpdatedQuantity = -1,
        };

        var result = await model.OnPostUpdateQuantityAsync();

        Assert.IsType<PageResult>(result);
        Assert.True(model.QuantityUpdateFailed);
        Assert.Equal("Quantity must be zero or greater.", model.QuantityUpdateMessage);
        Assert.Same(inventory, Assert.Single(model.CategoryInventories));
    }

    [Fact]
    public async Task OnPostUpdateQuantityAsync_UsesFallbackMessageWhenFailureHasNoMessage()
    {
        var quantityService = new StubInventoryQuantityService(new InventoryQuantityUpdateResult(false, null));
        var model = new IndexModel(new StubInventoryQueryService([], []), quantityService)
        {
            InventoryEntryId = 14,
            UpdatedQuantity = 10,
        };

        var result = await model.OnPostUpdateQuantityAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Quantity update failed.", model.QuantityUpdateMessage);
    }

    private sealed class StubInventoryQueryService(
        IReadOnlyList<InventoryCategoryListItem> categories,
        IReadOnlyList<CategoryInventoryView?> inventories) : IInventoryQueryService
    {
        private int inventoryIndex;

        public List<int> RequestedCategoryIds { get; } = [];

        public Task<IReadOnlyList<InventoryCategoryListItem>> GetCategoriesAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(categories);
        }

        public Task<CategoryInventoryView?> GetInventoryForCategoryAsync(
            int categoryId,
            CancellationToken cancellationToken = default)
        {
            RequestedCategoryIds.Add(categoryId);
            var inventory = inventoryIndex < inventories.Count ? inventories[inventoryIndex] : null;
            inventoryIndex++;
            return Task.FromResult(inventory);
        }
    }

    private sealed class StubInventoryQuantityService(
        InventoryQuantityUpdateResult? result = null) : IInventoryQuantityService
    {
        public int? RequestedInventoryEntryId { get; private set; }

        public double? RequestedQuantity { get; private set; }

        public Task<InventoryQuantityUpdateResult> UpdateCurrentQuantityAsync(
            int inventoryEntryId,
            double quantity,
            DateTime countedAtUtc,
            CancellationToken cancellationToken = default)
        {
            RequestedInventoryEntryId = inventoryEntryId;
            RequestedQuantity = quantity;
            return Task.FromResult(result ?? new InventoryQuantityUpdateResult(true, null));
        }
    }
}
