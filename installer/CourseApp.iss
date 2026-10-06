; ============================================================
;  我的课表 · My Schedule
;  Inno Setup 6 安装脚本
;  版本：1.0.0
; ============================================================

#define MyAppName "我的课表"
#define MyAppNameEn "My Schedule"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Kzure Lab"
#define MyAppURL "https://github.com/Azurelab-Azure/myclassroom"
#define MyAppExeName "CourseApp.exe"
#define MyAppId "CourseApp"

; ============================================================
;  [Setup] —— 安装配置
; ============================================================
[Setup]
AppId={{8F3A2B1C-4D5E-6F7A-8B9C-0D1E2F3A4B5C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
AppContact=https://github.com/Azurelab-Azure/myclassroom/issues
AppComments=无服务器、纯本地、跨语言的课程表应用

VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} 安装程序
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}
VersionInfoCopyright=© 2026 {#MyAppPublisher}

DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=no
AllowNoIcons=yes
DisableDirPage=no
DisableWelcomePage=no
DisableReadyPage=no

OutputDir=..\installer-output
OutputBaseFilename=CourseApp-Setup-{#MyAppVersion}
SetupIconFile=..\assets\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}

Compression=lzma2/max
SolidCompression=yes
LZMAUseSeparateProcess=yes

WizardStyle=modern
ShowLanguageDialog=auto
DisableFinishedPage=no
DisableStartupPrompt=no
AllowRootDirectory=no
AllowUNCPath=no
UsePreviousAppDir=yes
UsePreviousGroup=yes
UsePreviousTasks=yes
UsePreviousSetupType=yes
UsePreviousLanguage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0

CloseApplications=yes
RestartApplications=no
SetupLogging=yes

; ============================================================
;  [Languages] —— 语言
; ============================================================
[Languages]
Name: "chinese"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

; ============================================================
;  [CustomMessages] —— 自定义提示
; ============================================================
[CustomMessages]
chinese.CreateDesktopIcon=创建桌面快捷方式
chinese.AdditionalIcons=附加图标：
chinese.ProgramOnTheWeb={#MyAppName} 官网
chinese.UninstallProgram=卸载 {#MyAppName}
chinese.LaunchProgram=立即运行 {#MyAppName}
chinese.AutoStart=开机自动启动（可选）
chinese.InstallCore=核心程序（必需）
chinese.InstallLangFiles=语言文件（中 / 英 / 日 / 韩 等 20 种）
chinese.InstallIcons=图标资源
chinese.InstallAssets=应用资源
chinese.VisitGitHub=访问 GitHub 项目主页
chinese.ViewReadme=查看 README

english.CreateDesktopIcon=Create a desktop shortcut
english.AdditionalIcons=Additional icons:
english.ProgramOnTheWeb={#MyAppName} on the Web
english.UninstallProgram=Uninstall {#MyAppName}
english.LaunchProgram=Launch {#MyAppName}
english.AutoStart=Start with Windows (optional)
english.InstallCore=Core application (required)
english.InstallLangFiles=Language files (20 languages)
english.InstallIcons=Icon resources
english.InstallAssets=Application assets
english.VisitGitHub=Visit GitHub project
english.ViewReadme=View README

; ============================================================
;  [Types] —— 安装类型（只允许一个 iscustom）
; ============================================================
[Types]
Name: "full";    Description: "完整安装"
Name: "compact"; Description: "精简安装（仅核心）"
Name: "custom";  Description: "自定义安装";  Flags: iscustom

; ============================================================
;  [Components] —— 组件
; ============================================================
[Components]
Name: "core";     Description: "{cm:InstallCore}";       Types: full compact custom; Flags: fixed
Name: "lang";     Description: "{cm:InstallLangFiles}";  Types: full custom
Name: "icons";    Description: "{cm:InstallIcons}";      Types: full custom
Name: "assets";   Description: "{cm:InstallAssets}";     Types: full custom

; ============================================================
;  [Tasks] —— 附加任务
; ============================================================
[Tasks]
Name: "desktopicon";     Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "创建快速启动图标";       GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "autostart";       Description: "{cm:AutoStart}";         GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

; ============================================================
;  [Files] —— 要安装的文件
; ============================================================
[Files]
; ---------- 核心程序 ----------
Source: "..\publish\CourseApp.exe";                 DestDir: "{app}"; Flags: ignoreversion; Components: core
Source: "..\publish\CourseApp.dll";                 DestDir: "{app}"; Flags: ignoreversion; Components: core
Source: "..\publish\CourseApp.deps.json";           DestDir: "{app}"; Flags: ignoreversion; Components: core
Source: "..\publish\CourseApp.runtimeconfig.json";  DestDir: "{app}"; Flags: ignoreversion; Components: core

; ---------- 运行时（self-contained）----------
Source: "..\publish\*.dll";                         DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist; Components: core
Source: "..\publish\*.json";                        DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist; Components: core
Source: "..\publish\createdump.exe";                DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist; Components: core

; ---------- 语言文件 ----------
Source: "..\publish\lang\*.json";                   DestDir: "{app}\lang"; Flags: ignoreversion; Components: lang

; ---------- 图标 ----------
Source: "..\publish\icons\*";                       DestDir: "{app}\icons"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: icons

; ---------- 资源 ----------
Source: "..\publish\assets\*";                      DestDir: "{app}\assets"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: assets

; ---------- README / LICENSE ----------
Source: "..\README.md";                             DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "..\LICENSE";                               DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

; ============================================================
;  [Icons] —— 图标
; ============================================================
[Icons]
Name: "{group}\{#MyAppName}";                    Filename: "{app}\{#MyAppExeName}"; Comment: "启动 {#MyAppName}"
Name: "{group}\{cm:VisitGitHub}";                Filename: "{#MyAppURL}"
Name: "{group}\{cm:UninstallProgram}";           Filename: "{uninstallexe}"

Name: "{autodesktop}\{#MyAppName}";              Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon
; ============================================================
;  [Run] —— 安装后
; ============================================================
[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram}"; Flags: nowait postinstall skipifsilent

; ============================================================
;  [UninstallRun] —— 卸载前
; ============================================================
[UninstallRun]
Filename: "taskkill.exe"; Parameters: "/F /IM {#MyAppExeName}"; Flags: runhidden skipifdoesntexist

; ============================================================
;  [UninstallDelete] —— 卸载时删除
; ============================================================
[UninstallDelete]
Type: files;           Name: "{app}\*.log"
Type: files;           Name: "{app}\*.tmp"
Type: filesandordirs;  Name: "{app}\lang"
Type: filesandordirs;  Name: "{app}\icons"
Type: filesandordirs;  Name: "{app}\assets"

; 用户数据不删：
; courses.json / sections.json / teachers.json / students.json
; exams.json / duty.json / seats.json / config.json / photos/

; ============================================================
;  [Registry] —— 注册表
; ============================================================
[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
    ValueType: string; ValueName: "{#MyAppName}"; \
    ValueData: """{app}\{#MyAppExeName}"""; \
    Flags: uninsdeletevalue; Tasks: autostart

; ============================================================
;  [Code] —— 自定义逻辑
; ============================================================
[Code]

function InitializeSetup(): Boolean;
begin
  Result := True;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    if not DirExists(ExpandConstant('{app}\photos')) then
      CreateDir(ExpandConstant('{app}\photos'));
  end;
end;

function InitializeUninstall(): Boolean;
var
  DataPath: String;
begin
  Result := True;

  DataPath := ExpandConstant('{app}\courses.json');
  if FileExists(DataPath) then
  begin
    if MsgBox('检测到您的课程数据仍然保留在：' + #13#10 +
              ExpandConstant('{app}') + #13#10 + #13#10 +
              '卸载后数据不会被删除，您可以手动备份。' + #13#10 +
              '是否继续卸载？', mbConfirmation, MB_YESNO) = IDNO then
      Result := False;
  end;
end;