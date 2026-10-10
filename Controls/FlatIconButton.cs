using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// 只显示图标的按钮，悬停时改变背景色
    /// </summary>
    public class FlatIconButton : Control
    {
        private bool isHover;
        private bool isPressed;

        private string icon = "";
        private int iconSize = 16;
        private int cornerRadius = 4;
        private bool isCloseButton = false;

        /// <summary>
        /// 构造函数，初始化图标按钮基本属性
        /// </summary>
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

        /// <summary>圆角半径</summary>
        public int CornerRadius
        {
            get => cornerRadius;
            set { cornerRadius = value; Invalidate(); }
        }

        /// <summary>是否为关闭按钮，悬停时显示红色背景</summary>
        public bool IsCloseButton
        {
            get => isCloseButton;
            set { isCloseButton = value; Invalidate(); }
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

        /// <summary>绘制按钮外观和图标</summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var colors = AppTheme.Colors;
            Color background = Color.Transparent;

            if (isCloseButton)
            {
                if (isPressed) background = colors.DangerPressed;
                else if (isHover) background = colors.DangerHover;
            }
            else
            {
                if (isPressed) background = colors.ButtonPressed;
                else if (isHover) background = colors.HoverBg;
            }

            if (background.A > 0)
            {
                var rect = new Rectangle(0, 0, Width - 1, Height - 1);
                using var path = GraphicsExtensions.GetRoundPath(rect, cornerRadius);
                using var brush = new SolidBrush(background);
                g.FillPath(brush, path);
            }

            if (!string.IsNullOrEmpty(icon))
            {
                var iconRect = new Rectangle(
                    (Width - iconSize) / 2,
                    (Height - iconSize) / 2,
                    iconSize,
                    iconSize);
                IconRenderer.Draw(g, icon, iconRect, Color.Empty, iconSize);
            }
        }
    }
}