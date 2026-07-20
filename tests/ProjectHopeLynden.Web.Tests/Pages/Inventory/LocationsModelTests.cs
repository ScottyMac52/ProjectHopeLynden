using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjectHopeLynden.Application.Inventory;
using ProjectHopeLynden.Web.Pages.Inventory;

namespace ProjectHopeLynden.Web.Tests.Pages.Inventory;

public sealed class LocationsModelTests
{
    [Fact]
    public async Task OnGetAsync_ListsCurrentLocations()
    {
        var expected = new[]
        {
            new InventoryLocationListItem(2, "Back Room"),
            new InventoryLocationListItem(1, "Shelf"),
        };
        var model = new LocationsModel(new StubInventoryLocationService(expected));

        await model.OnGetAsync();

        Assert.Equal(expected, model.Locations);
    }

    [Fact]
    public async Task OnPostCreateAsync_RedirectsWithSuccessMessage()
    {
        var service = new StubInventoryLocationService(
            createResult: new InventoryLocationCreateResult(true, 3, "Loading Dock", null));
        var model = new LocationsModel(service) { NewLocationName = " Loading Dock " };

        var result = await model.OnPostCreateAsync();

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(" Loading Dock ", service.RequestedName);
        Assert.Equal("Loading Dock", model.AddedLocationName);
    }

    [Fact]
    public async Task OnPostCreateAsync_ReloadsListWithServiceError()
    {
        var expected = new[] { new InventoryLocationListItem(1, "Shelf") };
        var service = new StubInventoryLocationService(
            expected,
            new InventoryLocationCreateResult(false, 1, "Shelf", "The location 'Shelf' already exists."));
        var model = new LocationsModel(service) { NewLocationName = "shelf" };

        var result = await model.OnPostCreateAsync();

        Assert.IsType<PageResult>(result);
        Assert.True(model.CreateFailed);
        Assert.Equal("The location 'Shelf' already exists.", model.CreateMessage);
        Assert.Equal(expected, model.Locations);
    }

    [Fact]
    public async Task OnPostCreateAsync_UsesFallbackErrorMessage()
    {
        var service = new StubInventoryLocationService(
            createResult: new InventoryLocationCreateResult(false, null, null, null));
        var model = new LocationsModel(service);

        Assert.IsType<PageResult>(await model.OnPostCreateAsync());
        Assert.Equal("Location could not be added.", model.CreateMessage);
    }

    private sealed class StubInventoryLocationService(
        IReadOnlyList<InventoryLocationListItem>? locations = null,
        InventoryLocationCreateResult? createResult = null) : IInventoryLocationService
    {
        public string? RequestedName { get; private set; }

        public Task<IReadOnlyList<InventoryLocationListItem>> GetLocationsAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(locations ?? (IReadOnlyList<InventoryLocationListItem>)[]);

        public Task<InventoryLocationCreateResult> CreateLocationAsync(
            string? locationName,
            CancellationToken cancellationToken = default)
        {
            RequestedName = locationName;
            return Task.FromResult(createResult ?? new InventoryLocationCreateResult(true, 1, "Shelf", null));
        }
    }
}
