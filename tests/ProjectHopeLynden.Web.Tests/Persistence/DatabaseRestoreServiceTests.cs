using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ProjectHopeLynden.Application.Backup;
using ProjectHopeLynden.Domain.Inventory;
using ProjectHopeLynden.Infrastructure.Persistence;
using ProjectHopeLynden.Infrastructure.Persistence.Backup;
using Xunit;

namespace ProjectHopeLynden.Web.Tests.Persistence;

public sealed class DatabaseRestoreServiceTests
{
    [Fact]
    public async Task GetAvailableBackups_ReturnsOnlyValidTopLevelProjectHopeBackups()
    {
        var root = CreateTemporaryFolder();

        try
        {
            var backupFolder = Path.Combine(root, "Backups");
            var liveDatabasePath = Path.Combine(root, "live.db");
            var sourceDatabasePath = Path.Combine(root, "source.db");

            await CreateMigratedDatabaseAsync(liveDatabasePath, 5);
            await CreateMigratedDatabaseAsync(sourceDatabasePath, 42);

            var validBackupPath = await CreateBackupAsync(
                sourceDatabasePath,
                backupFolder,
                new DateTime(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc));

            Directory.CreateDirectory(backupFolder);
            await File.WriteAllTextAsync(
                Path.Combine(backupFolder, "ProjectHopeLynden-corrupt.db"),
                "not a sqlite database");

            File.Copy(
                validBackupPath,
                Path.Combine(backupFolder, "unrelated.db"));

            var nestedFolder = Path.Combine(backupFolder, "Nested");
            Directory.CreateDirectory(nestedFolder);
            File.Copy(
                validBackupPath,
                Path.Combine(nestedFolder, Path.GetFileName(validBackupPath)));

            await using var liveContext = CreateContext(liveDatabasePath);
            var service = CreateRestoreService(liveContext, backupFolder);

            var backups = service.GetAvailableBackups();

            var backup = Assert.Single(backups);
            Assert.Equal(Path.GetFileName(validBackupPath), backup.FileName);
            Assert.True(backup.SizeBytes > 0);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task RestoreBackupAsync_RestoresInventoryAndCreatesSafetyBackup()
    {
        var root = CreateTemporaryFolder();

        try
        {
            var backupFolder = Path.Combine(root, "Backups");
            var liveDatabasePath = Path.Combine(root, "live.db");
            var sourceDatabasePath = Path.Combine(root, "source.db");

            await CreateMigratedDatabaseAsync(liveDatabasePath, 5);
            await CreateMigratedDatabaseAsync(sourceDatabasePath, 42);

            var selectedBackupPath = await CreateBackupAsync(
                sourceDatabasePath,
                backupFolder,
                new DateTime(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc));

            await using var liveContext = CreateContext(liveDatabasePath);
            var service = CreateRestoreService(liveContext, backupFolder);

            var result = await service.RestoreBackupAsync(
                Path.GetFileName(selectedBackupPath),
                new DateTime(2026, 7, 16, 12, 30, 0, DateTimeKind.Utc));

            Assert.True(result.Succeeded, result.ErrorMessage);
            Assert.Equal(Path.GetFileName(selectedBackupPath), result.RestoredBackupFileName);
            Assert.NotNull(result.SafetyBackupFilePath);
            Assert.True(File.Exists(result.SafetyBackupFilePath));

            liveContext.ChangeTracker.Clear();
            Assert.Equal(
                42,
                await liveContext.InventoryEntries.Select(entry => entry.CurrentQuantity).SingleAsync());
            Assert.Equal(
                42,
                await liveContext.InventoryCountHistory.Select(history => history.CountedQuantity).SingleAsync());

            await using var safetyContext = CreateContext(result.SafetyBackupFilePath!);
            Assert.Equal(
                5,
                await safetyContext.InventoryEntries.Select(entry => entry.CurrentQuantity).SingleAsync());
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("../outside.db")]
    [InlineData("missing.db")]
    public async Task RestoreBackupAsync_RejectsInvalidSelections(string? backupFileName)
    {
        var root = CreateTemporaryFolder();

        try
        {
            var backupFolder = Path.Combine(root, "Backups");
            var liveDatabasePath = Path.Combine(root, "live.db");
            await CreateMigratedDatabaseAsync(liveDatabasePath, 5);

            await using var liveContext = CreateContext(liveDatabasePath);
            var service = CreateRestoreService(liveContext, backupFolder);

            var result = await service.RestoreBackupAsync(
                backupFileName,
                new DateTime(2026, 7, 16, 13, 0, 0, DateTimeKind.Utc));

            Assert.False(result.Succeeded);
            liveContext.ChangeTracker.Clear();
            Assert.Equal(
                5,
                await liveContext.InventoryEntries.Select(entry => entry.CurrentQuantity).SingleAsync());
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task RestoreBackupAsync_RejectsIncompatibleDatabase()
    {
        var root = CreateTemporaryFolder();

        try
        {
            var backupFolder = Path.Combine(root, "Backups");
            var liveDatabasePath = Path.Combine(root, "live.db");
            await CreateMigratedDatabaseAsync(liveDatabasePath, 5);

            Directory.CreateDirectory(backupFolder);
            var incompatiblePath = Path.Combine(
                backupFolder,
                "ProjectHopeLynden-incompatible.db");

            await using (var connection = new SqliteConnection($"Data Source={incompatiblePath};Pooling=False"))
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = "CREATE TABLE Unrelated (Id INTEGER PRIMARY KEY);";
                await command.ExecuteNonQueryAsync();
            }

            await using var liveContext = CreateContext(liveDatabasePath);
            var service = CreateRestoreService(liveContext, backupFolder);

            var result = await service.RestoreBackupAsync(
                Path.GetFileName(incompatiblePath),
                new DateTime(2026, 7, 16, 13, 30, 0, DateTimeKind.Utc));

            Assert.False(result.Succeeded);
            Assert.Contains("compatible", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task RestoreBackupAsync_RollsBackWhenRestoredDatabaseCannotBeMigrated()
    {
        var root = CreateTemporaryFolder();

        try
        {
            var backupFolder = Path.Combine(root, "Backups");
            var liveDatabasePath = Path.Combine(root, "live.db");
            var incompatibleSourcePath = Path.Combine(root, "unmigrated.db");

            await CreateMigratedDatabaseAsync(liveDatabasePath, 5);
            await CreateEnsureCreatedDatabaseAsync(incompatibleSourcePath, 99);

            Directory.CreateDirectory(backupFolder);
            var selectedBackupPath = Path.Combine(
                backupFolder,
                "ProjectHopeLynden-unmigrated.db");
            File.Copy(incompatibleSourcePath, selectedBackupPath);

            await using var liveContext = CreateContext(liveDatabasePath);
            var service = CreateRestoreService(liveContext, backupFolder);

            var result = await service.RestoreBackupAsync(
                Path.GetFileName(selectedBackupPath),
                new DateTime(2026, 7, 16, 14, 0, 0, DateTimeKind.Utc));

            Assert.False(result.Succeeded);
            Assert.NotNull(result.SafetyBackupFilePath);
            Assert.True(File.Exists(result.SafetyBackupFilePath));

            liveContext.ChangeTracker.Clear();
            Assert.Equal(
                5,
                await liveContext.InventoryEntries.Select(entry => entry.CurrentQuantity).SingleAsync());
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static DatabaseRestoreService CreateRestoreService(
        ProjectHopeDbContext context,
        string backupFolder)
    {
        var options = new DatabaseBackupOptions(backupFolder);
        var backupService = new DatabaseBackupService(
            context,
            options,
            NullLogger<DatabaseBackupService>.Instance);

        return new DatabaseRestoreService(
            context,
            backupService,
            options,
            NullLogger<DatabaseRestoreService>.Instance);
    }

    private static async Task<string> CreateBackupAsync(
        string databasePath,
        string backupFolder,
        DateTime attemptedAtUtc)
    {
        await using var context = CreateContext(databasePath);
        var service = new DatabaseBackupService(
            context,
            new DatabaseBackupOptions(backupFolder),
            NullLogger<DatabaseBackupService>.Instance);

        var result = await service.CreateBackupAsync(attemptedAtUtc);
        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.NotNull(result.BackupFilePath);
        return result.BackupFilePath!;
    }

    private static async Task CreateMigratedDatabaseAsync(
        string databasePath,
        double quantity)
    {
        await using var context = CreateContext(databasePath);
        await context.Database.MigrateAsync();
        await SeedDatabaseAsync(context, quantity);
    }

    private static async Task CreateEnsureCreatedDatabaseAsync(
        string databasePath,
        double quantity)
    {
        await using var context = CreateContext(databasePath);
        await context.Database.EnsureCreatedAsync();
        await SeedDatabaseAsync(context, quantity);
    }

    private static async Task SeedDatabaseAsync(
        ProjectHopeDbContext context,
        double quantity)
    {
        var category = new Category { Name = "Test Category" };
        var item = new Item { Name = "Test Item" };
        var location = new Location { Name = "Test Location" };
        var entry = new InventoryEntry
        {
            Category = category,
            Item = item,
            Location = location,
            CurrentQuantity = quantity,
            IsCommodity = true,
            IsMenuItem = false,
            LastUpdatedAtUtc = new DateTime(2026, 7, 16, 10, 0, 0, DateTimeKind.Utc),
        };

        context.InventoryEntries.Add(entry);
        await context.SaveChangesAsync();

        context.InventoryCountHistory.Add(new InventoryCountHistory
        {
            InventoryEntryId = entry.Id,
            CountedQuantity = quantity,
            CountedAtUtc = new DateTime(2026, 7, 16, 10, 0, 0, DateTimeKind.Utc),
            PreviousQuantity = null,
            QuantityChange = null,
            ItemIdAtCount = item.Id,
            ItemNameAtCount = item.Name,
            CategoryIdAtCount = category.Id,
            CategoryNameAtCount = category.Name,
            LocationIdAtCount = location.Id,
            LocationNameAtCount = location.Name,
            IsCommodityAtCount = true,
        });

        await context.SaveChangesAsync();
    }

    private static ProjectHopeDbContext CreateContext(string databasePath)
    {
        var options = new DbContextOptionsBuilder<ProjectHopeDbContext>()
            .UseSqlite($"Data Source={databasePath};Pooling=False")
            .Options;

        return new ProjectHopeDbContext(options);
    }

    private static string CreateTemporaryFolder()
    {
        var folder = Path.Combine(
            Path.GetTempPath(),
            "ProjectHopeRestoreTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(folder);
        return folder;
    }
}
