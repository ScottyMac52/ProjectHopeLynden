namespace ProjectHopeLynden.Application.Backup;

public sealed record DatabaseBackupResult(
    DateTime AttemptedAtUtc,
    string BackupFolder,
    bool Succeeded,
    string? BackupFilePath,
    string? ErrorMessage);
