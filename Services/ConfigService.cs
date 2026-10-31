using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace PlanPeopleAvalonia.Services
{
    // ★ 包含了所有配置项
    public class AppConfig
    {
        // 1. 基础与通用
        public string Language { get; set; } = "zh-CN";
        public bool AutoCheckUpdate { get; set; } = true;
        public bool StartMinimized { get; set; } = false;
        public bool RememberLastPage { get; set; } = true;
        public bool EnableDoubleClick { get; set; } = true;
        public bool AutoSaveOnExit { get; set; } = true;
        public int AutoSaveInterval { get; set; } = 5;

        // 2. 外观与主题
        public string Theme { get; set; } = "light";
        public string AccentColor { get; set; } = "#0078D4";
        public double WindowFontSize { get; set; } = 14;
        public string FontFamily { get; set; } = "Microsoft YaHei";
        public bool EnableBlur { get; set; } = true;
        public bool ShowBorder { get; set; } = true;
        public bool CompactMode { get; set; } = false;
        public bool ShowAvatar { get; set; } = true;

        // 3. 课表专属
        public int TotalSections { get; set; } = 8;
        public int DaysPerWeek { get; set; } = 5;
        public double CellWidth { get; set; } = 100;
        public double CellHeight { get; set; } = 80;
        public bool ShowWeekends { get; set; } = true;
        public bool ShowNonCurrentWeekCourses { get; set; } = false;
        public bool HighlightCurrentDay { get; set; } = true;
        public string DefaultCourseColor { get; set; } = "#4CAF50";
        public bool EnableConflictCheck { get; set; } = true;
        public bool EnableDragDrop { get; set; } = true;

        // 4. 灵动岛与悬浮
        public bool EnableIsland { get; set; } = false;
        public bool EnableFloatingWindow { get; set; } = false;
        public string IslandTheme { get; set; } = "dark";
        public int IslandWidth { get; set; } = 320;
        public int IslandHeight { get; set; } = 36;
        public bool IslandShowWeather { get; set; } = false;
        public bool IslandShowTime { get; set; } = true;
        public bool FloatingTopMost { get; set; } = true;
        public double FloatingOpacity { get; set; } = 0.9;

        // 5. 安全与高级
        public bool RequirePasswordOnCourseEdit { get; set; } = false;
        public bool EnableDataEncryption { get; set; } = false;
        public string DataBackupPath { get; set; } = "";
        public bool AutoBackupOnStartup { get; set; } = true;
        public bool DebugMode { get; set; } = false;
        public bool EnableTelemetry { get; set; } = false;

        // ★ 6. 隐私协议 (新增)
        public bool HasAcceptedPrivacy { get; set; } = false;
    }

    public static class ConfigService
    {
        private static readonly JsonSerializerOptions WriteOpts = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        private static readonly JsonSerializerOptions ReadOpts = new() { PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

        public static event Action<AppConfig>? ConfigChanged;

        public static AppConfig Load()
        {
            try
            {
                if (!File.Exists(AppPaths.ConfigJson)) return new AppConfig();
                var json = File.ReadAllText(AppPaths.ConfigJson);
                return JsonSerializer.Deserialize<AppConfig>(json, ReadOpts) ?? new AppConfig();
            }
            catch { return new AppConfig(); }
        }

        public static void Save(AppConfig cfg)
        {
            if (cfg == null) return;
            try
            {
                var dir = Path.GetDirectoryName(AppPaths.ConfigJson);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(AppPaths.ConfigJson, JsonSerializer.Serialize(cfg, WriteOpts));
                ConfigChanged?.Invoke(cfg);
            }
            catch { }
        }
    }
}