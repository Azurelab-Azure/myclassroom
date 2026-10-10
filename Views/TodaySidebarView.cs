using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using CourseApp.Localization;
using CourseApp.Models;
using CourseApp.Services;
using CourseApp.Theme;

namespace CourseApp.Views
{
    /// <summary>
    /// 今日侧边栏内容视图。
    /// 按配置的模块列表顺序竖向渲染：News / Schedule / ScheduleHint / App。
    /// 每个模块占一行，图标或首字居中显示。
    /// </summary>
    public class TodaySidebarView : Panel
    {
        /// <summary>每秒刷新计时器</summary>
        private readonly System.Windows.Forms.Timer _timer;

        /// <summary>渲染上下文</summary>
        private readonly OverlayContext _ctx = new();

        /// <summary>当前配置</summary>
        private OverlayConfig _config = new();

        /// <summary>折叠请求事件</summary>
        public event Action? ToggleCollapseRequested;

        /// <summary>每行高度</summary>
        private const int RowH = 36;

        /// <summary>顶部留白</summary>
        private const int PadTop = 8;

        /// <summary>底部留白</summary>
        private const int PadBottom = 32;

        /// <summary>
        /// 构造今日侧边栏内容视图。
        /// </summary>
        public TodaySidebarView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);

            Dock = DockStyle.Fill;
            BackColor = AppTheme.Colors.CardBg;

            _timer = new System.Windows.Forms.Timer { Interval = 1000 };
            _timer.Tick += (s, e) => OnTick();
            _timer.Start();
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
        public void SetData(
            List<Course> courses,
            List<SectionTime> sections,
            DutyRoster duty,
            int currentWeek,
            List<ClassCommittee> committees)
        {
            _ctx.Courses = courses ?? new List<Course>();
            _ctx.Sections = sections ?? new List<SectionTime>();
            _ctx.Duty = duty ?? new DutyRoster();
            _ctx.CurrentWeek = currentWeek;
            _ctx.Committees = committees ?? new List<ClassCommittee>();

            Invalidate();
        }

        /// <summary>
        /// 设置配置。
        /// </summary>
        public void SetConfig(OverlayConfig config)
        {
            _config = config ?? new OverlayConfig();
            Invalidate();
        }

        /// <summary>
        /// 设置当前新闻标题。
        /// </summary>
        public void SetNews(string news)
        {
            _ctx.CurrentNews = news ?? "";
            Invalidate();
        }

        /// <summary>
        /// 设置当前前台应用名。
        /// </summary>
        public void SetCurrentApp(string app)
        {
            _ctx.CurrentApp = app ?? "";
            Invalidate();
        }

        /// <summary>
        /// 每秒刷新上下文时间。
        /// </summary>
        private void OnTick()
        {
            _ctx.Now = DateTime.Now;
            Invalidate();
        }

        /// <summary>
        /// 绘制侧边栏内容。
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color bg = OverlayRenderer.ParseColor(_config.SidebarBg, AppTheme.Colors.CardBg);
            Color fg = OverlayRenderer.ParseColor(_config.SidebarFg, AppTheme.Colors.TextPrimary);

            using (var brush = new SolidBrush(bg))
                g.FillRectangle(brush, ClientRectangle);

            int y = PadTop;

            // 顶部"今"字
            using (var font = new Font(AppTheme.BodyFont.FontFamily, 9f, FontStyle.Bold))
            {
                var rect = new Rectangle(0, y, Width, 16);
                TextRenderer.DrawText(g, "今", font, rect, fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix);
            }
            y += 22;

            // 按模块竖向渲染
            var modules = _config.SidebarModules;
            if (modules != null && modules.Count > 0)
            {
                foreach (var m in modules)
                {
                    if (y > Height - PadBottom) break;

                    var rowRect = new Rectangle(0, y, Width, RowH);
                    DrawModuleRow(g, rowRect, m, fg);
                    y += RowH;
                }
            }

            // 底部折叠箭头
            using var pen = new Pen(fg, 1.5f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
            };
            int cx = Width / 2;
            int cy = Height - 16;
            g.DrawLines(pen, new[]
            {
                new Point(cx - 3, cy - 4),
                new Point(cx + 2, cy),
                new Point(cx - 3, cy + 4),
            });
        }

        /// <summary>
        /// 绘制一个模块行。
        /// </summary>
        private void DrawModuleRow(Graphics g, Rectangle rect, string module, Color fg)
        {
            // 模块图标（用短名代替）
            string icon = module switch
            {
                "News" => "闻",
                "Schedule" => "课",
                "ScheduleHint" => "时",
                "App" => "应",
                _ => "·",
            };

            using var iconFont = new Font(AppTheme.BodyFont.FontFamily, 11f, FontStyle.Bold);
            var iconRect = new Rectangle(0, rect.Y, Width, 16);
            TextRenderer.DrawText(g, icon, iconFont, iconRect, fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPrefix);

            // 模块内容：取前 2 个字符
            string text = OverlayRenderer.RenderText(module, _ctx);
            if (string.IsNullOrEmpty(text)) return;

            string shortText = text.Length > 2 ? text.Substring(0, 2) : text;

            using var textFont = new Font(AppTheme.BodyFont.FontFamily, 8f);
            var textRect = new Rectangle(0, rect.Y + 16, Width, 16);
            TextRenderer.DrawText(g, shortText, textFont, textRect, fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        /// <summary>
        /// 点击底部折叠箭头触发折叠。
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && e.Y >= Height - 30)
            {
                ToggleCollapseRequested?.Invoke();
                return;
            }
            base.OnMouseDown(e);
        }
    }
}