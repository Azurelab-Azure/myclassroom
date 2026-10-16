using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace CourseApp.Services
{
    /// <summary>
    /// 全局配置。
    /// 保存到 config.json，包含语言、主题、应用模式、悬浮组件、更新等设置。
    /// </summary>
    public class AppConfig
    {
        // =====================================================
        // 通用
        // =====================================================

        /// <summary>界面语言：zh-CN / en 等</summary>
        public string Language { get; set; } = "zh-CN";

        /// <summary>主题：light / dark / clear / fresh / system</summary>
        public string Theme { get; set; } = "light";

        /// <summary>应用模式：school 学校 / student 学生 / office 办公</summary>
        public string AppMode { get; set; } = "school";

        /// <summary>编辑课程是否需要密码</summary>
        public bool RequirePasswordOnCourseEdit { get; set; } = false;

        // =====================================================
        // 灵动岛
        // =====================================================

        /// <summary>灵动岛宽度（px）</summary>
        public int IslandWidth { get; set; } = 200;

        /// <summary>灵动岛高度（px）</summary>
        public int IslandHeight { get; set; } = 36;

        /// <summary>灵动岛显示新闻</summary>
        public bool IslandShowNews { get; set; } = false;

        /// <summary>新闻 RSS 源</summary>
        public string IslandNewsUrl { get; set; } = "https://rsshub.app/zhihu/hotlist";

        /// <summary>新闻轮播间隔（秒）</summary>
        public int IslandNewsIntervalSec { get; set; } = 10;

        // =====================================================
        // 侧边栏
        // =====================================================

        /// <summary>位置：left / right / top / bottom</summary>
        public string SidebarPosition { get; set; } = "left";

        /// <summary>尺寸（保留兼容）</summary>
        public int SidebarSize { get; set; } = 72;

        /// <summary>主对齐：start / center / end</summary>
        public string SidebarAlignPrimary { get; set; } = "start";

        /// <summary>交叉对齐：start / center / end</summary>
        public string SidebarAlignSecondary { get; set; } = "center";

        /// <summary>今日侧边栏是否显示</summary>
        public bool TodaySidebarVisible { get; set; } = true;

        /// <summary>今日侧边栏位置 X（-1 表示默认）</summary>
        public int TodaySidebarX { get; set; } = -1;

        /// <summary>今日侧边栏位置 Y（-1 表示默认）</summary>
        public int TodaySidebarY { get; set; } = -1;

        /// <summary>今日侧边栏折叠状态</summary>
        public bool TodaySidebarCollapsed { get; set; } = false;

        // =====================================================
        // 强调色
        // =====================================================

        /// <summary>强调色（hex）</summary>
        public string AccentColor { get; set; } = "#0078D4";

        // =====================================================
        // 更新
        // =====================================================

        /// <summary>启动时自动检查更新</summary>
        public bool AutoCheckUpdate { get; set; } = true;

        /// <summary>上次检查更新的时间（ISO 8601）</summary>
        public string LastUpdateCheck { get; set; } = "";
    }

    /// <summary>
    /// 读写 config.json。
    /// </summary>
    public static class ConfigService
    {
        /// <summary>JSON 写入选项（不转义中文）</summary>
        private static readonly JsonSerializerOptions WriteOpts = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        /// <summary>JSON 读取选项（大小写不敏感，允许注释）</summary>
        private static readonly JsonSerializerOptions ReadOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        /// <summary>
        /// 加载配置。文件不存在时返回默认配置。
        /// </summary>
        public static AppConfig Load()
        {
            try
            {
                if (!File.Exists(AppPaths.ConfigJson))
                    return new AppConfig();

                var json = File.ReadAllText(AppPaths.ConfigJson);
                var cfg = JsonSerializer.Deserialize<AppConfig>(json, ReadOpts);
                return cfg ?? new AppConfig();
            }
            catch
            {
                return new AppConfig();
            }
        }

        /// <summary>
        /// 保存配置。
        /// </summary>
        public static void Save(AppConfig cfg)
        {
            if (cfg == null) return;
            try
            {
                var dir = Path.GetDirectoryName(AppPaths.ConfigJson);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(cfg, WriteOpts);
                File.WriteAllText(AppPaths.ConfigJson, json);
            }
            catch { }
        }
    }
}