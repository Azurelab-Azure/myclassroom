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
    /// 首页仪表盘——WinUI 3 风格。
    /// 布局：问候 → 核心指标 → 班委公告 → 今日课表 → 值日生 → 积分 Top 3 → 快捷入口。
    /// </summary>
    public class PageDashboard : Panel
    {
        /// <summary>主窗口引用，用于页面切换</summary>
        private readonly Form1 _owner;

        /// <summary>每秒刷新计时器</summary>
        private readonly System.Windows.Forms.Timer _timer;

        /// <summary>动画帧计时器</summary>
        private readonly System.Windows.Forms.Timer _animTimer;

        /// <summary>课程数据</summary>
        private List<Course> _courses = new();

        /// <summary>节次数据</summary>
        private List<SectionTime> _sections = new();

        /// <summary>值日生数据</summary>
        private DutyRoster _duty = new();

        /// <summary>当前周次</summary>
        private int _currentWeek = 1;

        /// <summary>班委公告</summary>
        private List<ClassCommittee> _committees = new();

        /// <summary>课代表</summary>
        private List<CourseRepresentative> _reps = new();

        /// <summary>学生列表</summary>
        private List<Student> _students = new();

        /// <summary>积分记录</summary>
        private List<ScoreRecord> _records = new();

        /// <summary>快捷入口按钮</summary>
        private class QuickButton
        {
            /// <summary>图标</summary>
            public string Icon = "";

            /// <summary>文字 key</summary>
            public string Text = "";

            /// <summary>目标页索引</summary>
            public int TargetIndex;

            /// <summary>绘制矩形</summary>
            public Rectangle Rect;

            /// <summary>是否悬停</summary>
            public bool Hover;

            /// <summary>悬停动画进度</summary>
            public float HoverT;
        }

        /// <summary>快捷入口集合</summary>
        private readonly List<QuickButton> _quickButtons = new();

        /// <summary>
        /// 构造首页仪表盘。
        /// </summary>
        /// <param name="owner">主窗口</param>
        public PageDashboard(Form1 owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Colors.WindowBg;

            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            _timer = new System.Windows.Forms.Timer { Interval = 1000 };
            _timer.Tick += (s, e) => Invalidate();
            _timer.Start();

            _animTimer = new System.Windows.Forms.Timer { Interval = WinUI3Tokens.AnimInterval };
            _animTimer.Tick += (s, e) => AnimTick();
            _animTimer.Start();

            _quickButtons.Add(new QuickButton { Icon = Icons.App, Text = "nav.dashboard", TargetIndex = 0 });
            _quickButtons.Add(new QuickButton { Icon = Icons.Calendar, Text = "nav.home", TargetIndex = 1 });
            _quickButtons.Add(new QuickButton { Icon = Icons.Person, Text = "nav.teachers", TargetIndex = 2 });
            _quickButtons.Add(new QuickButton { Icon = Icons.Person, Text = "nav.students", TargetIndex = 3 });
            _quickButtons.Add(new QuickButton { Icon = Icons.Ok, Text = "nav.points", TargetIndex = 4 });
            _quickButtons.Add(new QuickButton { Icon = Icons.Book, Text = "nav.exam", TargetIndex = 5 });
            _quickButtons.Add(new QuickButton { Icon = Icons.Settings, Text = "nav.settings", TargetIndex = 6 });

            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseLeave += (s, e) => { foreach (var b in _quickButtons) b.Hover = false; };
        }

        /// <summary>
        /// 释放计时器资源。
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing) { _timer?.Dispose(); _animTimer?.Dispose(); }
            base.Dispose(disposing);
        }

        /// <summary>
        /// 动画帧：缓动快捷按钮的悬停进度。
        /// </summary>
        private void AnimTick()
        {
            bool changed = false;
            foreach (var b in _quickButtons)
            {
                float target = b.Hover ? 1f : 0f;
                if (Math.Abs(b.HoverT - target) > 0.01f)
                {
                    b.HoverT += (target - b.HoverT) * WinUI3Tokens.AnimEase;
                    changed = true;
                }
                else b.HoverT = target;
            }
            if (changed) Invalidate();
        }

        // =====================================================
        // 数据
        // =====================================================
        /// <summary>
        /// 设置首页数据。
        /// </summary>
        public void SetData(
            List<Course> courses,
            List<SectionTime> sections,
            DutyRoster duty,
            int currentWeek,
            List<ClassCommittee> committees,
            List<CourseRepresentative> reps,
            List<Student> students,
            List<ScoreRecord> records)
        {
            _courses = courses ?? new List<Course>();
            _sections = sections ?? new List<SectionTime>();
            _duty = duty ?? new DutyRoster();
            _currentWeek = currentWeek;
            _committees = committees ?? new List<ClassCommittee>();
            _reps = reps ?? new List<CourseRepresentative>();
            _students = students ?? new List<Student>();
            _records = records ?? new List<ScoreRecord>();
            Invalidate();
        }

        // =====================================================
        // 绘制
        // =====================================================
        /// <summary>
        /// 绘制首页。
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            var colors = AppTheme.Colors;

            using (var bg = new SolidBrush(colors.WindowBg))
                g.FillRectangle(bg, ClientRectangle);

            int padX = 40;
            int contentW = Width - padX * 2;
            int y = 32;

            y = DrawHero(g, padX, y, contentW);
            y += 20;
            y = DrawStats(g, padX, y, contentW);
            y += 24;
            y = DrawCommittees(g, padX, y, contentW);
            y += 20;
            y = DrawTodaySchedule(g, padX, y, contentW);
            y += 20;
            y = DrawDuty(g, padX, y, contentW);
            y += 20;
            y = DrawTopPoints(g, padX, y, contentW);

            DrawQuickButtons(g, Height - 96);
        }

        // =====================================================
        // Hero
        // =====================================================
        /// <summary>
        /// 绘制问候语和日期。
        /// </summary>
        private int DrawHero(Graphics g, int x, int y, int w)
        {
            var colors = AppTheme.Colors;
            var now = DateTime.Now;

            string greeting = now.Hour switch
            {
                < 6 => I18n.T("dashboard.greeting.night"),
                < 12 => I18n.T("dashboard.greeting.morning"),
                < 14 => I18n.T("dashboard.greeting.noon"),
                < 18 => I18n.T("dashboard.greeting.afternoon"),
                _ => I18n.T("dashboard.greeting.evening"),
            };

            using var heroFont = new Font(AppTheme.BodyFont.FontFamily, 26f, FontStyle.Bold);
            var heroRect = new Rectangle(x, y, w, 44);
            TextRenderer.DrawText(g, greeting, heroFont, heroRect, colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            y += 46;

            using var dateFont = new Font(AppTheme.BodyFont.FontFamily, 12f);
            string dateStr = now.ToString("yyyy 年 M 月 d 日  dddd");
            var dateRect = new Rectangle(x, y, w, 22);
            TextRenderer.DrawText(g, dateStr, dateFont, dateRect, colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            return y + 26;
        }

        // =====================================================
        // 核心指标
        // =====================================================
        /// <summary>
        /// 绘制四个核心指标卡片。
        /// </summary>
        private int DrawStats(Graphics g, int x, int y, int w)
        {
            var colors = AppTheme.Colors;

            int gap = 12;
            int cardW = (w - gap * 3) / 4;
            int cardH = 84;

            var now = DateTime.Now;
            int todayWeekday = (int)now.DayOfWeek;
            if (todayWeekday == 0) todayWeekday = 7;

            var todayCourses = _courses
                .Where(c => c.WeekDay == todayWeekday
                            && c.Weeks != null && c.Weeks.Contains(_currentWeek))
                .ToList();

            int todayDelta = _records
                .Where(r => DateTime.TryParse(r.Date, out var d) && d.Date == DateTime.Today)
                .Sum(r => r.Delta);

            int totalPoints = _students.Count == 0 ? 0 : _students.Sum(s => s.TotalPoints);

            var topStudent = _students
                .OrderByDescending(s => s.TotalPoints)
                .FirstOrDefault();

            DrawStatCard(g, x, y, cardW, cardH, now.ToString("HH:mm"),
                I18n.T("dashboard.now"), colors.Accent);
            DrawStatCard(g, x + cardW + gap, y, cardW, cardH,
                todayCourses.Count.ToString(), I18n.T("dashboard.todayCourses"), colors.TextPrimary);
            DrawStatCard(g, x + (cardW + gap) * 2, y, cardW, cardH,
                (todayDelta >= 0 ? "+" : "") + todayDelta, I18n.T("dashboard.todayPoints"),
                todayDelta >= 0 ? Color.FromArgb(0x4C, 0xAF, 0x50) : Color.FromArgb(0xE8, 0x1B, 0x1B));
            DrawStatCard(g, x + (cardW + gap) * 3, y, cardW, cardH,
                totalPoints.ToString(), I18n.T("dashboard.totalPoints"), colors.TextPrimary);

            return y + cardH;
        }

        /// <summary>
        /// 绘制单个指标卡片。
        /// </summary>
        private void DrawStatCard(Graphics g, int x, int y, int w, int h,
            string value, string label, Color accent)
        {
            var colors = AppTheme.Colors;
            var rect = new Rectangle(x, y, w, h);

            using (var path = GraphicsExtensions.GetRoundPath(rect, WinUI3Tokens.CardRadius))
            using (var brush = new SolidBrush(colors.CardBg))
                g.FillPath(brush, path);

            using (var path = GraphicsExtensions.GetRoundPath(rect, WinUI3Tokens.CardRadius))
            using (var pen = new Pen(colors.CardBorder, 1f))
                g.DrawPath(pen, path);

            using var valueFont = new Font(AppTheme.BodyFont.FontFamily, 22f, FontStyle.Bold);
            var valueRect = new Rectangle(rect.X + 16, rect.Y + 14, rect.Width - 32, 32);
            TextRenderer.DrawText(g, value, valueFont, valueRect, accent,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            using var labelFont = new Font(AppTheme.BodyFont.FontFamily, 11f);
            var labelRect = new Rectangle(rect.X + 16, rect.Y + 50, rect.Width - 32, 20);
            TextRenderer.DrawText(g, label, labelFont, labelRect, colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        // =====================================================
        // 班委公告
        // =====================================================
        /// <summary>
        /// 绘制班委公告区。
        /// </summary>
        private int DrawCommittees(Graphics g, int x, int y, int w)
        {
            var colors = AppTheme.Colors;

            using var titleFont = new Font(AppTheme.BodyFont.FontFamily, 15f, FontStyle.Bold);
            TextRenderer.DrawText(g, I18n.T("dashboard.committee"), titleFont,
                new Rectangle(x, y, w, 26), colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            y += 34;

            var list = _committees
                .OrderByDescending(c => c.Pinned)
                .ThenByDescending(c => c.Date)
                .Take(3)
                .ToList();

            if (list.Count == 0)
            {
                using var font = new Font(AppTheme.BodyFont.FontFamily, 12f);
                TextRenderer.DrawText(g, I18n.T("dashboard.noCommittee"), font,
                    new Rectangle(x, y, w, 32), colors.TextDisabled,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                return y + 36;
            }

            int cardH = 68;
            int gap = 12;
            int cardW = (w - gap * 2) / 3;

            for (int i = 0; i < list.Count; i++)
            {
                var c = list[i];
                var rect = new Rectangle(x + i * (cardW + gap), y, cardW, cardH);

                using (var path = GraphicsExtensions.GetRoundPath(rect, WinUI3Tokens.CardRadius))
                using (var brush = new SolidBrush(colors.CardBg))
                    g.FillPath(brush, path);

                using (var path = GraphicsExtensions.GetRoundPath(rect, WinUI3Tokens.CardRadius))
                using (var pen = new Pen(colors.CardBorder, 1f))
                    g.DrawPath(pen, path);

                if (c.Pinned)
                {
                    var barRect = new Rectangle(rect.X, rect.Y + 12, 3, rect.Height - 24);
                    using var barBrush = new SolidBrush(colors.Accent);
                    using var barPath = GraphicsExtensions.GetRoundPath(barRect, 2);
                    g.FillPath(barBrush, barPath);
                }

                using var titleF = new Font(AppTheme.BodyFont.FontFamily, 12f, FontStyle.Bold);
                TextRenderer.DrawText(g, c.Title ?? "", titleF,
                    new Rectangle(rect.X + 14, rect.Y + 10, rect.Width - 28, 20),
                    colors.TextPrimary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                using var contentF = new Font(AppTheme.BodyFont.FontFamily, 10f);
                TextRenderer.DrawText(g, c.Content ?? "", contentF,
                    new Rectangle(rect.X + 14, rect.Y + 32, rect.Width - 28, 26),
                    colors.TextSecondary,
                    TextFormatFlags.Left | TextFormatFlags.Top |
                    TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }

            return y + cardH + 8;
        }

        // =====================================================
        // 今日课表
        // =====================================================
        /// <summary>
        /// 绘制今日课表区。
        /// </summary>
        private int DrawTodaySchedule(Graphics g, int x, int y, int w)
        {
            var colors = AppTheme.Colors;

            using var titleFont = new Font(AppTheme.BodyFont.FontFamily, 15f, FontStyle.Bold);
            TextRenderer.DrawText(g, I18n.T("dashboard.todaySchedule"), titleFont,
                new Rectangle(x, y, w, 26), colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            y += 34;

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
                using var emptyFont = new Font(AppTheme.BodyFont.FontFamily, 12f);
                TextRenderer.DrawText(g, I18n.T("today.noCourse"), emptyFont,
                    new Rectangle(x, y, w, 32), colors.TextDisabled,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                return y + 36;
            }

            int cardH = 52;
            int gap = 8;

            foreach (var c in todayCourses.Take(3))
            {
                var rect = new Rectangle(x, y, w, cardH);
                DrawScheduleRow(g, c, rect);
                y += cardH + gap;
            }

            return y;
        }

        /// <summary>
        /// 绘制一行今日课程。
        /// </summary>
        private void DrawScheduleRow(Graphics g, Course c, Rectangle rect)
        {
            var colors = AppTheme.Colors;

            using (var path = GraphicsExtensions.GetRoundPath(rect, WinUI3Tokens.CardRadius))
            using (var brush = new SolidBrush(colors.CardBg))
                g.FillPath(brush, path);

            using (var path = GraphicsExtensions.GetRoundPath(rect, WinUI3Tokens.CardRadius))
            using (var pen = new Pen(colors.CardBorder, 1f))
                g.DrawPath(pen, path);

            int hue = Math.Abs((c.Name ?? "").GetHashCode()) % 360;
            var barColor = ColorFromHsl(hue, 0.65, 0.55);
            var barRect = new Rectangle(rect.X + 14, rect.Y + 12, 4, rect.Height - 24);
            using (var brush = new SolidBrush(barColor))
            using (var path = GraphicsExtensions.GetRoundPath(barRect, 2))
                g.FillPath(brush, path);

            using var timeFont = new Font(AppTheme.BodyFont.FontFamily, 11f);
            string timeStr = GetSectionTimeStr(c);
            TextRenderer.DrawText(g, timeStr, timeFont,
                new Rectangle(rect.X + 28, rect.Y + 6, 140, 18), colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            using var nameFont = new Font(AppTheme.BodyFont.FontFamily, 13f, FontStyle.Bold);
            TextRenderer.DrawText(g, c.Name ?? "", nameFont,
                new Rectangle(rect.X + 28, rect.Y + 26, rect.Width - 300, 20), colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            string sub = "";
            if (!string.IsNullOrEmpty(c.Teacher)) sub += c.Teacher;
            if (!string.IsNullOrEmpty(c.Classroom)) sub += (sub.Length > 0 ? "  ·  " : "") + c.Classroom;

            var rep = _reps.FirstOrDefault(r => r.CourseName == c.Name);
            if (rep != null) sub += (sub.Length > 0 ? "  ·  " : "") + I18n.T("courseRep.short") + " " + rep.StudentName;

            if (!string.IsNullOrEmpty(sub))
            {
                using var subFont = new Font(AppTheme.BodyFont.FontFamily, 11f);
                TextRenderer.DrawText(g, sub, subFont,
                    new Rectangle(rect.Right - 280, rect.Y + 16, 260, 20), colors.TextSecondary,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
        }

        // =====================================================
        // 值日生
        // =====================================================
        /// <summary>
        /// 绘制值日生区。
        /// </summary>
        private int DrawDuty(Graphics g, int x, int y, int w)
        {
            var colors = AppTheme.Colors;

            using var titleFont = new Font(AppTheme.BodyFont.FontFamily, 15f, FontStyle.Bold);
            TextRenderer.DrawText(g, I18n.T("dashboard.duty"), titleFont,
                new Rectangle(x, y, w, 26), colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            y += 34;

            int todayWeekday = (int)DateTime.Now.DayOfWeek;
            if (todayWeekday == 0) todayWeekday = 7;

            var dutyList = _duty.GetForDay(todayWeekday);

            if (dutyList.Count == 0)
            {
                using var font = new Font(AppTheme.BodyFont.FontFamily, 12f);
                TextRenderer.DrawText(g, I18n.T("today.noDuty"), font,
                    new Rectangle(x, y, w, 32), colors.TextDisabled,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                return y + 36;
            }

            using var itemFont = new Font(AppTheme.BodyFont.FontFamily, 12f);
            int cx = x;
            int cy = y;
            int chipH = 32;
            int chipGap = 8;

            foreach (var name in dutyList)
            {
                var size = TextRenderer.MeasureText(g, name, itemFont);
                int chipW = size.Width + 32;

                if (cx + chipW > x + w) { cx = x; cy += chipH + chipGap; }

                var chipRect = new Rectangle(cx, cy, chipW, chipH);
                using (var brush = new SolidBrush(colors.CardBg))
                using (var path = GraphicsExtensions.GetRoundPath(chipRect, chipH / 2))
                    g.FillPath(brush, path);

                using (var pen = new Pen(colors.CardBorder, 1f))
                using (var path = GraphicsExtensions.GetRoundPath(chipRect, chipH / 2))
                    g.DrawPath(pen, path);

                TextRenderer.DrawText(g, name, itemFont, chipRect, colors.TextPrimary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

                cx += chipW + chipGap;
            }

            return cy + chipH + 8;
        }

        // =====================================================
        // 积分 Top 3
        // =====================================================
        /// <summary>
        /// 绘制积分 Top 3。
        /// </summary>
        private int DrawTopPoints(Graphics g, int x, int y, int w)
        {
            var colors = AppTheme.Colors;

            using var titleFont = new Font(AppTheme.BodyFont.FontFamily, 15f, FontStyle.Bold);
            TextRenderer.DrawText(g, I18n.T("dashboard.topPoints"), titleFont,
                new Rectangle(x, y, w, 26), colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            y += 34;

            var top = _students
                .OrderByDescending(s => s.TotalPoints)
                .Take(3)
                .ToList();

            if (top.Count == 0)
            {
                using var font = new Font(AppTheme.BodyFont.FontFamily, 12f);
                TextRenderer.DrawText(g, I18n.T("student.empty"), font,
                    new Rectangle(x, y, w, 32), colors.TextDisabled,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                return y + 36;
            }

            int cardH = 60;
            int gap = 12;
            int cardW = (w - gap * 2) / 3;

            for (int i = 0; i < top.Count; i++)
            {
                var s = top[i];
                var rect = new Rectangle(x + i * (cardW + gap), y, cardW, cardH);

                using (var path = GraphicsExtensions.GetRoundPath(rect, WinUI3Tokens.CardRadius))
                using (var brush = new SolidBrush(colors.CardBg))
                    g.FillPath(brush, path);

                using (var path = GraphicsExtensions.GetRoundPath(rect, WinUI3Tokens.CardRadius))
                using (var pen = new Pen(colors.CardBorder, 1f))
                    g.DrawPath(pen, path);

                // 排名
                Color rankColor = i switch
                {
                    0 => Color.FromArgb(0xFF, 0xB9, 0x00),
                    1 => Color.FromArgb(0xB0, 0xB0, 0xB0),
                    2 => Color.FromArgb(0xCD, 0x7F, 0x32),
                    _ => colors.TextSecondary,
                };

                using var rankFont = new Font(AppTheme.BodyFont.FontFamily, 16f, FontStyle.Bold);
                TextRenderer.DrawText(g, (i + 1).ToString(), rankFont,
                    new Rectangle(rect.X + 14, rect.Y, 30, rect.Height), rankColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

                TextRenderer.DrawText(g, s.Name ?? "", AppTheme.BodyFont,
                    new Rectangle(rect.X + 48, rect.Y + 10, rect.Width - 130, 20), colors.TextPrimary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                string sub = (s.Id ?? "") + (string.IsNullOrEmpty(s.Badge) ? "" : "  ·  " + s.Badge);
                TextRenderer.DrawText(g, sub, AppTheme.SmallFont,
                    new Rectangle(rect.X + 48, rect.Y + 32, rect.Width - 130, 18), colors.TextSecondary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                using var pointFont = new Font(AppTheme.BodyFont.FontFamily, 16f, FontStyle.Bold);
                TextRenderer.DrawText(g, s.TotalPoints.ToString(), pointFont,
                    new Rectangle(rect.Right - 70, rect.Y, 60, rect.Height), colors.Accent,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }

            return y + cardH + 8;
        }

        // =====================================================
        // 快捷入口
        // =====================================================
        /// <summary>
        /// 绘制底部快捷入口。
        /// </summary>
        private void DrawQuickButtons(Graphics g, int y)
        {
            var colors = AppTheme.Colors;

            int btnW = 84;
            int btnH = 64;
            int gap = 10;

            int totalW = _quickButtons.Count * btnW + (_quickButtons.Count - 1) * gap;
            int startX = (Width - totalW) / 2;

            int cx = startX;
            foreach (var b in _quickButtons)
            {
                var rect = new Rectangle(cx, y, btnW, btnH);
                b.Rect = rect;

                if (b.HoverT > 0.01f)
                {
                    using (var path = GraphicsExtensions.GetRoundPath(rect, WinUI3Tokens.CardRadius))
                    {
                        var old = g.Clip;
                        g.SetClip(path);
                        using var semi = new SolidBrush(Color.FromArgb((int)(80 * b.HoverT), colors.HoverBg));
                        g.FillRectangle(semi, rect);
                        g.Clip = old;
                    }
                }

                int iconSize = 22;
                var iconRect = new Rectangle(rect.X + (rect.Width - iconSize) / 2, rect.Y + 8, iconSize, iconSize);
                IconRenderer.Draw(g, b.Icon, iconRect, colors.TextPrimary, iconSize);

                using var font = new Font(AppTheme.BodyFont.FontFamily, 10f);
                var textRect = new Rectangle(rect.X, rect.Y + 34, rect.Width, 20);
                TextRenderer.DrawText(g, I18n.T(b.Text), font, textRect, colors.TextPrimary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

                cx += btnW + gap;
            }
        }

        // =====================================================
        // 鼠标
        // =====================================================
        /// <summary>
        /// 鼠标点击快捷入口。
        /// </summary>
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

        /// <summary>
        /// 鼠标移动时更新悬停。
        /// </summary>
        private void OnMouseMove(object? sender, MouseEventArgs e)
        {
            foreach (var b in _quickButtons)
                b.Hover = b.Rect.Contains(e.Location);
        }

        // =====================================================
        // 工具
        // =====================================================
        /// <summary>
        /// 获取课程时间字符串。
        /// </summary>
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

        /// <summary>
        /// HSL 转 RGB。
        /// </summary>
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