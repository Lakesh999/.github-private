; Hugh CLI Installer
; Inno Setup Script for Hugh - Self-Operating Development Loop Orchestrator
;
; Build with: iscc /DMyAppVersion=1.0.0 hugh-setup.iss
; Output: hugh-setup-{version}.exe

; =============================================================================
; Application Metadata
; =============================================================================

#define MyAppName "Hugh"
#define MyAppPublisher "PCH Development"
#define MyAppURL "https://github.com/PointCHealth/.github-private"
#define MyAppExeName "Hugh.exe"
#define MyAppDescription "Self-Operating Development Loop Orchestrator"

; Version is passed via /DMyAppVersion=x.y.z during compilation
; Defaults to 0.0.0 for local development builds
#ifndef MyAppVersion
  #define MyAppVersion "0.0.0"
#endif

; =============================================================================
; Setup Configuration
; =============================================================================

[Setup]
; Unique application identifier - DO NOT CHANGE after first release
AppId={{009C6B3E-168E-41E7-879C-E0D884474568}

; Application information
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases

; Installation settings
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes

; Output settings
OutputDir=..\dist
OutputBaseFilename=hugh-setup-{#MyAppVersion}

; Compression settings - LZMA2/ultra64 for best compression
Compression=lzma2/ultra64
SolidCompression=yes

; Architecture settings - 64-bit only
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; Environment changes - notifies running applications to reload PATH
ChangesEnvironment=yes

; UI settings
WizardStyle=modern
; SetupIconFile=..\Hugh\assets\hugh-icon.ico  ; Uncomment when icon file is available
UninstallDisplayIcon={app}\{#MyAppExeName}

; Privilege settings - prefer per-user installation (no admin required)
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

; Version information for the installer executable
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppDescription} Setup
VersionInfoCopyright=Copyright (C) 2026 {#MyAppPublisher}
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}

; =============================================================================
; Files to Install
; =============================================================================

[Files]
; Main application files from self-contained publish output
; Note: Path is relative to installer/ directory; workflow publishes to ./publish/win-x64
Source: "..\publish\win-x64\*"; DestDir: "{app}"; \
    Flags: ignoreversion recursesubdirs createallsubdirs

; Agent prompt files - bundled as fallback for offline users
; At runtime, Hugh auto-discovers agents from GitHub organization repos
Source: "..\agents\*.md"; DestDir: "{app}\agents"; \
    Flags: ignoreversion skipifsourcedoesntexist

; =============================================================================
; Start Menu Icons
; =============================================================================

[Icons]
; Start menu shortcuts
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; \
    Comment: "{#MyAppDescription}"
Name: "{group}\{#MyAppName} Documentation"; Filename: "{#MyAppURL}"; \
    Comment: "View Hugh documentation online"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"; \
    Comment: "Remove {#MyAppName} from your computer"

; =============================================================================
; Registry Entries
; =============================================================================

[Registry]
; Application registration for "Apps & Features" panel
Root: HKCU; Subkey: "Software\{#MyAppPublisher}\{#MyAppName}"; \
    ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"; \
    Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\{#MyAppPublisher}\{#MyAppName}"; \
    ValueType: string; ValueName: "Version"; ValueData: "{#MyAppVersion}"

; =============================================================================
; Post-Installation Messages
; =============================================================================

[Messages]
FinishedLabel=Setup has finished installing [name] on your computer.%n%nIMPORTANT: Open a NEW terminal window to use the 'hugh' command.%n%nThe application may be launched from the Start menu or by typing 'hugh' in a new terminal.

; =============================================================================
; Run Section - Optional post-install actions
; =============================================================================

[Run]
; Option to display version after installation
Filename: "{app}\{#MyAppExeName}"; Parameters: "--version"; \
    Description: "Verify Hugh installation"; Flags: postinstall nowait skipifsilent shellexec runhidden

; =============================================================================
; Pascal Script - PATH Management
; =============================================================================

[Code]
const
  EnvironmentKey = 'Environment';

// Expands the current user's PATH environment variable
function GetCurrentPath(): String;
var
  Path: String;
begin
  if not RegQueryStringValue(HKEY_CURRENT_USER, EnvironmentKey, 'Path', Path) then
    Path := '';
  Result := Path;
end;

// Checks if a path already exists in the PATH variable (case-insensitive)
function PathExists(const InstallPath, CurrentPath: String): Boolean;
var
  NormalizedPath: String;
  NormalizedInstall: String;
begin
  NormalizedPath := ';' + Uppercase(CurrentPath) + ';';
  NormalizedInstall := ';' + Uppercase(InstallPath) + ';';
  Result := Pos(NormalizedInstall, NormalizedPath) > 0;
end;

// Adds Hugh's installation directory to the user's PATH
procedure EnvAddPath(const InstallPath: String);
var
  CurrentPath: String;
begin
  CurrentPath := GetCurrentPath();
  
  // Only add if not already present
  if not PathExists(InstallPath, CurrentPath) then
  begin
    // Append with semicolon separator
    if CurrentPath <> '' then
      CurrentPath := CurrentPath + ';';
    CurrentPath := CurrentPath + InstallPath;
    
    // Write to user's PATH (HKEY_CURRENT_USER - no admin required)
    RegWriteStringValue(HKEY_CURRENT_USER, EnvironmentKey, 'Path', CurrentPath);
    
    Log('Added to PATH: ' + InstallPath);
  end
  else
    Log('PATH already contains: ' + InstallPath);
end;

// Removes Hugh's installation directory from the user's PATH
procedure EnvRemovePath(const InstallPath: String);
var
  CurrentPath: String;
  SearchPath: String;
  StartPos: Integer;
begin
  CurrentPath := GetCurrentPath();
  
  if CurrentPath = '' then
    Exit;
  
  // Try different path patterns (with/without trailing semicolons)
  // Pattern: ;InstallPath; -> ;
  SearchPath := ';' + Uppercase(InstallPath) + ';';
  StartPos := Pos(SearchPath, ';' + Uppercase(CurrentPath) + ';');
  
  if StartPos > 0 then
  begin
    // Handle different positions in PATH
    if StartPos = 1 then
    begin
      // At start of PATH: remove InstallPath;
      StringChangeEx(CurrentPath, InstallPath + ';', '', True);
    end
    else
    begin
      // In middle or end: remove ;InstallPath
      StringChangeEx(CurrentPath, ';' + InstallPath, '', True);
    end;
    
    // Clean up any double semicolons
    StringChangeEx(CurrentPath, ';;', ';', True);
    
    // Remove leading/trailing semicolons
    if (Length(CurrentPath) > 0) and (CurrentPath[1] = ';') then
      Delete(CurrentPath, 1, 1);
    if (Length(CurrentPath) > 0) and (CurrentPath[Length(CurrentPath)] = ';') then
      Delete(CurrentPath, Length(CurrentPath), 1);
    
    RegWriteStringValue(HKEY_CURRENT_USER, EnvironmentKey, 'Path', CurrentPath);
    Log('Removed from PATH: ' + InstallPath);
  end
  else
    Log('PATH did not contain: ' + InstallPath);
end;

// Called after installation completes
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    EnvAddPath(ExpandConstant('{app}'));
    Log('Hugh installation completed. PATH updated.');
  end;
end;

// Called during uninstallation
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    EnvRemovePath(ExpandConstant('{app}'));
    Log('Hugh uninstallation completed. PATH cleaned.');
  end;
end;

// Display information to user about terminal restart
procedure DeinitializeSetup();
begin
  // Note: ChangesEnvironment=yes handles notifying Explorer
  // But terminals opened before installation need to be restarted
end;
