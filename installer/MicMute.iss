#ifndef SourceRoot
  #define SourceRoot ".."
#endif
#ifndef OutputDir
  #define OutputDir SourceRoot + "\dist\installer"
#endif
#define AppVersion "3.2.0"
#define HasRussian FileExists(AddBackslash(CompilerPath) + "Languages\Russian.isl")
#define HasSpanish FileExists(AddBackslash(CompilerPath) + "Languages\Spanish.isl")

[Setup]
AppId={{A944DB73-41EF-4860-A813-7EA43D27A59D}
AppName=MicMute
AppVersion={#AppVersion}
VersionInfoVersion={#AppVersion}
AppPublisher=MicMute contributors
DefaultDirName={localappdata}\Programs\MicMute
DefaultGroupName=MicMute
DisableProgramGroupPage=yes
UsePreviousTasks=no
PrivilegesRequired=lowest
MinVersion=10.0
OutputDir={#OutputDir}
OutputBaseFilename=MicMute-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
AppMutex=Local\MicMute.OpenSource.v3
CloseApplications=yes
RestartApplications=no
UninstallDisplayIcon={app}\MicMute2.exe
#if FileExists(SourceRoot + "\assets\default\mic_on.ico")
SetupIconFile={#SourceRoot}\assets\default\mic_on.ico
#endif
LicenseFile={#SourceRoot}\LICENSE

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
#if HasRussian
Name: "ru"; MessagesFile: "compiler:Languages\Russian.isl"
#endif
#if HasSpanish
Name: "es"; MessagesFile: "compiler:Languages\Spanish.isl"
#endif

[CustomMessages]
en.ShortcutGroup=Additional shortcuts:
en.CreateDesktopShortcut=Create a &desktop shortcut
en.StartAtSignIn=Start MicMute when I &sign in
en.LaunchMicMute=Launch MicMute
en.FrameworkRequired=MicMute requires Microsoft .NET Framework 4.8 or newer. Install it, then run setup again.
#if HasRussian
ru.ShortcutGroup=Дополнительные ярлыки:
ru.CreateDesktopShortcut=Создать ярлык на &рабочем столе
ru.StartAtSignIn=Запускать MicMute при &входе в Windows
ru.LaunchMicMute=Запустить MicMute
ru.FrameworkRequired=Для MicMute требуется Microsoft .NET Framework 4.8 или новее. Установите его, затем снова запустите установщик.
#endif
#if HasSpanish
es.ShortcutGroup=Accesos directos adicionales:
es.CreateDesktopShortcut=Crear un acceso directo en el &escritorio
es.StartAtSignIn=Iniciar MicMute al &iniciar sesión
es.LaunchMicMute=Abrir MicMute
es.FrameworkRequired=MicMute requiere Microsoft .NET Framework 4.8 o posterior. Instálelo y vuelva a ejecutar el instalador.
#endif

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopShortcut}"; GroupDescription: "{cm:ShortcutGroup}"; Flags: unchecked
Name: "startup"; Description: "{cm:StartAtSignIn}"; GroupDescription: "{cm:ShortcutGroup}"; Flags: unchecked

[Files]
Source: "{#SourceRoot}\dist\portable\MicMute2.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceRoot}\assets\default\mic_on.ico"; DestDir: "{app}\assets\default"; Flags: ignoreversion
Source: "{#SourceRoot}\assets\default\mic_off.ico"; DestDir: "{app}\assets\default"; Flags: ignoreversion
Source: "{#SourceRoot}\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceRoot}\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceRoot}\README.ru.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceRoot}\README.es.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceRoot}\PUBLISHING.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceRoot}\CHANGELOG.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceRoot}\THIRD-PARTY-NOTICES.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\MicMute"; Filename: "{app}\MicMute2.exe"
Name: "{autodesktop}\MicMute"; Filename: "{app}\MicMute2.exe"; Tasks: desktopicon
Name: "{userstartup}\MicMute"; Filename: "{app}\MicMute2.exe"; Parameters: "--startup"; WorkingDir: "{app}"; Check: ShouldWriteStartupLink; Flags: uninsneveruninstall

[Run]
Filename: "{app}\MicMute2.exe"; Description: "{cm:LaunchMicMute}"; Flags: nowait postinstall skipifsilent unchecked

[Code]
function IsOwnStartupLink(const LinkPath: String): Boolean;
var
  Shell, Shortcut: Variant;
  Target: String;
begin
  Result := False;
  if not FileExists(LinkPath) then
    Exit;
  try
    Shell := CreateOleObject('WScript.Shell');
    Shortcut := Shell.CreateShortcut(LinkPath);
    Target := Shortcut.TargetPath;
    Result := CompareText(Target, ExpandConstant('{app}\MicMute2.exe')) = 0;
  except
    Log('Could not inspect the existing MicMute startup shortcut: ' + GetExceptionMessage);
  end;
end;

function ShouldWriteStartupLink(): Boolean;
var
  LinkPath: String;
begin
  LinkPath := ExpandConstant('{userstartup}\MicMute.lnk');
  if FileExists(LinkPath) then
    Result := IsOwnStartupLink(LinkPath)
  else
    Result := WizardIsTaskSelected('startup');
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  LinkPath: String;
begin
  if CurUninstallStep = usUninstall then
  begin
    LinkPath := ExpandConstant('{userstartup}\MicMute.lnk');
    if IsOwnStartupLink(LinkPath) then
      if not DeleteFile(LinkPath) then
        Log('Could not remove the MicMute startup shortcut.');
  end;
end;

function InitializeSetup(): Boolean;
var
  Release: Cardinal;
begin
  Result := (RegQueryDWordValue(HKLM64, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) and (Release >= 528040)) or
    (RegQueryDWordValue(HKLM32, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) and (Release >= 528040));
  if not Result then
    MsgBox(CustomMessage('FrameworkRequired'), mbError, MB_OK);
end;
