using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectHopeLynden.Application.Backup;

namespace ProjectHopeLynden.Infrastructure.Persistence.Backup;

public sealed class DatabaseBackupService(
    ProjectHopeDbContext context,
    DatabaseBackupOptions options,
    ILogger<DatabaseBackupService> logger) : IDatabaseBackupService
{
    public string BackupFolder => options.Folder;

    public async Task<DatabaseBackupResult> CreateBackupAsync(
        DateTime attemptedAtUtc,
        CancellationToken cancellationToken = default)
    {
        string? backupFilePath = null;

        try
        {
            if (string.IsNullOrWhiteSpace(options.Folder))
            {
                throw new InvalidOperationException("The database backup folder is not configured.");
            }

            var backupFolder = Path.GetFullPath(options.Folder);
            Directory.CreateDirectory(backupFolder);

            backupFilePath = Path.Combine(backupFolder, CreateBackupFileName(attemptedAtUtc));

            var sourceConnectionString = context.Database.GetConnectionString()
                ?? throw new InvalidOperationException("The Project Hope database connection string is unavailable.");

            var sourceBuilder = new SqliteConnectionStringBuilder(sourceConnectionString)
            {
                Mode = SqliteOpenMode.ReadWrite,
                Pooling = false,
            };

            var destinationBuilder = new SqliteConnectionStringBuilder
            {
                DataSource = backupFilePath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = false,
            };

            await using var sourceConnection = new SqliteConnection(sourceBuilder.ConnectionString);
            await using var destinationConnection = new SqliteConnection(destinationBuilder.ConnectionString);

            await sourceConnection.OpenAsync(cancellationToken);
            await destinationConnection.OpenAsync(cancellationToken);
            sourceConnection.BackupDatabase(destinationConnection);

            logger.LogInformation(
                "Created Project Hope database backup at {BackupFilePath}.",
                backupFilePath);

            return new DatabaseBackupResult(
                attemptedAtUtc,
                backupFolder,
                true,
                backupFilePath,
                null);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Project Hope database backup failed for configured folder {BackupFolder}.",
                options.Folder);

            DeleteIncompleteBackup(backupFilePath);

            return new DatabaseBackupResult(
                attemptedAtUtc,
                options.Folder,
                false,
                null,
                exception.Message);
        }
    }

    private static string CreateBackupFileName(DateTime attemptedAtUtc)
    {
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        return $"ProjectHopeLynden-{attemptedAtUtc:yyyyMMdd-HHmmss-fffffff}-{uniqueSuffix}.db";
    }

    private static void DeleteIncompleteBackup(string? backupFilePath)
    {
        if (string.IsNullOrWhiteSpace(backupFilePath) || !File.Exists(backupFilePath))
        {
            return;
        }

        try
        {
            File.Delete(backupFilePath);
        }
        catch
        {
            // The original backup failure is the useful status to report.
        }
    }
}
