#define AppName "ProjectHopeLynden.Web"
#define AppVersion "0.0.0.0"
#define AppPublisher "Cisco"
#define AppExeName "ProjectHopeLynden.Web.exe"
#define ServiceName "ProjectHopeLynden"
#define ServiceDisplayName "Project Hope Lynden Inventory Server"
#define ServiceHelperName "ProjectHopeService.ps1"

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
Name: "enableincomingorders"; Description: "Enable Incoming Orders Feature"; GroupDescription: "Feature options:"; Flags: unchecked

[Dirs]
Name: "{commonappdata}\ProjectHopeLynden"; Permissions: users-modify

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#ServiceHelperName}"; DestDir: "{app}\tools"; Flags: ignoreversion
Source: "{#ServiceHelperName}"; Flags: dontcopy

[Icons]
Name: "{group}\Open Project Hope Inventory"; Filename: "http://localhost:5000/"; IconFilename: "{app}\{#AppExeName}"

[Run]
Filename: "http://localhost:5000/"; Description: "Open Project Hope Inventory"; Flags: shellexec nowait postinstall skipifsilent

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

function GetInstalledServiceHelperPath: string;
begin
  Result := ExpandConstant('{app}\tools\{#ServiceHelperName}');
end;

function GetInstalledAppSettingsPath: string;
begin
  Result := ExpandConstant('{app}\appsettings.json');
end;

function GetTemporaryServiceHelperPath: string;
begin
  Result := ExpandConstant('{tmp}\{#ServiceHelperName}');
end;

function GetIncomingOrdersEnabledValue: string;
begin
  if IsTaskSelected('enableincomingorders') then
  begin
    Result := 'true';
  end
  else
  begin
    Result := 'false';
  end;
end;

procedure BackupDatabaseIfPresent(DatabasePath: string; BackupPath: string);
begin
  if IsTaskSelected('preservedatabase') and FileExists(DatabasePath) then
  begin
    if not FileCopy(DatabasePath, BackupPath, false) then
    begin
      RaiseException('The existing Project Hope database could not be backed up before installation.');
    end;
  end;
end;

procedure RestoreDatabaseIfBackedUp(DatabasePath: string; BackupPath: string);
begin
  if IsTaskSelected('preservedatabase') and FileExists(BackupPath) then
  begin
    ForceDirectories(ExtractFileDir(DatabasePath));
    if not FileCopy(BackupPath, DatabasePath, false) then
    begin
      RaiseException('The preserved Project Hope database could not be restored after installation.');
    end;
  end;
end;

procedure RunServiceHelper(HelperPath: string; ActionName: string);
var
  Parameters: string;
  ResultCode: Integer;
begin
  if not FileExists(HelperPath) then
  begin
    RaiseException('The Project Hope Windows service helper was not found: ' + HelperPath);
  end;

  Parameters := '-NoProfile -NonInteractive -ExecutionPolicy Bypass' +
    ' -File "' + HelperPath + '"' +
    ' -Action "' + ActionName + '"' +
    ' -ServiceName "{#ServiceName}"';

  if ActionName = 'Install' then
  begin
    Parameters := Parameters +
      ' -DisplayName "{#ServiceDisplayName}"' +
      ' -ExecutablePath "' + ExpandConstant('{app}\{#AppExeName}') + '"';
  end;

  if ActionName = 'Configure' then
  begin
    Parameters := Parameters +
      ' -ConfigPath "' + GetInstalledAppSettingsPath + '"' +
      ' -IncomingOrdersEnabled "' + GetIncomingOrdersEnabledValue + '"';
  end;

  Log('Running Project Hope service action: ' + ActionName);

  if not Exec(
    ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'),
    Parameters,
    '',
    SW_HIDE,
    ewWaitUntilTerminated,
    ResultCode) then
  begin
    RaiseException('Windows could not start the Project Hope service configuration helper.');
  end;

  if ResultCode <> 0 then
  begin
    RaiseException(
      'The Project Hope Windows service action "' + ActionName +
      '" failed with exit code ' + IntToStr(ResultCode) + '.');
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then
  begin
    ExtractTemporaryFile('{#ServiceHelperName}');
    RunServiceHelper(GetTemporaryServiceHelperPath, 'Stop');

    BackupDatabaseIfPresent(GetProgramDataDatabasePath, GetProgramDataBackupPath);
    BackupDatabaseIfPresent(GetAppDatabasePath, GetAppBackupPath);
  end;

  if CurStep = ssPostInstall then
  begin
    RestoreDatabaseIfBackedUp(GetProgramDataDatabasePath, GetProgramDataBackupPath);
    RestoreDatabaseIfBackedUp(GetAppDatabasePath, GetAppBackupPath);

    RunServiceHelper(GetInstalledServiceHelperPath, 'Configure');
    RunServiceHelper(GetInstalledServiceHelperPath, 'Install');
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
  begin
    RunServiceHelper(GetInstalledServiceHelperPath, 'Remove');
  end;
end;
