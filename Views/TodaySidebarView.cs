using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CourseApp.Localization;
using CourseApp.Models;
using CourseApp.Theme;

namespace CourseApp.Views
{
    /// <summary>
    /// "今日"视图（细长条 24px）：
    ///   - 显示今日第一个课程的字（大字）
    ///   - 右上角折叠按钮
    /// </summary>
    public class TodaySidebarView : Panel
    {
        private List<Course> _courses = new();
        private List<SectionTime> _sections = new();
        private DutyRoster _duty = new();
        private int _currentWeek = 1;

        public event Action? ToggleCollapseRequested;

        public TodaySidebarView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);

            Dock = DockStyle.Fill;
            BackColor = AppTheme.Colors.CardBg;
        }

        public void SetData(List<Course> courses, List<SectionTime> sections,
            DutyRoster duty, int currentWeek)
        {
            _courses = courses ?? new List<Course>();
            _sections = sections ?? new List<SectionTime>();
            _duty = duty ?? new DutyRoster();
            _currentWeek = currentWeek;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var colors = AppTheme.Colors;

            using (var bg = new SolidBrush(colors.CardBg))
                g.FillRectangle(bg, ClientRectangle);

            // 顶部——"今"字
            using (var font = new Font(AppTheme.BodyFont.FontFamily, 9f, FontStyle.Bold))
            {
                var rect = new Rectangle(0, 6, Width, 16);
                TextRenderer.DrawText(g, "今", font, rect,
                    colors.Accent,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix);
            }

            // 中部——第一个课程的大字
            var now = DateTime.Now;
            int todayWeekday = (int)now.DayOfWeek;
            if (todayWeekday == 0) todayWeekday = 7;

            Course? currentOrNext = null;

            // 找"正在上"或"下一节"
            foreach (var c in _courses)
            {
                if (c.WeekDay != todayWeekday) continue;
                if (c.Weeks == null || !c.Weeks.Contains(_currentWeek)) continue;

                var end = GetSectionTime(c.TimeEnd);
                if (end != null && now < end.Value)
                {
                    currentOrNext = c;
                    break;
                }
            }

            if (currentOrNext != null)
            {
                string first = GetFirstChar(currentOrNext.Name);
                using var bigFont = new Font(AppTheme.BodyFont.FontFamily, 14f, FontStyle.Bold);

                var bigRect = new Rectangle(0, 40, Width, 28);
                TextRenderer.DrawText(g, first, bigFont, bigRect,
                    colors.TextPrimary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix);
            }

            // 底部——折叠箭头
            using var pen = new Pen(colors.TextSecondary, 1.5f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
            };
            int cx = Width / 2;
            int cy = Height - 20;
            g.DrawLines(pen, new[]
            {
                new Point(cx - 3, cy - 4),
                new Point(cx + 2, cy),
                new Point(cx - 3, cy + 4),
            });
        }

        private static string GetFirstChar(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            return name.Substring(0, 1).ToUpperInvariant();
        }

        private DateTime? GetSectionTime(int section)
        {
            foreach (var st in _sections)
            {
                if (st.Type == "normal" && st.Section == section)
                {
                    if (TimeSpan.TryParse(st.StartTime, out var t))
                        return DateTime.Today + t;
                }
            }
            return null;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // 点底部 → 折叠
                if (e.Y >= Height - 30)
                {
                    ToggleCollapseRequested?.Invoke();
                    return;
                }
            }
            base.OnMouseDown(e);
        }
    }
}