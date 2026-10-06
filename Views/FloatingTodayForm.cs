using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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
    /// 独立的"今日"浮动窗口：
    ///   - 无边框、置顶、不可拖动（位置由设置页控制）
    ///   - 显示今日课程 + 值日生
    ///   - 展开 24px 宽（细长条 + 图标）
    ///   - 折叠 5px 宽（极细缝）
    /// </summary>
    public class FloatingTodayForm : Form
    {
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);

        // 数据
        private List<Course> _courses = new();
        private List<SectionTime> _sections = new();
        private DutyRoster _duty = new();
        private int _currentWeek = 1;

        // 内容
        private readonly TodaySidebarView _content;
        private readonly System.Windows.Forms.Timer _timer;

        // 折叠
        private bool _collapsed = false;
        private const int ExpandedWidth = 24;    // 展开：24px 细长条
        private const int ExpandedHeight = 420;
        private const int CollapsedWidth = 5;    // 折叠：5px 极细缝

        public event Action? OpenMainRequested;
        public event Action? ExitRequested;

        public FloatingTodayForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            BackColor = AppTheme.Colors.CardBg;
            DoubleBuffered = true;

            Size = new Size(ExpandedWidth, ExpandedHeight);

            // 内容视图
            _content = new TodaySidebarView
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Colors.CardBg,
            };
            _content.ToggleCollapseRequested += () => ToggleCollapse();
            _content.MouseDown += Content_MouseDown;
            Controls.Add(_content);

            // 整窗——右键菜单
            MouseDown += Content_MouseDown;

            // 每秒刷新
            _timer = new System.Windows.Forms.Timer { Interval = 1000 };
            _timer.Tick += (s, e) =>
            {
                if (!_collapsed) _content.Invalidate();
            };
            _timer.Start();

            // 从配置恢复位置和折叠状态
            RestoreStateFromConfig();
            ApplyPositionFromConfig();

            ApplyRoundRegion();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _timer?.Dispose();
            base.Dispose(disposing);
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

            _content.SetData(_courses, _sections, _duty, _currentWeek);
        }

        // =====================================================
        // 位置（只能从配置读）
        // =====================================================
        private void ApplyPositionFromConfig()
        {
            var cfg = ConfigService.Load();
            var screen = Screen.PrimaryScreen ?? Screen.AllScreens[0];
            var wa = screen.WorkingArea;

            // 默认位置：屏幕右侧中部
            int x = cfg.TodaySidebarX >= 0
                ? cfg.TodaySidebarX
                : wa.Right - Width - 4;

            int y = cfg.TodaySidebarY >= 0
                ? cfg.TodaySidebarY
                : wa.Top + (wa.Height - Height) / 2;

            Location = new Point(x, y);
        }

        /// <summary>外部调用——设置页改了位置后刷新。</summary>
        public void RefreshPosition()
        {
            ApplyPositionFromConfig();
        }

        // =====================================================
        // 折叠
        // =====================================================
        private void RestoreStateFromConfig()
        {
            var cfg = ConfigService.Load();
            _collapsed = cfg.TodaySidebarCollapsed;
            ApplyCollapseState();
        }

        public void ToggleCollapse()
        {
            _collapsed = !_collapsed;

            var cfg = ConfigService.Load();
            cfg.TodaySidebarCollapsed = _collapsed;
            ConfigService.Save(cfg);

            ApplyCollapseState();
        }

        private void ApplyCollapseState()
        {
            // 保持"右边缘"不动——向左收缩
            int rightEdge = Location.X + Width;

            if (_collapsed)
            {
                Size = new Size(CollapsedWidth, ExpandedHeight);
                _content.Visible = false;
            }
            else
            {
                Size = new Size(ExpandedWidth, ExpandedHeight);
                _content.Visible = true;
            }

            // 位置：右边缘不变
            Location = new Point(rightEdge - Width, Location.Y);

            ApplyRoundRegion();
            Invalidate();
        }

        // =====================================================
        // 圆角
        // =====================================================
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyRoundRegion();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ApplyRoundRegion();
        }

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
        // 右键菜单（不拖动）
        // =====================================================
        private void Content_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                ShowContextMenu(e.Location);
            }
            else if (e.Button == MouseButtons.Left && _collapsed)
            {
                // 折叠时——点窄缝 → 展开
                ToggleCollapse();
            }
        }

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
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var colors = AppTheme.Colors;

            using (var bg = new SolidBrush(colors.CardBg))
                g.FillRectangle(bg, ClientRectangle);

            // 边框
            using (var pen = new Pen(colors.CardBorder, 1f))
                g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);

            if (_collapsed)
            {
                // 折叠——只画一条竖线（很细）
                return;
            }

            // 展开——画箭头
            int cx = Width / 2;
            int cy = Height / 2;

            using var arrowPen = new Pen(colors.TextPrimary, 1.5f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
            };
            // 向右箭头（展开状态，提示"点击折叠"）
            g.DrawLines(arrowPen, new[]
            {
                new Point(cx - 3, cy - 4),
                new Point(cx + 2, cy),
                new Point(cx - 3, cy + 4),
            });

            // 顶部小字——"今"（竖排）
            using var font = new Font(AppTheme.BodyFont.FontFamily, 8f, FontStyle.Bold);
            var textRect = new Rectangle(0, 6, Width, 14);
            TextRenderer.DrawText(g, "今", font, textRect,
                colors.Accent,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPrefix);
        }
    }
}