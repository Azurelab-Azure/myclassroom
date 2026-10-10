using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// WinUI 3 风格徽章：等级 / 职务 / 健康状态。
    /// </summary>
    public class WinUI3InfoBadge : Control
    {
        public enum BadgeKind { Neutral, Success, Warning, Danger, Accent }

        private BadgeKind _kind = BadgeKind.Neutral;

        public BadgeKind Kind
        {
            get => _kind;
            set { _kind = value; Invalidate(); }
        }

        public WinUI3InfoBadge()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Height = 22;
            Font = AppTheme.SmallFont;
        }

        public void AutoSizeToText()
        {
            if (string.IsNullOrEmpty(Text)) { Width = 0; return; }
            var size = TextRenderer.MeasureText(Text, Font);
            Width = size.Width + 16;
        }

        protected override void OnTextChanged(EventArgs e) { AutoSizeToText(); Invalidate(); base.OnTextChanged(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var colors = AppTheme.Colors;

            (Color bg, Color fg) = _kind switch
            {
                BadgeKind.Success => (Color.FromArgb(40, 0x4C, 0xAF, 0x50), Color.FromArgb(0x2E, 0x7D, 0x32)),
                BadgeKind.Warning => (Color.FromArgb(40, 0xFF, 0xB9, 0x00), Color.FromArgb(0x8A, 0x6D, 0x00)),
                BadgeKind.Danger => (Color.FromArgb(40, 0xE8, 0x1B, 0x1B), Color.FromArgb(0xC4, 0x2B, 0x1C)),
                BadgeKind.Accent => (Color.FromArgb(40, colors.Accent), colors.Accent),
                _ => (colors.HoverBg, colors.TextSecondary),
            };

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = GraphicsExtensions.GetRoundPath(rect, WinUI3Tokens.BadgeRadius);
            using var brush = new SolidBrush(bg);
            g.FillPath(brush, path);

            TextRenderer.DrawText(g, Text, Font, rect, fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPrefix);
        }
    }
}