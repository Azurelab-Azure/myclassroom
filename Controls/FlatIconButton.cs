using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// 只显示一个 PNG 图标的按钮。
    /// hover 时只变背景色，图标本身不变（PNG 不换色）。
    /// 主题切换由顶层 Form1 统一触发 Invalidate。
    /// </summary>
    public class FlatIconButton : Control
    {
        private bool _hover;
        private bool _pressed;

        private string _icon = "";
        private int _iconSize = 16;
        private int _cornerRadius = 4;
        private bool _isCloseButton = false;

        public FlatIconButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Size = new Size(32, 32);
        }

        // =====================================================
        // 属性
        // =====================================================
        public string Icon
        {
            get => _icon;
            set { _icon = value ?? ""; Invalidate(); }
        }

        public int IconSize
        {
            get => _iconSize;
            set { _iconSize = value; Invalidate(); }
        }

        public int CornerRadius
        {
            get => _cornerRadius;
            set { _cornerRadius = value; Invalidate(); }
        }

        /// <summary>关闭按钮：hover 时红底</summary>
        public bool IsCloseButton
        {
            get => _isCloseButton;
            set { _isCloseButton = value; Invalidate(); }
        }

        // =====================================================
        // 鼠标
        // =====================================================
        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true; Invalidate(); base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false; _pressed = false; Invalidate(); base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { _pressed = true; Invalidate(); }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_pressed) { _pressed = false; Invalidate(); }
            base.OnMouseUp(e);
        }

        // =====================================================
        // 绘制
        // =====================================================
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var colors = AppTheme.Colors;
            Color bg = Color.Transparent;

            if (_isCloseButton)
            {
                if (_pressed) bg = colors.DangerPressed;
                else if (_hover) bg = colors.DangerHover;
            }
            else
            {
                if (_pressed) bg = colors.ButtonPressed;
                else if (_hover) bg = colors.HoverBg;
            }

            if (bg.A > 0)
            {
                var rect = new Rectangle(0, 0, Width - 1, Height - 1);
                using var path = GraphicsExtensions.GetRoundPath(rect, _cornerRadius);
                using var brush = new SolidBrush(bg);
                g.FillPath(brush, path);
            }

            // 图标
            if (!string.IsNullOrEmpty(_icon))
            {
                var iconRect = new Rectangle(
                    (Width - _iconSize) / 2,
                    (Height - _iconSize) / 2,
                    _iconSize,
                    _iconSize);
                IconRenderer.Draw(g, _icon, iconRect, Color.Empty, _iconSize);
            }
        }
    }
}