using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjectHopeLynden.Application.Backup;

namespace ProjectHopeLynden.Web.Pages.Administration;

public sealed class BackupModel(IDatabaseBackupService databaseBackupService) : PageModel
{
    public string PageTitle { get; } = "Database Backup";

    public string Summary { get; } = "Create a recoverable copy of the Project Hope inventory database, including current inventory and count history.";

    public string BackupFolder => databaseBackupService.BackupFolder;

    public DatabaseBackupResult? LastResult { get; private set; }

    public async Task OnPostCreateBackupAsync()
    {
        LastResult = await databaseBackupService.CreateBackupAsync(DateTime.UtcNow);
    }
}
