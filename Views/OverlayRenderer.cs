using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CourseApp.Localization;
using CourseApp.Models;
using CourseApp.Theme;

namespace CourseApp.Views
{
    /// <summary>
    /// 悬浮组件渲染上下文。
    /// 由灵动岛 / 侧边栏在渲染前填充。
    /// </summary>
    public class OverlayContext
    {
        /// <summary>节次列表</summary>
        public List<SectionTime> Sections = new();

        /// <summary>课程列表</summary>
        public List<Course> Courses = new();

        /// <summary>当前周次</summary>
        public int CurrentWeek = 1;

        /// <summary>值日生表</summary>
        public DutyRoster Duty = new();

        /// <summary>班委公告</summary>
        public List<ClassCommittee> Committees = new();

        /// <summary>当前新闻标题</summary>
        public string CurrentNews = "";

        /// <summary>当前前台进程名</summary>
        public string CurrentApp = "";

        /// <summary>当前时间</summary>
        public DateTime Now = DateTime.Now;
    }

    /// <summary>
    /// 悬浮组件通用模块渲染器。
    /// 支持：News / Schedule / ScheduleHint / App。
    /// </summary>
    public static class OverlayRenderer
    {
        /// <summary>
        /// 模块 key → 中文名（用于设置页显示）。
        /// </summary>
        public static string ModuleDisplayName(string module)
        {
            return module switch
            {
                "News" => I18n.T("overlay.module.news"),
                "Schedule" => I18n.T("overlay.module.schedule"),
                "ScheduleHint" => I18n.T("overlay.module.scheduleHint"),
                "App" => I18n.T("overlay.module.app"),
                _ => module,
            };
        }

        /// <summary>
        /// 测量模块所需宽度。
        /// </summary>
        public static int MeasureWidth(Graphics g, string module, OverlayContext ctx, Font font)
        {
            string text = RenderText(module, ctx);
            if (string.IsNullOrEmpty(text)) return 0;
            return TextRenderer.MeasureText(g, text, font).Width;
        }

