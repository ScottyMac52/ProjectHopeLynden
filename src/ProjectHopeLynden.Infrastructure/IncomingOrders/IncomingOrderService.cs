using Microsoft.EntityFrameworkCore;
using ProjectHopeLynden.Application.IncomingOrders;
using ProjectHopeLynden.Domain.Inventory;
using ProjectHopeLynden.Infrastructure.Persistence;

namespace ProjectHopeLynden.Infrastructure.IncomingOrders;

public sealed class IncomingOrderService(ProjectHopeDbContext context) : IIncomingOrderService
{
    private static readonly SemaphoreSlim ProcessingLock = new(1, 1);

    public async Task<IncomingOrdersView> GetOrdersAsync(CancellationToken cancellationToken = default)
    {
        var inventoryOptions = await context.InventoryEntries
            .AsNoTracking()
            .OrderBy(entry => entry.Category.Name)
            .ThenBy(entry => entry.Item.Name)
            .ThenBy(entry => entry.Location.Name)
            .ThenBy(entry => entry.IsCommodity)
            .Select(entry => new IncomingOrderInventoryOption(
                entry.Id,
                entry.Item.Name,
                entry.Category.Name,
                entry.Location.Name,
                entry.IsCommodity))
            .ToListAsync(cancellationToken);

        var scheduledOrders = await BuildOrderQuery()
            .Where(order => order.Status == IncomingOrderStatus.Scheduled)
            .OrderBy(order => order.ExpectedDate)
            .ThenBy(order => order.InventoryEntry.Item.Name)
            .ThenBy(order => order.Id)
            .Select(ProjectOrder())
            .ToListAsync(cancellationToken);

        var completedOrders = await BuildOrderQuery()
            .Where(order => order.Status != IncomingOrderStatus.Scheduled)
            .OrderByDescending(order => order.UpdatedAtUtc)
            .ThenByDescending(order => order.Id)
            .Take(100)
            .Select(ProjectOrder())
            .ToListAsync(cancellationToken);

        return new IncomingOrdersView(inventoryOptions, scheduledOrders, completedOrders);
    }

    public async Task<IncomingOrderOperationResult> CreateAsync(
        IncomingOrderSaveRequest request,
        DateTime createdAtUtc,
        CancellationToken cancellationToken = default)
    {
        var validationMessage = ValidateRequest(request);
        if (validationMessage is not null)
        {
            return Failure(validationMessage);
        }

        var inventoryEntryExists = await context.InventoryEntries
            .AnyAsync(entry => entry.Id == request.InventoryEntryId!.Value, cancellationToken);
        if (!inventoryEntryExists)
        {
            return Failure("Inventory row was not found.");
        }

        var order = new IncomingOrderLine
        {
            InventoryEntryId = request.InventoryEntryId.Value,
            Quantity = request.Quantity!.Value,
            ExpectedDate = request.ExpectedDate!.Value,
            Source = NormalizeOptional(request.Source),
            Reference = NormalizeOptional(request.Reference),
            Status = IncomingOrderStatus.Scheduled,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = createdAtUtc,
        };

        context.IncomingOrderLines.Add(order);
        await context.SaveChangesAsync(cancellationToken);

        return Success(order.Id);
    }

    public async Task<IncomingOrderOperationResult> UpdateAsync(
        int incomingOrderId,
        IncomingOrderSaveRequest request,
        DateTime updatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var validationMessage = ValidateRequest(request);
        if (validationMessage is not null)
        {
            return Failure(validationMessage);
        }

        var order = await context.IncomingOrderLines
            .SingleOrDefaultAsync(existingOrder => existingOrder.Id == incomingOrderId, cancellationToken);
        if (order is null)
        {
            return Failure("Incoming order was not found.");
        }

        if (order.Status != IncomingOrderStatus.Scheduled)
        {
            return Failure("Only scheduled incoming orders can be changed.");
        }

        var inventoryEntryExists = await context.InventoryEntries
            .AnyAsync(entry => entry.Id == request.InventoryEntryId!.Value, cancellationToken);
        if (!inventoryEntryExists)
        {
            return Failure("Inventory row was not found.");
        }

        order.InventoryEntryId = request.InventoryEntryId.Value;
        order.Quantity = request.Quantity!.Value;
        order.ExpectedDate = request.ExpectedDate!.Value;
        order.Source = NormalizeOptional(request.Source);
        order.Reference = NormalizeOptional(request.Reference);
        order.UpdatedAtUtc = updatedAtUtc;

        await context.SaveChangesAsync(cancellationToken);
        return Success(order.Id);
    }

    public async Task<IncomingOrderOperationResult> CancelAsync(
        int incomingOrderId,
        DateTime cancelledAtUtc,
        CancellationToken cancellationToken = default)
    {
        var order = await context.IncomingOrderLines
            .SingleOrDefaultAsync(existingOrder => existingOrder.Id == incomingOrderId, cancellationToken);
        if (order is null)
        {
            return Failure("Incoming order was not found.");
        }

        if (order.Status != IncomingOrderStatus.Scheduled)
        {
            return Failure("Only scheduled incoming orders can be cancelled.");
        }

        order.Status = IncomingOrderStatus.Cancelled;
        order.CancelledAtUtc = cancelledAtUtc;
        order.UpdatedAtUtc = cancelledAtUtc;
        await context.SaveChangesAsync(cancellationToken);

        return Success(order.Id);
    }

