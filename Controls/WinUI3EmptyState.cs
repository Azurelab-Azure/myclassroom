using System;
using System.Drawing;
using System.Windows.Forms;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// WinUI 3 风格空状态占位。
    /// </summary>
    public class WinUI3EmptyState : Control
    {
        public string Icon { get; set; } = "";
        public string TitleText { get; set; } = "";
        public string Description { get; set; } = "";

        public WinUI3EmptyState()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var colors = AppTheme.Colors;

            int centerY = Height / 2 - 30;

            if (!string.IsNullOrEmpty(Icon))
            {
                var iconRect = new Rectangle((Width - 48) / 2, centerY - 60, 48, 48);
                IconRenderer.Draw(g, Icon, iconRect, colors.TextDisabled, 48);
            }

            using var titleFont = new Font(AppTheme.BodyFont.FontFamily, 16f, FontStyle.Bold);
            var titleRect = new Rectangle(0, centerY, Width, 30);
            TextRenderer.DrawText(g, TitleText, titleFont, titleRect, colors.TextSecondary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPrefix);

            var descRect = new Rectangle(0, centerY + 34, Width, 24);
            TextRenderer.DrawText(g, Description, AppTheme.SmallFont, descRect, colors.TextDisabled,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPrefix);
        }
    }
}