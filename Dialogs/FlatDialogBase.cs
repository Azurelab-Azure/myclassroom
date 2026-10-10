using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using CourseApp.Controls;
using CourseApp.Theme;

namespace CourseApp.Dialogs
{
    /// <summary>
    /// 所有弹窗的基类。
    /// 提供无边框圆角窗口、自绘标题栏、可拖动、Esc 关闭。
    /// 内容区支持两种模式：
    ///   固定模式（默认）：子控件加到 ContentPanel，不滚动。
    ///   滚动模式：调用 EnableScroll() 后，子控件应加到 ScrollPanel，自动滚动。
    /// </summary>
    public class FlatDialogBase : Form
    {
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);

        /// <summary>标题栏面板</summary>
        protected Panel TitleBar { get; private set; } = null!;

        /// <summary>标题文字</summary>
        protected Label TitleLabel { get; private set; } = null!;

        /// <summary>关闭按钮</summary>
        protected FlatIconButton CloseButton { get; private set; } = null!;

        /// <summary>固定内容宿主。默认子控件加到这里。</summary>
        protected Panel ContentPanel { get; private set; } = null!;

        /// <summary>可滚动内容宿主。调用 EnableScroll() 后才可用。</summary>
        protected Panel? ScrollPanel { get; private set; }

        /// <summary>滚动视口内的实际内容宿主。子控件加到 ScrollPanel 时会被自动转发到这里。</summary>
        private Panel? _contentHost;

        /// <summary>可滚动内容宿主的滚动条</summary>
        private FlatScrollBar? _scrollBar;

        /// <summary>可滚动内容宿主的实际内容高度</summary>
        private int _scrollContentHeight = 0;

        /// <summary>可滚动内容宿主的当前滚动偏移</summary>
        private int _scrollOffset = 0;

        /// <summary>拖动状态</summary>
        private bool _dragging;

        /// <summary>拖动起点</summary>
        private Point _dragStart;

        /// <summary>
        /// 构造弹窗。
        /// </summary>
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

