using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using ProjectHopeLynden.Application.IncomingOrders;
using ProjectHopeLynden.Domain.IncomingOrders;
using ProjectHopeLynden.Web.Features;
using ProjectHopeLynden.Web.Pages.IncomingOrders;

namespace ProjectHopeLynden.Web.Tests.Pages.IncomingOrders;

public sealed class IncomingOrdersFeatureTests
{
    [Fact]
    public async Task Index_ReturnsNotFoundAndDoesNotQueryDataWhenFeatureIsDisabled()
    {
        var service = new StubIncomingOrderService();
        var model = new IndexModel(service, Options.Create(new ProjectHopeFeatureOptions()));

        var result = await model.OnGetAsync();

        Assert.IsType<NotFoundResult>(result);
        Assert.Equal(0, service.GetOrdersCallCount);
    }

    [Fact]
    public async Task Index_LoadsOrdersWhenFeatureIsEnabled()
    {
        var expected = new IncomingOrderListItem(7, DateTime.Today, "Food Lifeline",
            IncomingOrderStatus.Pending, DateTime.Today,
            IncomingOrderDateState.DueToday, "Milk", 1);
        var service = new StubIncomingOrderService([expected]);
        var model = new IndexModel(service, EnabledOptions());

        var result = await model.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Same(expected, Assert.Single(model.Orders));
    }

    [Fact]
    public async Task EditPost_ReturnsNotFoundWithoutSavingWhenFeatureIsDisabled()
    {
        var service = new StubIncomingOrderService();
        var model = new EditModel(service, Options.Create(new ProjectHopeFeatureOptions()));

        var result = await model.OnPostSaveAsync();

        Assert.IsType<NotFoundResult>(result);
        Assert.Equal(0, service.SaveCallCount);
    }

    [Fact]
    public async Task EditGet_InitializesNewOrderWhenEnabled()
    {
        var model = new EditModel(new StubIncomingOrderService(), EnabledOptions());

        var result = await model.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal(DateTime.Today, model.OrderDate);
        Assert.Equal(DateTime.Today, model.ExpectedDate);
        Assert.Equal(IncomingOrderStatus.Pending, model.Status);
        Assert.Single(model.Lines);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EditGet_MarksMissingOrReceivedOrderUnavailable(bool received)
    {
        var order = received ? CreateOrder(IncomingOrderStatus.Received) : null;
        var model = new EditModel(new StubIncomingOrderService(order: order), EnabledOptions()) { OrderId = 12 };

        var result = await model.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.True(model.OrderNotFound);
    }

    [Fact]
    public async Task EditGet_LoadsEditableOrder()
    {
        var order = CreateOrder(IncomingOrderStatus.Ordered);
        var model = new EditModel(new StubIncomingOrderService(order: order), EnabledOptions()) { OrderId = order.Id };

        var result = await model.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Food Lifeline", model.Vendor);
        Assert.Equal(order.InvoiceAmount, model.InvoiceAmount);
        Assert.Equal(order.Lines.Single().InventoryEntryId, model.Lines.Single().InventoryEntryId);
    }

    [Fact]
    public async Task EditPost_RedirectsAfterSuccessfulSave()
    {
        var service = new StubIncomingOrderService();
        var model = ValidEditModel(service);

        var result = await model.OnPostSaveAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/IncomingOrders/Index", redirect.PageName);
        Assert.Equal(1, service.SaveCallCount);
    }

    [Fact]
    public async Task EditPost_ReloadsOptionsAndAddsBlankLineAfterValidationFailure()
    {
        var service = new StubIncomingOrderService(saveResult: new(false, null, null));
        var model = ValidEditModel(service);
        model.Lines = [];

        var result = await model.OnPostSaveAsync();

        Assert.IsType<PageResult>(result);
        Assert.True(model.SaveFailed);
        Assert.Equal("Incoming order could not be saved.", model.SaveMessage);
        Assert.Single(model.Lines);
    }

    [Fact]
    public async Task EditPost_ShowsUnavailableWhenOrderDisappears()
    {
        var service = new StubIncomingOrderService(throwOnSave: true);
        var model = ValidEditModel(service);

        var result = await model.OnPostSaveAsync();

        Assert.IsType<PageResult>(result);
        Assert.True(model.OrderNotFound);
    }

    [Fact]
    public async Task ReceiveGet_ReturnsNotFoundWhenFeatureDisabled()
    {
        var model = new ReceiveModel(new StubIncomingOrderService(), Options.Create(new ProjectHopeFeatureOptions()));

        Assert.IsType<NotFoundResult>(await model.OnGetAsync());
    }

    [Theory]
    [InlineData(null)]
    [InlineData(IncomingOrderStatus.Received)]
    [InlineData(IncomingOrderStatus.Cancelled)]
    public async Task ReceiveGet_RejectsUnavailableOrders(IncomingOrderStatus? status)
    {
        var order = status is null ? null : CreateOrder(status.Value);
        var model = new ReceiveModel(new StubIncomingOrderService(order: order), EnabledOptions()) { OrderId = 7 };

        Assert.IsType<NotFoundResult>(await model.OnGetAsync());
    }

    [Fact]
    public async Task ReceiveGet_DefaultsReceivedQuantityToExpectedQuantity()
    {
        var order = CreateOrder(IncomingOrderStatus.Ordered);
        var model = new ReceiveModel(new StubIncomingOrderService(order: order), EnabledOptions()) { OrderId = order.Id };

        var result = await model.OnGetAsync();

        Assert.IsType<PageResult>(result);
        var line = Assert.Single(model.Lines);
        Assert.Equal(line.ExpectedQuantity, line.ReceivedQuantity);
        Assert.Equal("Canned Goods — Milk — Cooler", line.InventoryEntryName);
    }

    [Fact]
    public async Task ReceivePost_RedirectsAfterSuccessfulReceipt()
    {
        var service = new StubIncomingOrderService(order: CreateOrder(IncomingOrderStatus.Ordered));
        var model = ValidReceiveModel(service);

        var result = await model.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/IncomingOrders/Index", redirect.PageName);
        Assert.Equal(1, service.ReceiveCallCount);
    }

    [Fact]
    public async Task ReceivePost_ShowsServiceFailureOrNotFoundWhenOrderDisappears()
    {
        var visibleOrder = CreateOrder(IncomingOrderStatus.Ordered);
        var visibleService = new StubIncomingOrderService(order: visibleOrder, receiptResult: new(false, null));
        var visibleModel = ValidReceiveModel(visibleService);
        var missingService = new StubIncomingOrderService(receiptResult: new(false, "Invalid receipt."));
        var missingModel = ValidReceiveModel(missingService);

        Assert.IsType<PageResult>(await visibleModel.OnPostAsync());
        Assert.True(visibleModel.ReceiveFailed);
        Assert.Equal("The order could not be received.", visibleModel.ReceiveMessage);
        Assert.IsType<NotFoundResult>(await missingModel.OnPostAsync());
    }

    [Fact]
    public void AppSettings_DisablesIncomingOrdersByDefault()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestAssets", "appsettings.json"));
        using var document = JsonDocument.Parse(json);

        Assert.False(document.RootElement.GetProperty("Features").GetProperty("IncomingOrders").GetBoolean());
    }

