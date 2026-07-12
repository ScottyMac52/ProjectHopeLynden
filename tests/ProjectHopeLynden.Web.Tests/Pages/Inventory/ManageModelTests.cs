using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjectHopeLynden.Application.Inventory;
using ProjectHopeLynden.Web.Pages.Inventory;

namespace ProjectHopeLynden.Web.Tests.Pages.Inventory;

public sealed class ManageModelTests
{
    [Fact]
    public void PageCopy_DescribesCategoryFocusedEditor()
    {
        var model = CreateModel(new StubInventoryQueryService([], null));

        Assert.Equal("Manage Inventory", model.PageTitle);
        Assert.Contains("category-focused editor", model.Summary);
    }

    [Fact]
    public async Task OnGetAsync_SelectsFirstCategoryWhenNoCategoryIsProvided()
    {
        var inventory = new CategoryInventoryView(2, "Cereals", []);
        var queryService = new StubInventoryQueryService(
            [new InventoryCategoryListItem(2, "Cereals")],
            inventory);
        var model = CreateModel(queryService);

        await model.OnGetAsync();

        Assert.Equal(2, model.CategoryId);
        Assert.Same(inventory, model.Inventory);
        Assert.False(model.CategoryWasNotFound);
        Assert.Equal(2, queryService.RequestedCategoryId);
    }

    [Fact]
    public async Task OnGetAsync_LoadsRequestedCategory()
    {
        var inventory = new CategoryInventoryView(4, "Produce", []);
        var queryService = new StubInventoryQueryService(
            [new InventoryCategoryListItem(2, "Cereals"), new InventoryCategoryListItem(4, "Produce")],
            inventory);
        var model = CreateModel(queryService) { CategoryId = 4 };

        await model.OnGetAsync();

        Assert.Same(inventory, model.Inventory);
        Assert.Equal(4, queryService.RequestedCategoryId);
    }

    [Fact]
    public async Task OnGetAsync_LeavesEditorEmptyWhenNoCategoriesExist()
    {
        var queryService = new StubInventoryQueryService([], null);
        var model = CreateModel(queryService);

        await model.OnGetAsync();

        Assert.Empty(model.Categories);
        Assert.Null(model.CategoryId);
        Assert.Null(model.Inventory);
        Assert.Null(queryService.RequestedCategoryId);
    }

    [Fact]
    public async Task OnGetAsync_SetsNotFoundForMissingCategory()
    {
        var queryService = new StubInventoryQueryService(
            [new InventoryCategoryListItem(2, "Cereals")],
            null);
        var model = CreateModel(queryService) { CategoryId = 404 };

        await model.OnGetAsync();

        Assert.True(model.CategoryWasNotFound);
        Assert.Equal(404, queryService.RequestedCategoryId);
    }

    [Fact]
    public async Task OnPostCreateCategoryAsync_CreatesCategoryAndRedirectsToIt()
    {
        var categoryService = new StubInventoryCategoryService(
            new InventoryCategoryCreateResult(true, 9, null));
        var model = CreateModel(new StubInventoryQueryService([], null), categoryService: categoryService)
        {
            NewCategoryName = "Baking Supplies",
        };

        var result = await model.OnPostCreateCategoryAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(9, redirect.RouteValues?["categoryId"]);
        Assert.Equal("Baking Supplies", categoryService.RequestedName);
    }

    [Fact]
    public async Task OnPostCreateCategoryAsync_ReloadsEditorWhenCreationFails()
    {
        var inventory = new CategoryInventoryView(2, "Cereals", []);
        var queryService = new StubInventoryQueryService(
            [new InventoryCategoryListItem(2, "Cereals")],
            inventory);
        var categoryService = new StubInventoryCategoryService(
            new InventoryCategoryCreateResult(false, 2, "That category already exists."));
        var model = CreateModel(queryService, categoryService: categoryService)
        {
            NewCategoryName = "Cereals",
        };

        var result = await model.OnPostCreateCategoryAsync();

        Assert.IsType<PageResult>(result);
        Assert.True(model.CategoryCreateFailed);
        Assert.Equal("That category already exists.", model.CategoryCreateMessage);
        Assert.Same(inventory, model.Inventory);
    }

