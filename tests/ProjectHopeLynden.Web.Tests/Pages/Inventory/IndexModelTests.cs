using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjectHopeLynden.Application.Inventory;
using ProjectHopeLynden.Web.Pages.Inventory;

namespace ProjectHopeLynden.Web.Tests.Pages.Inventory;

public sealed class IndexModelTests
{
    [Fact]
    public async Task OnGetAsync_SelectsFirstCategoryWhenNoCategoryIsProvided()
    {
        var categories = new[]
        {
            new InventoryCategoryListItem(2, "Cereals"),
        };
        var inventory = new CategoryInventoryView(2, "Cereals", []);
        var service = new StubInventoryQueryService(categories, inventory);
        var quantityService = new StubInventoryQuantityService();
        var model = new IndexModel(service, quantityService);

        await model.OnGetAsync();

        Assert.Equal(2, model.CategoryId);
        Assert.Same(categories, model.Categories);
        Assert.Same(inventory, model.Inventory);
        Assert.False(model.CategoryWasNotFound);
    }

    [Fact]
    public async Task OnGetAsync_LoadsRequestedCategoryInventory()
    {
        var categories = new[]
        {
            new InventoryCategoryListItem(1, "Canned Vegetables"),
            new InventoryCategoryListItem(2, "Cereals"),
        };
        var inventory = new CategoryInventoryView(2, "Cereals", []);
        var service = new StubInventoryQueryService(categories, inventory);
        var quantityService = new StubInventoryQuantityService();
        var model = new IndexModel(service, quantityService) { CategoryId = 2 };

        await model.OnGetAsync();

        Assert.Equal(2, model.CategoryId);
        Assert.Same(inventory, model.Inventory);
        Assert.False(model.CategoryWasNotFound);
        Assert.Equal(2, service.RequestedCategoryId);
    }

    [Fact]
    public async Task OnGetAsync_SetsNotFoundWhenRequestedCategoryDoesNotExist()
    {
        var categories = new[]
        {
            new InventoryCategoryListItem(1, "Canned Vegetables"),
        };
        var service = new StubInventoryQueryService(categories, inventory: null);
        var quantityService = new StubInventoryQuantityService();
        var model = new IndexModel(service, quantityService) { CategoryId = 99 };

        await model.OnGetAsync();

        Assert.Equal(99, model.CategoryId);
        Assert.Null(model.Inventory);
        Assert.True(model.CategoryWasNotFound);
        Assert.Equal(99, service.RequestedCategoryId);
    }

    [Fact]
    public async Task OnGetAsync_LeavesInventoryEmptyWhenNoCategoriesExist()
    {
        var service = new StubInventoryQueryService([], inventory: null);
        var quantityService = new StubInventoryQuantityService();
        var model = new IndexModel(service, quantityService);

        await model.OnGetAsync();

        Assert.Empty(model.Categories);
        Assert.Null(model.CategoryId);
        Assert.Null(model.Inventory);
        Assert.False(model.CategoryWasNotFound);
        Assert.Null(service.RequestedCategoryId);
    }

    [Fact]
    public async Task OnPostUpdateQuantityAsync_UpdatesQuantityAndRedirectsToSelectedCategory()
    {
        var service = new StubInventoryQueryService([], inventory: null);
        var quantityService = new StubInventoryQuantityService();
        var model = new IndexModel(service, quantityService)
        {
            CategoryId = 2,
            InventoryEntryId = 14,
            UpdatedQuantity = 25,
        };

        var result = await model.OnPostUpdateQuantityAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.NotNull(redirect.RouteValues);
        Assert.Equal(2, redirect.RouteValues["categoryId"]);
        Assert.Equal(14, quantityService.RequestedInventoryEntryId);
        Assert.Equal(25, quantityService.RequestedQuantity);
    }

    [Fact]
    public async Task OnPostUpdateQuantityAsync_ReloadsInventoryWhenQuantityUpdateFails()
    {
        var categories = new[]
        {
            new InventoryCategoryListItem(2, "Cereals"),
        };
        var inventory = new CategoryInventoryView(2, "Cereals", []);
        var service = new StubInventoryQueryService(categories, inventory);
        var quantityService = new StubInventoryQuantityService(
            new InventoryQuantityUpdateResult(false, "Quantity must be zero or greater."));
        var model = new IndexModel(service, quantityService)
        {
            CategoryId = 2,
            InventoryEntryId = 14,
            UpdatedQuantity = -1,
        };

        var result = await model.OnPostUpdateQuantityAsync();

        Assert.IsType<PageResult>(result);
        Assert.True(model.QuantityUpdateFailed);
        Assert.Equal("Quantity must be zero or greater.", model.QuantityUpdateMessage);
        Assert.Same(categories, model.Categories);
        Assert.Same(inventory, model.Inventory);
        Assert.Equal(2, service.RequestedCategoryId);
    }

    [Fact]
    public async Task OnPostUpdateQuantityAsync_RejectsMissingQuantityWithoutCallingUpdateService()
    {
        var categories = new[]
        {
            new InventoryCategoryListItem(2, "Cereals"),
        };
        var service = new StubInventoryQueryService(categories, new CategoryInventoryView(2, "Cereals", []));
        var quantityService = new StubInventoryQuantityService();
        var model = new IndexModel(service, quantityService)
        {
            CategoryId = 2,
            InventoryEntryId = 14,
        };

        var result = await model.OnPostUpdateQuantityAsync();

        Assert.IsType<PageResult>(result);
        Assert.True(model.QuantityUpdateFailed);
        Assert.Equal("Quantity is required.", model.QuantityUpdateMessage);
        Assert.Null(quantityService.RequestedInventoryEntryId);
    }

    private sealed class StubInventoryQueryService(
        IReadOnlyList<InventoryCategoryListItem> categories,
        CategoryInventoryView? inventory) : IInventoryQueryService
    {
        public int? RequestedCategoryId { get; private set; }

        public Task<IReadOnlyList<InventoryCategoryListItem>> GetCategoriesAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(categories);
        }

        public Task<CategoryInventoryView?> GetInventoryForCategoryAsync(
            int categoryId,
            CancellationToken cancellationToken = default)
        {
            RequestedCategoryId = categoryId;
            return Task.FromResult(inventory);
        }
    }

    private sealed class StubInventoryQuantityService(
        InventoryQuantityUpdateResult? result = null) : IInventoryQuantityService
    {
        public int? RequestedInventoryEntryId { get; private set; }

        public int? RequestedQuantity { get; private set; }

        public DateTime? RequestedCountedAtUtc { get; private set; }

        public Task<InventoryQuantityUpdateResult> UpdateCurrentQuantityAsync(
            int inventoryEntryId,
            int quantity,
            DateTime countedAtUtc,
            CancellationToken cancellationToken = default)
        {
            RequestedInventoryEntryId = inventoryEntryId;
            RequestedQuantity = quantity;
            RequestedCountedAtUtc = countedAtUtc;
            return Task.FromResult(result ?? new InventoryQuantityUpdateResult(true, null));
        }
    }
}
