using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProjectHopeLynden.Application.IncomingOrders;
using ProjectHopeLynden.Domain.IncomingOrders;
using ProjectHopeLynden.Domain.Inventory;
using ProjectHopeLynden.Infrastructure.IncomingOrders;
using ProjectHopeLynden.Infrastructure.Persistence;

namespace ProjectHopeLynden.Infrastructure.Tests.IncomingOrders;

public sealed class IncomingOrderServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private ProjectHopeDbContext context = null!;
    private IncomingOrderService service = null!;

    public async Task InitializeAsync()
    {
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ProjectHopeDbContext>().UseSqlite(connection).Options;
        context = new ProjectHopeDbContext(options);
        await context.Database.EnsureCreatedAsync();
        service = new IncomingOrderService(context);
    }

    public async Task DisposeAsync()
    {
        await context.DisposeAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task SaveOrderAsync_PersistsMultipleStructuredLines()
    {
        var milk = await AddInventoryEntryAsync("Milk", 10, true);
        var eggs = await AddInventoryEntryAsync("Eggs", 6, false);

        var result = await service.SaveOrderAsync(null, ValidRequest(
            new IncomingOrderLineRequest(null, milk.Id, 18),
            new IncomingOrderLineRequest(null, eggs.Id, 24)), Utc(18));

        Assert.True(result.Succeeded);
        var order = await context.IncomingOrders.Include(saved => saved.Lines).SingleAsync();
        Assert.Equal("Edaleen Dairy", order.Vendor);
        Assert.Equal(IncomingOrderStatus.Pending, order.Status);
        Assert.Equal([18d, 24d], order.Lines.OrderBy(line => line.ExpectedQuantity).Select(line => line.ExpectedQuantity));
    }

    [Fact]
    public async Task SaveOrderAsync_RejectsDuplicateOrUnknownInventoryRows()
    {
        var milk = await AddInventoryEntryAsync("Milk", 10, false);

        var duplicate = await service.SaveOrderAsync(null, ValidRequest(
            new IncomingOrderLineRequest(null, milk.Id, 5),
            new IncomingOrderLineRequest(null, milk.Id, 7)), Utc(18));
        var unknown = await service.SaveOrderAsync(null, ValidRequest(
            new IncomingOrderLineRequest(null, 404, 5)), Utc(18));

        Assert.False(duplicate.Succeeded);
        Assert.Contains("same inventory row", duplicate.ErrorMessage);
        Assert.False(unknown.Succeeded);
        Assert.Contains("not found", unknown.ErrorMessage);
        Assert.False(await context.IncomingOrders.AnyAsync());
    }

    [Fact]
    public async Task GetOrdersAsync_ClassifiesUpcomingDueTodayAndOverdue()
    {
        var entry = await AddInventoryEntryAsync("Milk", 10, false);
        await AddOrderAsync(entry, new DateTime(2026, 7, 17), IncomingOrderStatus.Pending);
        await AddOrderAsync(entry, new DateTime(2026, 7, 18), IncomingOrderStatus.Ordered);
        await AddOrderAsync(entry, new DateTime(2026, 7, 19), IncomingOrderStatus.Pending);
        await AddOrderAsync(entry, new DateTime(2026, 7, 16), IncomingOrderStatus.Cancelled);

        var orders = await service.GetOrdersAsync(new DateTime(2026, 7, 18));

        Assert.Contains(orders, order => order.DateState == IncomingOrderDateState.Overdue);
        Assert.Contains(orders, order => order.DateState == IncomingOrderDateState.DueToday);
        Assert.Contains(orders, order => order.DateState == IncomingOrderDateState.Upcoming);
        Assert.Contains(orders, order => order.Status == IncomingOrderStatus.Cancelled && order.DateState == IncomingOrderDateState.Complete);
    }

    [Fact]
    public async Task ReceiveOrderAsync_AddsAdjustedQuantitiesAndStoresImmutableHistory()
    {
        var commodityMilk = await AddInventoryEntryAsync("Milk", 10, true);
        var regularMilk = await AddInventoryEntryAsync("Milk", 4, false);
        var order = await AddOrderAsync(commodityMilk, new DateTime(2026, 7, 18), IncomingOrderStatus.Ordered);
        order.Lines.Add(new IncomingOrderLine { InventoryEntryId = regularMilk.Id, ExpectedQuantity = 8 });
        await context.SaveChangesAsync();
        var lines = order.Lines.OrderBy(line => line.InventoryEntryId).ToArray();
        var receivedAtUtc = Utc(19);

        var result = await service.ReceiveOrderAsync(order.Id,
            [new(lines[0].Id, 12), new(lines[1].Id, 6)], receivedAtUtc);

        Assert.True(result.Succeeded);
        var quantities = await context.InventoryEntries.AsNoTracking()
            .OrderBy(entry => entry.Id).Select(entry => entry.CurrentQuantity).ToArrayAsync();
        Assert.Equal([22d, 10d], quantities);
        var history = await context.InventoryCountHistory.AsNoTracking().OrderBy(record => record.InventoryEntryId).ToArrayAsync();
        Assert.Equal([12d, 6d], history.Select(record => record.QuantityChange));
        Assert.Equal([true, false], history.Select(record => record.IsCommodityAtCount));
        Assert.All(history, record => Assert.Equal(receivedAtUtc, record.CountedAtUtc));
        var savedOrder = await context.IncomingOrders.AsNoTracking().SingleAsync();
        Assert.Equal(IncomingOrderStatus.Received, savedOrder.Status);
        Assert.Equal(receivedAtUtc, savedOrder.ReceivedAtUtc);
    }

    [Fact]
    public async Task ReceiveOrderAsync_IsIdempotent()
    {
        var entry = await AddInventoryEntryAsync("Eggs", 5, false);
        var order = await AddOrderAsync(entry, new DateTime(2026, 7, 18), IncomingOrderStatus.Pending);
        var receipt = new[] { new IncomingOrderReceiptLineRequest(order.Lines.Single().Id, 4) };

        var first = await service.ReceiveOrderAsync(order.Id, receipt, Utc(18));
        var second = await service.ReceiveOrderAsync(order.Id, receipt, Utc(19));

        Assert.True(first.Succeeded);
        Assert.False(second.Succeeded);
        Assert.Contains("already", second.ErrorMessage);
        Assert.Equal(9d, (await context.InventoryEntries.AsNoTracking().SingleAsync()).CurrentQuantity);
        Assert.Single(await context.InventoryCountHistory.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task ReceiveOrderAsync_RejectsCancelledOrderWithoutChangingInventory()
    {
        var entry = await AddInventoryEntryAsync("Beans", 8, false);
        var order = await AddOrderAsync(entry, new DateTime(2026, 7, 18), IncomingOrderStatus.Cancelled);

        var result = await service.ReceiveOrderAsync(order.Id,
            [new(order.Lines.Single().Id, 3)], Utc(18));

        Assert.False(result.Succeeded);
        Assert.Equal(8d, (await context.InventoryEntries.AsNoTracking().SingleAsync()).CurrentQuantity);
        Assert.False(await context.InventoryCountHistory.AnyAsync());
    }

    [Fact]
    public async Task ReceiveOrderAsync_RejectsIncompleteReceiptAtomically()
    {
        var milk = await AddInventoryEntryAsync("Milk", 10, false);
        var eggs = await AddInventoryEntryAsync("Eggs", 20, false);
        var order = await AddOrderAsync(milk, new DateTime(2026, 7, 18), IncomingOrderStatus.Pending);
        order.Lines.Add(new IncomingOrderLine { InventoryEntryId = eggs.Id, ExpectedQuantity = 4 });
        await context.SaveChangesAsync();

        var result = await service.ReceiveOrderAsync(order.Id,
            [new(order.Lines.First().Id, 3)], Utc(18));
        var savedQuantities = await context.InventoryEntries.AsNoTracking()
            .OrderBy(entry => entry.Id)
            .Select(entry => entry.CurrentQuantity)
            .ToArrayAsync();

        Assert.False(result.Succeeded);
        Assert.Equal([10d, 20d], savedQuantities);
        Assert.False(await context.InventoryCountHistory.AnyAsync());
        Assert.Equal(IncomingOrderStatus.Pending, (await context.IncomingOrders.AsNoTracking().SingleAsync()).Status);
    }

    [Fact]
    public async Task SaveOrderAsync_DoesNotAllowReceivedOrderToBeRewritten()
    {
        var entry = await AddInventoryEntryAsync("Rice", 10, false);
        var order = await AddOrderAsync(entry, new DateTime(2026, 7, 18), IncomingOrderStatus.Received);
        order.ReceivedAtUtc = Utc(18);
        await context.SaveChangesAsync();

        var result = await service.SaveOrderAsync(order.Id, ValidRequest(
            new IncomingOrderLineRequest(order.Lines.Single().Id, entry.Id, 99)), Utc(19));

        Assert.False(result.Succeeded);
        Assert.Contains("cannot be edited", result.ErrorMessage);
        Assert.Equal(6d, (await context.IncomingOrderLines.AsNoTracking().SingleAsync()).ExpectedQuantity);
    }

    [Fact]
    public async Task InventoryOptionsAndOrderView_PreserveEntryClassificationAndOrderFields()
    {
        var entry = await AddInventoryEntryAsync("Milk", 10, true);
        var order = await AddOrderAsync(entry, new DateTime(2026, 7, 22), IncomingOrderStatus.Ordered);
        order.InvoiceNumber = "AOR-123";
        order.InvoiceAmount = 338.58;
        order.ProductSummary = "Milk";
        order.Notes = "18 cases";
        await context.SaveChangesAsync();

        var options = await service.GetInventoryOptionsAsync();
        var view = await service.GetOrderAsync(order.Id);

        Assert.Contains("Milk", Assert.Single(options).DisplayName);
        Assert.Contains("Commodity", options.Single().DisplayName);
        Assert.NotNull(view);
        Assert.Equal("AOR-123", view.InvoiceNumber);
        Assert.Equal(338.58, view.InvoiceAmount);
        Assert.Contains("Commodity", view.Lines.Single().InventoryEntryName);
        Assert.Null(await service.GetOrderAsync(404));
    }

    [Theory]
    [MemberData(nameof(InvalidSaveRequests))]
    public async Task SaveOrderAsync_RejectsInvalidOrderFields(
        IncomingOrderSaveRequest request,
        string expectedMessage)
    {
        if (request.Lines.Any(line => line.InventoryEntryId == 1))
        {
            await AddInventoryEntryAsync("Milk", 10, false);
        }

        var result = await service.SaveOrderAsync(null, request, Utc(18));

        Assert.False(result.Succeeded);
        Assert.Contains(expectedMessage, result.ErrorMessage);
        Assert.False(await context.IncomingOrders.AnyAsync());
    }

    public static TheoryData<IncomingOrderSaveRequest, string> InvalidSaveRequests => new()
    {
        { ValidRequestForValidation() with { OrderDate = null }, "Order date" },
        { ValidRequestForValidation() with { Vendor = " " }, "Vendor" },
        { ValidRequestForValidation() with { ExpectedDate = null }, "Expected" },
        { ValidRequestForValidation() with { Status = IncomingOrderStatus.Received }, "Receive Order" },
        { ValidRequestForValidation() with { Status = (IncomingOrderStatus)99 }, "Choose Pending" },
        { ValidRequestForValidation() with { InvoiceAmount = -1 }, "cannot be negative" },
        { ValidRequestForValidation() with { Weight = -1 }, "cannot be negative" },
        { ValidRequestForValidation() with { Lines = [] }, "At least one" },
        { ValidRequestForValidation() with { Lines = [new(null, 1, 0)] }, "At least one" },
    };

    [Fact]
    public async Task SaveOrderAsync_UpdatesEditableOrderAndNormalizesOptionalText()
    {
        var entry = await AddInventoryEntryAsync("Milk", 10, false);
        var order = await AddOrderAsync(entry, new DateTime(2026, 7, 18), IncomingOrderStatus.Pending);
        var request = ValidRequest(new IncomingOrderLineRequest(order.Lines.Single().Id, entry.Id, 9)) with
        {
            Vendor = " Food Lifeline ",
            InvoiceNumber = " ",
            Notes = " updated ",
        };

        var result = await service.SaveOrderAsync(order.Id, request, Utc(19));

        Assert.True(result.Succeeded);
        var saved = await context.IncomingOrders.Include(savedOrder => savedOrder.Lines).SingleAsync();
        Assert.Equal("Food Lifeline", saved.Vendor);
        Assert.Null(saved.InvoiceNumber);
        Assert.Equal("updated", saved.Notes);
        Assert.Equal(9d, saved.Lines.Single().ExpectedQuantity);
    }

    [Theory]
    [InlineData(false, false, "not found")]
    [InlineData(true, false, "every order line")]
    [InlineData(false, true, "greater than zero")]
    public async Task ReceiveOrderAsync_RejectsMissingDuplicateOrNonPositiveLines(
        bool duplicateLines,
        bool zeroQuantity,
        string expectedMessage)
    {
        var entry = await AddInventoryEntryAsync("Milk", 10, false);
        if (!duplicateLines && !zeroQuantity)
        {
            var missing = await service.ReceiveOrderAsync(404, [], Utc(18));
            Assert.Contains(expectedMessage, missing.ErrorMessage);
            return;
        }

        var order = await AddOrderAsync(entry, new DateTime(2026, 7, 18), IncomingOrderStatus.Pending);
        var lineId = order.Lines.Single().Id;
        var receiptLines = duplicateLines
            ? new[] { new IncomingOrderReceiptLineRequest(lineId, 2), new IncomingOrderReceiptLineRequest(lineId, 3) }
            : [new IncomingOrderReceiptLineRequest(lineId, 0)];

        var result = await service.ReceiveOrderAsync(order.Id, receiptLines, Utc(18));

        Assert.False(result.Succeeded);
        Assert.Contains(expectedMessage, result.ErrorMessage);
        Assert.Equal(10d, (await context.InventoryEntries.AsNoTracking().SingleAsync()).CurrentQuantity);
    }

    private async Task<InventoryEntry> AddInventoryEntryAsync(string itemName, double quantity, bool isCommodity)
    {
        var entry = new InventoryEntry
        {
            Category = new Category { Name = $"Category {itemName} {isCommodity}" },
            Item = await context.Items.SingleOrDefaultAsync(item => item.Name == itemName) ?? new Item { Name = itemName },
            Location = new Location { Name = $"Location {itemName} {isCommodity}" },
            CurrentQuantity = quantity,
            IsCommodity = isCommodity,
            LastUpdatedAtUtc = Utc(1),
        };
        context.InventoryEntries.Add(entry);
        await context.SaveChangesAsync();
        return entry;
    }

    private async Task<IncomingOrder> AddOrderAsync(InventoryEntry entry, DateTime expectedDate, IncomingOrderStatus status)
    {
        var order = new IncomingOrder
        {
            Vendor = "Food Lifeline",
            OrderDate = new DateTime(2026, 7, 15),
            ExpectedDate = expectedDate,
            Status = status,
            CreatedAtUtc = Utc(15),
            Lines = [new IncomingOrderLine { InventoryEntryId = entry.Id, ExpectedQuantity = 6 }],
        };
        context.IncomingOrders.Add(order);
        await context.SaveChangesAsync();
        return order;
    }

    private static IncomingOrderSaveRequest ValidRequest(params IncomingOrderLineRequest[] lines) => new(
        new DateTime(2026, 7, 15), " Edaleen Dairy ", IncomingOrderStatus.Pending,
        null, null, 338.58, null, "BFB", "EFAP", new DateTime(2026, 7, 22), 729,
        "Milk", "18 cases", lines);

    private static IncomingOrderSaveRequest ValidRequestForValidation() => ValidRequest(new IncomingOrderLineRequest(null, 1, 4));

    private static DateTime Utc(int day) => new(2026, 7, day, 12, 0, 0, DateTimeKind.Utc);
}
