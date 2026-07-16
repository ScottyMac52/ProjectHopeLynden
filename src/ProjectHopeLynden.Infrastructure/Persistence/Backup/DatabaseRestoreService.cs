using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectHopeLynden.Application.Backup;

namespace ProjectHopeLynden.Infrastructure.Persistence.Backup;

public sealed class DatabaseRestoreService(
    ProjectHopeDbContext context,
    IDatabaseBackupService databaseBackupService,
    DatabaseBackupOptions options,
    ILogger<DatabaseRestoreService> logger) : IDatabaseRestoreService
{
    private static readonly SemaphoreSlim RestoreLock = new(1, 1);

    private static readonly string[] RequiredTables =
    [
        "Categories",
        "Items",
        "Locations",
        "InventoryEntries",
        "InventoryCountHistory",
    ];

    public IReadOnlyList<DatabaseBackupFile> GetAvailableBackups()
    {
        try
        {
            var backupFolder = GetBackupFolder();
            if (!Directory.Exists(backupFolder))
            {
                return [];
            }

            return Directory
                .EnumerateFiles(backupFolder, "ProjectHopeLynden-*.db", SearchOption.TopDirectoryOnly)
                .Select(path => new FileInfo(path))
                .Where(file => IsValidProjectHopeDatabase(file.FullName))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .Select(file => new DatabaseBackupFile(
                    file.Name,
                    file.LastWriteTimeUtc,
                    file.Length))
                .ToArray();
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Could not enumerate Project Hope database backups from {BackupFolder}.",
                options.Folder);

            return [];
        }
    }

    public async Task<DatabaseRestoreResult> RestoreBackupAsync(
        string? backupFileName,
        DateTime attemptedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await RestoreLock.WaitAsync(cancellationToken);

        try
        {
            var backupPath = ResolveBackupPath(backupFileName);
            ValidateProjectHopeDatabase(backupPath);

            var activeConnectionString = context.Database.GetConnectionString()
                ?? throw new InvalidOperationException("The Project Hope database connection string is unavailable.");

            RejectActiveDatabaseSelection(backupPath, activeConnectionString);

            var safetyBackup = await databaseBackupService.CreateBackupAsync(
                attemptedAtUtc,
                cancellationToken);

            if (!safetyBackup.Succeeded || string.IsNullOrWhiteSpace(safetyBackup.BackupFilePath))
            {
                return Failure(
                    attemptedAtUtc,
                    backupFileName,
                    null,
                    "The restore was cancelled because a safety backup of the current database could not be created.");
            }

            try
            {
                await CopyDatabaseAsync(backupPath, activeConnectionString, cancellationToken);
                context.ChangeTracker.Clear();
                await context.Database.MigrateAsync(cancellationToken);

                logger.LogInformation(
                    "Restored Project Hope database backup {BackupFileName}. Safety backup: {SafetyBackupFilePath}.",
                    Path.GetFileName(backupPath),
                    safetyBackup.BackupFilePath);

                return new DatabaseRestoreResult(
                    attemptedAtUtc,
                    true,
                    Path.GetFileName(backupPath),
                    safetyBackup.BackupFilePath,
                    null);
            }
            catch (Exception restoreException)
            {
                logger.LogError(
                    restoreException,
                    "Project Hope database restore failed for {BackupFileName}; attempting safety rollback.",
                    Path.GetFileName(backupPath));

                var rollbackError = await TryRollbackAsync(
                    safetyBackup.BackupFilePath,
                    activeConnectionString,
                    cancellationToken);

                var errorMessage = rollbackError is null
                    ? "The restore failed. The original database was recovered from the safety backup."
                    : "The restore failed and the automatic safety rollback also failed. Stop the service and restore the safety backup manually.";

                return Failure(
                    attemptedAtUtc,
                    Path.GetFileName(backupPath),
                    safetyBackup.BackupFilePath,
                    errorMessage);
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Rejected Project Hope database restore request for {BackupFileName}.",
                backupFileName);

            return Failure(
                attemptedAtUtc,
                backupFileName,
                null,
                exception.Message);
        }
        finally
        {
            RestoreLock.Release();
        }
    }

    private async Task<string?> TryRollbackAsync(
        string safetyBackupPath,
        string activeConnectionString,
        CancellationToken cancellationToken)
    {
        try
        {
            await CopyDatabaseAsync(safetyBackupPath, activeConnectionString, cancellationToken);
            context.ChangeTracker.Clear();

            logger.LogWarning(
                "Recovered the Project Hope database from safety backup {SafetyBackupFilePath}.",
                safetyBackupPath);

            return null;
        }
        catch (Exception rollbackException)
        {
            logger.LogCritical(
                rollbackException,
                "Could not recover the Project Hope database from safety backup {SafetyBackupFilePath}.",
                safetyBackupPath);

            return rollbackException.Message;
        }
    }

    private string ResolveBackupPath(string? backupFileName)
    {
        if (string.IsNullOrWhiteSpace(backupFileName))
        {
            throw new InvalidOperationException("Select a database backup to restore.");
        }

        if (!string.Equals(
                backupFileName,
                Path.GetFileName(backupFileName),
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The selected backup is not in the configured backup folder.");
        }

        var backupPath = Path.GetFullPath(Path.Combine(GetBackupFolder(), backupFileName));
        if (!File.Exists(backupPath))
        {
            throw new InvalidOperationException("The selected backup no longer exists.");
        }

        return backupPath;
    }

    private string GetBackupFolder()
    {
        if (string.IsNullOrWhiteSpace(options.Folder))
        {
            throw new InvalidOperationException("The database backup folder is not configured.");
        }

        return Path.GetFullPath(options.Folder);
    }

    private static void RejectActiveDatabaseSelection(
        string backupPath,
        string activeConnectionString)
    {
        var activeBuilder = new SqliteConnectionStringBuilder(activeConnectionString);
        if (string.IsNullOrWhiteSpace(activeBuilder.DataSource) ||
            string.Equals(activeBuilder.DataSource, ":memory:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var activeDatabasePath = Path.GetFullPath(activeBuilder.DataSource);
        if (string.Equals(
                backupPath,
                activeDatabasePath,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The active production database cannot be selected as its own backup.");
        }
    }

    private static bool IsValidProjectHopeDatabase(string databasePath)
    {
        try
        {
            ValidateProjectHopeDatabase(databasePath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void ValidateProjectHopeDatabase(string databasePath)
    {
        try
        {
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false,
            };

            using var connection = new SqliteConnection(builder.ConnectionString);
            connection.Open();

            using (var integrityCommand = connection.CreateCommand())
            {
                integrityCommand.CommandText = "PRAGMA integrity_check;";
                var integrityResult = Convert.ToString(integrityCommand.ExecuteScalar());
                if (!string.Equals(integrityResult, "ok", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("The selected backup failed SQLite integrity validation.");
                }
            }

            using var schemaCommand = connection.CreateCommand();
            schemaCommand.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table';";

            using var reader = schemaCommand.ExecuteReader();
            var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (reader.Read())
            {
                tables.Add(reader.GetString(0));
            }

            if (RequiredTables.Any(table => !tables.Contains(table)))
            {
                throw new InvalidOperationException("The selected file is not a compatible Project Hope database backup.");
            }
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                "The selected file is not a readable SQLite database backup.",
                exception);
        }
    }

    private static async Task CopyDatabaseAsync(
        string sourceDatabasePath,
        string activeConnectionString,
        CancellationToken cancellationToken)
    {
        var sourceBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = sourceDatabasePath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false,
        };

        var destinationBuilder = new SqliteConnectionStringBuilder(activeConnectionString)
        {
            Mode = SqliteOpenMode.ReadWrite,
            Pooling = false,
        };

        await using var sourceConnection = new SqliteConnection(sourceBuilder.ConnectionString);
        await using var destinationConnection = new SqliteConnection(destinationBuilder.ConnectionString);

        await sourceConnection.OpenAsync(cancellationToken);
        await destinationConnection.OpenAsync(cancellationToken);
        sourceConnection.BackupDatabase(destinationConnection);
    }

    private static DatabaseRestoreResult Failure(
        DateTime attemptedAtUtc,
        string? backupFileName,
        string? safetyBackupFilePath,
        string errorMessage)
    {
        return new DatabaseRestoreResult(
            attemptedAtUtc,
            false,
            backupFileName,
            safetyBackupFilePath,
            errorMessage);
    }
}
