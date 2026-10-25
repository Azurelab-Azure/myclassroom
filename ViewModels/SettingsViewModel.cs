using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanPeopleAvalonia.Services;

namespace PlanPeopleAvalonia.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    // 内存中的设置字典
    public Dictionary<string, object> SettingsData { get; } = new();

    // 兼容旧版
    [ObservableProperty] private string _appVersion = AppInfo.Version;
    [ObservableProperty] private string _dataPath = AppPaths.BaseDir;
    [ObservableProperty] private bool _isAutoBackupEnabled = true;
    [ObservableProperty] private int _selectedLanguageIndex = 0;

    public SettingsViewModel()
    {
        LoadFromConfig();
    }

    /// <summary>
    /// 从本地的 config.json 真实读取
    /// </summary>
    public void LoadFromConfig()
    {
        var cfg = ConfigService.Load();
        
        SettingsData.Clear();

        // 1. 通用设置
        SettingsData["StartupPage"] = "首页"; 
        SettingsData["Language"] = cfg.Language;
        SettingsData["AutoUpdate"] = cfg.AutoCheckUpdate;
        SettingsData["StartMinimized"] = false; // AppConfig 暂无此字段，后续扩展
        SettingsData["CloseToTray"] = true;
        SettingsData["MinimizeToTray"] = true;
        SettingsData["RememberWindowSize"] = true;
        SettingsData["CheckBeta"] = false;

        // 2. 课表设置
        SettingsData["ShowWeekends"] = true;
        SettingsData["MaxSections"] = 12;
        SettingsData["ShowCurrentWeek"] = true;
        SettingsData["AutoJumpWeek"] = true;
        SettingsData["StartDayOfWeek"] = "周一";
        SettingsData["TimeFormat"] = "24小时制";
        SettingsData["ShowCourseTeacher"] = true;
        SettingsData["ShowCourseRoom"] = true;
        SettingsData["HighlightCurrentCourse"] = true;
        SettingsData["EnableDoubleClickEdit"] = !cfg.RequirePasswordOnCourseEdit; 

        // 3. 外观设置
        SettingsData["ThemeMode"] = cfg.Theme;
        SettingsData["AccentColor"] = cfg.AccentColor;
        SettingsData["FontSize"] = 14;
        SettingsData["FontFamily"] = "微软雅黑";
        SettingsData["CornerRadius"] = 8;
        SettingsData["EnableAnimation"] = true;
        SettingsData["AnimationSpeed"] = 1.0;
        SettingsData["ShowIcons"] = true;
        SettingsData["CompactMode"] = false;
        SettingsData["Transparency"] = 0;

        // 4. 声音与提醒
        SettingsData["EnableSound"] = true;
        SettingsData["Volume"] = 50;
        SettingsData["ReminderBeforeClass"] = 5;
        SettingsData["ReminderSound"] = "默认";
        SettingsData["VibrateMode"] = false;
        SettingsData["EnableDoNotDisturb"] = false;
        SettingsData["DoNotDisturbStart"] = "22:00";
        SettingsData["DoNotDisturbEnd"] = "07:00";

        // 5. 数据与隐私
        SettingsData["AutoBackup"] = true;
        SettingsData["BackupInterval"] = "每天";
        SettingsData["LocalEncryption"] = false;
        SettingsData["LogLevel"] = "普通";
        SettingsData["ClearCacheDays"] = 30;
        SettingsData["ExportFormat"] = "JSON";
        SettingsData["AutoSave"] = true;
        SettingsData["AutoSaveInterval"] = 5;

        // 6. 高级设置
        SettingsData["DebugMode"] = false;
        SettingsData["DevTools"] = false;
        SettingsData["NetworkProxy"] = "";
        SettingsData["HardwareAcceleration"] = true;
        SettingsData["MaxMemoryUsage"] = 512;
        SettingsData["ThreadCount"] = 4;
        SettingsData["CustomCss"] = false;
        SettingsData["ExperimentalFeatures"] = false;
    }

    /// <summary>
    /// 真实写入到 config.json
    /// </summary>
    [RelayCommand]
    private void SaveSettings()
    {
        try
        {
            var cfg = ConfigService.Load();

            cfg.Language = SettingsData["Language"].ToString() ?? "zh-CN";
            cfg.AutoCheckUpdate = (bool)SettingsData["AutoUpdate"];
            cfg.RequirePasswordOnCourseEdit = !(bool)SettingsData["EnableDoubleClickEdit"];
            cfg.Theme = SettingsData["ThemeMode"].ToString() ?? "light";
            cfg.AccentColor = SettingsData["AccentColor"].ToString() ?? "#0078D4";

            ConfigService.Save(cfg);
            Console.WriteLine("【设置】已成功保存到本地 config.json！");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"【设置】保存失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 恢复默认配置
    /// </summary>
    [RelayCommand]
    private void ResetToDefault()
    {
        try
        {
            var defaultCfg = new AppConfig(); // 默认 AppConfig
            ConfigService.Save(defaultCfg);
            LoadFromConfig(); // 重新读取回字典
            Console.WriteLine("【设置】已恢复默认！");
        }
        catch { }
    }

    // 兼容旧版 XAML
    [RelayCommand]
    private void OpenDataFolder()
    {
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = AppPaths.BaseDir, UseShellExecute = true }); } catch { }
    }

    [RelayCommand]
    private void BackupNow()
    {
        try
        {
            string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"PlanPeople_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
            if (BackupService.CreateFullBackup(path)) Console.WriteLine("备份成功");
        }
        catch { }
    }
}