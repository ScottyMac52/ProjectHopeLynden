namespace ProjectHopeLynden.Application.Backup;

public interface IDatabaseRestoreService
{
    IReadOnlyList<DatabaseBackupFile> GetAvailableBackups();

    Task<DatabaseRestoreResult> RestoreBackupAsync(
        string? backupFileName,
        DateTime attemptedAtUtc,
        CancellationToken cancellationToken = default);
}
