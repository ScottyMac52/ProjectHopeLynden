namespace ProjectHopeLynden.Application.Backup;

public sealed record DatabaseRestoreResult(
    DateTime AttemptedAtUtc,
    bool Succeeded,
    string? RestoredBackupFileName,
    string? SafetyBackupFilePath,
    string? ErrorMessage);