        /// <summary>
        /// 在指定矩形内绘制单个模块。
        /// </summary>
        public static void Render(Graphics g, Rectangle rect, string module,
            OverlayContext ctx, Color fg, Font font)
        {
            string text = RenderText(module, ctx);
            if (string.IsNullOrEmpty(text)) return;

            TextRenderer.DrawText(g, text, font, rect, fg,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        /// <summary>
        /// 计算模块在指定时间点的显示文本。
        /// </summary>
        public static string RenderText(string module, OverlayContext ctx)
        {
            return module switch
            {
                "News" => RenderNews(ctx),
                "Schedule" => RenderSchedule(ctx),
                "ScheduleHint" => RenderScheduleHint(ctx),
                "App" => RenderApp(ctx),
                _ => "",
            };
        }

        // =====================================================
        // 模块：新闻
        // =====================================================
        /// <summary>
        /// 新闻：显示当前标题。
        /// </summary>
        private static string RenderNews(OverlayContext ctx)
        {
            if (string.IsNullOrEmpty(ctx.CurrentNews)) return I18n.T("common.loading");
            return ctx.CurrentNews;
        }

        // =====================================================
        // 模块：课表简写
        // =====================================================
        /// <summary>
        /// 课表简写：今日所有课程，每门取首字。
        /// </summary>
        private static string RenderSchedule(OverlayContext ctx)
        {
            var today = GetTodayCourses(ctx);
            if (today.Count == 0) return I18n.T("island.noCourse");

            var chars = today.Select(c => GetFirstChar(c.Name));
            return string.Join(" ", chars);
        }

        // =====================================================
        // 模块：上课提示
        // =====================================================
        /// <summary>
        /// 上课提示：正在上 / 即将上 / 即将下。
        /// </summary>
        private static string RenderScheduleHint(OverlayContext ctx)
        {
            var now = ctx.Now;

            foreach (var st in ctx.Sections.Where(s => s.Type == "normal").OrderBy(s => s.Section))
            {
                if (string.IsNullOrEmpty(st.StartTime) || string.IsNullOrEmpty(st.EndTime))
                    continue;

                if (!TryParseTime(now, st.StartTime, out var startDt)) continue;
                if (!TryParseTime(now, st.EndTime, out var endDt)) continue;

                if (now >= startDt && now < endDt)
                {
                    var course = FindCourseAt(ctx, st.Section);
                    string name = course?.Name ?? string.Format(I18n.T("schedule.section"), st.Section);
                    var remain = endDt - now;

                    if (remain.TotalMinutes <= 5)
                        return string.Format(I18n.T("island.soonEnd"), name, FormatSpan(remain));
                    return string.Format(I18n.T("island.remain"), name, FormatSpan(remain));
                }

                if (now < startDt)
                {
                    var course = FindCourseAt(ctx, st.Section);
                    string name = course?.Name ?? string.Format(I18n.T("schedule.section"), st.Section);
                    var remain = startDt - now;
                    return string.Format(I18n.T("island.nextAt"), name, st.StartTime, FormatSpan(remain));
                }
            }

            return I18n.T("island.noCourse");
        }

        // =====================================================
        // 模块：正在运行的应用
        // =====================================================
        /// <summary>
        /// 应用：显示前台进程名。
        /// </summary>
        private static string RenderApp(OverlayContext ctx)
        {
            if (string.IsNullOrEmpty(ctx.CurrentApp)) return "";
            return ctx.CurrentApp;
        }

        // =====================================================
        // 工具
        // =====================================================
        /// <summary>
        /// 取今日课程。
        /// </summary>
        private static List<Course> GetTodayCourses(OverlayContext ctx)
        {
            int todayWeekday = (int)ctx.Now.DayOfWeek;
            if (todayWeekday == 0) todayWeekday = 7;

            return ctx.Courses
                .Where(c => c.WeekDay == todayWeekday
                            && c.Weeks != null && c.Weeks.Contains(ctx.CurrentWeek))
                .OrderBy(c => c.TimeStart)
                .ToList();
        }

        /// <summary>
        /// 找某节课对应的课程。
        /// </summary>
        private static Course? FindCourseAt(OverlayContext ctx, int section)
        {
            var today = GetTodayCourses(ctx);
            return today.FirstOrDefault(c => section >= c.TimeStart && section <= c.TimeEnd);
        }

        /// <summary>
        /// 取课程名首字。
        /// </summary>
        private static string GetFirstChar(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            return name.Substring(0, 1).ToUpperInvariant();
        }

        /// <summary>
        /// 解析时间字符串为今天的 DateTime。
        /// </summary>
        private static bool TryParseTime(DateTime now, string timeStr, out DateTime result)
        {
            result = default;
            if (string.IsNullOrEmpty(timeStr)) return false;
            if (!TimeSpan.TryParse(timeStr, out var ts)) return false;
            result = now.Date + ts;
            return true;
        }

        /// <summary>
        /// 格式化时间跨度。
        /// </summary>
        private static string FormatSpan(TimeSpan span)
        {
            if (span.TotalHours >= 1)
                return $"{(int)span.TotalHours}h{span.Minutes}m";
            return $"{span.Minutes}m";
        }

        /// <summary>
        /// hex 转 Color，失败返回 fallback。
        /// </summary>
        public static Color ParseColor(string hex, Color fallback)
        {
            if (string.IsNullOrEmpty(hex)) return fallback;
            try
            {
                if (hex.StartsWith("#")) hex = hex.Substring(1);
                if (hex.Length == 6)
                {
                    int r = Convert.ToInt32(hex.Substring(0, 2), 16);
                    int g = Convert.ToInt32(hex.Substring(2, 2), 16);
                    int b = Convert.ToInt32(hex.Substring(4, 2), 16);
                    return Color.FromArgb(r, g, b);
                }
                if (hex.Length == 8)
                {
                    int a = Convert.ToInt32(hex.Substring(0, 2), 16);
                    int r = Convert.ToInt32(hex.Substring(2, 2), 16);
                    int g = Convert.ToInt32(hex.Substring(4, 2), 16);
                    int b = Convert.ToInt32(hex.Substring(6, 2), 16);
                    return Color.FromArgb(a, r, g, b);
                }
            }
            catch { }
            return fallback;
        }

        /// <summary>
        /// Color 转 hex。
        /// </summary>
        public static string ToHex(Color c)
        {
            return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        }
    }
}