using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// WinUI 3 风格左侧导航项：图标 + 文字 + 选中指示条。
    /// </summary>
    public class WinUI3NavItem : Control
    {
        private bool _hover;
        private bool _selected;

        public string Icon { get; set; } = "";
        public new string Text { get; set; } = "";
        public int BadgeCount { get; set; } = 0;

        public event Action? Clicked;

        public bool Selected
        {
            get => _selected;
            set { _selected = value; Invalidate(); }
        }

        public WinUI3NavItem()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;
            Height = 40;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) Clicked?.Invoke();
            base.OnMouseDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var colors = AppTheme.Colors;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            if (_selected || _hover)
            {
                Color bg = _selected ? colors.SelectedBg : colors.HoverBg;
                using var path = GraphicsExtensions.GetRoundPath(rect, WinUI3Tokens.ControlRadius);
                using var brush = new SolidBrush(bg);
                g.FillPath(brush, path);
            }

            if (_selected)
            {
                var barRect = new Rectangle(0, 8, 3, Height - 16);
                using var barBrush = new SolidBrush(colors.Accent);
                using var barPath = GraphicsExtensions.GetRoundPath(barRect, 2);
                g.FillPath(barBrush, barPath);
            }

            int iconSize = 18;
            var iconRect = new Rectangle(12, (Height - iconSize) / 2, iconSize, iconSize);
            IconRenderer.Draw(g, Icon, iconRect, Color.Empty, iconSize);

            int rightPad = BadgeCount > 0 ? 44 : 12;
            var textRect = new Rectangle(iconRect.Right + 10, 0, Width - iconRect.Right - 10 - rightPad, Height);
            Color fg = _selected ? colors.TextPrimary : colors.TextSecondary;
            TextRenderer.DrawText(g, Text, AppTheme.BodyFont, textRect, fg,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            if (BadgeCount > 0)
            {
                string badge = BadgeCount > 99 ? "99+" : BadgeCount.ToString();
                using var badgeFont = new Font(AppTheme.BodyFont.FontFamily, 10f, FontStyle.Bold);
                var size = TextRenderer.MeasureText(g, badge, badgeFont);
                int bw = Math.Max(18, size.Width + 10);
                var badgeRect = new Rectangle(Width - bw - 10, (Height - 18) / 2, bw, 18);

                using var badgeBrush = new SolidBrush(colors.Accent);
                using var badgePath = GraphicsExtensions.GetRoundPath(badgeRect, 9);
                g.FillPath(badgeBrush, badgePath);

                TextRenderer.DrawText(g, badge, badgeFont, badgeRect, colors.AccentForeground,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
        }
    }
}