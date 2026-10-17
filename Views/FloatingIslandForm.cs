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
    /// 浮动组件窗口。
    /// </summary>
    public class FloatingIslandForm : Form
    {
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);

        private readonly TimeIslandView _island;
        private readonly Panel _host;

        private int _islandRadius = 18;
        private bool _dragging;
        private Point _dragStart;
        private bool _isResizing = false; // 防止尺寸递归

        public event Action? IslandClicked;
        public event Action? ExitRequested;

        private OverlayConfig _config = new();

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

            _island.ContentWidthChanged += (w) =>
            {
                if (_isResizing || _config.IslandMode == "topbar" || IsDisposed || !IsHandleCreated) return;

                if (Math.Abs(Width - w) > 5)
                {
                    _isResizing = true;
                    try
                    {
                        int oldRight = Location.X + Width;
                        Width = w;
                        
                        var screen = Screen.PrimaryScreen ?? Screen.AllScreens[0];
                        var wa = screen.WorkingArea;
                        Location = new Point(wa.Left + (wa.Width - Width) / 2, Location.Y);
                        
                        ApplyRoundRegion();
                    }
                    finally
                    {
                        _isResizing = false;
                    }
                }
            };

            _island.MouseDown += Island_MouseDown;
            MouseDown += Island_MouseDown;

            MouseMove += Form_MouseMove;
            MouseUp += (s, e) => _dragging = false;

            ApplyConfig();

            AppTheme.ThemeChanged += OnThemeChanged;
            _host.Resize += (s, e) => UpdateRadius();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) AppTheme.ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
        }

        private void OnThemeChanged() => Invalidate();

        public void ApplyConfig()
        {
            _config = OverlayConfigService.Load();
            _island.SetConfig(_config);

            if (!_config.IslandEnabled)
            {
                Hide();
                return;
            }

            Color bg = OverlayRenderer.ParseColor(_config.IslandBg, Color.FromArgb(0x1A, 0x1A, 0x1A));
            BackColor = bg;
            _host.BackColor = bg;
            _island.BackColor = bg;

            _isResizing = true;
            try
            {
                switch (_config.IslandMode)
                {
                    case "notch": ApplyNotchMode(); break;
                    case "topbar": ApplyTopBarMode(); break;
                    default: ApplyIslandMode(); break;
                }
            }
            finally
            {
                _isResizing = false;
            }

            ApplyRoundRegion();
            PositionTop();

            if (!Visible) Show();
        }

        private void ApplyIslandMode()
        {
            int w = Math.Max(160, _config.IslandWidth);
            int h = Math.Max(24, _config.IslandHeight);
            Size = new Size(w, h);
            _islandRadius = h / 2;
        }

        private void ApplyNotchMode()
        {
            int w = Math.Max(200, _config.IslandWidth);
            int h = Math.Max(28, _config.IslandHeight);
            Size = new Size(w, h);
            _islandRadius = 8;
        }

        private void ApplyTopBarMode()
        {
            var screen = Screen.PrimaryScreen ?? Screen.AllScreens[0];
            int h = Math.Max(24, _config.IslandHeight);
            Size = new Size(screen.Bounds.Width, h);
            _islandRadius = 0;
        }

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
            if (_config.IslandMode == "topbar")
                _islandRadius = 0;
            else if (_config.IslandMode == "notch")
                _islandRadius = 8;
            else
                _islandRadius = Height / 2;

            ApplyRoundRegion();
        }

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

        public void SetData(List<SectionTime> sections, List<Course> courses, int currentWeek,
            DutyRoster duty, List<ClassCommittee> committees)
        {
            _island.SetData(sections, courses, currentWeek, duty, committees);
        }

        public void SetOfficeData(List<ScheduleItem> schedules)
        {
            _island.SetOfficeData(schedules);
        }

        public void SetNews(string news) => _island.SetNews(news);

        public void SetCurrentApp(string app) => _island.SetCurrentApp(app);

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