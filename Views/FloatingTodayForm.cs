using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using CourseApp.Controls;
using CourseApp.Localization;
using CourseApp.Models;
using CourseApp.Services;
using CourseApp.Theme;

namespace CourseApp.Views
{
    /// <summary>
    /// 今日侧边栏浮动窗口。
    /// 显示：无边框、置顶、不可拖动（位置由配置控制）。
    /// 展开宽度由 OverlayConfig.SidebarWidth 控制。
    /// </summary>
    public class FloatingTodayForm : Form
    {
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);

        /// <summary>课程数据</summary>
        private List<Course> _courses = new();

        /// <summary>节次数据</summary>
        private List<SectionTime> _sections = new();

        /// <summary>值日生数据</summary>
        private DutyRoster _duty = new();

        /// <summary>班委公告</summary>
        private List<ClassCommittee> _committees = new();

        /// <summary>当前周次</summary>
        private int _currentWeek = 1;

        /// <summary>内容视图</summary>
        private readonly TodaySidebarView _content;

        /// <summary>每秒刷新计时器</summary>
        private readonly System.Windows.Forms.Timer _timer;

        /// <summary>折叠状态</summary>
        private bool _collapsed = false;

        /// <summary>展开宽度</summary>
        private int _expandedWidth = 24;

        /// <summary>展开高度</summary>
        private int _expandedHeight = 420;

        /// <summary>折叠宽度</summary>
        private const int CollapsedWidth = 5;

        /// <summary>当前配置</summary>
        private OverlayConfig _config = new();

        /// <summary>打开主窗口请求</summary>
        public event Action? OpenMainRequested;

        /// <summary>退出请求</summary>
        public event Action? ExitRequested;

        /// <summary>
        /// 构造今日侧边栏窗口。
        /// </summary>
        public FloatingTodayForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            BackColor = AppTheme.Colors.CardBg;
            DoubleBuffered = true;

            Size = new Size(_expandedWidth, _expandedHeight);

