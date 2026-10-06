using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// 按钮样式。
    ///   Primary   ：强调色底，前景色字（主要操作）
    ///   Secondary ：卡片色底 + 边框（次要操作）
    ///   Subtle    ：透明底，hover 才显色（标题栏按钮、菜单项）
    ///   Danger    ：红底白字（危险操作）
    /// </summary>
    public enum FlatButtonStyle
    {
        Primary,
        Secondary,
        Subtle,
        Danger,
    }

    /// <summary>
    /// 完全自绘的按钮。
    /// 主题切换由 Form1 统一触发 Invalidate，本控件不订阅 ThemeChanged。
    /// </summary>
    public class FlatButton : Control
    {
        private bool _hover;
        private bool _pressed;

        private FlatButtonStyle _style = FlatButtonStyle.Secondary;
        private int _cornerRadius = 6;
        private string _icon = "";
        private int _iconSize = 16;
        private int _iconTextGap = 6;
        private Padding _contentPadding = new Padding(12, 0, 12, 0);

        public FlatButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Font = AppTheme.BodyFont;
            Size = new Size(80, 32);
        }

        // =====================================================
        // 属性
        // =====================================================
        public FlatButtonStyle ButtonStyle
        {
            get => _style;
            set { _style = value; Invalidate(); }
        }

        public int CornerRadius
        {
            get => _cornerRadius;
            set { _cornerRadius = value; Invalidate(); }
        }

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

        public int IconTextGap
        {
            get => _iconTextGap;
            set { _iconTextGap = value; Invalidate(); }
        }

        public Padding ContentPadding
        {
            get => _contentPadding;
            set { _contentPadding = value; Invalidate(); }
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

        protected override void OnEnabledChanged(EventArgs e)
        {
            Invalidate(); base.OnEnabledChanged(e);
        }

        protected override void OnTextChanged(EventArgs e)
        {
            Invalidate(); base.OnTextChanged(e);
        }

        // =====================================================
        // 绘制
        // =====================================================
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            var colors = AppTheme.Colors;
            GetColors(colors, out Color bg, out Color fg, out Color border);

            // 背景 + 边框
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = GraphicsExtensions.GetRoundPath(rect, _cornerRadius))
            {
                if (bg.A > 0)
                {
                    using var brush = new SolidBrush(bg);
                    g.FillPath(brush, path);
                }
                if (border.A > 0)
                {
                    using var pen = new Pen(border, 1f);
                    g.DrawPath(pen, path);
                }
            }

            // 图标 + 文字
            bool hasIcon = !string.IsNullOrEmpty(_icon);
            bool hasText = !string.IsNullOrEmpty(Text);

            var contentRect = new Rectangle(
                _contentPadding.Left,
                0,
                Width - _contentPadding.Left - _contentPadding.Right,
                Height);

            if (hasIcon && hasText)
            {
                var textSize = TextRenderer.MeasureText(Text, Font);
                int totalWidth = _iconSize + _iconTextGap + textSize.Width;
                int startX = contentRect.X + (contentRect.Width - totalWidth) / 2;
                if (startX < contentRect.X) startX = contentRect.X;

                var iconRect = new Rectangle(startX, (Height - _iconSize) / 2, _iconSize, _iconSize);
                IconRenderer.Draw(g, _icon, iconRect, fg, _iconSize);

                var textRect = new Rectangle(
                    startX + _iconSize + _iconTextGap,
                    0,
                    contentRect.Right - (startX + _iconSize + _iconTextGap),
                    Height);
                TextRenderer.DrawText(g, Text, Font, textRect, fg,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
            else if (hasIcon)
            {
                var iconRect = new Rectangle(
                    (Width - _iconSize) / 2,
                    (Height - _iconSize) / 2,
                    _iconSize,
                    _iconSize);
                IconRenderer.Draw(g, _icon, iconRect, fg, _iconSize);
            }
            else if (hasText)
            {
                TextRenderer.DrawText(g, Text, Font, contentRect, fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
        }

        // =====================================================
        // 取色（每个主题不同）
        // =====================================================
        private void GetColors(ThemeColors c, out Color bg, out Color fg, out Color border)
        {
            // 禁用
            if (!Enabled)
            {
                bg = c.ButtonBg;
                fg = c.TextDisabled;
                border = c.ButtonBorder;
                return;
            }

            switch (_style)
            {
                case FlatButtonStyle.Primary:
                    bg = _pressed ? c.AccentPressed
                       : _hover ? c.AccentHover
                                : c.Accent;
                    fg = c.AccentForeground;
                    border = Color.Transparent;
                    break;

                case FlatButtonStyle.Danger:
                    bg = _pressed ? c.DangerPressed
                       : _hover ? c.DangerHover
                                : c.Danger;
                    fg = c.DangerForeground;
                    border = Color.Transparent;
                    break;

                case FlatButtonStyle.Subtle:
                    bg = _pressed ? c.SubtlePressed
                       : _hover ? c.SubtleHover
                                : Color.Transparent;
                    fg = (_hover || _pressed) ? c.SubtleHoverForeground : c.SubtleForeground;
                    border = Color.Transparent;
                    break;

                case FlatButtonStyle.Secondary:
                default:
                    bg = _pressed ? c.ButtonPressed
                       : _hover ? c.ButtonHover
                                : c.ButtonBg;
                    fg = (_hover || _pressed) ? c.ButtonHoverForeground : c.ButtonForeground;
                    border = (_hover || _pressed) ? Color.Transparent : c.ButtonBorder;
                    break;
            }
        }
    }
}