using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// WinUI 3 风格评分条 / 星级。
    /// </summary>
    public class WinUI3Rating : Control
    {
        private int _value = 0;
        private int _max = 100;
        private bool _showStars = false;

        public int Value
        {
            get => _value;
            set { _value = Math.Max(0, Math.Min(_max, value)); Invalidate(); }
        }

        public int Max
        {
            get => _max;
            set { _max = Math.Max(1, value); Invalidate(); }
        }

        public bool ShowStars
        {
            get => _showStars;
            set { _showStars = value; Invalidate(); }
        }

        public WinUI3Rating()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Height = 20;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var colors = AppTheme.Colors;

            if (_showStars)
            {
                int stars = 5;
                int starSize = Math.Min(Height, Width / stars);
                float ratio = (float)_value / _max;

                for (int i = 0; i < stars; i++)
                {
                    var rect = new Rectangle(i * starSize, (Height - starSize) / 2, starSize - 2, starSize - 2);
                    bool filled = (i + 1) <= ratio * stars + 0.5f;
                    DrawStar(g, rect, filled ? Color.FromArgb(0xFF, 0xB9, 0x00) : colors.Divider);
                }
                return;
            }

            var barRect = new Rectangle(0, (Height - 6) / 2, Width, 6);
            using (var bgPath = GraphicsExtensions.GetRoundPath(barRect, 3))
            using (var bgBrush = new SolidBrush(colors.Divider))
                g.FillPath(bgBrush, bgPath);

            int fillW = (int)(Width * ((float)_value / _max));
            if (fillW > 0)
            {
                var fillRect = new Rectangle(0, (Height - 6) / 2, fillW, 6);
                using var fillPath = GraphicsExtensions.GetRoundPath(fillRect, 3);
                using var fillBrush = new SolidBrush(ValueColor());
                g.FillPath(fillBrush, fillPath);
            }
        }

        private Color ValueColor()
        {
            float ratio = (float)_value / _max;
            if (ratio >= 0.85f) return Color.FromArgb(0x4C, 0xAF, 0x50);
            if (ratio >= 0.7f) return Color.FromArgb(0x2E, 0x86, 0xE8);
            if (ratio >= 0.6f) return Color.FromArgb(0xFF, 0xB9, 0x00);
            return Color.FromArgb(0xE8, 0x1B, 0x1B);
        }

        private static void DrawStar(Graphics g, Rectangle rect, Color color)
        {
            var center = new PointF(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);
            float r = Math.Min(rect.Width, rect.Height) / 2f;
            float inner = r * 0.45f;
            var pts = new PointF[10];
            for (int i = 0; i < 10; i++)
            {
                float angle = (float)(Math.PI / 5 * i - Math.PI / 2);
                float radius = (i % 2 == 0) ? r : inner;
                pts[i] = new PointF(center.X + radius * (float)Math.Cos(angle),
                                    center.Y + radius * (float)Math.Sin(angle));
            }
            using var brush = new SolidBrush(color);
            g.FillPolygon(brush, pts);
        }
    }
}