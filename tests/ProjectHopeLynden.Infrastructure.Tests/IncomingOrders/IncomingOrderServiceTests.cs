using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProjectHopeLynden.Application.IncomingOrders;
using ProjectHopeLynden.Domain.Inventory;
using ProjectHopeLynden.Infrastructure.IncomingOrders;
using ProjectHopeLynden.Infrastructure.Persistence;

namespace ProjectHopeLynden.Infrastructure.Tests.IncomingOrders;

public sealed class IncomingOrderServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private ProjectHopeDbContext context = null!;

    public async Task InitializeAsync()
    {
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ProjectHopeDbContext>()
            .UseSqlite(connection)
            .Options;

        context = new ProjectHopeDbContext(options);
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await context.DisposeAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task GetOrdersAsync_ReturnsInventoryOptionsAndSeparatesScheduledFromCompleted()
    {
        var beans = await AddInventoryEntryAsync("Dry Beans", "Pinto Beans", "Shelf", 10, false);
        var cereal = await AddInventoryEntryAsync("Cereals", "Oat Cereal", "Back Room", 5, true);
        var createdAtUtc = new DateTime(2026, 7, 18, 8, 0, 0, DateTimeKind.Utc);

        context.IncomingOrderLines.AddRange(
            CreateOrder(beans, 4, new DateOnly(2026, 7, 20), createdAtUtc),
            CreateOrder(cereal, 2, new DateOnly(2026, 7, 19), createdAtUtc, IncomingOrderStatus.Received));
        await context.SaveChangesAsync();

        var service = new IncomingOrderService(context);

        var view = await service.GetOrdersAsync();

        Assert.Equal(2, view.InventoryOptions.Count);
        Assert.Equal("Cereals", view.InventoryOptions[0].CategoryName);
        Assert.Contains("Commodity", view.InventoryOptions[0].DisplayName);
        Assert.Single(view.ScheduledOrders);
        Assert.Equal("Pinto Beans", view.ScheduledOrders[0].ItemName);
        Assert.Single(view.CompletedOrders);
        Assert.Equal(IncomingOrderStatus.Received, view.CompletedOrders[0].Status);
    }

    [Fact]
    public async Task GetForEditAsync_ReturnsOrderAndOptionsOrNull()
    {
        var entry = await AddInventoryEntryAsync("Dry Beans", "Pinto Beans", "Shelf", 10, false);
        var order = CreateOrder(entry, 4, new DateOnly(2026, 7, 20), DateTime.UtcNow);
        context.IncomingOrderLines.Add(order);
        await context.SaveChangesAsync();
        var service = new IncomingOrderService(context);

        var editView = await service.GetForEditAsync(order.Id);
        var missing = await service.GetForEditAsync(404);

        Assert.NotNull(editView);
        Assert.Equal(order.Id, editView.Order.Id);
        Assert.Single(editView.InventoryOptions);
        Assert.Null(missing);
    }

    [Fact]
    public async Task CreateAsync_SavesTrimmedScheduledOrder()
    {
        var entry = await AddInventoryEntryAsync("Dry Beans", "Pinto Beans", "Shelf", 10, false);
        var createdAtUtc = new DateTime(2026, 7, 18, 9, 0, 0, DateTimeKind.Utc);
        var service = new IncomingOrderService(context);

        var result = await service.CreateAsync(
            new IncomingOrderSaveRequest(
                entry.Id,
                12.5,
                new DateOnly(2026, 7, 25),
                "  Bellingham Food Bank  ",
                "  PO-79  "),
            createdAtUtc);

        Assert.True(result.Succeeded, result.ErrorMessage);
        var saved = await context.IncomingOrderLines.SingleAsync();
        Assert.Equal(12.5, saved.Quantity);
        Assert.Equal(new DateOnly(2026, 7, 25), saved.ExpectedDate);
        Assert.Equal("Bellingham Food Bank", saved.Source);
        Assert.Equal("PO-79", saved.Reference);
        Assert.Equal(IncomingOrderStatus.Scheduled, saved.Status);
        Assert.Equal(createdAtUtc, saved.CreatedAtUtc);
        Assert.Equal(createdAtUtc, saved.UpdatedAtUtc);
    }

    [Theory]
    [InlineData(null, 2.0, "2026-07-20", "Inventory row is required.")]
    [InlineData(0, 2.0, "2026-07-20", "Inventory row is required.")]
    [InlineData(1, 0.0, "2026-07-20", "Incoming quantity must be greater than zero.")]
    [InlineData(1, -1.0, "2026-07-20", "Incoming quantity must be greater than zero.")]
    [InlineData(1, 2.0, null, "Expected date is required.")]
    public async Task CreateAsync_RejectsMissingCoreValues(
        int? inventoryEntryId,
        double? quantity,
        string? expectedDateText,
        string expectedMessage)
    {
        DateOnly? expectedDate = expectedDateText is null
            ? null
            : DateOnly.Parse(expectedDateText);
        var service = new IncomingOrderService(context);

        var result = await service.CreateAsync(
            new IncomingOrderSaveRequest(inventoryEntryId, quantity, expectedDate, null, null),
            DateTime.UtcNow);

        Assert.False(result.Succeeded);
        Assert.Equal(expectedMessage, result.ErrorMessage);
        Assert.False(await context.IncomingOrderLines.AnyAsync());
    }

    [Fact]
    public async Task CreateAsync_RejectsUnknownEntryAndOverlongOptionalValues()
    {
        var entry = await AddInventoryEntryAsync("Dry Beans", "Pinto Beans", "Shelf", 10, false);
        var service = new IncomingOrderService(context);
        var date = new DateOnly(2026, 7, 20);

        var unknown = await service.CreateAsync(
            new IncomingOrderSaveRequest(404, 2, date, null, null),
            DateTime.UtcNow);
        var longSource = await service.CreateAsync(
            new IncomingOrderSaveRequest(entry.Id, 2, date, new string('s', 151), null),
            DateTime.UtcNow);
        var longReference = await service.CreateAsync(
            new IncomingOrderSaveRequest(entry.Id, 2, date, null, new string('r', 101)),
            DateTime.UtcNow);

        Assert.Equal("Inventory row was not found.", unknown.ErrorMessage);
        Assert.Equal("Source must be 150 characters or fewer.", longSource.ErrorMessage);
        Assert.Equal("Reference must be 100 characters or fewer.", longReference.ErrorMessage);
    }

    [Fact]
    public async Task UpdateAsync_ChangesScheduledOrderAndRejectsCompletedOrder()
    {
        var firstEntry = await AddInventoryEntryAsync("Dry Beans", "Pinto Beans", "Shelf", 10, false);
        var secondEntry = await AddInventoryEntryAsync("Cereals", "Oat Cereal", "Back Room", 5, true);
        var createdAtUtc = new DateTime(2026, 7, 18, 8, 0, 0, DateTimeKind.Utc);
        var order = CreateOrder(firstEntry, 4, new DateOnly(2026, 7, 20), createdAtUtc);
        context.IncomingOrderLines.Add(order);
        await context.SaveChangesAsync();
        var service = new IncomingOrderService(context);
        var updatedAtUtc = createdAtUtc.AddHours(2);

        var updated = await service.UpdateAsync(
            order.Id,
            new IncomingOrderSaveRequest(
                secondEntry.Id,
                8,
                new DateOnly(2026, 7, 22),
                "Supplier",
                "REF-2"),
            updatedAtUtc);

        Assert.True(updated.Succeeded, updated.ErrorMessage);
        var saved = await context.IncomingOrderLines.SingleAsync();
        Assert.Equal(secondEntry.Id, saved.InventoryEntryId);
        Assert.Equal(8, saved.Quantity);
        Assert.Equal(new DateOnly(2026, 7, 22), saved.ExpectedDate);
        Assert.Equal(updatedAtUtc, saved.UpdatedAtUtc);

        saved.Status = IncomingOrderStatus.Received;
        await context.SaveChangesAsync();
        var rejected = await service.UpdateAsync(
            saved.Id,
            new IncomingOrderSaveRequest(firstEntry.Id, 1, new DateOnly(2026, 7, 23), null, null),
            updatedAtUtc.AddHours(1));

        Assert.False(rejected.Succeeded);
        Assert.Equal("Only scheduled incoming orders can be changed.", rejected.ErrorMessage);
    }

    [Fact]
    public async Task CancelAsync_CancelsScheduledOrderAndRejectsRepeat()
    {
        var entry = await AddInventoryEntryAsync("Dry Beans", "Pinto Beans", "Shelf", 10, false);
        var order = CreateOrder(entry, 4, new DateOnly(2026, 7, 20), DateTime.UtcNow);
        context.IncomingOrderLines.Add(order);
        await context.SaveChangesAsync();
        var service = new IncomingOrderService(context);
        var cancelledAtUtc = new DateTime(2026, 7, 19, 12, 0, 0, DateTimeKind.Utc);

        var result = await service.CancelAsync(order.Id, cancelledAtUtc);
        var repeat = await service.CancelAsync(order.Id, cancelledAtUtc.AddMinutes(1));

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.False(repeat.Succeeded);
        var saved = await context.IncomingOrderLines.SingleAsync();
        Assert.Equal(IncomingOrderStatus.Cancelled, saved.Status);
        Assert.Equal(cancelledAtUtc, saved.CancelledAtUtc);
        Assert.Equal(cancelledAtUtc, saved.UpdatedAtUtc);
    }

    [Fact]
    public async Task ReceiveAsync_AddsQuantityOnceAndStoresNormalInventoryHistory()
    {
        var entry = await AddInventoryEntryAsync("Dry Beans", "Pinto Beans", "Shelf", 10, true);
        var order = CreateOrder(entry, 4.5, new DateOnly(2026, 7, 20), DateTime.UtcNow);
        context.IncomingOrderLines.Add(order);
        await context.SaveChangesAsync();
        var service = new IncomingOrderService(context);
        var receivedAtUtc = new DateTime(2026, 7, 19, 12, 0, 0, DateTimeKind.Utc);

        var result = await service.ReceiveAsync(order.Id, receivedAtUtc);
        var repeat = await service.ReceiveAsync(order.Id, receivedAtUtc.AddMinutes(1));

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.False(repeat.Succeeded);
        var savedEntry = await context.InventoryEntries.AsNoTracking().SingleAsync();
        Assert.Equal(14.5, savedEntry.CurrentQuantity);
        Assert.Equal(receivedAtUtc, savedEntry.LastUpdatedAtUtc);

        var history = await context.InventoryCountHistory.AsNoTracking().SingleAsync();
        Assert.Equal(10, history.PreviousQuantity);
        Assert.Equal(14.5, history.CountedQuantity);
        Assert.Equal(4.5, history.QuantityChange);
        Assert.Equal("Pinto Beans", history.ItemNameAtCount);
        Assert.Equal("Dry Beans", history.CategoryNameAtCount);
        Assert.Equal("Shelf", history.LocationNameAtCount);
        Assert.True(history.IsCommodityAtCount);

        var savedOrder = await context.IncomingOrderLines.AsNoTracking().SingleAsync();
        Assert.Equal(IncomingOrderStatus.Received, savedOrder.Status);
        Assert.Equal(receivedAtUtc, savedOrder.ReceivedAtUtc);
    }

    [Fact]
    public async Task ReceiveDueAsync_ReceivesOnlyDueScheduledOrdersAndIsIdempotent()
    {
        var entry = await AddInventoryEntryAsync("Dry Beans", "Pinto Beans", "Shelf", 10, false);
        var now = new DateTime(2026, 7, 20, 16, 0, 0, DateTimeKind.Utc);
        context.IncomingOrderLines.AddRange(
            CreateOrder(entry, 2, new DateOnly(2026, 7, 19), now.AddDays(-2)),
            CreateOrder(entry, 3, new DateOnly(2026, 7, 20), now.AddDays(-2)),
            CreateOrder(entry, 8, new DateOnly(2026, 7, 21), now.AddDays(-2)),
            CreateOrder(entry, 20, new DateOnly(2026, 7, 18), now.AddDays(-2), IncomingOrderStatus.Cancelled));
        await context.SaveChangesAsync();
        var service = new IncomingOrderService(context);

        var result = await service.ReceiveDueAsync(new DateOnly(2026, 7, 20), now);
        var repeat = await service.ReceiveDueAsync(new DateOnly(2026, 7, 20), now.AddMinutes(1));

        Assert.Equal(2, result.ReceivedOrderCount);
        Assert.Equal(5, result.AddedQuantity);
        Assert.Equal(0, repeat.ReceivedOrderCount);
        Assert.Equal(0, repeat.AddedQuantity);

        var savedEntry = await context.InventoryEntries.AsNoTracking().SingleAsync();
        Assert.Equal(15, savedEntry.CurrentQuantity);
        Assert.Equal(2, await context.InventoryCountHistory.CountAsync());
        Assert.Equal(2, await context.IncomingOrderLines.CountAsync(order => order.Status == IncomingOrderStatus.Received));
        Assert.Single(await context.IncomingOrderLines.Where(order => order.Status == IncomingOrderStatus.Scheduled).ToListAsync());
    }

    [Fact]
    public async Task Operations_ReturnNotFoundForUnknownOrder()
    {
        var service = new IncomingOrderService(context);
        var request = new IncomingOrderSaveRequest(1, 2, new DateOnly(2026, 7, 20), null, null);

        var update = await service.UpdateAsync(404, request, DateTime.UtcNow);
        var cancel = await service.CancelAsync(404, DateTime.UtcNow);
        var receive = await service.ReceiveAsync(404, DateTime.UtcNow);

        Assert.Equal("Incoming order was not found.", update.ErrorMessage);
        Assert.Equal("Incoming order was not found.", cancel.ErrorMessage);
        Assert.Equal("Incoming order was not found.", receive.ErrorMessage);
    }

    private async Task<InventoryEntry> AddInventoryEntryAsync(
        string categoryName,
        string itemName,
        string locationName,
        double quantity,
        bool isCommodity)
    {
        var entry = new InventoryEntry
        {
            Category = new Category { Name = categoryName },
            Item = new Item { Name = itemName },
            Location = new Location { Name = locationName },
            CurrentQuantity = quantity,
            IsCommodity = isCommodity,
            IsMenuItem = false,
            LastUpdatedAtUtc = new DateTime(2026, 7, 18, 7, 0, 0, DateTimeKind.Utc),
        };

        context.InventoryEntries.Add(entry);
        await context.SaveChangesAsync();
        return entry;
    }

    private static IncomingOrderLine CreateOrder(
        InventoryEntry entry,
        double quantity,
        DateOnly expectedDate,
        DateTime createdAtUtc,
        IncomingOrderStatus status = IncomingOrderStatus.Scheduled)
    {
        return new IncomingOrderLine
        {
            InventoryEntryId = entry.Id,
            InventoryEntry = entry,
            Quantity = quantity,
            ExpectedDate = expectedDate,
            Status = status,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = createdAtUtc,
            ReceivedAtUtc = status == IncomingOrderStatus.Received ? createdAtUtc : null,
            CancelledAtUtc = status == IncomingOrderStatus.Cancelled ? createdAtUtc : null,
        };
    }
}
