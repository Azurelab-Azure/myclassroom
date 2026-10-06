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
    /// 首页仪表盘——苹果官网风格。
    /// 大留白、极简、居中、无边框。
    /// </summary>
    public class PageDashboard : Panel
    {
        private readonly Form1 _owner;
        private readonly System.Windows.Forms.Timer _timer;

        private List<Course> _courses = new();
        private List<SectionTime> _sections = new();
        private DutyRoster _duty = new();
        private int _currentWeek = 1;

        // 快捷入口
        private class QuickButton
        {
            public string Icon;
            public string Text;
            public int TargetIndex;
            public Rectangle Rect;
            public bool Hover;
            public float HoverT;   // 0~1，动画
        }

        private readonly List<QuickButton> _quickButtons = new();
        private readonly System.Windows.Forms.Timer _animTimer;

        public PageDashboard(Form1 owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Colors.WindowBg;

            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            // 每秒刷新
            _timer = new System.Windows.Forms.Timer { Interval = 1000 };
            _timer.Tick += (s, e) => Invalidate();
            _timer.Start();

            // 动画帧
            _animTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _animTimer.Tick += (s, e) => AnimTick();
            _animTimer.Start();

            // 快捷按钮
            _quickButtons.Add(new QuickButton { Icon = Icons.Calendar, Text = "nav.home",     TargetIndex = 1 });
            _quickButtons.Add(new QuickButton { Icon = Icons.Person,   Text = "nav.teachers", TargetIndex = 2 });
            _quickButtons.Add(new QuickButton { Icon = Icons.Person,   Text = "nav.students", TargetIndex = 3 });
            _quickButtons.Add(new QuickButton { Icon = Icons.Calendar, Text = "nav.scores",   TargetIndex = 4 });
            _quickButtons.Add(new QuickButton { Icon = Icons.Settings, Text = "nav.settings", TargetIndex = 5 });

            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseLeave += (s, e) =>
            {
                foreach (var b in _quickButtons) b.Hover = false;
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer?.Dispose();
                _animTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void AnimTick()
        {
            bool anyChanged = false;
            foreach (var b in _quickButtons)
            {
                float target = b.Hover ? 1f : 0f;
                if (Math.Abs(b.HoverT - target) > 0.01f)
                {
                    b.HoverT += (target - b.HoverT) * 0.25f;
                    anyChanged = true;
                }
                else
                {
                    b.HoverT = target;
                }
            }
            if (anyChanged) Invalidate();
        }

        // =====================================================
        // 数据
        // =====================================================
        public void SetData(List<Course> courses, List<SectionTime> sections,
            DutyRoster duty, int currentWeek)
        {
            _courses = courses ?? new List<Course>();
            _sections = sections ?? new List<SectionTime>();
            _duty = duty ?? new DutyRoster();
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
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            var colors = AppTheme.Colors;

            using (var bg = new SolidBrush(colors.WindowBg))
                g.FillRectangle(bg, ClientRectangle);

            // 页边距（大留白）
            int padX = 64;
            int contentW = Width - padX * 2;

            int y = 56;

            // ---------- 顶部：日期 + 问候（居左，大留白）----------
            y = DrawHero(g, padX, y, contentW);

            // ---------- 核心指标（居中卡片）----------
            y += 40;
            y = DrawStats(g, padX, y, contentW);

            // ---------- 今日课表（无边框，列表式）----------
            y += 48;
            y = DrawTodaySchedule(g, padX, y, contentW);

            // ---------- 值日生 ----------
            y += 48;
            y = DrawDuty(g, padX, y, contentW);

            // ---------- 底部快捷入口（居中）----------
            DrawQuickButtons(g, Height - 120);
        }

        // =====================================================
        // Hero：问候 + 日期
        // =====================================================
        private int DrawHero(Graphics g, int x, int y, int w)
        {
            var colors = AppTheme.Colors;
            var now = DateTime.Now;

            // 问候语（超大字）
            string greeting = now.Hour switch
            {
                < 6 => "夜深了",
                < 12 => "早上好",
                < 14 => "中午好",
                < 18 => "下午好",
                _ => "晚上好",
            };

            using var heroFont = new Font(AppTheme.BodyFont.FontFamily, 44f, FontStyle.Bold);
            var heroRect = new Rectangle(x, y, w, 64);
            TextRenderer.DrawText(g, greeting, heroFont, heroRect, colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            y += 68;

            // 日期（小字，灰色）
            using var dateFont = new Font(AppTheme.BodyFont.FontFamily, 15f);
            string dateStr = now.ToString("yyyy 年 M 月 d 日  dddd");
            var dateRect = new Rectangle(x, y, w, 26);
            TextRenderer.DrawText(g, dateStr, dateFont, dateRect, colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            return y + 34;
        }

        // =====================================================
        // 核心指标：三个大数字卡片
        // =====================================================
        private int DrawStats(Graphics g, int x, int y, int w)
        {
            var colors = AppTheme.Colors;

            int gap = 20;
            int cardW = (w - gap * 2) / 3;
            int cardH = 140;

            var now = DateTime.Now;

            // 今日课程
            int todayWeekday = (int)now.DayOfWeek;
            if (todayWeekday == 0) todayWeekday = 7;
            var todayCourses = _courses
                .Where(c => c.WeekDay == todayWeekday
                            && c.Weeks != null && c.Weeks.Contains(_currentWeek))
                .ToList();

            DrawStatCard(g, x, y, cardW, cardH, 
                now.ToString("HH:mm"), "现在时间", colors.TextPrimary, false);
            DrawStatCard(g, x + cardW + gap, y, cardW, cardH,
                todayCourses.Count.ToString(), I18n.T("dashboard.todayCourses"), colors.Accent, false);
            DrawStatCard(g, x + (cardW + gap) * 2, y, cardW, cardH,
                string.Format(I18n.T("toolbar.week"), _currentWeek), "当前周次", colors.TextPrimary, true);

            return y + cardH;
        }

        private void DrawStatCard(Graphics g, int x, int y, int w, int h,
            string value, string label, Color accent, bool smallValue)
        {
            var colors = AppTheme.Colors;
            var rect = new Rectangle(x, y, w, h);

            // 卡片背景（无边框）
            using (var path = GraphicsExtensions.GetRoundPath(rect, 16))
            using (var brush = new SolidBrush(colors.CardBg))
                g.FillPath(brush, path);

            // 值
            float fontSize = smallValue ? 26f : 40f;
            using var valueFont = new Font(AppTheme.BodyFont.FontFamily, fontSize, FontStyle.Bold);
            var valueRect = new Rectangle(rect.X + 28, rect.Y + 32, rect.Width - 56, 52);
            TextRenderer.DrawText(g, value, valueFont, valueRect, accent,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            // 标签
            using var labelFont = new Font(AppTheme.BodyFont.FontFamily, 12f);
            var labelRect = new Rectangle(rect.X + 28, rect.Y + 90, rect.Width - 56, 24);
            TextRenderer.DrawText(g, label, labelFont, labelRect, colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        // =====================================================
        // 今日课表
        // =====================================================
        private int DrawTodaySchedule(Graphics g, int x, int y, int w)
        {
            var colors = AppTheme.Colors;

            // 标题
            using var titleFont = new Font(AppTheme.BodyFont.FontFamily, 18f, FontStyle.Bold);
            var titleRect = new Rectangle(x, y, w, 30);
            TextRenderer.DrawText(g, I18n.T("dashboard.todaySchedule"), titleFont, titleRect,
                colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            y += 42;

            var now = DateTime.Now;
            int todayWeekday = (int)now.DayOfWeek;
            if (todayWeekday == 0) todayWeekday = 7;

            var todayCourses = _courses
                .Where(c => c.WeekDay == todayWeekday
                            && c.Weeks != null && c.Weeks.Contains(_currentWeek))
                .OrderBy(c => c.TimeStart)
                .ToList();

            if (todayCourses.Count == 0)
            {
                using var emptyFont = new Font(AppTheme.BodyFont.FontFamily, 14f);
                var emptyRect = new Rectangle(x, y, w, 80);
                TextRenderer.DrawText(g, I18n.T("today.noCourse"), emptyFont, emptyRect,
                    colors.TextSecondary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix);
                return y + 90;
            }

            // 列表（每行一个大卡片）
            int cardH = 68;
            int gap = 12;

            // 找当前 / 下一节
            Course? currentCourse = null;
            Course? nextCourse = null;
            foreach (var c in todayCourses)
            {
                var start = GetSectionTime(c.TimeStart);
                var end = GetSectionTime(c.TimeEnd);
                if (start != null && end != null && now >= start.Value && now < end.Value)
                    currentCourse = c;
                else if (start != null && start.Value > now && nextCourse == null)
                    nextCourse = c;
            }

            foreach (var c in todayCourses)
            {
                var rect = new Rectangle(x, y, w, cardH);
                bool isCurrent = (c == currentCourse);
                bool isNext = (c == nextCourse);
                DrawScheduleRow(g, c, rect, isCurrent, isNext);
                y += cardH + gap;
            }

            return y;
        }

        private void DrawScheduleRow(Graphics g, Course c, Rectangle rect, bool isCurrent, bool isNext)
        {
            var colors = AppTheme.Colors;

            // 背景
            Color bg = colors.CardBg;
            if (isCurrent) bg = Color.FromArgb(30, 0x4C, 0xAF, 0x50);
            else if (isNext) bg = Color.FromArgb(30, 0xFF, 0xB9, 0x00);

            using (var path = GraphicsExtensions.GetRoundPath(rect, 14))
            using (var brush = new SolidBrush(bg))
                g.FillPath(brush, path);

            // 左侧色条（圆角）
            int hue = Math.Abs((c.Name ?? "").GetHashCode()) % 360;
            var barColor = ColorFromHsl(hue, 0.65, 0.55);

            var barRect = new Rectangle(rect.X + 20, rect.Y + 16, 4, rect.Height - 32);
            using (var brush = new SolidBrush(barColor))
            using (var path = GraphicsExtensions.GetRoundPath(barRect, 2))
                g.FillPath(brush, path);

            // 时间
            using var timeFont = new Font(AppTheme.BodyFont.FontFamily, 12f);
            string timeStr = GetSectionTimeStr(c);
            var timeRect = new Rectangle(rect.X + 40, rect.Y + 12, 160, 20);
            TextRenderer.DrawText(g, timeStr, timeFont, timeRect, colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            // 课程名
            using var nameFont = new Font(AppTheme.BodyFont.FontFamily, 16f, FontStyle.Bold);
            var nameRect = new Rectangle(rect.X + 40, rect.Y + 34, rect.Width - 320, 28);
            TextRenderer.DrawText(g, c.Name ?? "", nameFont, nameRect, colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            // 教师 + 教室（右侧）
            string sub = "";
            if (!string.IsNullOrEmpty(c.Teacher)) sub += c.Teacher;
            if (!string.IsNullOrEmpty(c.Classroom))
                sub += (sub.Length > 0 ? "  ·  " : "") + c.Classroom;

            if (!string.IsNullOrEmpty(sub))
            {
                using var subFont = new Font(AppTheme.BodyFont.FontFamily, 13f);
                var subRect = new Rectangle(rect.Right - 280, rect.Y + 24, 260, 22);
                TextRenderer.DrawText(g, sub, subFont, subRect, colors.TextSecondary,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }

            // 状态标签
            if (isCurrent || isNext)
            {
                string label = isCurrent ? I18n.T("today.inClass") : I18n.T("today.next");
                Color badgeColor = isCurrent
                    ? Color.FromArgb(0x4C, 0xAF, 0x50)
                    : Color.FromArgb(0xFF, 0xB9, 0x00);

                using var badgeFont = new Font(AppTheme.BodyFont.FontFamily, 11f, FontStyle.Bold);
                var size = TextRenderer.MeasureText(g, label, badgeFont);
                var badgeRect = new Rectangle(rect.Right - 84, rect.Y + 22, 64, 24);

                using var brush = new SolidBrush(badgeColor);
                using var path = GraphicsExtensions.GetRoundPath(badgeRect, 12);
                g.FillPath(brush, path);

                TextRenderer.DrawText(g, label, badgeFont, badgeRect, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix);
            }
        }

        // =====================================================
        // 值日生
        // =====================================================
        private int DrawDuty(Graphics g, int x, int y, int w)
        {
            var colors = AppTheme.Colors;

            using var titleFont = new Font(AppTheme.BodyFont.FontFamily, 18f, FontStyle.Bold);
            var titleRect = new Rectangle(x, y, w, 30);
            TextRenderer.DrawText(g, I18n.T("dashboard.duty"), titleFont, titleRect, colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            y += 42;

            int todayWeekday = (int)DateTime.Now.DayOfWeek;
            if (todayWeekday == 0) todayWeekday = 7;

            var dutyList = _duty.GetForDay(todayWeekday);

            if (dutyList.Count == 0)
            {
                using var font = new Font(AppTheme.BodyFont.FontFamily, 14f);
                var rect = new Rectangle(x, y, w, 40);
                TextRenderer.DrawText(g, I18n.T("today.noDuty"), font, rect, colors.TextSecondary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                return y + 50;
            }

            // 每个学生一个胶囊标签
            using var itemFont = new Font(AppTheme.BodyFont.FontFamily, 14f);
            int cx = x;
            int cy = y;
            int chipH = 40;
            int chipGap = 10;

            foreach (var name in dutyList)
            {
                var size = TextRenderer.MeasureText(g, name, itemFont);
                int chipW = size.Width + 40;

                if (cx + chipW > x + w)
                {
                    cx = x;
                    cy += chipH + chipGap;
                }

                var chipRect = new Rectangle(cx, cy, chipW, chipH);

                using (var brush = new SolidBrush(colors.CardBg))
                using (var path = GraphicsExtensions.GetRoundPath(chipRect, 20))
                    g.FillPath(brush, path);

                TextRenderer.DrawText(g, name, itemFont, chipRect, colors.TextPrimary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix);

                cx += chipW + chipGap;
            }

            return cy + chipH + 20;
        }

        // =====================================================
        // 快捷入口
        // =====================================================
        private void DrawQuickButtons(Graphics g, int y)
        {
            var colors = AppTheme.Colors;

            int btnW = 96;
            int btnH = 84;
            int gap = 20;

            int totalW = _quickButtons.Count * btnW + (_quickButtons.Count - 1) * gap;
            int startX = (Width - totalW) / 2;

            int cx = startX;
            foreach (var b in _quickButtons)
            {
                var rect = new Rectangle(cx, y, btnW, btnH);
                b.Rect = rect;

                // 背景色（无边框，hover 时变浅）
                int alpha = (int)(20 * b.HoverT);
                Color bg = Color.FromArgb(alpha, colors.HoverBg);
                if (b.HoverT > 0.01f)
                {
                    using (var path = GraphicsExtensions.GetRoundPath(rect, 14))
                    using (var brush = new SolidBrush(colors.HoverBg))
                    {
                        var old = g.Clip;
                        g.SetClip(path);
                        using var semi = new SolidBrush(Color.FromArgb((int)(80 * b.HoverT), colors.HoverBg));
                        g.FillRectangle(semi, rect);
                        g.Clip = old;
                    }
                }

                // 图标
                int iconSize = 28;
                var iconRect = new Rectangle(
                    rect.X + (rect.Width - iconSize) / 2,
                    rect.Y + 14,
                    iconSize, iconSize);
                IconRenderer.Draw(g, b.Icon, iconRect, colors.TextPrimary, iconSize);

                // 文字
                using var font = new Font(AppTheme.BodyFont.FontFamily, 12f);
                var textRect = new Rectangle(rect.X, rect.Y + 48, rect.Width, 24);
                TextRenderer.DrawText(g, I18n.T(b.Text), font, textRect, colors.TextPrimary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix);

                cx += btnW + gap;
            }
        }

        // =====================================================
        // 鼠标
        // =====================================================
        private void OnMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            foreach (var b in _quickButtons)
            {
                if (b.Rect.Contains(e.Location))
                {
                    _owner.SwitchPagePublic(b.TargetIndex);
                    return;
                }
            }
        }

        private void OnMouseMove(object? sender, MouseEventArgs e)
        {
            foreach (var b in _quickButtons)
                b.Hover = b.Rect.Contains(e.Location);
        }

        // =====================================================
        // 工具
        // =====================================================
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

        private string GetSectionTimeStr(Course c)
        {
            string start = "", end = "";
            foreach (var st in _sections)
            {
                if (st.Type == "normal" && st.Section == c.TimeStart) start = st.StartTime;
                if (st.Type == "normal" && st.Section == c.TimeEnd) end = st.EndTime;
            }
            if (string.IsNullOrEmpty(start) && string.IsNullOrEmpty(end))
                return $"第 {c.TimeStart}-{c.TimeEnd} 节";
            return $"{start} – {end}";
        }

        private static Color ColorFromHsl(double h, double s, double l)
        {
            double c = (1 - Math.Abs(2 * l - 1)) * s;
            double x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
            double m = l - c / 2;
            double r, g, b;

            if (h < 60) { r = c; g = x; b = 0; }
            else if (h < 120) { r = x; g = c; b = 0; }
            else if (h < 180) { r = 0; g = c; b = x; }
            else if (h < 240) { r = 0; g = x; b = c; }
            else if (h < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }

            return Color.FromArgb(
                Math.Max(0, Math.Min(255, (int)Math.Round((r + m) * 255))),
                Math.Max(0, Math.Min(255, (int)Math.Round((g + m) * 255))),
                Math.Max(0, Math.Min(255, (int)Math.Round((b + m) * 255))));
        }
    }
}