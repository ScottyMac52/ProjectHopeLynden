using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjectHopeLynden.Application.Backup;

namespace ProjectHopeLynden.Web.Pages.Administration;

public sealed class BackupModel : PageModel
{
    private readonly IDatabaseBackupService databaseBackupService;
    private readonly IDatabaseRestoreService? databaseRestoreService;

    public BackupModel(
        IDatabaseBackupService databaseBackupService,
        IDatabaseRestoreService? databaseRestoreService = null)
    {
        this.databaseBackupService = databaseBackupService;
        this.databaseRestoreService = databaseRestoreService;
    }

    public string PageTitle { get; } = "Database Backup";

    public string Summary { get; } = "Create a recoverable copy of the Project Hope inventory database, including current inventory and count history.";

    public string BackupFolder => databaseBackupService.BackupFolder;

    public IReadOnlyList<DatabaseBackupFile> AvailableBackups { get; private set; } = [];

    public DatabaseBackupResult? LastResult { get; private set; }

    public DatabaseRestoreResult? RestoreResult { get; private set; }

    [BindProperty]
    public string? SelectedBackupFileName { get; set; }

    [BindProperty]
    public string? RestoreConfirmation { get; set; }

    public void OnGet()
    {
        LoadAvailableBackups();
    }

    public async Task OnPostCreateBackupAsync()
    {
        LastResult = await databaseBackupService.CreateBackupAsync(DateTime.UtcNow);
        LoadAvailableBackups();
    }

    public async Task OnPostRestoreBackupAsync()
    {
        var attemptedAtUtc = DateTime.UtcNow;

        if (!string.Equals(RestoreConfirmation, "RESTORE", StringComparison.Ordinal))
        {
            RestoreResult = new DatabaseRestoreResult(
                attemptedAtUtc,
                false,
                SelectedBackupFileName,
                null,
                "Type RESTORE exactly to confirm that the current database will be replaced.");
        }
        else if (databaseRestoreService is null)
        {
            RestoreResult = new DatabaseRestoreResult(
                attemptedAtUtc,
                false,
                SelectedBackupFileName,
                null,
                "Database restore is not available in this installation.");
        }
        else
        {
            RestoreResult = await databaseRestoreService.RestoreBackupAsync(
                SelectedBackupFileName,
                attemptedAtUtc);
        }

        LoadAvailableBackups();
    }

    private void LoadAvailableBackups()
    {
        AvailableBackups = databaseRestoreService?.GetAvailableBackups() ?? [];
    }
}
