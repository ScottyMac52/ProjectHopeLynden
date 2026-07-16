# Project Hope Inventory Windows Service

The Windows installer registers the Project Hope Inventory server as a Windows service. No user needs to keep a command prompt open or manually launch the server after Windows starts.

## Service identity

| Setting | Value |
| --- | --- |
| Service name | `ProjectHopeLynden` |
| Display name | `Project Hope Lynden Inventory Server` |
| Startup | Automatic, delayed start |
| Default address on the server | `http://localhost:5000` |
| Default LAN listener | Port `5000` |
| Database | `C:\ProgramData\ProjectHopeLynden\ProjectHopeLynden.db` |
| Backups | `C:\ProgramData\ProjectHopeLynden\Backups` |

The application configuration uses `http://*:5000`, which allows the server to listen on its available network interfaces. Access from another computer still depends on the Windows Firewall rule and the server's current IP address.

HTTPS/TLS is intentionally separate and remains tracked by issue #62.

## Installation and upgrades

During installation, the installer:

1. Stops an existing Project Hope service when upgrading.
2. Preserves the existing database when the **Preserve the existing Project Hope Lynden database** option is selected.
3. Replaces the application files.
4. Restores the preserved database.
5. Registers or updates the Windows service.
6. Configures delayed automatic startup and service recovery.
7. Starts the service.

The installer opens the site in the default browser after the service starts. It does not launch a second interactive copy of the server executable.

## Check service status

Open an elevated PowerShell window:

```powershell
Get-Service -Name ProjectHopeLynden
```

The normal result is `Status` equal to `Running` and `StartType` equal to `Automatic`.

The Services management console also shows the service under the friendly name **Project Hope Lynden Inventory Server**.

## Start, stop, or restart

Run these commands from an elevated PowerShell window:

```powershell
Start-Service -Name ProjectHopeLynden
Stop-Service -Name ProjectHopeLynden
Restart-Service -Name ProjectHopeLynden
```

After starting or restarting, open:

```text
http://localhost:5000
```

From another computer on the Project Hope network, use the server computer's LAN IP address with port 5000.

## Logs

When running as a Windows service, host lifetime and startup errors are written to the Windows Application event log.

Open **Event Viewer**, then navigate to:

```text
Windows Logs → Application
```

Look for recent entries associated with the Project Hope server or .NET host around the service start time.

## URL overrides

The installed default is stored in:

```text
C:\Program Files\Cisco\Project Hope Lynden Server\appsettings.json
```

The setting is:

```json
"Urls": "http://*:5000"
```

Stop the service before editing this file, then restart it afterward. A future HTTPS configuration under issue #62 may replace this HTTP listener.

## Recovery

The service is configured to restart automatically after its first two unexpected failures. If it will not remain running:

1. Check the Windows Application event log.
2. Confirm the database folder exists under `C:\ProgramData\ProjectHopeLynden`.
3. Confirm port 5000 is not already in use.
4. Confirm the installed `appsettings.json` is valid JSON.
5. Restart the service after correcting the problem.

The application's manual database backup page remains available after the service is running.

## Uninstall

The uninstaller stops and removes the Windows service before deleting application files. Database retention remains governed by the installer's existing database-preservation behavior and any backups created separately by the application.
