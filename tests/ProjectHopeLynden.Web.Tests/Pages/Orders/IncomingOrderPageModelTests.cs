using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjectHopeLynden.Application.IncomingOrders;
using ProjectHopeLynden.Domain.Inventory;
using EditModel = ProjectHopeLynden.Web.Pages.Orders.EditModel;
using IndexModel = ProjectHopeLynden.Web.Pages.Orders.IndexModel;

namespace ProjectHopeLynden.Web.Tests.Pages.Orders;

public sealed class IncomingOrderPageModelTests
{
    [Fact]
    public async Task Index_OnGet_LoadsOrdersAndDefaultsExpectedDate()
    {
        var view = CreateOrdersView();
        var service = new StubIncomingOrderService { OrdersView = view };
        var model = new IndexModel(service);

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal("Incoming Orders", model.PageTitle);
        Assert.Contains("automatically add", model.Summary);
        Assert.Same(view, model.Orders);
        Assert.NotNull(model.ExpectedDate);
    }

    [Fact]
    public async Task Index_Create_SendsRequestAndRedirectsOnSuccess()
    {
        var service = new StubIncomingOrderService();
        var model = new IndexModel(service)
        {
            InventoryEntryId = 12,
            Quantity = 8.5,
            ExpectedDate = new DateOnly(2026, 7, 25),
            Source = "Supplier",
            Reference = "PO-79",
        };

        var result = await model.OnPostCreateAsync(CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Null(redirect.PageName);
        Assert.Equal("Incoming order scheduled.", model.StatusMessage);
        Assert.Equal(12, service.LastRequest?.InventoryEntryId);
        Assert.Equal(8.5, service.LastRequest?.Quantity);
        Assert.Equal(new DateOnly(2026, 7, 25), service.LastRequest?.ExpectedDate);
    }

    [Fact]
    public async Task Index_Create_ReloadsPageWhenServiceFails()
    {
        var view = CreateOrdersView();
        var service = new StubIncomingOrderService
        {
            OrdersView = view,
            OperationResult = new IncomingOrderOperationResult(false, "Expected date is required."),
        };
        var model = new IndexModel(service);

        var result = await model.OnPostCreateAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.True(model.OperationFailed);
        Assert.Equal("Expected date is required.", model.OperationMessage);
        Assert.Same(view, model.Orders);
    }

    [Fact]
    public async Task Index_CancelAndReceive_InvokeExpectedOperations()
    {
        var service = new StubIncomingOrderService();
        var cancelModel = new IndexModel(service);
        var receiveModel = new IndexModel(service);

        var cancelResult = await cancelModel.OnPostCancelAsync(7, CancellationToken.None);
        var receiveResult = await receiveModel.OnPostReceiveAsync(8, CancellationToken.None);

        Assert.IsType<RedirectToPageResult>(cancelResult);
        Assert.Equal(7, service.CancelledOrderId);
        Assert.Equal("Incoming order cancelled.", cancelModel.StatusMessage);
        Assert.IsType<RedirectToPageResult>(receiveResult);
        Assert.Equal(8, service.ReceivedOrderId);
        Assert.Equal("Incoming order received and added to inventory.", receiveModel.StatusMessage);
    }

    [Fact]
    public async Task Index_CancelAndReceive_ShowFallbackFailures()
    {
        var service = new StubIncomingOrderService
        {
            OrdersView = CreateOrdersView(),
            OperationResult = new IncomingOrderOperationResult(false, null),
        };
        var cancelModel = new IndexModel(service);
        var receiveModel = new IndexModel(service);

        var cancelResult = await cancelModel.OnPostCancelAsync(7, CancellationToken.None);
        var receiveResult = await receiveModel.OnPostReceiveAsync(8, CancellationToken.None);

        Assert.IsType<PageResult>(cancelResult);
        Assert.Equal("Incoming order could not be cancelled.", cancelModel.OperationMessage);
        Assert.IsType<PageResult>(receiveResult);
        Assert.Equal("Incoming order could not be received.", receiveModel.OperationMessage);
    }

    [Fact]
    public async Task Edit_OnGet_LoadsOrderValuesOrReturnsNotFound()
    {
        var editView = CreateEditView();
        var service = new StubIncomingOrderService { EditView = editView };
        var model = new EditModel(service) { IncomingOrderId = 7 };

        var result = await model.OnGetAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.Equal(12, model.InventoryEntryId);
        Assert.Equal(8.5, model.Quantity);
        Assert.Equal(new DateOnly(2026, 7, 25), model.ExpectedDate);
        Assert.Equal("Supplier", model.Source);
        Assert.Equal("PO-79", model.Reference);

        service.EditView = null;
        var missing = await model.OnGetAsync(CancellationToken.None);
        Assert.IsType<NotFoundResult>(missing);
    }

    [Fact]
    public async Task Edit_Save_UpdatesAndRedirectsOnSuccess()
    {
        var service = new StubIncomingOrderService { EditView = CreateEditView() };
        var model = new EditModel(service)
        {
            IncomingOrderId = 7,
            InventoryEntryId = 13,
            Quantity = 9,
            ExpectedDate = new DateOnly(2026, 7, 26),
            Source = "New supplier",
            Reference = "PO-80",
        };

        var result = await model.OnPostSaveAsync(CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Orders/Index", redirect.PageName);
        Assert.Equal("Incoming order updated.", model.StatusMessage);
        Assert.Equal(7, service.UpdatedOrderId);
        Assert.Equal(13, service.LastRequest?.InventoryEntryId);
    }

    [Fact]
    public async Task Edit_Save_ReloadsFailureWithoutOverwritingSubmittedValues()
    {
        var service = new StubIncomingOrderService
        {
            EditView = CreateEditView(),
            OperationResult = new IncomingOrderOperationResult(false, "Incoming quantity must be greater than zero."),
        };
        var model = new EditModel(service)
        {
            IncomingOrderId = 7,
            InventoryEntryId = 13,
            Quantity = -1,
            ExpectedDate = new DateOnly(2026, 7, 26),
        };

        var result = await model.OnPostSaveAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.True(model.OperationFailed);
        Assert.Equal("Incoming quantity must be greater than zero.", model.OperationMessage);
        Assert.Equal(13, model.InventoryEntryId);
        Assert.Equal(-1, model.Quantity);
    }

    [Fact]
    public async Task Edit_Save_ReturnsNotFoundWhenOrderDisappearsAfterFailure()
    {
        var service = new StubIncomingOrderService
        {
            EditView = null,
            OperationResult = new IncomingOrderOperationResult(false, null),
        };
        var model = new EditModel(service) { IncomingOrderId = 404 };

        var result = await model.OnPostSaveAsync(CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        Assert.Equal("Incoming order could not be updated.", model.OperationMessage);
    }

    private static IncomingOrdersView CreateOrdersView()
    {
        var option = new IncomingOrderInventoryOption(12, "Pinto Beans", "Dry Beans", "Shelf", false);
        var order = CreateOrder();
        return new IncomingOrdersView([option], [order], []);
    }

    private static IncomingOrderEditView CreateEditView()
    {
        return new IncomingOrderEditView(
            CreateOrder(),
            [new IncomingOrderInventoryOption(12, "Pinto Beans", "Dry Beans", "Shelf", false)]);
    }

    private static IncomingOrderListItem CreateOrder()
    {
        var timestamp = new DateTime(2026, 7, 18, 8, 0, 0, DateTimeKind.Utc);
        return new IncomingOrderListItem(
            7,
            12,
            "Pinto Beans",
            "Dry Beans",
            "Shelf",
            false,
            8.5,
            new DateOnly(2026, 7, 25),
            "Supplier",
            "PO-79",
            IncomingOrderStatus.Scheduled,
            timestamp,
            timestamp,
            null,
            null);
    }

    private sealed class StubIncomingOrderService : IIncomingOrderService
    {
        public IncomingOrdersView OrdersView { get; set; } = new([], [], []);

        public IncomingOrderEditView? EditView { get; set; }

        public IncomingOrderOperationResult OperationResult { get; set; } = new(true, null, 1);

        public IncomingOrderSaveRequest? LastRequest { get; private set; }

        public int? UpdatedOrderId { get; private set; }

        public int? CancelledOrderId { get; private set; }

        public int? ReceivedOrderId { get; private set; }

        public Task<IncomingOrdersView> GetOrdersAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(OrdersView);
        }

        public Task<IncomingOrderEditView?> GetForEditAsync(
            int incomingOrderId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(EditView);
        }

        public Task<IncomingOrderOperationResult> CreateAsync(
            IncomingOrderSaveRequest request,
            DateTime createdAtUtc,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(OperationResult);
        }

        public Task<IncomingOrderOperationResult> UpdateAsync(
            int incomingOrderId,
            IncomingOrderSaveRequest request,
            DateTime updatedAtUtc,
            CancellationToken cancellationToken = default)
        {
            UpdatedOrderId = incomingOrderId;
            LastRequest = request;
            return Task.FromResult(OperationResult);
        }

        public Task<IncomingOrderOperationResult> CancelAsync(
            int incomingOrderId,
            DateTime cancelledAtUtc,
            CancellationToken cancellationToken = default)
        {
            CancelledOrderId = incomingOrderId;
            return Task.FromResult(OperationResult);
        }

        public Task<IncomingOrderOperationResult> ReceiveAsync(
            int incomingOrderId,
            DateTime receivedAtUtc,
            CancellationToken cancellationToken = default)
        {
            ReceivedOrderId = incomingOrderId;
            return Task.FromResult(OperationResult);
        }

        public Task<IncomingOrderProcessingResult> ReceiveDueAsync(
            DateOnly dueThroughDate,
            DateTime receivedAtUtc,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new IncomingOrderProcessingResult(0, 0));
        }
    }
}