            // 内容视图
            _content = new TodaySidebarView
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Colors.CardBg,
            };
            _content.ToggleCollapseRequested += () => ToggleCollapse();
            _content.MouseDown += Content_MouseDown;
            Controls.Add(_content);

            // 整窗右键
            MouseDown += Content_MouseDown;

            // 每秒刷新
            _timer = new System.Windows.Forms.Timer { Interval = 1000 };
            _timer.Tick += (s, e) =>
            {
                if (!_collapsed) _content.Invalidate();
            };
            _timer.Start();

            // 从配置恢复
            RestoreStateFromConfig();
            ApplyPositionFromConfig();

            ApplyRoundRegion();
        }

        /// <summary>
        /// 释放计时器。
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing) _timer?.Dispose();
            base.Dispose(disposing);
        }

        // =====================================================
        // 配置
        // =====================================================
        /// <summary>
        /// 从 overlay.json 应用配置。
        /// </summary>
        public void ApplyConfig()
        {
            _config = OverlayConfigService.Load();
            _content.SetConfig(_config);

            if (!_config.SidebarEnabled)
            {
                Hide();
                return;
            }

            _expandedWidth = Math.Max(20, _config.SidebarWidth);
            _expandedHeight = Math.Max(120, _config.SidebarHeight);

            Color bg = OverlayRenderer.ParseColor(_config.SidebarBg, AppTheme.Colors.CardBg);
            BackColor = bg;
            _content.BackColor = bg;

            ApplyCollapseState();
            ApplyPositionFromConfig();

            if (!Visible) Show();
        }

        // =====================================================
        // 数据
        // =====================================================
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
            _courses = courses ?? new List<Course>();
            _sections = sections ?? new List<SectionTime>();
            _duty = duty ?? new DutyRoster();
            _currentWeek = currentWeek;
            _committees = committees ?? new List<ClassCommittee>();

            _content.SetData(_courses, _sections, _duty, _currentWeek, _committees);
        }

        /// <summary>
        /// 设置当前新闻。
        /// </summary>
        public void SetNews(string news) => _content.SetNews(news);

        /// <summary>
        /// 设置当前前台应用。
        /// </summary>
        public void SetCurrentApp(string app) => _content.SetCurrentApp(app);

        // =====================================================
        // 位置
        // =====================================================
        /// <summary>
        /// 从配置读取位置。
        /// </summary>
        private void ApplyPositionFromConfig()
        {
            var cfg = ConfigService.Load();
            var screen = Screen.PrimaryScreen ?? Screen.AllScreens[0];
            var wa = screen.WorkingArea;

            int x = cfg.TodaySidebarX >= 0
                ? cfg.TodaySidebarX
                : wa.Right - Width - 4;

            int y = cfg.TodaySidebarY >= 0
                ? cfg.TodaySidebarY
                : wa.Top + (wa.Height - Height) / 2;

            Location = new Point(x, y);
        }

        /// <summary>
        /// 外部调用：刷新位置。
        /// </summary>
        public void RefreshPosition()
        {
            ApplyPositionFromConfig();
        }

        // =====================================================
        // 折叠
        // =====================================================
        /// <summary>
        /// 从配置恢复折叠状态。
        /// </summary>
        private void RestoreStateFromConfig()
        {
            var cfg = ConfigService.Load();
            _collapsed = cfg.TodaySidebarCollapsed;
            ApplyCollapseState();
        }

        /// <summary>
        /// 切换折叠状态。
        /// </summary>
        public void ToggleCollapse()
        {
            _collapsed = !_collapsed;

            var cfg = ConfigService.Load();
            cfg.TodaySidebarCollapsed = _collapsed;
            ConfigService.Save(cfg);

            ApplyCollapseState();
        }

        /// <summary>
        /// 应用折叠状态。
        /// </summary>
        private void ApplyCollapseState()
        {
            int rightEdge = Location.X + Width;

            if (_collapsed)
            {
                Size = new Size(CollapsedWidth, _expandedHeight);
                _content.Visible = false;
            }
            else
            {
                Size = new Size(_expandedWidth, _expandedHeight);
                _content.Visible = true;
            }

            Location = new Point(rightEdge - Width, Location.Y);

            ApplyRoundRegion();
            Invalidate();
        }

        // =====================================================
        // 圆角
        // =====================================================
        /// <summary>
        /// 句柄创建后应用圆角。
        /// </summary>
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyRoundRegion();
        }

        /// <summary>
        /// 尺寸变化时重新应用圆角。
        /// </summary>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ApplyRoundRegion();
        }

        /// <summary>
        /// 应用圆角区域。
        /// </summary>
        private void ApplyRoundRegion()
        {
            try
            {
                if (Region != null) Region.Dispose();
                Region = Region.FromHrgn(CreateRoundRectRgn(
                    0, 0, Width + 1, Height + 1, 6, 6));
            }
            catch { }
        }

        // =====================================================
        // 右键菜单
        // =====================================================
        /// <summary>
        /// 鼠标按下：右键菜单 / 折叠时左键展开。
        /// </summary>
        private void Content_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                ShowContextMenu(e.Location);
            }
            else if (e.Button == MouseButtons.Left && _collapsed)
            {
                ToggleCollapse();
            }
        }

        /// <summary>
        /// 右键菜单。
        /// </summary>
        private void ShowContextMenu(Point localPt)
        {
            var menu = new FlatContextMenu();
            menu.AddItem(FlatMenuItem.Create(
                _collapsed ? I18n.T("today.expand") : I18n.T("today.collapse"),
                () => ToggleCollapse(),
                _collapsed ? Icons.ChevronLeft : Icons.ChevronRight));
            menu.AddSeparator();
            menu.AddItem(FlatMenuItem.Create(I18n.T("today.openMain"),
                () => OpenMainRequested?.Invoke(), Icons.Calendar));
            menu.AddSeparator();
            menu.AddItem(FlatMenuItem.CreateDanger(I18n.T("common.exit"),
                () => ExitRequested?.Invoke(), Icons.Close));

            menu.ShowAt(this, PointToScreen(localPt));
        }

        // =====================================================
        // 绘制
        // =====================================================
        /// <summary>
        /// 绘制窗口边框。
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            Color bg = OverlayRenderer.ParseColor(_config.SidebarBg, AppTheme.Colors.CardBg);
            using (var brush = new SolidBrush(bg))
                g.FillRectangle(brush, ClientRectangle);

            using var pen = new Pen(AppTheme.Colors.CardBorder, 1f);
            g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);

            if (_collapsed) return;

            // 展开时画箭头
            Color fg = OverlayRenderer.ParseColor(_config.SidebarFg, AppTheme.Colors.TextPrimary);
            int cx = Width / 2;
            int cy = Height - 16;

            using var arrowPen = new Pen(fg, 1.5f)
            {
                StartCap = System.Drawing.Drawing2D.LineCap.Round,
                EndCap = System.Drawing.Drawing2D.LineCap.Round,
            };
            g.DrawLines(arrowPen, new[]
            {
                new Point(cx - 3, cy - 4),
                new Point(cx + 2, cy),
                new Point(cx - 3, cy + 4),
            });
        }
    }
}