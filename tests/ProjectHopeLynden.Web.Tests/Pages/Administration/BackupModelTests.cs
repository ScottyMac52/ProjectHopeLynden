using ProjectHopeLynden.Application.Backup;
using ProjectHopeLynden.Web.Pages.Administration;
using Xunit;

namespace ProjectHopeLynden.Web.Tests.Pages.Administration;

public sealed class BackupModelTests
{
    [Fact]
    public void PageCopy_ExplainsBackupCoverageAndConfiguredFolder()
    {
        var service = new StubDatabaseBackupService("C:\\Backups");
        var model = new BackupModel(service);

        Assert.Equal("Database Backup", model.PageTitle);
        Assert.Contains("current inventory", model.Summary);
        Assert.Contains("count history", model.Summary);
        Assert.Equal("C:\\Backups", model.BackupFolder);
        Assert.Null(model.LastResult);
    }

    [Fact]
    public async Task OnPostCreateBackupAsync_ExposesSuccessfulBackupStatus()
    {
        var attemptedAtUtc = new DateTime(2026, 7, 12, 10, 0, 0, DateTimeKind.Utc);
        var expected = new DatabaseBackupResult(
            attemptedAtUtc,
            "C:\\Backups",
            true,
            "C:\\Backups\\ProjectHopeLynden-test.db",
            null);
        var service = new StubDatabaseBackupService("C:\\Backups", expected);
        var model = new BackupModel(service);

        await model.OnPostCreateBackupAsync();

        Assert.Same(expected, model.LastResult);
        Assert.NotNull(service.AttemptedAtUtc);
        Assert.Equal(DateTimeKind.Utc, service.AttemptedAtUtc.Value.Kind);
    }

    [Fact]
    public async Task OnPostCreateBackupAsync_ExposesFailureStatus()
    {
        var expected = new DatabaseBackupResult(
            new DateTime(2026, 7, 12, 10, 0, 0, DateTimeKind.Utc),
            "Z:\\Unavailable",
            false,
            null,
            "Access denied.");
        var service = new StubDatabaseBackupService("Z:\\Unavailable", expected);
        var model = new BackupModel(service);

        await model.OnPostCreateBackupAsync();

        Assert.NotNull(model.LastResult);
        Assert.False(model.LastResult.Succeeded);
        Assert.Equal("Access denied.", model.LastResult.ErrorMessage);
    }

    private sealed class StubDatabaseBackupService(
        string backupFolder,
        DatabaseBackupResult? result = null) : IDatabaseBackupService
    {
        public string BackupFolder { get; } = backupFolder;

        public DateTime? AttemptedAtUtc { get; private set; }

        public Task<DatabaseBackupResult> CreateBackupAsync(
            DateTime attemptedAtUtc,
            CancellationToken cancellationToken = default)
        {
            AttemptedAtUtc = attemptedAtUtc;
            return Task.FromResult(result ?? new DatabaseBackupResult(
                attemptedAtUtc,
                BackupFolder,
                true,
                Path.Combine(BackupFolder, "ProjectHopeLynden-test.db"),
                null));
        }
    }
}
