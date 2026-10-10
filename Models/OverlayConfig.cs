using System.Collections.Generic;

namespace CourseApp.Models
{
    /// <summary>
    /// 悬浮组件配置。
    /// 管理灵动岛（或刘海屏 / 顶部栏）和今日侧边栏的显示。
    /// 独立保存到 overlay.json，支持导出 / 导入。
    /// </summary>
    public class OverlayConfig
    {
        // =====================================================
        // 灵动岛
        // =====================================================

        /// <summary>是否启用灵动岛</summary>
        public bool IslandEnabled { get; set; } = true;

        /// <summary>显示模式：island 灵动岛 / notch 刘海屏 / topbar 顶部栏</summary>
        public string IslandMode { get; set; } = "island";

        /// <summary>灵动岛宽度</summary>
        public int IslandWidth { get; set; } = 320;

        /// <summary>灵动岛高度</summary>
        public int IslandHeight { get; set; } = 36;

        /// <summary>灵动岛背景色（hex）</summary>
        public string IslandBg { get; set; } = "#1A1A1A";

        /// <summary>灵动岛前景色（hex）</summary>
        public string IslandFg { get; set; } = "#F0F0F0";

        /// <summary>灵动岛模块列表，按顺序渲染</summary>
        public List<string> IslandModules { get; set; } = new() { "ScheduleHint" };

        // =====================================================
        // 今日侧边栏
        // =====================================================

        /// <summary>是否启用今日侧边栏</summary>
        public bool SidebarEnabled { get; set; } = true;

        /// <summary>侧边栏宽度</summary>
        public int SidebarWidth { get; set; } = 24;

        /// <summary>侧边栏高度</summary>
        public int SidebarHeight { get; set; } = 420;

        /// <summary>侧边栏背景色（hex）</summary>
        public string SidebarBg { get; set; } = "#FFFFFF";

        /// <summary>侧边栏前景色（hex）</summary>
        public string SidebarFg { get; set; } = "#1A1A1A";

        /// <summary>侧边栏模块列表，按顺序渲染</summary>
        public List<string> SidebarModules { get; set; } = new() { "Schedule" };

        // =====================================================
        // 新闻
        // =====================================================

        /// <summary>新闻 RSS 源</summary>
        public string NewsUrl { get; set; } = "https://rsshub.app/zhihu/hotlist";

        /// <summary>新闻轮播间隔（秒）</summary>
        public int NewsIntervalSec { get; set; } = 10;

        // =====================================================
        // 工具
        // =====================================================

        /// <summary>
        /// 复制一份配置，用于导出 / 备份。
        /// </summary>
        public OverlayConfig Clone()
        {
            return new OverlayConfig
            {
                IslandEnabled = IslandEnabled,
                IslandMode = IslandMode,
                IslandWidth = IslandWidth,
                IslandHeight = IslandHeight,
                IslandBg = IslandBg,
                IslandFg = IslandFg,
                IslandModules = new List<string>(IslandModules ?? new List<string>()),
                SidebarEnabled = SidebarEnabled,
                SidebarWidth = SidebarWidth,
                SidebarHeight = SidebarHeight,
                SidebarBg = SidebarBg,
                SidebarFg = SidebarFg,
                SidebarModules = new List<string>(SidebarModules ?? new List<string>()),
                NewsUrl = NewsUrl,
                NewsIntervalSec = NewsIntervalSec,
            };
        }

        /// <summary>
        /// 返回所有可用模块的 key。
        /// </summary>
        public static string[] AllModules => new[]
        {
            "News",
            "Schedule",
            "ScheduleHint",
            "App",
        };
    }
}