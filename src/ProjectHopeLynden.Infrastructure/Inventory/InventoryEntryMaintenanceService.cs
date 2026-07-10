using Microsoft.EntityFrameworkCore;
using ProjectHopeLynden.Application.Inventory;
using ProjectHopeLynden.Domain.Inventory;
using ProjectHopeLynden.Infrastructure.Persistence;

namespace ProjectHopeLynden.Infrastructure.Inventory;

public sealed class InventoryEntryMaintenanceService(ProjectHopeDbContext context) : IInventoryEntryMaintenanceService
{
    public async Task<InventoryEntryFormOptions> GetFormOptionsAsync(CancellationToken cancellationToken = default)
    {
        var categories = await context.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => new InventoryCategoryListItem(category.Id, category.Name))
            .ToListAsync(cancellationToken);

        var locations = await context.Locations
            .AsNoTracking()
            .OrderBy(location => location.Name)
            .Select(location => new InventoryLocationListItem(location.Id, location.Name))
            .ToListAsync(cancellationToken);

        return new InventoryEntryFormOptions(categories, locations);
    }

    public async Task<InventoryEntryEditView?> GetEntryForEditAsync(
        int inventoryEntryId,
        CancellationToken cancellationToken = default)
    {
        return await context.InventoryEntries
            .AsNoTracking()
            .Where(entry => entry.Id == inventoryEntryId)
            .Select(entry => new InventoryEntryEditView(
                entry.Id,
                entry.Item.Name,
                entry.CategoryId,
                entry.LocationId,
                entry.CurrentQuantity,
                entry.BestByDate,
                entry.IsCommodity,
                entry.IsMenuItem))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<InventoryEntrySaveResult> CreateEntryAsync(
        InventoryEntrySaveRequest request,
        DateTime savedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var validationMessage = ValidateRequest(request, requireCurrentQuantity: true);
        if (validationMessage is not null)
        {
            return Failure(validationMessage);
        }

        var categoryExists = await context.Categories
            .AnyAsync(category => category.Id == request.CategoryId!.Value, cancellationToken);
        if (!categoryExists)
        {
            return Failure("Category is required.");
        }

        var locationExists = await context.Locations
            .AnyAsync(location => location.Id == request.LocationId!.Value, cancellationToken);
        if (!locationExists)
        {
            return Failure("Location is required.");
        }

        var item = await FindOrCreateItemAsync(request.ItemName!, cancellationToken);

        var entry = new InventoryEntry
        {
            Item = item,
            CategoryId = request.CategoryId.Value,
            LocationId = request.LocationId.Value,
            CurrentQuantity = request.CurrentQuantity!.Value,
            BestByDate = request.BestByDate,
            IsCommodity = request.IsCommodity,
            IsMenuItem = request.IsMenuItem,
            LastUpdatedAtUtc = savedAtUtc,
        };

        context.InventoryEntries.Add(entry);
        await context.SaveChangesAsync(cancellationToken);

        return Success(entry.Id, entry.CategoryId);
    }

    public async Task<InventoryEntrySaveResult> UpdateEntryAsync(
        int inventoryEntryId,
        InventoryEntrySaveRequest request,
        DateTime savedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var validationMessage = ValidateRequest(request, requireCurrentQuantity: false);
        if (validationMessage is not null)
        {
            return Failure(validationMessage);
        }

        var entry = await context.InventoryEntries
            .SingleOrDefaultAsync(inventoryEntry => inventoryEntry.Id == inventoryEntryId, cancellationToken);
        if (entry is null)
        {
            return Failure("Inventory entry was not found.");
        }

        var categoryExists = await context.Categories
            .AnyAsync(category => category.Id == request.CategoryId!.Value, cancellationToken);
        if (!categoryExists)
        {
            return Failure("Category is required.");
        }

        var locationExists = await context.Locations
            .AnyAsync(location => location.Id == request.LocationId!.Value, cancellationToken);
        if (!locationExists)
        {
            return Failure("Location is required.");
        }

        var item = await FindOrCreateItemAsync(request.ItemName!, cancellationToken);

        entry.Item = item;
        entry.CategoryId = request.CategoryId.Value;
        entry.LocationId = request.LocationId.Value;
        entry.BestByDate = request.BestByDate;
        entry.IsCommodity = request.IsCommodity;
        entry.IsMenuItem = request.IsMenuItem;
        entry.LastUpdatedAtUtc = savedAtUtc;

        await context.SaveChangesAsync(cancellationToken);

        return Success(entry.Id, entry.CategoryId);
    }

    private async Task<Item> FindOrCreateItemAsync(string itemName, CancellationToken cancellationToken)
    {
        var trimmedItemName = itemName.Trim();
        var item = await context.Items
            .SingleOrDefaultAsync(existingItem => existingItem.Name == trimmedItemName, cancellationToken);

        if (item is not null)
        {
            return item;
        }

        item = new Item { Name = trimmedItemName };
        context.Items.Add(item);
        return item;
    }

    private static string? ValidateRequest(InventoryEntrySaveRequest request, bool requireCurrentQuantity)
    {
        if (string.IsNullOrWhiteSpace(request.ItemName))
        {
            return "Item name is required.";
        }

        if (request.CategoryId is null or <= 0)
        {
            return "Category is required.";
        }

        if (request.LocationId is null or <= 0)
        {
            return "Location is required.";
        }

        if (requireCurrentQuantity && request.CurrentQuantity is null)
        {
            return "Current quantity is required.";
        }

        if (request.CurrentQuantity < 0)
        {
            return "Current quantity must be zero or greater.";
        }

        return null;
    }

    private static InventoryEntrySaveResult Success(int inventoryEntryId, int categoryId)
    {
        return new InventoryEntrySaveResult(true, null, inventoryEntryId, categoryId);
    }

    private static InventoryEntrySaveResult Failure(string errorMessage)
    {
        return new InventoryEntrySaveResult(false, errorMessage, null, null);
    }
}
