namespace ProjectHopeLynden.Application.Backup;

public interface IDatabaseBackupService
{
    string BackupFolder { get; }

    Task<DatabaseBackupResult> CreateBackupAsync(
        DateTime attemptedAtUtc,
        CancellationToken cancellationToken = default);
}