            ContentPanel = new DoubleBufferedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Colors.WindowBg,
                Padding = new Padding(20, 0, 20, 20),
            };

            Controls.Add(ContentPanel);
            Controls.Add(TitleBar);

            AppTheme.ThemeChanged += OnThemeChanged;

            KeyDown += FlatDialogBase_KeyDown;
        }

        /// <summary>
        /// 释放资源，取消主题订阅。
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing) AppTheme.ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
        }

        /// <summary>
        /// 主题变化时刷新颜色。
        /// </summary>
        private void OnThemeChanged()
        {
            BackColor = AppTheme.Colors.WindowBg;
            TitleBar.BackColor = AppTheme.Colors.WindowBg;
            ContentPanel.BackColor = AppTheme.Colors.WindowBg;
            if (ScrollPanel != null) ScrollPanel.BackColor = AppTheme.Colors.WindowBg;
            if (_contentHost != null) _contentHost.BackColor = AppTheme.Colors.WindowBg;
            TitleLabel.ForeColor = AppTheme.Colors.TextPrimary;
            Invalidate(true);
        }

        // =====================================================
        // 滚动模式
        // =====================================================
        /// <summary>
        /// 启用滚动模式。
        /// 调用后 ContentPanel 的 Padding 清零，ScrollPanel 覆盖整个内容区。
        /// 子控件应加到 ScrollPanel，会自动转发到内部内容宿主。
        /// </summary>
        protected void EnableScroll()
        {
            if (ScrollPanel != null) return;

            ContentPanel.Padding = new Padding(0);

            // 外层：视口
            ScrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Colors.WindowBg,
                AutoScroll = false,
            };

            // 内层：实际内容宿主，滚动时移动它的 Top
            _contentHost = new Panel
            {
                Left = 20,
                Top = 0,
                Width = ScrollPanel.Width - 20 - 20,
                BackColor = AppTheme.Colors.WindowBg,
                AutoScroll = false,
            };
            _contentHost.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // 把外层的 Controls.Add 重定向到 _contentHost
            ScrollPanel.ControlAdded += (s, e) =>
            {
                if (e.Control != null && e.Control != _contentHost && _contentHost != null)
                {
                    ScrollPanel.Controls.Remove(e.Control);
                    _contentHost.Controls.Add(e.Control);
                }
            };

            // 把 ControlAdded 转发：通过 AddOwnedForm 不行，使用自定义容器
            // 这里改用自定义 AddControlToContent 方法 + 反射不可靠，
            // 直接提供 ScrollContent 属性更稳。

            ScrollPanel.Controls.Add(_contentHost);
            ScrollPanel.MouseWheel += ScrollPanel_MouseWheel;
            ScrollPanel.Resize += (s, e) => UpdateLayout();
            _contentHost.MouseWheel += ScrollPanel_MouseWheel;

            ContentPanel.Controls.Add(ScrollPanel);

            _scrollBar = new FlatScrollBar
            {
                Width = 8,
                Visible = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right,
            };
            _scrollBar.ValueChanged += (s, e) =>
            {
                _scrollOffset = _scrollBar.Value;
                ApplyScroll();
            };
            ContentPanel.Controls.Add(_scrollBar);
            _scrollBar.BringToFront();

            ContentPanel.Resize += (s, e) => UpdateLayout();

            // 初始化布局
            UpdateLayout();
        }

        /// <summary>
        /// 实际内容宿主。启用滚动模式后，子控件应加到这里。
        /// 为兼容旧代码，也允许加 ScrollPanel，但推荐加这个。
        /// </summary>
        protected Panel ScrollContent
        {
            get
            {
                if (_contentHost == null)
                    throw new InvalidOperationException("请先调用 EnableScroll()");
                return _contentHost;
            }
        }

        /// <summary>
        /// 更新布局：重新计算内容宿主宽度和滚动条位置。
        /// </summary>
        private void UpdateLayout()
        {
            if (ScrollPanel == null || _contentHost == null || _scrollBar == null) return;

            _contentHost.Left = 20;
            _contentHost.Width = ScrollPanel.Width - 40;
            _contentHost.Top = -_scrollOffset;

            _scrollBar.Left = ContentPanel.Width - 12;
            _scrollBar.Top = 0;
            _scrollBar.Height = ContentPanel.Height;

            UpdateScrollBar();
        }

        /// <summary>
        /// 设置滚动内容实际高度。子控件布局完成后调用。
        /// </summary>
        protected void SetScrollContentHeight(int contentHeight)
        {
            _scrollContentHeight = contentHeight;
            if (_contentHost != null)
                _contentHost.Height = contentHeight;
            UpdateScrollBar();
        }

        /// <summary>
        /// 根据内容和视口高度更新滚动条。
        /// </summary>
        private void UpdateScrollBar()
        {
            if (ScrollPanel == null || _scrollBar == null) return;

            int viewportH = ScrollPanel.Height;
            int contentH = _scrollContentHeight;

            if (contentH <= viewportH)
            {
                _scrollBar.Visible = false;
                _scrollOffset = 0;
                if (_contentHost != null) _contentHost.Top = 0;
                return;
            }

            _scrollBar.Visible = true;
            _scrollBar.Maximum = contentH;
            _scrollBar.LargeChange = viewportH;
            _scrollBar.Value = Math.Min(_scrollOffset, Math.Max(0, contentH - viewportH));
        }

        /// <summary>
        /// 应用滚动偏移到内容宿主。
        /// </summary>
        private void ApplyScroll()
        {
            if (_contentHost == null) return;
            _contentHost.Top = -_scrollOffset;
        }

        /// <summary>
        /// 滚动宿主滚轮事件。
        /// </summary>
        private void ScrollPanel_MouseWheel(object? sender, MouseEventArgs e)
        {
            if (_scrollBar == null || !_scrollBar.Visible) return;

            int maxScroll = Math.Max(0, _scrollBar.Maximum - _scrollBar.LargeChange);
            _scrollOffset -= Math.Sign(e.Delta) * 60;
            if (_scrollOffset < 0) _scrollOffset = 0;
            if (_scrollOffset > maxScroll) _scrollOffset = maxScroll;

            _scrollBar.Value = _scrollOffset;
            _scrollBar.Wake();
            ApplyScroll();
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
            UpdateLayout();
        }

        /// <summary>
        /// 用 GDI 创建圆角区域。
        /// </summary>
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
        /// <summary>
        /// 标题栏鼠标按下，开始拖动。
        /// </summary>
        private void TitleBar_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _dragging = true;
                _dragStart = Cursor.Position;
            }
        }

        /// <summary>
        /// 标题栏鼠标移动，执行拖动。
        /// </summary>
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

        /// <summary>
        /// 标题栏鼠标抬起，结束拖动。
        /// </summary>
        private void TitleBar_MouseUp(object? sender, MouseEventArgs e)
        {
            _dragging = false;
        }

        // =====================================================
        // 键盘
        // =====================================================
        /// <summary>
        /// Esc 关闭弹窗。
        /// </summary>
        private void FlatDialogBase_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                OnEscapePressed();
                e.Handled = true;
            }
        }

        /// <summary>
        /// 按下 Esc 时的默认行为。
        /// </summary>
        protected virtual void OnEscapePressed()
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        /// <summary>
        /// 点击关闭按钮时的默认行为。
        /// </summary>
        protected virtual void OnCloseClicked()
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}