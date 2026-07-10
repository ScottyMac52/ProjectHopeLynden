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
        var model = new IndexModel(service);

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
        var model = new IndexModel(service) { CategoryId = 2 };

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
        var model = new IndexModel(service) { CategoryId = 99 };

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
        var model = new IndexModel(service);

        await model.OnGetAsync();

        Assert.Empty(model.Categories);
        Assert.Null(model.CategoryId);
        Assert.Null(model.Inventory);
        Assert.False(model.CategoryWasNotFound);
        Assert.Null(service.RequestedCategoryId);
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
}
