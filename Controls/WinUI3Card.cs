using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// WinUI 3 风格卡片：圆角、边框、悬停抬升。
    /// </summary>
    public class WinUI3Card : Panel
    {
        private bool _hover;
        private bool _hoverEnabled;

        /// <summary>是否启用悬停抬升效果</summary>
        public bool HoverEnabled
        {
            get => _hoverEnabled;
            set { _hoverEnabled = value; Invalidate(); }
        }

        /// <summary>是否使用强调色边框</summary>
        public bool AccentBorder { get; set; } = false;

        public WinUI3Card()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;
            Padding = new Padding(WinUI3Tokens.CardPadding);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            if (_hoverEnabled) { _hover = true; Invalidate(); }
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (_hoverEnabled) { _hover = false; Invalidate(); }
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var colors = AppTheme.Colors;

            int lift = (_hover && _hoverEnabled) ? WinUI3Tokens.HoverLift : 0;
            var rect = new Rectangle(0, -lift, Width - 1, Height - 1);

            using var path = GraphicsExtensions.GetRoundPath(rect, WinUI3Tokens.CardRadius);

            using (var bg = new SolidBrush(colors.CardBg))
                g.FillPath(bg, path);

            Color border = AccentBorder ? colors.Accent : colors.CardBorder;
            using (var pen = new Pen(border, 1f))
                g.DrawPath(pen, path);

            base.OnPaint(e);
        }
    }
}