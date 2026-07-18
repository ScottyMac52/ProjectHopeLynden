using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ProjectHopeLynden.Application.Backup;
using ProjectHopeLynden.Domain.IncomingOrders;
using ProjectHopeLynden.Domain.Inventory;
using ProjectHopeLynden.Infrastructure.Persistence;
using ProjectHopeLynden.Infrastructure.Persistence.Backup;
using Xunit;

namespace ProjectHopeLynden.Infrastructure.Tests.Persistence.Backup;

public sealed class DatabaseBackupServiceTests
{
    [Fact]
    public async Task CreateBackupAsync_CreatesTimestampedCopyContainingInventoryAndHistory()
    {
        var rootFolder = CreateTemporaryFolder();
        var sourcePath = Path.Combine(rootFolder, "ProjectHopeLynden.db");
        var backupFolder = Path.Combine(rootFolder, "Backups");
        var attemptedAtUtc = new DateTime(2026, 7, 12, 8, 30, 45, 123, DateTimeKind.Utc).AddTicks(4567);

        try
        {
            await using var context = await CreateDatabaseAsync(sourcePath);
            await SeedInventoryAndHistoryAsync(context);
            var service = CreateService(context, backupFolder);

            var result = await service.CreateBackupAsync(attemptedAtUtc);

            Assert.True(result.Succeeded, result.ErrorMessage);
            Assert.NotNull(result.BackupFilePath);
            Assert.True(File.Exists(result.BackupFilePath));
            Assert.Matches(
                @"ProjectHopeLynden-20260712-083045-1234567-[0-9a-f]{8}\.db$",
                result.BackupFilePath);

            await using var backupContext = await OpenDatabaseAsync(result.BackupFilePath);
            var backedUpEntry = await backupContext.InventoryEntries.SingleAsync();
            var backedUpHistory = await backupContext.InventoryCountHistory.SingleAsync();
            var backedUpOrder = await backupContext.IncomingOrders.Include(order => order.Lines).SingleAsync();

            Assert.Equal(12, backedUpEntry.CurrentQuantity);
            Assert.True(backedUpEntry.IsCommodity);
            Assert.Equal(12, backedUpHistory.CountedQuantity);
            Assert.Equal(-3, backedUpHistory.QuantityChange);
            Assert.Equal("Food Lifeline", backedUpOrder.Vendor);
            Assert.Single(backedUpOrder.Lines);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(rootFolder, true);
        }
    }

    [Fact]
    public async Task CreateBackupAsync_CreatesUniqueNamesForMultipleBackupsAtSameTime()
    {
        var rootFolder = CreateTemporaryFolder();
        var sourcePath = Path.Combine(rootFolder, "ProjectHopeLynden.db");
        var backupFolder = Path.Combine(rootFolder, "Backups");
        var attemptedAtUtc = new DateTime(2026, 7, 12, 9, 15, 0, DateTimeKind.Utc);

        try
        {
            await using var context = await CreateDatabaseAsync(sourcePath);
            await SeedInventoryAndHistoryAsync(context);
            var service = CreateService(context, backupFolder);

            var first = await service.CreateBackupAsync(attemptedAtUtc);
            var second = await service.CreateBackupAsync(attemptedAtUtc);

            Assert.True(first.Succeeded, first.ErrorMessage);
            Assert.True(second.Succeeded, second.ErrorMessage);
            Assert.NotEqual(first.BackupFilePath, second.BackupFilePath);
            Assert.Equal(2, Directory.GetFiles(backupFolder, "ProjectHopeLynden-*.db").Length);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(rootFolder, true);
        }
    }

    [Fact]
    public async Task CreateBackupAsync_ReturnsVisibleFailureWhenBackupFolderCannotBeCreated()
    {
        var rootFolder = CreateTemporaryFolder();
        var sourcePath = Path.Combine(rootFolder, "ProjectHopeLynden.db");
        var blockedFolderPath = Path.Combine(rootFolder, "not-a-folder");
        await File.WriteAllTextAsync(blockedFolderPath, "This file blocks directory creation.");

        try
        {
            await using var context = await CreateDatabaseAsync(sourcePath);
            await SeedInventoryAndHistoryAsync(context);
            var service = CreateService(context, blockedFolderPath);

            var result = await service.CreateBackupAsync(DateTime.UtcNow);

            Assert.False(result.Succeeded);
            Assert.Null(result.BackupFilePath);
            Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
            Assert.Equal(blockedFolderPath, result.BackupFolder);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(rootFolder, true);
        }
    }

    private static DatabaseBackupService CreateService(ProjectHopeDbContext context, string backupFolder)
    {
        return new DatabaseBackupService(
            context,
            new DatabaseBackupOptions(backupFolder),
            NullLogger<DatabaseBackupService>.Instance);
    }

    private static async Task<ProjectHopeDbContext> CreateDatabaseAsync(string databasePath)
    {
        var context = CreateContext(databasePath);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static async Task<ProjectHopeDbContext> OpenDatabaseAsync(string databasePath)
    {
        var context = CreateContext(databasePath);
        await context.Database.OpenConnectionAsync();
        return context;
    }

    private static ProjectHopeDbContext CreateContext(string databasePath)
    {
        var options = new DbContextOptionsBuilder<ProjectHopeDbContext>()
            .UseSqlite($"Data Source={databasePath};Pooling=False")
            .Options;

        return new ProjectHopeDbContext(options);
    }

    private static async Task SeedInventoryAndHistoryAsync(ProjectHopeDbContext context)
    {
        var entry = new InventoryEntry
        {
            Item = new Item { Name = "Green Beans" },
            Category = new Category { Name = "Canned Vegetables" },
            Location = new Location { Name = "Back Room" },
            CurrentQuantity = 12,
            IsCommodity = true,
            IsMenuItem = false,
            LastUpdatedAtUtc = new DateTime(2026, 7, 12, 8, 0, 0, DateTimeKind.Utc),
        };

        context.InventoryEntries.Add(entry);
        await context.SaveChangesAsync();

        context.InventoryCountHistory.Add(new InventoryCountHistory
        {
            InventoryEntryId = entry.Id,
            CountedQuantity = 12,
            CountedAtUtc = entry.LastUpdatedAtUtc,
            PreviousQuantity = 15,
            QuantityChange = -3,
        });
        context.IncomingOrders.Add(new IncomingOrder
        {
            OrderDate = new DateTime(2026, 7, 12),
            Vendor = "Food Lifeline",
            Status = IncomingOrderStatus.Pending,
            ExpectedDate = new DateTime(2026, 7, 19),
            CreatedAtUtc = entry.LastUpdatedAtUtc,
            Lines = [new IncomingOrderLine { InventoryEntryId = entry.Id, ExpectedQuantity = 4 }],
        });
        await context.SaveChangesAsync();
    }

    private static string CreateTemporaryFolder()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"ProjectHopeLynden-backup-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        return folder;
    }
}
