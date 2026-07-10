using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjectHopeLynden.Application.Inventory;
using ProjectHopeLynden.Web.Pages.Inventory;

namespace ProjectHopeLynden.Web.Tests.Pages.Inventory;

public sealed class MaintainModelTests
{
    [Fact]
    public async Task OnGetAsync_DefaultsNewEntryToRequestedCategoryAndFirstLocation()
    {
        var options = CreateOptions();
        var service = new StubInventoryEntryMaintenanceService(options);
        var model = new MaintainModel(service) { CategoryId = 2 };

        await model.OnGetAsync();

        Assert.False(model.IsEditing);
        Assert.Same(options, model.Options);
        Assert.Equal(2, model.SelectedCategoryId);
        Assert.Equal(4, model.LocationId);
        Assert.Equal(0, model.CurrentQuantity);
    }

    [Fact]
    public async Task OnGetAsync_DefaultsNewEntryWhenNoCategoryOrLocationOptionsExist()
    {
        var options = new InventoryEntryFormOptions([], []);
        var service = new StubInventoryEntryMaintenanceService(options);
        var model = new MaintainModel(service);

        await model.OnGetAsync();

        Assert.False(model.IsEditing);
        Assert.Same(options, model.Options);
        Assert.Null(model.SelectedCategoryId);
        Assert.Null(model.LocationId);
        Assert.Equal(0, model.CurrentQuantity);
    }

    [Fact]
    public async Task OnGetAsync_LoadsExistingEntryForEdit()
    {
        var options = CreateOptions();
        var editView = new InventoryEntryEditView(
            InventoryEntryId: 14,
            ItemName: "Green Beans",
            CategoryId: 2,
            LocationId: 4,
            CurrentQuantity: 24,
            BestByDate: new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            IsCommodity: true,
            IsMenuItem: false);
        var service = new StubInventoryEntryMaintenanceService(options, editView: editView);
        var model = new MaintainModel(service) { InventoryEntryId = 14 };

        await model.OnGetAsync();

        Assert.True(model.IsEditing);
        Assert.False(model.EntryWasNotFound);
        Assert.Equal("Green Beans", model.ItemName);
        Assert.Equal(2, model.SelectedCategoryId);
        Assert.Equal(4, model.LocationId);
        Assert.Equal(24, model.CurrentQuantity);
        Assert.True(model.IsCommodity);
        Assert.False(model.IsMenuItem);
    }

    [Fact]
    public async Task OnGetAsync_SetsNotFoundWhenExistingEntryIsMissing()
    {
        var options = CreateOptions();
        var service = new StubInventoryEntryMaintenanceService(options, editView: null);
        var model = new MaintainModel(service) { InventoryEntryId = 404 };

        await model.OnGetAsync();

        Assert.True(model.EntryWasNotFound);
    }

    [Fact]
    public async Task OnPostSaveAsync_CreatesEntryAndRedirectsToSavedCategory()
    {
        var options = CreateOptions();
        var result = new InventoryEntrySaveResult(true, null, InventoryEntryId: 14, CategoryId: 2);
        var service = new StubInventoryEntryMaintenanceService(options, saveResult: result);
        var model = new MaintainModel(service)
        {
            ItemName = "Green Beans",
            SelectedCategoryId = 2,
            LocationId = 4,
            CurrentQuantity = 24,
            IsCommodity = true,
        };

        var actionResult = await model.OnPostSaveAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(actionResult);
        Assert.Equal("/Inventory/Index", redirect.PageName);
        Assert.NotNull(redirect.RouteValues);
        Assert.Equal(2, redirect.RouteValues["categoryId"]);
        Assert.Equal("Green Beans", service.LastRequest?.ItemName);
        Assert.True(service.LastRequest?.IsCommodity);
    }

    [Fact]
    public async Task OnPostSaveAsync_RedirectsToSelectedCategoryWhenSaveResultDoesNotIncludeCategory()
    {
        var options = CreateOptions();
        var result = new InventoryEntrySaveResult(true, null, InventoryEntryId: 14, CategoryId: null);
        var service = new StubInventoryEntryMaintenanceService(options, saveResult: result);
        var model = new MaintainModel(service)
        {
            ItemName = "Green Beans",
            SelectedCategoryId = 2,
            LocationId = 4,
            CurrentQuantity = 24,
            IsCommodity = true,
        };

        var actionResult = await model.OnPostSaveAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(actionResult);
        Assert.Equal(2, redirect.RouteValues?["categoryId"]);
    }

