# Project Hope database backup and restore

The application provides database maintenance controls at `/Administration/Backup`.

## Production locations

```text
Database: C:\ProgramData\ProjectHopeLynden\ProjectHopeLynden.db
Backups:  C:\ProgramData\ProjectHopeLynden\Backups
```

## Create a backup

1. Open **Backup** from the application navigation.
2. Select **Create backup now**.
3. Confirm that the page reports success and shows the generated file path.

The application uses SQLite's online backup API, so the Windows service can remain running while a backup is created.

## Restore a backup

1. Open **Backup** from the application navigation.
2. Select a valid backup from the restore list.
3. Type `RESTORE` exactly in the confirmation field.
4. Select **Restore selected backup**.
5. Confirm that the page identifies both the restored file and the new safety backup.

Before changing the active database, the application always creates a complete safety backup of its current contents. The selected backup is checked for SQLite integrity and for the expected Project Hope tables before the safety backup or restore begins.

Restore uses SQLite's online backup API rather than copying over an open database file. This avoids manual service shutdown and stale `-wal`, `-shm`, or `-journal` sidecar-file replacement.

After copying the selected backup, the application runs any pending EF Core migrations. This allows an older compatible Project Hope backup to be brought forward to the currently installed schema.

## Failure recovery

If restore or migration fails after the active database has been changed, the application immediately attempts to recover the original database from the safety backup.

If the page reports that automatic rollback also failed:

1. Stop the `ProjectHopeLynden` Windows service.
2. Preserve the current `ProjectHopeLynden.db` for diagnosis.
3. Copy the reported safety backup over `ProjectHopeLynden.db`.
4. Remove stale `ProjectHopeLynden.db-wal`, `ProjectHopeLynden.db-shm`, or `ProjectHopeLynden.db-journal` files if present.
5. Start the service and confirm that `http://localhost:5000` opens normally.
