using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using CourseApp.Controls;
using CourseApp.Theme;

namespace CourseApp.Dialogs
{
    /// <summary>
    /// 所有弹窗的基类：
    ///   - 无边框、圆角
    ///   - 自绘标题栏（标题 + 关闭按钮 + 拖动）
    ///   - 主题切换由 Form1 统一触发（弹窗自己订阅，因为独立窗口）
    /// </summary>
    public class FlatDialogBase : Form
    {
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);

        protected Panel TitleBar { get; private set; } = null!;
        protected Label TitleLabel { get; private set; } = null!;
        protected FlatIconButton CloseButton { get; private set; } = null!;
        protected Panel ContentPanel { get; private set; } = null!;

        private bool _dragging;
        private Point _dragStart;

        public FlatDialogBase(string title, int width, int height)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            BackColor = AppTheme.Colors.WindowBg;
            ClientSize = new Size(width, height);

            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);
            UpdateStyles();
            DoubleBuffered = true;

            KeyPreview = true;

            // ---------- 标题栏 ----------
            TitleBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = AppTheme.Colors.WindowBg,
            };
            TitleBar.MouseDown += TitleBar_MouseDown;
            TitleBar.MouseMove += TitleBar_MouseMove;
            TitleBar.MouseUp += TitleBar_MouseUp;

            TitleLabel = new Label
            {
                Text = title,
                Font = AppTheme.TitleFont,
                ForeColor = AppTheme.Colors.TextPrimary,
                Left = 16,
                Top = 12,
                AutoSize = true,
                BackColor = Color.Transparent,
            };
            TitleLabel.MouseDown += TitleBar_MouseDown;
            TitleLabel.MouseMove += TitleBar_MouseMove;
            TitleLabel.MouseUp += TitleBar_MouseUp;
            TitleBar.Controls.Add(TitleLabel);

            CloseButton = new FlatIconButton
            {
                Icon = Icons.Close,
                IconSize = 12,
                IsCloseButton = true,
                Width = 32,
                Height = 32,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };
            CloseButton.Click += (s, e) => OnCloseClicked();
            TitleBar.Controls.Add(CloseButton);
            TitleBar.Resize += (s, e) =>
            {
                CloseButton.Left = TitleBar.Width - 40;
                CloseButton.Top = 6;
            };

            // ---------- 内容区 ----------
            ContentPanel = new DoubleBufferedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Colors.WindowBg,
                Padding = new Padding(20, 0, 20, 20),
            };

            Controls.Add(ContentPanel);
            Controls.Add(TitleBar);

            // 弹窗是独立窗口，自己订阅主题变化
            AppTheme.ThemeChanged += OnThemeChanged;

            KeyDown += FlatDialogBase_KeyDown;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) AppTheme.ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
        }

        private void OnThemeChanged()
        {
            BackColor = AppTheme.Colors.WindowBg;
            TitleBar.BackColor = AppTheme.Colors.WindowBg;
            ContentPanel.BackColor = AppTheme.Colors.WindowBg;
            TitleLabel.ForeColor = AppTheme.Colors.TextPrimary;
            Invalidate(true);
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
                    0, 0, Width, Height,
                    AppTheme.Radius.Dialog * 2, AppTheme.Radius.Dialog * 2));
            }
            catch { }
        }

        // =====================================================
        // 拖动
        // =====================================================
        private void TitleBar_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _dragging = true;
                _dragStart = Cursor.Position;
            }
        }

        private void TitleBar_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_dragging)
            {
                var now = Cursor.Position;
                Location = new Point(
                    Location.X + now.X - _dragStart.X,
                    Location.Y + now.Y - _dragStart.Y);
                _dragStart = now;
            }
        }

        private void TitleBar_MouseUp(object? sender, MouseEventArgs e)
        {
            _dragging = false;
        }

        // =====================================================
        // 键盘
        // =====================================================
        private void FlatDialogBase_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                OnEscapePressed();
                e.Handled = true;
            }
        }

        protected virtual void OnEscapePressed()
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        protected virtual void OnCloseClicked()
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}