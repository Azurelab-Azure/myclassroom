using System.Collections.Generic;

namespace CourseApp.Models
{
    /// <summary>
    /// 悬浮组件配置。
    /// </summary>
    public class OverlayConfig
    {
        public bool IslandEnabled { get; set; } = true;
        public string IslandMode { get; set; } = "island";
        public int IslandWidth { get; set; } = 320;
        public int IslandHeight { get; set; } = 36;
        public string IslandBg { get; set; } = "#1A1A1A";
        public string IslandFg { get; set; } = "#F0F0F0";
        public List<string> IslandModules { get; set; } = new() { "ScheduleHint" };

        public bool SidebarEnabled { get; set; } = true;
        public int SidebarWidth { get; set; } = 24;
        public int SidebarHeight { get; set; } = 420;
        public string SidebarBg { get; set; } = "#FFFFFF";
        public string SidebarFg { get; set; } = "#1A1A1A";
        public List<string> SidebarModules { get; set; } = new() { "Schedule" };

        public string NewsUrl { get; set; } = "https://rsshub.app/zhihu/hotlist";
        public int NewsIntervalSec { get; set; } = 10;

        /// <summary>
        /// 复制一份配置。
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
        /// 返回学校模式的可用模块。
        /// </summary>
        public static string[] AllModules => new[]
        {
            "News",
            "Schedule",
            "ScheduleHint",
            "App",
        };

        /// <summary>
        /// 返回办公模式的可用模块。
        /// </summary>
        public static string[] OfficeModules => new[]
        {
            "News",
            "OfficeSchedule",
            "OfficeTodo",
            "Weather",
            "App",
        };
    }
}