    [Fact]
    public async Task OnPostSaveAsync_UpdatesExistingEntryAndRedirectsToSavedCategory()
    {
        var options = CreateOptions();
        var result = new InventoryEntrySaveResult(true, null, InventoryEntryId: 14, CategoryId: 2);
        var service = new StubInventoryEntryMaintenanceService(options, saveResult: result);
        var model = new MaintainModel(service)
        {
            InventoryEntryId = 14,
            ItemName = "Green Beans",
            SelectedCategoryId = 2,
            LocationId = 5,
            CurrentQuantity = 24,
            IsCommodity = false,
            IsMenuItem = true,
        };

        var actionResult = await model.OnPostSaveAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(actionResult);
        Assert.Equal(14, service.UpdatedInventoryEntryId);
        Assert.False(service.LastRequest?.IsCommodity);
        Assert.True(service.LastRequest?.IsMenuItem);
        Assert.Equal(2, redirect.RouteValues?["categoryId"]);
    }

    [Fact]
    public async Task OnPostSaveAsync_ReloadsOptionsWhenSaveFails()
    {
        var options = CreateOptions();
        var result = new InventoryEntrySaveResult(false, "Item name is required.", null, null);
        var service = new StubInventoryEntryMaintenanceService(options, saveResult: result);
        var model = new MaintainModel(service)
        {
            SelectedCategoryId = 2,
            LocationId = 4,
        };

        var actionResult = await model.OnPostSaveAsync();

        Assert.IsType<PageResult>(actionResult);
        Assert.True(model.SaveFailed);
        Assert.Equal("Item name is required.", model.SaveMessage);
        Assert.Same(options, model.Options);
    }

    [Fact]
    public async Task OnPostSaveAsync_UsesFallbackMessageWhenSaveFailsWithoutMessage()
    {
        var options = CreateOptions();
        var result = new InventoryEntrySaveResult(false, null, null, null);
        var service = new StubInventoryEntryMaintenanceService(options, saveResult: result);
        var model = new MaintainModel(service)
        {
            SelectedCategoryId = 2,
            LocationId = 4,
        };

        var actionResult = await model.OnPostSaveAsync();

        Assert.IsType<PageResult>(actionResult);
        Assert.True(model.SaveFailed);
        Assert.Equal("Inventory row could not be saved.", model.SaveMessage);
        Assert.Same(options, model.Options);
    }

    private static InventoryEntryFormOptions CreateOptions()
    {
        return new InventoryEntryFormOptions(
            [new InventoryCategoryListItem(2, "Canned Vegetables")],
            [new InventoryLocationListItem(4, "Shelf"), new InventoryLocationListItem(5, "Back Room")]);
    }

    private sealed class StubInventoryEntryMaintenanceService(
        InventoryEntryFormOptions options,
        InventoryEntryEditView? editView = null,
        InventoryEntrySaveResult? saveResult = null) : IInventoryEntryMaintenanceService
    {
        public InventoryEntrySaveRequest? LastRequest { get; private set; }

        public int? UpdatedInventoryEntryId { get; private set; }

        public Task<InventoryEntryFormOptions> GetFormOptionsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(options);
        }

        public Task<InventoryEntryEditView?> GetEntryForEditAsync(
            int inventoryEntryId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(editView);
        }

        public Task<InventoryEntrySaveResult> CreateEntryAsync(
            InventoryEntrySaveRequest request,
            DateTime savedAtUtc,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(saveResult ?? new InventoryEntrySaveResult(true, null, 1, request.CategoryId));
        }

        public Task<InventoryEntrySaveResult> UpdateEntryAsync(
            int inventoryEntryId,
            InventoryEntrySaveRequest request,
            DateTime savedAtUtc,
            CancellationToken cancellationToken = default)
        {
            UpdatedInventoryEntryId = inventoryEntryId;
            LastRequest = request;
            return Task.FromResult(saveResult ?? new InventoryEntrySaveResult(true, null, inventoryEntryId, request.CategoryId));
        }
    }
}
