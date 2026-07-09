#define AppName "ProjectHopeLynden.Web"
#define AppVersion "0.0.0.0"
#define AppPublisher "Cisco"
#define AppExeName "ProjectHopeLynden.Web.exe"

[Setup]
AppId={{8C4F365B-094E-4D69-B53E-0903E4F1CF50}}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppPublisher}\Project Hope Lynden Server
DefaultGroupName=Project Hope Lynden Server
OutputDir=.
OutputBaseFilename=setup
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#AppExeName}
VersionInfoCompany={#AppPublisher}
VersionInfoDescription={#AppName} Installer
VersionInfoProductName={#AppName}
VersionInfoProductVersion={#AppVersion}

[Tasks]
Name: "preservedatabase"; Description: "Preserve the existing Project Hope Lynden database if one is present"; GroupDescription: "Database options:"; Flags: checkedonce

[Dirs]
Name: "{commonappdata}\ProjectHopeLynden"; Permissions: users-modify

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Project Hope Lynden Server"; Filename: "{app}\{#AppExeName}"

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch Project Hope Lynden Server"; Flags: nowait postinstall skipifsilent

[Code]
function GetProgramDataDatabasePath: string;
begin
  Result := ExpandConstant('{commonappdata}\ProjectHopeLynden\ProjectHopeLynden.db');
end;

function GetProgramDataBackupPath: string;
begin
  Result := ExpandConstant('{tmp}\ProjectHopeLynden.db.backup');
end;

function GetAppDatabasePath: string;
begin
  Result := AddBackslash(WizardDirValue) + 'ProjectHopeLynden.db';
end;

function GetAppBackupPath: string;
begin
  Result := ExpandConstant('{tmp}\ProjectHopeLynden.app.db.backup');
end;

procedure BackupDatabaseIfPresent(DatabasePath: string; BackupPath: string);
begin
  if IsTaskSelected('preservedatabase') and FileExists(DatabasePath) then
  begin
    FileCopy(DatabasePath, BackupPath, false);
  end;
end;

procedure RestoreDatabaseIfBackedUp(DatabasePath: string; BackupPath: string);
begin
  if IsTaskSelected('preservedatabase') and FileExists(BackupPath) then
  begin
    ForceDirectories(ExtractFileDir(DatabasePath));
    FileCopy(BackupPath, DatabasePath, false);
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then
  begin
    BackupDatabaseIfPresent(GetProgramDataDatabasePath, GetProgramDataBackupPath);
    BackupDatabaseIfPresent(GetAppDatabasePath, GetAppBackupPath);
  end;

  if CurStep = ssPostInstall then
  begin
    RestoreDatabaseIfBackedUp(GetProgramDataDatabasePath, GetProgramDataBackupPath);
    RestoreDatabaseIfBackedUp(GetAppDatabasePath, GetAppBackupPath);
  end;
end;