using System.Drawing;
using System.Drawing.Drawing2D;

namespace CourseApp.Theme
{
    /// <summary>
    /// GDI+ 绘图扩展方法。所有自绘控件的公用绘图工具。
    /// </summary>
    public static class GraphicsExtensions
    {
        /// <summary>
        /// 生成圆角矩形路径。
        /// 尺寸非法（宽或高 ≤ 0）时返回空路径，避免 AddArc 崩溃。
        /// </summary>
        public static GraphicsPath GetRoundPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();

            // 尺寸保护
            if (rect.Width <= 0 || rect.Height <= 0)
                return path;

            if (radius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            int d = radius * 2;
            if (d > rect.Width) d = rect.Width;
            if (d > rect.Height) d = rect.Height;

            // 缩小后 d 若 ≤ 0，退化成矩形
            if (d <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>绘制居中文字</summary>
        public static void DrawTextCentered(Graphics g, string text, Font font, Color color, Rectangle rect)
        {
            TextRenderer.DrawText(g, text ?? "", font, rect, color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        /// <summary>绘制左对齐单行文字（垂直居中）</summary>
        public static void DrawTextLeft(Graphics g, string text, Font font, Color color, Rectangle rect)
        {
            TextRenderer.DrawText(g, text ?? "", font, rect, color,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        /// <summary>绘制多行文字（自动换行）</summary>
        public static void DrawTextWrap(Graphics g, string text, Font font, Color color, Rectangle rect,
            TextFormatFlags extra = TextFormatFlags.NoPrefix)
        {
            TextRenderer.DrawText(g, text ?? "", font, rect, color,
                TextFormatFlags.Left | TextFormatFlags.Top |
                TextFormatFlags.WordBreak | extra);
        }
    }
}