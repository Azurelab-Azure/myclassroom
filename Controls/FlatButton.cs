using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// 按钮样式
    /// Primary 主操作按钮，强调色背景
    /// Secondary 次操作按钮，卡片色背景加边框
    /// Subtle 弱化按钮，透明背景，悬停时显色
    /// Danger 危险操作按钮，红色背景
    /// </summary>
    public enum FlatButtonStyle
    {
        Primary,
        Secondary,
        Subtle,
        Danger,
    }

    /// <summary>
    /// 自绘按钮控件，主题切换由 Form1 统一触发重绘
    /// </summary>
    public class FlatButton : Control
    {
        private bool isHover;
        private bool isPressed;

        private FlatButtonStyle buttonStyle = FlatButtonStyle.Secondary;
        private int cornerRadius = 6;
        private string icon = "";
        private int iconSize = 16;
        private int iconTextGap = 6;
        private Padding contentPadding = new Padding(12, 0, 12, 0);

        /// <summary>
        /// 构造函数，初始化按钮基本属性
        /// </summary>
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

        /// <summary>按钮样式</summary>
        public FlatButtonStyle ButtonStyle
        {
            get => buttonStyle;
            set { buttonStyle = value; Invalidate(); }
        }

        /// <summary>圆角半径</summary>
        public int CornerRadius
        {
            get => cornerRadius;
            set { cornerRadius = value; Invalidate(); }
        }

        /// <summary>图标</summary>
        public string Icon
        {
            get => icon;
            set { icon = value ?? ""; Invalidate(); }
        }

        /// <summary>图标尺寸</summary>
        public int IconSize
        {
            get => iconSize;
            set { iconSize = value; Invalidate(); }
        }

        /// <summary>图标与文字间距</summary>
        public int IconTextGap
        {
            get => iconTextGap;
            set { iconTextGap = value; Invalidate(); }
        }

        /// <summary>内容边距</summary>
        public Padding ContentPadding
        {
            get => contentPadding;
            set { contentPadding = value; Invalidate(); }
        }

        /// <summary>鼠标进入时标记悬停状态</summary>
        protected override void OnMouseEnter(EventArgs e)
        {
            isHover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        /// <summary>鼠标离开时清除悬停和按下状态</summary>
        protected override void OnMouseLeave(EventArgs e)
        {
            isHover = false;
            isPressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        /// <summary>鼠标按下时标记按下状态</summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { isPressed = true; Invalidate(); }
            base.OnMouseDown(e);
        }

        /// <summary>鼠标抬起时清除按下状态</summary>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (isPressed) { isPressed = false; Invalidate(); }
            base.OnMouseUp(e);
        }

        /// <summary>可用状态变化时重绘</summary>
        protected override void OnEnabledChanged(EventArgs e)
        {
            Invalidate();
            base.OnEnabledChanged(e);
        }

        /// <summary>文字变化时重绘</summary>
        protected override void OnTextChanged(EventArgs e)
        {
            Invalidate();
            base.OnTextChanged(e);
        }

        /// <summary>绘制按钮外观和内容</summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            var colors = AppTheme.Colors;
            GetColors(colors, out Color background, out Color foreground, out Color border);

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = GraphicsExtensions.GetRoundPath(rect, cornerRadius))
            {
                if (background.A > 0)
                {
                    using var brush = new SolidBrush(background);
                    g.FillPath(brush, path);
                }
                if (border.A > 0)
                {
                    using var pen = new Pen(border, 1f);
                    g.DrawPath(pen, path);
                }
            }

            bool hasIcon = !string.IsNullOrEmpty(icon);
            bool hasText = !string.IsNullOrEmpty(Text);

            var contentRect = new Rectangle(
                contentPadding.Left, 0,
                Width - contentPadding.Left - contentPadding.Right,
                Height);

            if (hasIcon && hasText)
            {
                var textSize = TextRenderer.MeasureText(Text, Font);
                int totalWidth = iconSize + iconTextGap + textSize.Width;
                int startX = contentRect.X + (contentRect.Width - totalWidth) / 2;
                if (startX < contentRect.X) startX = contentRect.X;

                var iconRect = new Rectangle(startX, (Height - iconSize) / 2, iconSize, iconSize);
                IconRenderer.Draw(g, icon, iconRect, foreground, iconSize);

                var textRect = new Rectangle(
                    startX + iconSize + iconTextGap, 0,
                    contentRect.Right - (startX + iconSize + iconTextGap),
                    Height);
                TextRenderer.DrawText(g, Text, Font, textRect, foreground,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
            else if (hasIcon)
            {
                var iconRect = new Rectangle(
                    (Width - iconSize) / 2, (Height - iconSize) / 2,
                    iconSize, iconSize);
                IconRenderer.Draw(g, icon, iconRect, foreground, iconSize);
            }
            else if (hasText)
            {
                TextRenderer.DrawText(g, Text, Font, contentRect, foreground,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
        }

        /// <summary>根据按钮样式和状态获取配色</summary>
        private void GetColors(ThemeColors colors, out Color background, out Color foreground, out Color border)
        {
            if (!Enabled)
            {
                background = colors.ButtonBg;
                foreground = colors.TextDisabled;
                border = colors.ButtonBorder;
                return;
            }

            switch (buttonStyle)
            {
                case FlatButtonStyle.Primary:
                    background = isPressed ? colors.AccentPressed
                               : isHover ? colors.AccentHover
                                         : colors.Accent;
                    foreground = colors.AccentForeground;
                    border = Color.Transparent;
                    break;

                case FlatButtonStyle.Danger:
                    background = isPressed ? colors.DangerPressed
                               : isHover ? colors.DangerHover
                                         : colors.Danger;
                    foreground = colors.DangerForeground;
                    border = Color.Transparent;
                    break;

                case FlatButtonStyle.Subtle:
                    background = isPressed ? colors.SubtlePressed
                               : isHover ? colors.SubtleHover
                                         : Color.Transparent;
                    foreground = (isHover || isPressed) ? colors.SubtleHoverForeground : colors.SubtleForeground;
                    border = Color.Transparent;
                    break;

                case FlatButtonStyle.Secondary:
                default:
                    background = isPressed ? colors.ButtonPressed
                               : isHover ? colors.ButtonHover
                                         : colors.ButtonBg;
                    foreground = (isHover || isPressed) ? colors.ButtonHoverForeground : colors.ButtonForeground;
                    border = (isHover || isPressed) ? Color.Transparent : colors.ButtonBorder;
                    break;
            }
        }
    }
}