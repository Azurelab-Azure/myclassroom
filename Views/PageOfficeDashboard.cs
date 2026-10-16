using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using CourseApp.Localization;
using CourseApp.Models;
using CourseApp.Theme;
using CourseApp.Services;

namespace CourseApp.Views
{
    /// <summary>
    /// 办公模式首页。
    /// 问候 + 核心指标 + 今日日程 + 最近待办 + 快捷入口。
    /// </summary>
    public class PageOfficeDashboard : Panel
    {
        /// <summary>主窗口引用</summary>
        private readonly Form2 _owner;

        /// <summary>每秒刷新计时器</summary>
        private readonly System.Windows.Forms.Timer _timer;

        /// <summary>日程数据</summary>
        private List<ScheduleItem> _schedules = new();

        /// <summary>
        /// 构造办公首页。
        /// </summary>
        /// <param name="owner">办公模式主窗口</param>
        public PageOfficeDashboard(Form2 owner)
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

            MouseDoubleClick += OnMouseDoubleClick;
        }

        /// <summary>
        /// 释放计时器。
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing) _timer?.Dispose();
            base.Dispose(disposing);
        }

        /// <summary>
        /// 设置数据。
        /// </summary>
        public void SetData(List<ScheduleItem> schedules)
        {
            _schedules = schedules ?? new List<ScheduleItem>();
            Invalidate();
        }

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
            y = DrawTodaySchedule(g, padX, y, contentW);
            y += 20;
            y = DrawRecentTodo(g, padX, y, contentW);
        }

        /// <summary>
        /// 问候 + 日期。
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
            TextRenderer.DrawText(g, greeting, heroFont,
                new Rectangle(x, y, w, 44), colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            y += 46;

            using var dateFont = new Font(AppTheme.BodyFont.FontFamily, 12f);
            string dateStr = now.ToString("yyyy 年 M 月 d 日  dddd");
            TextRenderer.DrawText(g, dateStr, dateFont,
                new Rectangle(x, y, w, 22), colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            return y + 26;
        }

        /// <summary>
        /// 核心指标卡片。
        /// </summary>
        private int DrawStats(Graphics g, int x, int y, int w)
        {
            var colors = AppTheme.Colors;

            int gap = 12;
            int cardW = (w - gap * 3) / 4;
            int cardH = 84;

            var now = DateTime.Now;
            var today = ScheduleRepository.GetForDay(_schedules, now);

            var weekStart = now.Date.AddDays(-(int)now.DayOfWeek);
            var weekEnd = weekStart.AddDays(6);
            var week = ScheduleRepository.GetInRange(_schedules, weekStart, weekEnd);

            var todos = _schedules.Where(s => s.IsTodo).ToList();
            double doneRate = todos.Count == 0 ? 1.0 : (double)todos.Count(s => s.Done) / todos.Count;

            DrawStatCard(g, x, y, cardW, cardH, now.ToString("HH:mm"),
                I18n.T("office.now"), colors.Accent);
            DrawStatCard(g, x + cardW + gap, y, cardW, cardH,
                today.Count.ToString(), I18n.T("office.todayCount"), colors.TextPrimary);
            DrawStatCard(g, x + (cardW + gap) * 2, y, cardW, cardH,
                week.Count.ToString(), I18n.T("office.weekCount"), colors.TextPrimary);
            DrawStatCard(g, x + (cardW + gap) * 3, y, cardW, cardH,
                (doneRate * 100).ToString("F0") + "%", I18n.T("office.todoRate"), colors.TextPrimary);

            return y + cardH;
        }

        /// <summary>
        /// 单个统计卡片。
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
            TextRenderer.DrawText(g, value, valueFont,
                new Rectangle(rect.X + 16, rect.Y + 14, rect.Width - 32, 32), accent,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            using var labelFont = new Font(AppTheme.BodyFont.FontFamily, 11f);
            TextRenderer.DrawText(g, label, labelFont,
                new Rectangle(rect.X + 16, rect.Y + 50, rect.Width - 32, 20), colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        /// <summary>
        /// 今日日程。
        /// </summary>
        private int DrawTodaySchedule(Graphics g, int x, int y, int w)
        {
            var colors = AppTheme.Colors;

            using var titleFont = new Font(AppTheme.BodyFont.FontFamily, 15f, FontStyle.Bold);
            TextRenderer.DrawText(g, I18n.T("office.todaySchedule"), titleFont,
                new Rectangle(x, y, w, 26), colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            y += 34;

            var today = ScheduleRepository.GetForDay(_schedules, DateTime.Now)
                .Where(s => !s.IsTodo)
                .OrderBy(s => s.StartTime)
                .ToList();

            if (today.Count == 0)
            {
                using var emptyFont = new Font(AppTheme.BodyFont.FontFamily, 12f);
                TextRenderer.DrawText(g, I18n.T("office.noSchedule"), emptyFont,
                    new Rectangle(x, y, w, 32), colors.TextDisabled,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                return y + 36;
            }

            int cardH = 52;
            int gap = 8;

            foreach (var item in today.Take(5))
            {
                var rect = new Rectangle(x, y, w, cardH);
                DrawScheduleRow(g, item, rect);
                y += cardH + gap;
            }

            return y;
        }

        /// <summary>
        /// 单条日程行。
        /// </summary>
        private void DrawScheduleRow(Graphics g, ScheduleItem item, Rectangle rect)
        {
            var colors = AppTheme.Colors;

            using (var path = GraphicsExtensions.GetRoundPath(rect, WinUI3Tokens.CardRadius))
            using (var brush = new SolidBrush(colors.CardBg))
                g.FillPath(brush, path);

            using (var path = GraphicsExtensions.GetRoundPath(rect, WinUI3Tokens.CardRadius))
            using (var pen = new Pen(colors.CardBorder, 1f))
                g.DrawPath(pen, path);

            var barColor = ParseColor(item.Color, colors.Accent);
            var barRect = new Rectangle(rect.X + 14, rect.Y + 12, 4, rect.Height - 24);
            using (var brush = new SolidBrush(barColor))
            using (var path = GraphicsExtensions.GetRoundPath(barRect, 2))
                g.FillPath(brush, path);

            using var timeFont = new Font(AppTheme.BodyFont.FontFamily, 11f);
            string timeStr = string.IsNullOrEmpty(item.StartTime) ? "" : item.StartTime;
            if (!string.IsNullOrEmpty(item.EndTime))
                timeStr += " - " + item.EndTime;

            TextRenderer.DrawText(g, timeStr, timeFont,
                new Rectangle(rect.X + 28, rect.Y + 6, 160, 18), colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            using var nameFont = new Font(AppTheme.BodyFont.FontFamily, 13f, FontStyle.Bold);
            TextRenderer.DrawText(g, item.Title ?? "", nameFont,
                new Rectangle(rect.X + 28, rect.Y + 26, rect.Width - 300, 20), colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            string sub = "";
            if (!string.IsNullOrEmpty(item.Location)) sub += item.Location;
            if (!string.IsNullOrEmpty(sub)) sub += "  ·  " + CategoryText(item.Category);

            if (!string.IsNullOrEmpty(sub))
            {
                using var subFont = new Font(AppTheme.BodyFont.FontFamily, 11f);
                TextRenderer.DrawText(g, sub, subFont,
                    new Rectangle(rect.Right - 280, rect.Y + 16, 260, 20), colors.TextSecondary,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
        }

        /// <summary>
        /// 最近待办。
        /// </summary>
        private int DrawRecentTodo(Graphics g, int x, int y, int w)
        {
            var colors = AppTheme.Colors;

            using var titleFont = new Font(AppTheme.BodyFont.FontFamily, 15f, FontStyle.Bold);
            TextRenderer.DrawText(g, I18n.T("office.recentTodo"), titleFont,
                new Rectangle(x, y, w, 26), colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            y += 34;

            var todos = _schedules
                .Where(s => s.IsTodo && !s.Done)
                .Take(5)
                .ToList();

            if (todos.Count == 0)
            {
                using var emptyFont = new Font(AppTheme.BodyFont.FontFamily, 12f);
                TextRenderer.DrawText(g, I18n.T("office.noTodo"), emptyFont,
                    new Rectangle(x, y, w, 32), colors.TextDisabled,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                return y + 36;
            }

            int rowH = 32;
            foreach (var todo in todos)
            {
                if (y > Height - 40) break;

                var boxRect = new Rectangle(x, y + 6, 16, 16);
                using (var pen = new Pen(colors.ButtonBorder, 1.5f))
                    g.DrawRectangle(pen, boxRect);

                TextRenderer.DrawText(g, todo.Title ?? "", AppTheme.BodyFont,
                    new Rectangle(x + 28, y, w - 40, rowH), colors.TextPrimary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                y += rowH;
            }

            return y;
        }

        /// <summary>
        /// 双击打开日程编辑。
        /// </summary>
        private void OnMouseDoubleClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                _owner.SwitchPagePublic(1);
        }

        /// <summary>
        /// 分类翻译。
        /// </summary>
        private static string CategoryText(string category)
        {
            return category switch
            {
                "meeting" => I18n.T("schedule.category.meeting"),
                "task" => I18n.T("schedule.category.task"),
                "reminder" => I18n.T("schedule.category.reminder"),
                "other" => I18n.T("schedule.category.other"),
                _ => "",
            };
        }

        /// <summary>
        /// hex 转 Color。
        /// </summary>
        private static Color ParseColor(string hex, Color fallback)
        {
            if (string.IsNullOrEmpty(hex)) return fallback;
            try
            {
                if (hex.StartsWith("#")) hex = hex.Substring(1);
                if (hex.Length != 6) return fallback;
                int r = Convert.ToInt32(hex.Substring(0, 2), 16);
                int g = Convert.ToInt32(hex.Substring(2, 2), 16);
                int b = Convert.ToInt32(hex.Substring(4, 2), 16);
                return Color.FromArgb(r, g, b);
            }
            catch { return fallback; }
        }
    }
}