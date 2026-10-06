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
    /// 独立的悬浮灵动岛窗口。圆角、无边框、置顶、屏幕顶部中央。
    /// 注意：这是独立窗口，不在 Form1 的控件树里，所以保留 ThemeChanged 订阅。
    /// </summary>
    public class FloatingIslandForm : Form
    {
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);

        private readonly TimeIslandView _island;
        private readonly NewsIslandView _news;
        private readonly Panel _host;

        private int _islandRadius = 18;
        private bool _dragging;
        private Point _dragStart;

        public event Action? IslandClicked;
        public event Action? ExitRequested;

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
                Visible = true,
            };
            _host.Controls.Add(_island);

            _news = new NewsIslandView
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                Visible = false,
            };
            _host.Controls.Add(_news);

            _island.MouseDown += Island_MouseDown;
            _news.MouseDown += Island_MouseDown;
            MouseDown += Island_MouseDown;

            MouseMove += Form_MouseMove;
            MouseUp += (s, e) => _dragging = false;

            ApplyConfig();

            // 独立窗口：自己订阅主题变化
            AppTheme.ThemeChanged += OnThemeChanged;
            _host.Resize += (s, e) => UpdateRadius();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) AppTheme.ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
        }

        private void OnThemeChanged() => Invalidate();

        // =====================================================
        // 配置
        // =====================================================
        public void ApplyConfig()
        {
            var cfg = ConfigService.Load();

            int w = Math.Max(120, cfg.IslandWidth);
            int h = Math.Max(24, cfg.IslandHeight);

            Size = new Size(w, h);
            _islandRadius = h / 2;

            _island.Visible = !cfg.IslandShowNews;
            _news.Visible = cfg.IslandShowNews;

            if (cfg.IslandShowNews)
                _news.Configure(cfg.IslandNewsUrl, cfg.IslandNewsIntervalSec);

            ApplyRoundRegion();
            PositionTopCenter();
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

        private void UpdateRadius()
        {
            _islandRadius = Height / 2;
            ApplyRoundRegion();
        }

        private void ApplyRoundRegion()
        {
            try
            {
                if (Region != null) Region.Dispose();
                Region = Region.FromHrgn(CreateRoundRectRgn(
                    0, 0, Width + 1, Height + 1,
                    _islandRadius * 2, _islandRadius * 2));
            }
            catch { }
        }

        public void PositionTopCenter()
        {
            var screen = Screen.PrimaryScreen ?? Screen.AllScreens[0];
            var wa = screen.WorkingArea;
            Location = new Point(
                wa.Left + (wa.Width - Width) / 2,
                wa.Top + 2);
        }

        public void SetData(List<SectionTime> sections, List<Course> courses, int currentWeek)
        {
            _island.SetData(sections, courses, currentWeek);
        }

        // =====================================================
        // 拖动 + 右键菜单
        // =====================================================
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

        private void Form_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!_dragging) return;
            var now = Cursor.Position;
            Location = new Point(
                Location.X + now.X - _dragStart.X,
                Location.Y + now.Y - _dragStart.Y);
            _dragStart = now;
        }

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

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            IslandClicked?.Invoke();
            base.OnMouseDoubleClick(e);
        }
    }
}