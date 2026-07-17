namespace ProjectHopeLynden.Application.Backup;

public sealed record DatabaseBackupFile(
    string FileName,
    DateTime LastWriteTimeUtc,
    long SizeBytes);
