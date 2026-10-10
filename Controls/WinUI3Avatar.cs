using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// WinUI 3 风格头像：照片 / 首字母 / 状态点。
    /// </summary>
    public class WinUI3Avatar : Control
    {
        private string _photoPath = "";
        private Image? _image;
        private string _initial = "?";
        private Color _statusColor = Color.Transparent;

        public string PhotoPath
        {
            get => _photoPath;
            set { _photoPath = value ?? ""; LoadImage(); Invalidate(); }
        }

        public string Initial
        {
            get => _initial;
            set { _initial = string.IsNullOrEmpty(value) ? "?" : value.Substring(0, 1); Invalidate(); }
        }

        public Color StatusColor
        {
            get => _statusColor;
            set { _statusColor = value; Invalidate(); }
        }

        public WinUI3Avatar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(WinUI3Tokens.AvatarMedium, WinUI3Tokens.AvatarMedium);
        }

        private void LoadImage()
        {
            try
            {
                _image?.Dispose();
                _image = null;
                if (!string.IsNullOrEmpty(_photoPath) && File.Exists(_photoPath))
                {
                    using var fs = new FileStream(_photoPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    _image = Image.FromStream(fs);
                }
            }
            catch { _image = null; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _image?.Dispose(); _image = null; }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var colors = AppTheme.Colors;

            int size = Math.Min(Width, Height);
            var rect = new Rectangle((Width - size) / 2, (Height - size) / 2, size - 1, size - 1);

            using var path = GraphicsExtensions.GetRoundPath(rect, size / 2);

            if (_image != null)
            {
                var old = g.Clip;
                g.SetClip(path);
                try
                {
                    float scale = Math.Max((float)rect.Width / _image.Width, (float)rect.Height / _image.Height);
                    int w = (int)(_image.Width * scale);
                    int h = (int)(_image.Height * scale);
                    int x = rect.X + (rect.Width - w) / 2;
                    int y = rect.Y + (rect.Height - h) / 2;
                    g.DrawImage(_image, new Rectangle(x, y, w, h));
                }
                finally { g.Clip = old; }
            }
            else
            {
                using var bg = new SolidBrush(colors.HoverBg);
                g.FillPath(bg, path);

                using var font = new Font(AppTheme.BodyFont.FontFamily, size * 0.4f, FontStyle.Bold);
                TextRenderer.DrawText(g, _initial, font, rect, colors.TextSecondary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix);
            }

            if (_statusColor.A > 0)
            {
                int dot = Math.Max(8, size / 4);
                var dotRect = new Rectangle(rect.Right - dot + 1, rect.Bottom - dot + 1, dot, dot);
                using var dotBrush = new SolidBrush(_statusColor);
                using var dotPath = GraphicsExtensions.GetRoundPath(dotRect, dot / 2);
                g.FillPath(dotBrush, dotPath);
            }
        }
    }
}