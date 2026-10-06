using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace CourseApp.Theme
{
    /// <summary>
    /// 绘制 SVG path 图标。
    /// Icons.Get 返回 "P:&lt;path&gt;" 格式，交给这里解析并绘制。
    /// </summary>
    public static class IconRenderer
    {
        /// <summary>
        /// 绘制图标。
        /// </summary>
        /// <param name="icon">"P:...path..." 字符串</param>
        /// <param name="rect">目标矩形</param>
        /// <param name="color">线条颜色（传 Color.Empty 用主题主文字色）</param>
        /// <param name="size">（兼容参数，忽略）</param>
        public static void Draw(Graphics g, string icon, Rectangle rect, Color color, float size = 14f)
        {
            if (string.IsNullOrEmpty(icon)) return;
            if (!icon.StartsWith("P:")) return;

            var pathData = icon.Substring(2);
            if (string.IsNullOrEmpty(pathData)) return;

            // ★ 空颜色 → 用主题主文字色（原 PNG 版图标自带颜色，现在 SVG 只有路径，
            //   颜色完全由这里决定；调用方大多传 Color.Empty，需回退到 TextPrimary）
            if (color.IsEmpty || color.A == 0)
                color = AppTheme.Colors.TextPrimary;

            try
            {
                // SvgPathParser 解析——源 24×24，缩放到目标 rect
                using var path = SvgPathParser.Parse(pathData, rect.Width, rect.Height, 24);

                // 平移到 rect 位置
                using var m = new Matrix();
                m.Translate(rect.X, rect.Y);
                path.Transform(m);

                g.SmoothingMode = SmoothingMode.AntiAlias;

                // 线条宽度按尺寸缩放（24×24 画布 → 2px）
                float scale = Math.Min(rect.Width, rect.Height) / 24f;
                float lineWidth = Math.Max(1.2f, 2f * scale);

                using var pen = new Pen(color, lineWidth)
                {
                    LineJoin = LineJoin.Round,
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                };

                g.DrawPath(pen, path);
            }
            catch
            {
                // 解析失败静默
            }
        }
    }
}