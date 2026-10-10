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
    /// 灵动岛内容视图。
    /// 按配置的模块列表顺序渲染：News / Schedule / ScheduleHint / App。
    /// 模块之间用分隔符连接。
    /// </summary>
    public class TimeIslandView : Panel
    {
        /// <summary>每秒刷新计时器</summary>
        private readonly System.Windows.Forms.Timer _timer;

        /// <summary>渲染上下文</summary>
        private readonly OverlayContext _ctx = new();

        /// <summary>当前配置</summary>
        private OverlayConfig _config = new();

        /// <summary>当前新闻标题</summary>
        private string _currentNews = "";

        /// <summary>当前前台进程名</summary>
        private string _currentApp = "";

        /// <summary>模块之间的分隔符</summary>
        private const string Separator = "  ·  ";

        /// <summary>左侧留白</summary>
        private const int PadLeft = 14;

        /// <summary>右侧留白</summary>
        private const int PadRight = 14;

        /// <summary>
        /// 构造灵动岛内容视图。
        /// </summary>
        public TimeIslandView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            BackColor = Color.FromArgb(0x1A, 0x1A, 0x1A);

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
            List<SectionTime> sections,
            List<Course> courses,
            int currentWeek,
            DutyRoster duty,
            List<ClassCommittee> committees)
        {
            _ctx.Sections = sections ?? new List<SectionTime>();
            _ctx.Courses = courses ?? new List<Course>();
            _ctx.CurrentWeek = currentWeek;
            _ctx.Duty = duty ?? new DutyRoster();
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
            _currentNews = news ?? "";
            _ctx.CurrentNews = _currentNews;
            Invalidate();
        }

        /// <summary>
        /// 设置当前前台应用名。
        /// </summary>
        public void SetCurrentApp(string app)
        {
            _currentApp = app ?? "";
            _ctx.CurrentApp = _currentApp;
            Invalidate();
        }

        /// <summary>
        /// 每秒更新上下文时间。
        /// </summary>
        private void OnTick()
        {
            _ctx.Now = DateTime.Now;

            // 更新前台应用
            if (_config.IslandModules.Contains("App"))
                _ctx.CurrentApp = GetForegroundProcessName();

            Invalidate();
        }

        /// <summary>
        /// 绘制灵动岛内容。
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var bg = new SolidBrush(BackColor))
                g.FillRectangle(bg, ClientRectangle);

            var modules = _config.IslandModules;
            if (modules == null || modules.Count == 0)
            {
                DrawEmpty(g);
                return;
            }

            Color fg = OverlayRenderer.ParseColor(_config.IslandFg, Color.FromArgb(0xF0, 0xF0, 0xF0));

            // 左侧状态点
            int left = PadLeft;
            int midY = Height / 2;
            var dotRect = new Rectangle(left, midY - 4, 8, 8);
            Color dotColor = GetStatusDotColor();
            using (var dotBrush = new SolidBrush(dotColor))
                g.FillEllipse(dotBrush, dotRect);
            left += 16;

            // 右侧时间
            string timeText = _ctx.Now.ToString("HH:mm");
            var timeSize = TextRenderer.MeasureText(g, timeText, AppTheme.SmallFont);
            int right = Width - PadRight;
            var timeRect = new Rectangle(right - timeSize.Width, 0, timeSize.Width, Height);
            TextRenderer.DrawText(g, timeText, AppTheme.SmallFont, timeRect,
                Color.FromArgb(0xB0, 0xB0, 0xB0),
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            int textRight = timeRect.Left - 10;

            // 中间模块
            var parts = new List<string>();
            foreach (var m in modules)
            {
                string text = OverlayRenderer.RenderText(m, _ctx);
                if (!string.IsNullOrEmpty(text)) parts.Add(text);
            }

            if (parts.Count == 0)
            {
                DrawEmpty(g);
                return;
            }

            string fullText = string.Join(Separator, parts);
            var mainRect = new Rectangle(left, 0, Math.Max(10, textRight - left), Height);
            TextRenderer.DrawText(g, fullText, AppTheme.BodyFont, mainRect, fg,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        /// <summary>
        /// 绘制空状态。
        /// </summary>
        private void DrawEmpty(Graphics g)
        {
            var fg = OverlayRenderer.ParseColor(_config.IslandFg, Color.FromArgb(0xB0, 0xB0, 0xB0));
            TextRenderer.DrawText(g, I18n.T("island.noCourse"), AppTheme.BodyFont,
                ClientRectangle, fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        /// <summary>
        /// 状态点颜色。
        /// </summary>
        private Color GetStatusDotColor()
        {
            var now = _ctx.Now;

            foreach (var st in _ctx.Sections.Where(s => s.Type == "normal").OrderBy(s => s.Section))
            {
                if (string.IsNullOrEmpty(st.StartTime) || string.IsNullOrEmpty(st.EndTime))
                    continue;

                if (!TimeSpan.TryParse(st.StartTime, out var startTs)) continue;
                if (!TimeSpan.TryParse(st.EndTime, out var endTs)) continue;

                var startDt = now.Date + startTs;
                var endDt = now.Date + endTs;

                if (now >= startDt && now < endDt)
                {
                    var remain = endDt - now;
                    if (remain.TotalMinutes <= 5)
                        return Color.FromArgb(0xFF, 0x6B, 0x00);
                    return Color.FromArgb(0x4C, 0xAF, 0x50);
                }

                if (now < startDt)
                    return Color.FromArgb(0xFF, 0xB9, 0x00);
            }

            return Color.FromArgb(0x9E, 0x9E, 0x9E);
        }

        /// <summary>
        /// 获取前台进程名（去扩展名）。
        /// </summary>
        private static string GetForegroundProcessName()
        {
            try
            {
                var hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero) return "";

                GetWindowThreadProcessId(hwnd, out uint pid);
                if (pid == 0) return "";

                var proc = System.Diagnostics.Process.GetProcessById((int)pid);
                return proc.ProcessName ?? "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>获取前台窗口句柄</summary>
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        /// <summary>获取窗口所属进程 ID</summary>
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    }
}