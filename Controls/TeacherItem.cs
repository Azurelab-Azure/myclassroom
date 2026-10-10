using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using CourseApp.Models;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// 教师名片项，左侧头像右侧姓名与信息，高度 64 像素
    /// </summary>
    public class TeacherItem : IFlatComboItem, IDisposable
    {
        /// <summary>关联的教师数据</summary>
        public Teacher Source { get; }

        private Image? photoImage;

        /// <summary>项高度</summary>
        public int Height => 64;

        /// <summary>显示文本</summary>
        public string DisplayText => Source.Name;

        /// <summary>
        /// 构造函数，加载教师照片
        /// </summary>
        public TeacherItem(Teacher teacher)
        {
            Source = teacher ?? throw new ArgumentNullException(nameof(teacher));
            LoadPhoto();
        }

        /// <summary>从文件加载教师照片</summary>
        private void LoadPhoto()
        {
            try
            {
                if (string.IsNullOrEmpty(Source.Photo)) return;
                if (!File.Exists(Source.Photo)) return;

                using var fileStream = new FileStream(Source.Photo, FileMode.Open, FileAccess.Read, FileShare.Read);
                photoImage = Image.FromStream(fileStream);
            }
            catch { }
        }

        /// <summary>绘制教师名片</summary>
        public void Draw(Graphics g, Rectangle rect, bool hover, bool selected)
        {
            var colors = AppTheme.Colors;

            Color background;
            if (selected) background = colors.SelectedBg;
            else if (hover) background = colors.HoverBg;
            else background = Color.Transparent;

            if (background.A > 0)
            {
                using var brush = new SolidBrush(background);
                g.FillRectangle(brush, rect);
            }

            const int Padding = 8;
            int avatarSize = rect.Height - Padding * 2;
            var avatarRect = new Rectangle(rect.X + Padding, rect.Y + Padding, avatarSize, avatarSize);

            DrawAvatar(g, avatarRect, colors);

            int textLeft = avatarRect.Right + 10;
            int textWidth = rect.Right - textLeft - Padding;
            if (textWidth < 10) return;

            var nameRect = new Rectangle(textLeft, rect.Y + Padding, textWidth, avatarSize / 2 + 2);
            TextRenderer.DrawText(g, Source.Name ?? "", AppTheme.BodyFont, nameRect,
                colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.Bottom |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            var infoRect = new Rectangle(textLeft, rect.Y + Padding + avatarSize / 2, textWidth, avatarSize / 2);
            TextRenderer.DrawText(g, Source.Info ?? "", AppTheme.SmallFont, infoRect,
                colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.Top |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        /// <summary>绘制圆形头像或首字母占位</summary>
        private void DrawAvatar(Graphics g, Rectangle rect, ThemeColors colors)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using var path = GraphicsExtensions.GetRoundPath(rect, 6);

            if (photoImage != null)
            {
                var oldClip = g.Clip;
                g.SetClip(path);
                try
                {
                    float scale = Math.Max((float)rect.Width / photoImage.Width,
                                           (float)rect.Height / photoImage.Height);
                    int width = (int)(photoImage.Width * scale);
                    int height = (int)(photoImage.Height * scale);
                    int x = rect.X + (rect.Width - width) / 2;
                    int y = rect.Y + (rect.Height - height) / 2;
                    g.DrawImage(photoImage, new Rectangle(x, y, width, height));
                }
                finally { g.Clip = oldClip; }
            }
            else
            {
                using var background = new SolidBrush(colors.HoverBg);
                g.FillPath(background, path);

                string initial = string.IsNullOrEmpty(Source.Name) ? "?" : Source.Name.Substring(0, 1);
                using var font = new Font(AppTheme.BodyFont.FontFamily, rect.Height * 0.4f, FontStyle.Bold);
                TextRenderer.DrawText(g, initial, font, rect, colors.TextSecondary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix);
            }
        }

        /// <summary>释放照片资源</summary>
        public void Dispose()
        {
            photoImage?.Dispose();
            photoImage = null;
        }
    }
}