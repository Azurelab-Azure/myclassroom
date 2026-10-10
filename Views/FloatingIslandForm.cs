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
    /// 浮动组件窗口（灵动岛 / 刘海屏 / 顶部栏）。
    /// 三种模式统一由一个窗口实现，通过 OverlayConfig.IslandMode 切换。
    /// 独立窗口，主题变化由自己处理。
    /// </summary>
    public class FloatingIslandForm : Form
    {
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);

        /// <summary>灵动岛内容视图</summary>
        private readonly TimeIslandView _island;

        /// <summary>内容宿主</summary>
        private readonly Panel _host;

        /// <summary>圆角半径</summary>
        private int _islandRadius = 18;

        /// <summary>拖动状态</summary>
        private bool _dragging;

        /// <summary>拖动起点</summary>
        private Point _dragStart;

        /// <summary>灵动岛点击事件</summary>
        public event Action? IslandClicked;

        /// <summary>退出请求事件</summary>
        public event Action? ExitRequested;

        /// <summary>当前配置</summary>
        private OverlayConfig _config = new();

        /// <summary>
        /// 构造浮动组件窗口。
        /// </summary>
        public FloatingIslandForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            BackColor = Color.Black;
            DoubleBuffered = true;

            _host = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
            };
            Controls.Add(_host);

            _island = new TimeIslandView
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
            };
            _host.Controls.Add(_island);

            _island.MouseDown += Island_MouseDown;
            MouseDown += Island_MouseDown;

            MouseMove += Form_MouseMove;
            MouseUp += (s, e) => _dragging = false;

            ApplyConfig();

            AppTheme.ThemeChanged += OnThemeChanged;
            _host.Resize += (s, e) => UpdateRadius();
        }

        /// <summary>
        /// 释放资源。
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing) AppTheme.ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
        }

        /// <summary>
        /// 主题变化时刷新。
        /// </summary>
        private void OnThemeChanged() => Invalidate();

        // =====================================================
        // 配置
        // =====================================================
        /// <summary>
        /// 从 overlay.json 应用配置。
        /// </summary>
        public void ApplyConfig()
        {
            _config = OverlayConfigService.Load();
            _island.SetConfig(_config);

            // 未启用则隐藏
            if (!_config.IslandEnabled)
            {
                Hide();
                return;
            }

            Color bg = OverlayRenderer.ParseColor(_config.IslandBg, Color.FromArgb(0x1A, 0x1A, 0x1A));
            BackColor = bg;
            _host.BackColor = bg;
            _island.BackColor = bg;

            switch (_config.IslandMode)
            {
                case "notch": ApplyNotchMode(); break;
                case "topbar": ApplyTopBarMode(); break;
                default: ApplyIslandMode(); break;
            }

            ApplyRoundRegion();
            PositionTop();

            if (!Visible) Show();
        }

        /// <summary>
        /// 灵动岛模式：顶部中央胶囊。
        /// </summary>
        private void ApplyIslandMode()
        {
            int w = Math.Max(160, _config.IslandWidth);
            int h = Math.Max(24, _config.IslandHeight);
            Size = new Size(w, h);
            _islandRadius = h / 2;
        }

        /// <summary>
        /// 刘海屏模式：顶部中央贴顶矩形。
        /// </summary>
        private void ApplyNotchMode()
        {
            int w = Math.Max(200, _config.IslandWidth);
            int h = Math.Max(28, _config.IslandHeight);
            Size = new Size(w, h);
            _islandRadius = 8;
        }

        /// <summary>
        /// 顶部栏模式：横跨整个屏幕顶部。
        /// </summary>
        private void ApplyTopBarMode()
        {
            var screen = Screen.PrimaryScreen ?? Screen.AllScreens[0];
            int h = Math.Max(24, _config.IslandHeight);
            Size = new Size(screen.Bounds.Width, h);
            _islandRadius = 0;
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
        /// 更新圆角。
        /// </summary>
        private void UpdateRadius()
        {
            if (_config.IslandMode == "topbar")
                _islandRadius = 0;
            else if (_config.IslandMode == "notch")
                _islandRadius = 8;
            else
                _islandRadius = Height / 2;

            ApplyRoundRegion();
        }

        /// <summary>
        /// 用 GDI 创建圆角区域。
        /// </summary>
        private void ApplyRoundRegion()
        {
            try
            {
                if (Region != null) Region.Dispose();

                if (_islandRadius <= 0)
                {
                    Region = null;
                    return;
                }

                Region = Region.FromHrgn(CreateRoundRectRgn(
                    0, 0, Width + 1, Height + 1,
                    _islandRadius * 2, _islandRadius * 2));
            }
            catch { }
        }

        // =====================================================
        // 位置
        // =====================================================
        /// <summary>
        /// 定位到屏幕顶部。
        /// </summary>
        public void PositionTop()
        {
            var screen = Screen.PrimaryScreen ?? Screen.AllScreens[0];
            var wa = screen.WorkingArea;

            if (_config.IslandMode == "topbar")
            {
                Location = new Point(wa.Left, wa.Top);
            }
            else
            {
                Location = new Point(
                    wa.Left + (wa.Width - Width) / 2,
                    wa.Top + 2);
            }
        }

        // =====================================================
        // 数据
        // =====================================================
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
            _island.SetData(sections, courses, currentWeek, duty, committees);
        }

        /// <summary>
        /// 设置当前新闻。
        /// </summary>
        public void SetNews(string news) => _island.SetNews(news);

        /// <summary>
        /// 设置当前前台应用。
        /// </summary>
        public void SetCurrentApp(string app) => _island.SetCurrentApp(app);

        // =====================================================
        // 拖动 + 右键菜单
        // =====================================================
        /// <summary>
        /// 鼠标按下：左键拖动，右键菜单。
        /// </summary>
        private void Island_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _dragging = true;
                _dragStart = Cursor.Position;
            }
            else if (e.Button == MouseButtons.Right)
            {
                ShowContextMenu(e.Location);
            }
        }

        /// <summary>
        /// 鼠标移动：拖动窗口。
        /// </summary>
        private void Form_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!_dragging) return;
            var now = Cursor.Position;
            Location = new Point(
                Location.X + now.X - _dragStart.X,
                Location.Y + now.Y - _dragStart.Y);
            _dragStart = now;
        }

        /// <summary>
        /// 右键菜单。
        /// </summary>
        private void ShowContextMenu(Point localPt)
        {
            var menu = new FlatContextMenu();
            menu.AddItem(FlatMenuItem.Create(I18n.T("island.openMain"),
                () => IslandClicked?.Invoke(), Icons.Calendar));
            menu.AddSeparator();
            menu.AddItem(FlatMenuItem.CreateDanger(I18n.T("common.exit"),
                () => ExitRequested?.Invoke(), Icons.Close));

            menu.ShowAt(this, PointToScreen(localPt));
        }

        /// <summary>
        /// 双击打开主窗口。
        /// </summary>
        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            IslandClicked?.Invoke();
            base.OnMouseDoubleClick(e);
        }
    }
}