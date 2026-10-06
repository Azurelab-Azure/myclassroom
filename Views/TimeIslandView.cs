using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using CourseApp.Localization;
using CourseApp.Models;
using CourseApp.Theme;

namespace CourseApp.Views
{
    /// <summary>
    /// 灵动岛——课程状态：
    ///   - 正在上课：课程名 + "剩 12m"（下课倒计时）
    ///   - 即将上课：课程名 + "08:00（3m）"（上课倒计时）
    ///   - 即将下课：剩 ≤ 5 分钟时高亮
    ///   - 暂无课程
    /// </summary>
    public class TimeIslandView : Panel
    {
        private readonly System.Windows.Forms.Timer _timer;

        private List<SectionTime> _sections = new();
        private List<Course> _courses = new();
        private int _currentWeek = 1;

        public TimeIslandView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            BackColor = Color.FromArgb(0x1A, 0x1A, 0x1A);

            _timer = new System.Windows.Forms.Timer { Interval = 1000 };
            _timer.Tick += (s, e) => Invalidate();
            _timer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _timer?.Dispose();
            base.Dispose(disposing);
        }

        public void SetData(List<SectionTime> sections, List<Course> courses, int currentWeek)
        {
            _sections = sections ?? new List<SectionTime>();
            _courses = courses ?? new List<Course>();
            _currentWeek = currentWeek;
            Invalidate();
        }

