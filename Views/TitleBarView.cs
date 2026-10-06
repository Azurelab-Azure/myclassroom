using System;
using System.Drawing;
using System.Windows.Forms;
using CourseApp.Controls;
using CourseApp.Localization;
using CourseApp.Theme;

namespace CourseApp.Views
{
    /// <summary>
    /// 自定义标题栏：图标 + 标题 + 三按钮（最小化 / 最大化 / 关闭）+ 拖动。
    /// 主题切换由 Form1 统一触发 Invalidate。
    /// </summary>
    public class TitleBarView : Panel
    {
        private readonly Form _owner;
        private readonly Label _titleLabel;
        private readonly FlatIconButton _minBtn;
        private readonly FlatIconButton _maxBtn;
        private readonly FlatIconButton _closeBtn;

        private bool _dragging;
        private Point _dragStart;

        public TitleBarView(Form owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));

            Dock = DockStyle.Top;
            Height = 44;
            BackColor = AppTheme.Colors.WindowBg;

            // ---------- 标题 ----------
            _titleLabel = new Label
            {
                Text = I18n.T("app.title"),
                Font = AppTheme.TitleFont,
                ForeColor = AppTheme.Colors.TextPrimary,
                Left = 16,
                Top = 12,
                AutoSize = true,
                BackColor = Color.Transparent,
            };
            _titleLabel.MouseDown += TitleBar_MouseDown;
            _titleLabel.MouseMove += TitleBar_MouseMove;
            _titleLabel.MouseUp += TitleBar_MouseUp;
            _titleLabel.DoubleClick += TitleBar_DoubleClick;
            Controls.Add(_titleLabel);

            // ---------- 三按钮 ----------
            _minBtn = MakeButton(Icons.Minimize);
            _minBtn.Click += (s, e) => _owner.WindowState = FormWindowState.Minimized;
            Controls.Add(_minBtn);

            _maxBtn = MakeButton(Icons.Maximize);
            _maxBtn.Click += (s, e) => ToggleMaximize();
            Controls.Add(_maxBtn);

            _closeBtn = MakeButton(Icons.Close, isClose: true);
            _closeBtn.Click += (s, e) => _owner.Close();
            Controls.Add(_closeBtn);

            MouseDown += TitleBar_MouseDown;
            MouseMove += TitleBar_MouseMove;
            MouseUp += TitleBar_MouseUp;
            DoubleClick += TitleBar_DoubleClick;

            // 监听窗口状态，更新最大化图标
            _owner.Resize += (s, e) => UpdateMaximizeIcon();
            _owner.HandleCreated += (s, e) => UpdateMaximizeIcon();

            Resize += (s, e) => LayoutButtons();
        }

        private FlatIconButton MakeButton(string icon, bool isClose = false)
        {
            return new FlatIconButton
            {
                Icon = icon,
                IconSize = 16,
                IsCloseButton = isClose,
                Width = 44,
                Height = 32,
                Top = 6,
            };
        }

        private void LayoutButtons()
        {
            _closeBtn.Left = Width - _closeBtn.Width;
            _maxBtn.Left = _closeBtn.Left - _maxBtn.Width;
            _minBtn.Left = _maxBtn.Left - _minBtn.Width;
        }

        // =====================================================
        // 最大化 / 还原
        // =====================================================
        private void ToggleMaximize()
        {
            if (_owner.WindowState == FormWindowState.Maximized)
                _owner.WindowState = FormWindowState.Normal;
            else
                _owner.WindowState = FormWindowState.Maximized;

            UpdateMaximizeIcon();
        }

        private void UpdateMaximizeIcon()
        {
            if (_owner.WindowState == FormWindowState.Maximized)
                _maxBtn.Icon = Icons.Restore;
            else
                _maxBtn.Icon = Icons.Maximize;
        }

        // =====================================================
        // 拖动
        // =====================================================
        private void TitleBar_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            _dragging = true;
            _dragStart = Cursor.Position;
        }

        private void TitleBar_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!_dragging) return;
            if (_owner.WindowState != FormWindowState.Normal) return;

            var now = Cursor.Position;
            _owner.Location = new Point(
                _owner.Location.X + now.X - _dragStart.X,
                _owner.Location.Y + now.Y - _dragStart.Y);
            _dragStart = now;
        }

        private void TitleBar_MouseUp(object? sender, MouseEventArgs e)
        {
            _dragging = false;
        }

        private void TitleBar_DoubleClick(object? sender, EventArgs e)
        {
            ToggleMaximize();
        }
    }
}