    [Fact]
    public async Task OnPostCreateCategoryAsync_UsesFallbackMessage()
    {
        var categoryService = new StubInventoryCategoryService(
            new InventoryCategoryCreateResult(false, null, null));
        var model = CreateModel(new StubInventoryQueryService([], null), categoryService: categoryService);

        var result = await model.OnPostCreateCategoryAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Category creation failed.", model.CategoryCreateMessage);
    }

    [Fact]
    public async Task OnPostUpdateQuantityAsync_UpdatesAndReturnsToSelectedCategory()
    {
        var quantityService = new StubInventoryQuantityService();
        var model = CreateModel(new StubInventoryQueryService([], null), quantityService)
        {
            CategoryId = 2,
            InventoryEntryId = 14,
            UpdatedQuantity = 1.5,
        };

        var result = await model.OnPostUpdateQuantityAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(2, redirect.RouteValues?["categoryId"]);
        Assert.Equal(14, quantityService.RequestedInventoryEntryId);
        Assert.Equal(1.5, quantityService.RequestedQuantity);
    }

    [Theory]
    [InlineData(null, 2.0, "Inventory entry is required.")]
    [InlineData(14, null, "Quantity is required.")]
    public async Task OnPostUpdateQuantityAsync_ReloadsForMissingInput(
        int? inventoryEntryId,
        double? updatedQuantity,
        string expectedMessage)
    {
        var inventory = new CategoryInventoryView(2, "Cereals", []);
        var queryService = new StubInventoryQueryService(
            [new InventoryCategoryListItem(2, "Cereals")],
            inventory);
        var model = CreateModel(queryService)
        {
            InventoryEntryId = inventoryEntryId,
            UpdatedQuantity = updatedQuantity,
        };

        var result = await model.OnPostUpdateQuantityAsync();

        Assert.IsType<PageResult>(result);
        Assert.True(model.QuantityUpdateFailed);
        Assert.Equal(expectedMessage, model.QuantityUpdateMessage);
        Assert.Same(inventory, model.Inventory);
    }

    [Fact]
    public async Task OnPostUpdateQuantityAsync_ReloadsWhenServiceFails()
    {
        var inventory = new CategoryInventoryView(2, "Cereals", []);
        var queryService = new StubInventoryQueryService(
            [new InventoryCategoryListItem(2, "Cereals")],
            inventory);
        var quantityService = new StubInventoryQuantityService(
            new InventoryQuantityUpdateResult(false, "Quantity must be zero or greater."));
        var model = CreateModel(queryService, quantityService)
        {
            InventoryEntryId = 14,
            UpdatedQuantity = -1,
        };

        var result = await model.OnPostUpdateQuantityAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Quantity must be zero or greater.", model.QuantityUpdateMessage);
        Assert.Same(inventory, model.Inventory);
    }

    [Fact]
    public async Task OnPostUpdateQuantityAsync_UsesFallbackMessage()
    {
        var quantityService = new StubInventoryQuantityService(new InventoryQuantityUpdateResult(false, null));
        var model = CreateModel(new StubInventoryQueryService([], null), quantityService)
        {
            InventoryEntryId = 14,
            UpdatedQuantity = 10,
        };

        var result = await model.OnPostUpdateQuantityAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Quantity update failed.", model.QuantityUpdateMessage);
    }

    private static ManageModel CreateModel(
        IInventoryQueryService queryService,
        IInventoryQuantityService? quantityService = null,
        IInventoryCategoryService? categoryService = null)
    {
        return new ManageModel(
            queryService,
            quantityService ?? new StubInventoryQuantityService(),
            categoryService ?? new StubInventoryCategoryService());
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

    private sealed class StubInventoryCategoryService(
        InventoryCategoryCreateResult? result = null) : IInventoryCategoryService
    {
        public string? RequestedName { get; private set; }

        public Task<InventoryCategoryCreateResult> CreateCategoryAsync(
            string? categoryName,
            CancellationToken cancellationToken = default)
        {
            RequestedName = categoryName;
            return Task.FromResult(result ?? new InventoryCategoryCreateResult(true, 1, null));
        }
    }
}
