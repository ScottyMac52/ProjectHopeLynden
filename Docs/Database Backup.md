# Project Hope Database Backups

The server includes a manual backup page at `/Administration/Backup`.

## Creating a backup

1. Open **Backup** from the application navigation.
2. Confirm the configured backup folder shown on the page.
3. Select **Create backup now**.
4. Keep the success message and generated file path for reference.

The production configuration stores backups in:

```text
C:\ProgramData\ProjectHopeLynden\Backups
```

Development runs use the repository-local `Backups` folder unless configuration overrides it.

## Backup contents

The backup uses SQLite's online backup operation rather than copying an open database file directly. Each generated `.db` file is a complete recovery database containing:

- categories, items, and locations
- current inventory entries and quantities
- Commodity and Menu Item status
- best-by dates
- historical inventory count records

## File names

Backup files use a UTC timestamp and a short unique suffix:

```text
ProjectHopeLynden-yyyyMMdd-HHmmss-fffffff-xxxxxxxx.db
```

The timestamp makes the backup time understandable, and the suffix prevents two backups created at the same instant from overwriting each other.

## Failure visibility

A failed backup is shown immediately on the Backup page and is also written to the server log. No successful backup status is shown unless the destination database was created.

The backup folder can be changed with the `DatabaseBackup:Folder` configuration value or the equivalent ASP.NET Core environment-variable override.