    public async Task<IncomingOrderOperationResult> ReceiveAsync(
        int incomingOrderId,
        DateTime receivedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await ProcessingLock.WaitAsync(cancellationToken);
        try
        {
            var order = await BuildOrderQuery()
                .SingleOrDefaultAsync(existingOrder => existingOrder.Id == incomingOrderId, cancellationToken);
            if (order is null)
            {
                return Failure("Incoming order was not found.");
            }

            if (order.Status != IncomingOrderStatus.Scheduled)
            {
                return Failure("Only scheduled incoming orders can be received.");
            }

            ApplyToInventory(order, receivedAtUtc);
            await context.SaveChangesAsync(cancellationToken);

            return Success(order.Id);
        }
        finally
        {
            ProcessingLock.Release();
        }
    }

    public async Task<IncomingOrderProcessingResult> ReceiveDueAsync(
        DateOnly dueThroughDate,
        DateTime receivedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await ProcessingLock.WaitAsync(cancellationToken);
        try
        {
            var dueOrders = await BuildOrderQuery()
                .Where(order =>
                    order.Status == IncomingOrderStatus.Scheduled &&
                    order.ExpectedDate <= dueThroughDate)
                .OrderBy(order => order.ExpectedDate)
                .ThenBy(order => order.Id)
                .ToListAsync(cancellationToken);

            foreach (var order in dueOrders)
            {
                ApplyToInventory(order, receivedAtUtc);
            }

            if (dueOrders.Count > 0)
            {
                await context.SaveChangesAsync(cancellationToken);
            }

            return new IncomingOrderProcessingResult(
                dueOrders.Count,
                dueOrders.Sum(order => order.Quantity));
        }
        finally
        {
            ProcessingLock.Release();
        }
    }

    private IQueryable<IncomingOrderLine> BuildOrderQuery()
    {
        return context.IncomingOrderLines
            .Include(order => order.InventoryEntry)
                .ThenInclude(entry => entry.Item)
            .Include(order => order.InventoryEntry)
                .ThenInclude(entry => entry.Category)
            .Include(order => order.InventoryEntry)
                .ThenInclude(entry => entry.Location);
    }

    private static System.Linq.Expressions.Expression<Func<IncomingOrderLine, IncomingOrderListItem>> ProjectOrder()
    {
        return order => new IncomingOrderListItem(
            order.Id,
            order.InventoryEntryId,
            order.InventoryEntry.Item.Name,
            order.InventoryEntry.Category.Name,
            order.InventoryEntry.Location.Name,
            order.InventoryEntry.IsCommodity,
            order.Quantity,
            order.ExpectedDate,
            order.Source,
            order.Reference,
            order.Status,
            order.CreatedAtUtc,
            order.UpdatedAtUtc,
            order.ReceivedAtUtc,
            order.CancelledAtUtc);
    }

    private void ApplyToInventory(IncomingOrderLine order, DateTime receivedAtUtc)
    {
        var entry = order.InventoryEntry;
        var previousQuantity = entry.CurrentQuantity;
        var updatedQuantity = previousQuantity + order.Quantity;

        entry.CurrentQuantity = updatedQuantity;
        entry.LastUpdatedAtUtc = receivedAtUtc;

        context.InventoryCountHistory.Add(new InventoryCountHistory
        {
            InventoryEntryId = entry.Id,
            CountedQuantity = updatedQuantity,
            CountedAtUtc = receivedAtUtc,
            PreviousQuantity = previousQuantity,
            QuantityChange = order.Quantity,
            ItemIdAtCount = entry.ItemId,
            ItemNameAtCount = entry.Item.Name,
            CategoryIdAtCount = entry.CategoryId,
            CategoryNameAtCount = entry.Category.Name,
            LocationIdAtCount = entry.LocationId,
            LocationNameAtCount = entry.Location.Name,
            IsCommodityAtCount = entry.IsCommodity,
        });

        order.Status = IncomingOrderStatus.Received;
        order.ReceivedAtUtc = receivedAtUtc;
        order.UpdatedAtUtc = receivedAtUtc;
    }

    private static string? ValidateRequest(IncomingOrderSaveRequest request)
    {
        if (request.InventoryEntryId is null or <= 0)
        {
            return "Inventory row is required.";
        }

        if (request.Quantity is null or <= 0)
        {
            return "Incoming quantity must be greater than zero.";
        }

        if (request.ExpectedDate is null)
        {
            return "Expected date is required.";
        }

        if (request.Source?.Trim().Length > 150)
        {
            return "Source must be 150 characters or fewer.";
        }

        if (request.Reference?.Trim().Length > 100)
        {
            return "Reference must be 100 characters or fewer.";
        }

        return null;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static IncomingOrderOperationResult Success(int incomingOrderId)
    {
        return new IncomingOrderOperationResult(true, null, incomingOrderId);
    }

    private static IncomingOrderOperationResult Failure(string errorMessage)
    {
        return new IncomingOrderOperationResult(false, errorMessage);
    }
}