        // =====================================================
        // 绘制
        // =====================================================
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var bg = new SolidBrush(BackColor))
                g.FillRectangle(bg, ClientRectangle);

            var now = DateTime.Now;
            var status = ComputeStatus(now);

            int left = 14;
            int midY = Height / 2;

            // 状态点
            Color dotColor = status.Kind switch
            {
                StatusKind.InClassSoonEnd => Color.FromArgb(0xFF, 0x6B, 0x00),  // 橙——即将下课
                StatusKind.InClass => Color.FromArgb(0x4C, 0xAF, 0x50),          // 绿——正在上
                StatusKind.BeforeClass => Color.FromArgb(0xFF, 0xB9, 0x00),      // 黄——即将上
                _ => Color.FromArgb(0x9E, 0x9E, 0x9E),                            // 灰——无
            };

            var dotRect = new Rectangle(left, midY - 4, 8, 8);
            using (var dotBrush = new SolidBrush(dotColor))
                g.FillEllipse(dotBrush, dotRect);

            left += 16;

            // 右侧时间
            string timeText = now.ToString("HH:mm");
            var timeSize = TextRenderer.MeasureText(g, timeText, AppTheme.SmallFont);
            int right = Width - 14;
            var timeRect = new Rectangle(right - timeSize.Width, 0, timeSize.Width, Height);
            TextRenderer.DrawText(g, timeText, AppTheme.SmallFont, timeRect,
                Color.FromArgb(0xB0, 0xB0, 0xB0),
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            int textRight = timeRect.Left - 10;

            // 中间：主文本 + 副文本
            Color textColor = Color.FromArgb(0xF0, 0xF0, 0xF0);
            Color subColor = status.Kind == StatusKind.InClassSoonEnd
                ? Color.FromArgb(0xFF, 0x8C, 0x42)
                : Color.FromArgb(0xB0, 0xB0, 0xB0);

            string mainText = status.MainText;
            string subText = status.SubText;

            if (!string.IsNullOrEmpty(subText))
            {
                var subSize = TextRenderer.MeasureText(g, subText, AppTheme.SmallFont);
                var subRect = new Rectangle(textRight - subSize.Width, 0, subSize.Width, Height);
                TextRenderer.DrawText(g, subText, AppTheme.SmallFont, subRect, subColor,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix);

                int mainRight = subRect.Left - 8;
                var mainRect = new Rectangle(left, 0, mainRight - left, Height);
                TextRenderer.DrawText(g, mainText, AppTheme.BodyFont, mainRect, textColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
            else
            {
                var mainRect = new Rectangle(left, 0, textRight - left, Height);
                TextRenderer.DrawText(g, mainText, AppTheme.BodyFont, mainRect, textColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
        }

        // =====================================================
        // 状态
        // =====================================================
        private enum StatusKind
        {
            None,           // 无课
            BeforeClass,    // 即将上课（> 5 分钟）
            InClass,        // 正在上课
            InClassSoonEnd, // 即将下课（≤ 5 分钟）
        }

        private class Status
        {
            public StatusKind Kind;
            public string MainText = "";
            public string SubText = "";
        }

        private Status ComputeStatus(DateTime now)
        {
            var result = new Status();

            int todayWeekday = (int)now.DayOfWeek;
            if (todayWeekday == 0) todayWeekday = 7;

            var todayCourses = _courses
                .Where(c => c.WeekDay == todayWeekday
                            && c.Weeks != null && c.Weeks.Contains(_currentWeek))
                .OrderBy(c => c.TimeStart)
                .ToList();

            if (todayCourses.Count == 0)
            {
                result.Kind = StatusKind.None;
                result.MainText = I18n.T("island.noCourse");
                return result;
            }

            // 找"正在上"或"下一节"
            SectionTime? inSection = null;
            Course? inCourse = null;
            DateTime inEndTime = default;

            SectionTime? nextSection = null;
            Course? nextCourse = null;
            DateTime nextStartTime = default;

            foreach (var st in _sections.Where(s => s.Type == "normal").OrderBy(s => s.Section))
            {
                if (string.IsNullOrEmpty(st.StartTime) || string.IsNullOrEmpty(st.EndTime))
                    continue;

                if (!TryParseTime(now, st.StartTime, out var startDt)) continue;
                if (!TryParseTime(now, st.EndTime, out var endDt)) continue;

                // 正在上
                if (now >= startDt && now < endDt)
                {
                    inSection = st;
                    inEndTime = endDt;
                    inCourse = todayCourses.FirstOrDefault(c =>
                        st.Section >= c.TimeStart && st.Section <= c.TimeEnd);
                    break;
                }

                // 下一节
                if (now < startDt)
                {
                    nextSection = st;
                    nextStartTime = startDt;
                    nextCourse = todayCourses.FirstOrDefault(c =>
                        st.Section >= c.TimeStart && st.Section <= c.TimeEnd);
                    break;
                }
            }

            // ---------- 正在上课 ----------
            if (inSection != null)
            {
                var remain = inEndTime - now;
                string name = inCourse?.Name
                    ?? string.Format(I18n.T("schedule.section"), inSection.Section);

                // 即将下课（≤ 5 分钟）
                if (remain.TotalMinutes <= 5)
                {
                    result.Kind = StatusKind.InClassSoonEnd;
                    result.MainText = name;
                    result.SubText = string.Format(I18n.T("island.soonEnd"), FormatSpan(remain));
                }
                else
                {
                    result.Kind = StatusKind.InClass;
                    result.MainText = name;
                    result.SubText = string.Format(I18n.T("island.remain"), FormatSpan(remain));
                }
                return result;
            }

            // ---------- 即将上课 ----------
            if (nextSection != null)
            {
                var remain = nextStartTime - now;
                string name = nextCourse?.Name
                    ?? string.Format(I18n.T("schedule.section"), nextSection.Section);

                result.Kind = StatusKind.BeforeClass;
                result.MainText = name;
                result.SubText = string.Format(I18n.T("island.nextAt"),
                    nextSection.StartTime, FormatSpan(remain));
                return result;
            }

            // ---------- 今天已上完 ----------
            result.Kind = StatusKind.None;
            result.MainText = I18n.T("island.noCourse");
            return result;
        }

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
            if (span.TotalHours >= 1)
                return $"{(int)span.TotalHours}h{span.Minutes}m";
            return $"{span.Minutes}m";
        }
    }
}