    [Fact]
    public void Layout_ShowsNavigationOnlyWhenFeatureIsEnabled()
    {
        var layout = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestAssets", "_Layout.cshtml"));

        Assert.Contains("FeatureOptions.Value.IncomingOrders", layout);
        Assert.Contains("asp-page=\"/IncomingOrders/Index\">Incoming Orders", layout);
    }

    private static IOptions<ProjectHopeFeatureOptions> EnabledOptions() =>
        Options.Create(new ProjectHopeFeatureOptions { IncomingOrders = true });

    private static EditModel ValidEditModel(StubIncomingOrderService service) => new(service, EnabledOptions())
    {
        OrderDate = DateTime.Today,
        Vendor = "Food Lifeline",
        Status = IncomingOrderStatus.Pending,
        ExpectedDate = DateTime.Today.AddDays(1),
        Lines = [new EditModel.IncomingOrderLineInput { InventoryEntryId = 3, ExpectedQuantity = 4 }],
    };

    private static ReceiveModel ValidReceiveModel(StubIncomingOrderService service) => new(service, EnabledOptions())
    {
        OrderId = 7,
        Lines = [new ReceiveModel.ReceiptLineInput { LineId = 4, ReceivedQuantity = 6 }],
    };

    private static IncomingOrderEditView CreateOrder(IncomingOrderStatus status) => new(
        7,
        new DateTime(2026, 7, 15),
        "Food Lifeline",
        status,
        new DateTime(2026, 7, 16),
        "AOR-123",
        338.58,
        new DateTime(2026, 8, 5),
        "BFB",
        "EFAP",
        new DateTime(2026, 7, 22),
        729,
        "Milk",
        "18 cases",
        status == IncomingOrderStatus.Received ? DateTime.UtcNow : null,
        [new IncomingOrderLineEditView(4, 3, "Canned Goods — Milk — Cooler", 18, null)]);

    private sealed class StubIncomingOrderService(
        IReadOnlyList<IncomingOrderListItem>? orders = null,
        IncomingOrderEditView? order = null,
        IncomingOrderSaveResult? saveResult = null,
        IncomingOrderReceiptResult? receiptResult = null,
        bool throwOnSave = false) : IIncomingOrderService
    {
        public int GetOrdersCallCount { get; private set; }

        public int SaveCallCount { get; private set; }

        public int ReceiveCallCount { get; private set; }

        public Task<IReadOnlyList<IncomingOrderListItem>> GetOrdersAsync(DateTime operatingDate, CancellationToken cancellationToken = default)
        {
            GetOrdersCallCount++;
            return Task.FromResult(orders ?? (IReadOnlyList<IncomingOrderListItem>)[]);
        }

        public Task<IReadOnlyList<IncomingOrderInventoryOption>> GetInventoryOptionsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<IncomingOrderInventoryOption>>([]);

        public Task<IncomingOrderEditView?> GetOrderAsync(int orderId, CancellationToken cancellationToken = default) =>
            Task.FromResult(order);

        public Task<IncomingOrderSaveResult> SaveOrderAsync(int? orderId, IncomingOrderSaveRequest request, DateTime createdAtUtc, CancellationToken cancellationToken = default)
        {
            SaveCallCount++;
            if (throwOnSave)
            {
                throw new InvalidOperationException();
            }

            return Task.FromResult(saveResult ?? new IncomingOrderSaveResult(true, null, 1));
        }

        public Task<IncomingOrderReceiptResult> ReceiveOrderAsync(int orderId, IReadOnlyList<IncomingOrderReceiptLineRequest> lines, DateTime receivedAtUtc, CancellationToken cancellationToken = default)
        {
            ReceiveCallCount++;
            return Task.FromResult(receiptResult ?? new IncomingOrderReceiptResult(true, null));
        }
    }
}
