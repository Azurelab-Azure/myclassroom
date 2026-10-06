using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using CourseApp.Models;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// 老师名片项（用于下拉框 / 列表）。
    /// 布局：左侧方形圆角头像，右侧姓名 + 信息。
    /// 高度 64px。
    /// </summary>
    public class TeacherItem : IFlatComboItem, IDisposable
    {
        public Teacher Source { get; }
        private Image? _photo;

        public int Height => 64;
        public string DisplayText => Source.Name;

        public TeacherItem(Teacher teacher)
        {
            Source = teacher ?? throw new ArgumentNullException(nameof(teacher));
            LoadPhoto();
        }

        private void LoadPhoto()
        {
            try
            {
                if (string.IsNullOrEmpty(Source.Photo)) return;
                if (!File.Exists(Source.Photo)) return;

                using var fs = new FileStream(Source.Photo, FileMode.Open, FileAccess.Read, FileShare.Read);
                _photo = Image.FromStream(fs);
            }
            catch { /* 加载失败，_photo 保持 null */ }
        }

        public void Draw(Graphics g, Rectangle rect, bool hover, bool selected)
        {
            var colors = AppTheme.Colors;

            // 背景
            Color bgColor;
            if (selected) bgColor = colors.SelectedBg;
            else if (hover) bgColor = colors.HoverBg;
            else bgColor = Color.Transparent;

            if (bgColor.A > 0)
            {
                using var brush = new SolidBrush(bgColor);
                g.FillRectangle(brush, rect);
            }

            const int pad = 8;
            int avatarSize = rect.Height - pad * 2;
            var avatarRect = new Rectangle(rect.X + pad, rect.Y + pad, avatarSize, avatarSize);

            DrawAvatar(g, avatarRect, colors);

            // 文字区
            int textLeft = avatarRect.Right + 10;
            int textWidth = rect.Right - textLeft - pad;
            if (textWidth < 10) return;

            // 姓名
            var nameRect = new Rectangle(textLeft, rect.Y + pad, textWidth, avatarSize / 2 + 2);
            TextRenderer.DrawText(g, Source.Name ?? "", AppTheme.BodyFont, nameRect,
                colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.Bottom |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            // 信息
            var infoRect = new Rectangle(textLeft, rect.Y + pad + avatarSize / 2, textWidth, avatarSize / 2);
            TextRenderer.DrawText(g, Source.Info ?? "", AppTheme.SmallFont, infoRect,
                colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.Top |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        private void DrawAvatar(Graphics g, Rectangle rect, ThemeColors colors)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using var path = GraphicsExtensions.GetRoundPath(rect, 6);

            if (_photo != null)
            {
                var old = g.Clip;
                g.SetClip(path);
                try
                {
                    float scale = Math.Max((float)rect.Width / _photo.Width, (float)rect.Height / _photo.Height);
                    int w = (int)(_photo.Width * scale);
                    int h = (int)(_photo.Height * scale);
                    int x = rect.X + (rect.Width - w) / 2;
                    int y = rect.Y + (rect.Height - h) / 2;
                    g.DrawImage(_photo, new Rectangle(x, y, w, h));
                }
                finally { g.Clip = old; }
            }
            else
            {
                using var bg = new SolidBrush(colors.HoverBg);
                g.FillPath(bg, path);

                string initial = string.IsNullOrEmpty(Source.Name) ? "?" : Source.Name.Substring(0, 1);
                using var font = new Font(AppTheme.BodyFont.FontFamily, rect.Height * 0.4f, FontStyle.Bold);
                TextRenderer.DrawText(g, initial, font, rect, colors.TextSecondary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix);
            }
        }

        public void Dispose()
        {
            _photo?.Dispose();
            _photo = null;
        }
    }
}