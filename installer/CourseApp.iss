; ============================================================
; 我的课表 · My Schedule
; Inno Setup 安装脚本
; ============================================================

#define MyAppName "我的课表"
#define MyAppNameEn "My Schedule"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Kzure Lab"
#define MyAppURL "https://example.com"
#define MyAppExeName "CourseApp.exe"

[Setup]
; ---------- 基本 ----------
AppId={{8F3A2B1C-4D5E-6F7A-8B9C-0D1E2F3A4B5C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} 安装程序
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}

; ---------- 目录 ----------
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
AllowNoIcons=yes

; ---------- 文件 ----------
OutputDir=..\installer-output
OutputBaseFilename=CourseApp-Setup-{#MyAppVersion}
SetupIconFile=..\assets\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

; ---------- 语言 ----------
ShowLanguageDialog=auto

[Languages]
Name: "chinese"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1

[Files]
; ---------- 主程序（publish 目录下全部文件）----------
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; ---------- 开始菜单 ----------
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"

; ---------- 桌面 ----------
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

; ---------- 快速启动栏 ----------
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
; ---------- 安装完运行 ----------
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; ---------- 卸载时清掉（可选）----------
Type: filesandordirs; Name: "{app}\lang"
Type: filesandordirs; Name: "{app}\icons"
Type: filesandordirs; Name: "{app}\assets"

[Code]
// ============================================================
// 自定义逻辑
// ============================================================

// 是否勾选"创建桌面快捷方式"
function InitializeSetup(): Boolean;
begin
  Result := True;
end;

// 用户目录下检测是否有旧配置（可选）
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // 安装后逻辑
  end;
end;