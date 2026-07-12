using ProjectHopeLynden.Application.Inventory;
using ProjectHopeLynden.Web.Pages.Inventory;

namespace ProjectHopeLynden.Web.Tests.Pages.Inventory;

public sealed class SearchModelTests
{
    [Fact]
    public void PageCopy_ExplainsCrossCategoryPartialNameSearch()
    {
        var model = new SearchModel(new StubInventorySearchService());

        Assert.Equal("Search Inventory", model.PageTitle);
        Assert.Contains("part of an item name", model.Summary);
        Assert.Contains("without knowing its category", model.Summary);
        Assert.False(model.HasSearched);
    }

    [Fact]
    public async Task OnGetAsync_SearchesAndNormalizesTheDisplayedTerm()
    {
        var row = new InventorySearchRow(
            12,
            "Green Beans",
            "Canned Vegetables",
            "Shelf",
            24,
            true,
            new DateTime(2028, 4, 1),
            false,
            new DateTime(2026, 7, 12, 10, 0, 0, DateTimeKind.Utc));
        var returnedResult = new InventorySearchResult("beans", [row]);
        var service = new StubInventorySearchService(returnedResult);
        var model = new SearchModel(service)
        {
            SearchTerm = "  beans  ",
        };

        await model.OnGetAsync();

        Assert.Equal("  beans  ", service.RequestedSearchTerm);
        Assert.Same(returnedResult, model.Results);
        Assert.Equal("beans", model.SearchTerm);
        Assert.True(model.HasSearched);
        Assert.True(model.Results.HasResults);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task OnGetAsync_LeavesSearchInPromptStateWhenTermIsMissing(string? searchTerm)
    {
        var service = new StubInventorySearchService(new InventorySearchResult(string.Empty, []));
        var model = new SearchModel(service)
        {
            SearchTerm = searchTerm,
        };

        await model.OnGetAsync();

        Assert.Equal(searchTerm, service.RequestedSearchTerm);
        Assert.Equal(string.Empty, model.SearchTerm);
        Assert.False(model.HasSearched);
        Assert.False(model.Results.HasResults);
    }

    [Fact]
    public async Task OnGetAsync_PreservesACompletedSearchWithNoMatches()
    {
        var returnedResult = new InventorySearchResult("coffee", []);
        var model = new SearchModel(new StubInventorySearchService(returnedResult))
        {
            SearchTerm = "coffee",
        };

        await model.OnGetAsync();

        Assert.True(model.HasSearched);
        Assert.False(model.Results.HasResults);
        Assert.Equal("coffee", model.Results.SearchTerm);
    }

    private sealed class StubInventorySearchService(
        InventorySearchResult? returnedResult = null) : IInventorySearchService
    {
        public string? RequestedSearchTerm { get; private set; }

        public Task<InventorySearchResult> SearchAsync(
            string? searchTerm,
            CancellationToken cancellationToken = default)
        {
            RequestedSearchTerm = searchTerm;
            return Task.FromResult(returnedResult ?? new InventorySearchResult(string.Empty, []));
        }
    }
}
