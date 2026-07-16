using ProjectHopeLynden.Application.Backup;
using ProjectHopeLynden.Web.Pages.Administration;
using Xunit;

namespace ProjectHopeLynden.Web.Tests.Pages.Administration;

public sealed class BackupModelRestoreTests
{
    [Fact]
    public void OnGet_LoadsAvailableBackups()
    {
        var restoreService = new FakeRestoreService();
        var model = new BackupModel(new FakeBackupService(), restoreService);

        model.OnGet();

        var backup = Assert.Single(model.AvailableBackups);
        Assert.Equal("ProjectHopeLynden-test.db", backup.FileName);
    }

    [Fact]
    public async Task OnPostRestoreBackupAsync_RequiresExactConfirmation()
    {
        var restoreService = new FakeRestoreService();
        var model = new BackupModel(new FakeBackupService(), restoreService)
        {
            SelectedBackupFileName = "ProjectHopeLynden-test.db",
            RestoreConfirmation = "restore",
        };

        await model.OnPostRestoreBackupAsync();

        Assert.NotNull(model.RestoreResult);
        Assert.False(model.RestoreResult.Succeeded);
        Assert.Null(restoreService.RequestedFileName);
        Assert.Contains("RESTORE", model.RestoreResult.ErrorMessage);
    }

    [Fact]
    public async Task OnPostRestoreBackupAsync_RestoresSelectedBackup()
    {
        var restoreService = new FakeRestoreService();
        var model = new BackupModel(new FakeBackupService(), restoreService)
        {
            SelectedBackupFileName = "ProjectHopeLynden-test.db",
            RestoreConfirmation = "RESTORE",
        };

        await model.OnPostRestoreBackupAsync();

        Assert.Equal("ProjectHopeLynden-test.db", restoreService.RequestedFileName);
        Assert.NotNull(model.RestoreResult);
        Assert.True(model.RestoreResult.Succeeded);
    }

    private sealed class FakeBackupService : IDatabaseBackupService
    {
        public string BackupFolder => "Backups";

        public Task<DatabaseBackupResult> CreateBackupAsync(
            DateTime attemptedAtUtc,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DatabaseBackupResult(
                attemptedAtUtc,
                "Backups",
                true,
                "Backups/ProjectHopeLynden-test.db",
                null));
        }
    }

    private sealed class FakeRestoreService : IDatabaseRestoreService
    {
        public string? RequestedFileName { get; private set; }

        public IReadOnlyList<DatabaseBackupFile> GetAvailableBackups()
        {
            return
            [
                new DatabaseBackupFile(
                    "ProjectHopeLynden-test.db",
                    new DateTime(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc),
                    1024),
            ];
        }

        public Task<DatabaseRestoreResult> RestoreBackupAsync(
            string? backupFileName,
            DateTime attemptedAtUtc,
            CancellationToken cancellationToken = default)
        {
            RequestedFileName = backupFileName;

            return Task.FromResult(new DatabaseRestoreResult(
                attemptedAtUtc,
                true,
                backupFileName,
                "Backups/ProjectHopeLynden-safety.db",
                null));
        }
    }
}
