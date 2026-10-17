using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CourseApp.Localization;
using CourseApp.Models;
using CourseApp.Services;
using CourseApp.Theme;

namespace CourseApp.Views
{
    /// <summary>
    /// 悬浮组件渲染上下文。
    /// </summary>
    public class OverlayContext
    {
        public List<SectionTime> Sections = new();
        public List<Course> Courses = new();
        public int CurrentWeek = 1;
        public DutyRoster Duty = new();
        public List<ClassCommittee> Committees = new();
        public string CurrentNews = "";
        public string CurrentApp = "";
        public DateTime Now = DateTime.Now;

        /// <summary>办公模式的日程数据</summary>
        public List<ScheduleItem> Schedules = new();

        /// <summary>天气信息（例如：多云 24°C）</summary>
        public string WeatherText = "晴 25°C";
    }

    /// <summary>
    /// 悬浮组件通用模块渲染器。
    /// </summary>
    public static class OverlayRenderer
    {
        /// <summary>
        /// 模块 key 转换为本地化显示名。
        /// </summary>
        public static string ModuleDisplayName(string module)
        {
            return module switch
            {
                "News" => I18n.T("overlay.module.news"),
                "Schedule" => I18n.T("overlay.module.schedule"),
                "ScheduleHint" => I18n.T("overlay.module.scheduleHint"),
                "App" => I18n.T("overlay.module.app"),
                "OfficeSchedule" => I18n.T("overlay.module.officeSchedule"),
                "OfficeTodo" => I18n.T("overlay.module.officeTodo"),
                "Weather" => "天气",
                _ => module,
            };
        }

        /// <summary>
        /// 获取模块的主题色（用于 UI 彩色点缀）。
        /// </summary>
        public static Color GetModuleColor(string module, OverlayContext ctx)
        {
            return module switch
            {
                "News" => Color.FromArgb(0xFF, 0x45, 0x00),       // 橙色新闻
                "Schedule" => Color.FromArgb(0x60, 0xCD, 0xFF),   // 亮蓝
                "ScheduleHint" => Color.FromArgb(0x4C, 0xAF, 0x50), // 绿色
                "App" => Color.FromArgb(0x9E, 0x9E, 0x9E),        // 灰色
                "OfficeSchedule" => Color.FromArgb(0xFF, 0xB9, 0x00), // 黄色日程
                "OfficeTodo" => Color.FromArgb(0xE9, 0x1E, 0x63), // 粉色待办
                "Weather" => Color.FromArgb(0x2E, 0x86, 0xE8),    // 蓝色天气
                _ => Color.White,
            };
        }

        public static string RenderText(string module, OverlayContext ctx)
        {
            return module switch
            {
                "News" => RenderNews(ctx),
                "Schedule" => RenderSchedule(ctx),
                "ScheduleHint" => RenderScheduleHint(ctx),
                "App" => RenderApp(ctx),
                "OfficeSchedule" => RenderOfficeSchedule(ctx),
                "OfficeTodo" => RenderOfficeTodo(ctx),
                "Weather" => RenderWeather(ctx),
                _ => "",
            };
        }

        private static string RenderNews(OverlayContext ctx)
        {
            if (string.IsNullOrEmpty(ctx.CurrentNews)) return I18n.T("common.loading");
            return ctx.CurrentNews;
        }

        private static string RenderWeather(OverlayContext ctx)
        {
            return ctx.WeatherText;
        }

        private static string RenderSchedule(OverlayContext ctx)
        {
            var today = GetTodayCourses(ctx);
            if (today.Count == 0) return I18n.T("island.noCourse");
            var chars = today.Select(c => GetFirstChar(c.Name));
            return string.Join(" ", chars);
        }

        private static string RenderScheduleHint(OverlayContext ctx)
        {
            var now = ctx.Now;
            foreach (var st in ctx.Sections.Where(s => s.Type == "normal").OrderBy(s => s.Section))
            {
                if (string.IsNullOrEmpty(st.StartTime) || string.IsNullOrEmpty(st.EndTime)) continue;
                if (!TryParseTime(now, st.StartTime, out var startDt)) continue;
                if (!TryParseTime(now, st.EndTime, out var endDt)) continue;

                if (now >= startDt && now < endDt)
                {
                    var course = FindCourseAt(ctx, st.Section);
                    string name = course?.Name ?? string.Format(I18n.T("schedule.section"), st.Section);
                    var remain = endDt - now;
                    if (remain.TotalMinutes <= 5) return string.Format(I18n.T("island.soonEnd"), name, FormatSpan(remain));
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

        private static string RenderApp(OverlayContext ctx)
        {
            if (string.IsNullOrEmpty(ctx.CurrentApp)) return "Desktop";
            return ctx.CurrentApp;
        }

        private static string RenderOfficeSchedule(OverlayContext ctx)
        {
            var today = ScheduleRepository.GetForDay(ctx.Schedules, ctx.Now);
            if (today.Count == 0) return I18n.T("office.noSchedule");
            var next = today.OrderBy(s => s.StartTime).FirstOrDefault();
            return next == null ? "" : $"{next.StartTime} {next.Title}";
        }

        private static string RenderOfficeTodo(OverlayContext ctx)
        {
            int undone = ctx.Schedules.Count(s => s.IsTodo && !s.Done);
            if (undone == 0) return I18n.T("office.noTodo");
            return $"{I18n.T("nav.todo")} {undone}";
        }

        private static List<Course> GetTodayCourses(OverlayContext ctx)
        {
            int todayWeekday = (int)ctx.Now.DayOfWeek;
            if (todayWeekday == 0) todayWeekday = 7;
            return ctx.Courses.Where(c => c.WeekDay == todayWeekday && c.Weeks != null && c.Weeks.Contains(ctx.CurrentWeek)).OrderBy(c => c.TimeStart).ToList();
        }

        private static Course? FindCourseAt(OverlayContext ctx, int section)
        {
            return GetTodayCourses(ctx).FirstOrDefault(c => section >= c.TimeStart && section <= c.TimeEnd);
        }

        private static string GetFirstChar(string name) => string.IsNullOrEmpty(name) ? "" : name.Substring(0, 1).ToUpperInvariant();

        private static bool TryParseTime(DateTime now, string timeStr, out DateTime result)
        {
            result = default;
            if (string.IsNullOrEmpty(timeStr)) return false;
            if (!TimeSpan.TryParse(timeStr, out var ts)) return false;
            result = now.Date + ts;
            return true;
        }

        private static string FormatSpan(TimeSpan span)
        {
            if (span.TotalHours >= 1) return $"{(int)span.TotalHours}h{span.Minutes}m";
            return $"{span.Minutes}m";
        }

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
            }
            catch { }
            return fallback;
        }

        public static string ToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";
    }
}