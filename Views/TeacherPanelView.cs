using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CourseApp.Controls;
using CourseApp.Localization;
using CourseApp.Models;
using CourseApp.Theme;

namespace CourseApp.Views
{
    /// <summary>
    /// 左侧教师面板：搜索框 + 教师卡片列表。
    /// 主题切换由 Form1 统一触发 Invalidate。
    /// </summary>
    public class TeacherPanelView : Panel
    {
        private Label _titleLabel = null!;
        private FlatTextBox _searchBox = null!;
        private FlatScrollBar _scrollBar = null!;
        private Panel _listHost = null!;

        private List<Teacher> _teachers = new();
        private List<TeacherCard> _cards = new();
        private int _scrollY = 0;
        private const int CardHeight = 76;
        private const int CardGap = 8;

        public event Action<Teacher>? TeacherClicked;
        public event Action<Teacher>? TeacherDoubleClicked;

        public TeacherPanelView()
        {
            Dock = DockStyle.Left;
            Width = 260;
            BackColor = AppTheme.Colors.CardBg;

            BuildUI();
        }

        private void BuildUI()
        {
            _titleLabel = new Label
            {
                Text = I18n.T("teacher.title"),
                Font = AppTheme.TitleFont,
                ForeColor = AppTheme.Colors.TextPrimary,
                Left = 16,
                Top = 16,
                AutoSize = true,
                BackColor = Color.Transparent,
            };
            Controls.Add(_titleLabel);

            _searchBox = new FlatTextBox
            {
                Left = 16,
                Top = 50,
                Width = Width - 32 - 8,
                Height = 32,
                Placeholder = I18n.T("teacher.search"),
            };
            _searchBox.TextChanged += (s, e) => RenderCards();
            Controls.Add(_searchBox);

            _listHost = new Panel
            {
                Left = 16,
                Top = 96,
                Width = Width - 32 - 12,
                Height = Height - 96 - 16,
                BackColor = AppTheme.Colors.CardBg,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            };
            _listHost.Paint += ListHost_Paint;
            _listHost.MouseDown += ListHost_MouseDown;
            _listHost.MouseDoubleClick += ListHost_MouseDoubleClick;
            _listHost.MouseWheel += ListHost_MouseWheel;
            Controls.Add(_listHost);

            _scrollBar = new FlatScrollBar
            {
                Left = Width - 16 - 8,
                Top = 96,
                Width = 8,
                Height = Height - 96 - 16,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right,
            };
            _scrollBar.ValueChanged += (s, e) =>
            {
                _scrollY = _scrollBar.Value;
                _listHost.Invalidate();
            };
            Controls.Add(_scrollBar);

            Resize += (s, e) => LayoutOnResize();
        }

        private void LayoutOnResize()
        {
            _searchBox.Width = Width - 32 - 8;
            _listHost.Width = Width - 32 - 12;
            _listHost.Height = Height - 96 - 16;
            _scrollBar.Left = Width - 16 - 8;
            _scrollBar.Height = Height - 96 - 16;
        }

        public void SetTeachers(List<Teacher> teachers)
        {
            _teachers = teachers ?? new List<Teacher>();
            foreach (var c in _cards) c.Dispose();
            _cards.Clear();
            foreach (var t in _teachers)
                _cards.Add(new TeacherCard(t));
            RenderCards();
        }

        private void RenderCards()
        {
            string keyword = _searchBox.Text.Trim();
            _scrollY = 0;
            int contentH = 0;
            foreach (var c in _cards)
            {
                bool visible = string.IsNullOrEmpty(keyword)
                    || (c.Teacher.Name ?? "").Contains(keyword)
                    || (c.Teacher.Info ?? "").Contains(keyword);
                c.Visible = visible;
                if (visible) contentH += CardHeight + CardGap;
            }

            _scrollBar.Maximum = Math.Max(contentH, _listHost.Height);
            _scrollBar.LargeChange = _listHost.Height;
            _scrollBar.Value = 0;
            _listHost.Invalidate();
        }

        private void ListHost_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var bg = new SolidBrush(AppTheme.Colors.CardBg))
                g.FillRectangle(bg, _listHost.ClientRectangle);

            int y = -_scrollY;
            foreach (var card in _cards)
            {
                if (!card.Visible) continue;
                var rect = new Rectangle(0, y, _listHost.Width, CardHeight);
                if (rect.Bottom > 0 && rect.Top < _listHost.Height)
                    card.Draw(g, rect);
                y += CardHeight + CardGap;
            }
        }

        private Teacher? HitTest(Point p)
        {
            int y = -_scrollY;
            foreach (var card in _cards)
            {
                if (!card.Visible) continue;
                var rect = new Rectangle(0, y, _listHost.Width, CardHeight);
                if (rect.Contains(p)) return card.Teacher;
                y += CardHeight + CardGap;
            }
            return null;
        }

        private void ListHost_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var t = HitTest(e.Location);
                if (t != null) TeacherClicked?.Invoke(t);
            }
            else if (e.Button == MouseButtons.Right)
            {
                var t = HitTest(e.Location);
                if (t != null)
                {
                    var menu = new FlatContextMenu();
                    menu.AddItem(FlatMenuItem.Create(I18n.T("menu.viewDetails"),
                        () => TeacherDoubleClicked?.Invoke(t), Icons.Person));
                    var screenPt = _listHost.PointToScreen(e.Location);
                    menu.ShowAt(_listHost, screenPt);
                }
            }
        }

        private void ListHost_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            var t = HitTest(e.Location);
            if (t != null) TeacherDoubleClicked?.Invoke(t);
        }

        private void ListHost_MouseWheel(object? sender, MouseEventArgs e)
        {
            int contentH = 0;
            foreach (var c in _cards) if (c.Visible) contentH += CardHeight + CardGap;
            int maxScroll = Math.Max(0, contentH - _listHost.Height);
            _scrollY -= Math.Sign(e.Delta) * 60;
            if (_scrollY < 0) _scrollY = 0;
            if (_scrollY > maxScroll) _scrollY = maxScroll;
            _scrollBar.Value = _scrollY;
            _scrollBar.Wake();
            _listHost.Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                foreach (var c in _cards) c.Dispose();
            base.Dispose(disposing);
        }

        // =====================================================
        // 内部：教师卡片
        // =====================================================
        private class TeacherCard : IDisposable
        {
            public Teacher Teacher { get; }
            public bool Visible { get; set; } = true;

            private Image? _photo;

            public TeacherCard(Teacher t)
            {
                Teacher = t;
                LoadPhoto();
            }

            private void LoadPhoto()
            {
                try
                {
                    if (string.IsNullOrEmpty(Teacher.Photo)) return;
                    if (!File.Exists(Teacher.Photo)) return;
                    using var fs = new FileStream(Teacher.Photo, FileMode.Open, FileAccess.Read, FileShare.Read);
                    _photo = Image.FromStream(fs);
                }
                catch { }
            }

            public void Draw(Graphics g, Rectangle rect)
            {
                var colors = AppTheme.Colors;

                using (var path = GraphicsExtensions.GetRoundPath(
                    new Rectangle(rect.X, rect.Y, rect.Width - 1, rect.Height - 1), 8))
                {
                    using var bg = new SolidBrush(colors.CardBg);
                    g.FillPath(bg, path);

                    using var pen = new Pen(colors.CardBorder, 1f);
                    g.DrawPath(pen, path);
                }

                int pad = 10;
                int avatarSize = rect.Height - pad * 2;
                var avatarRect = new Rectangle(rect.X + pad, rect.Y + pad, avatarSize, avatarSize);

                DrawAvatar(g, avatarRect);

                int textLeft = avatarRect.Right + 10;
                int textWidth = rect.Right - textLeft - pad;

                var nameRect = new Rectangle(textLeft, rect.Y + pad, textWidth, avatarSize / 2);
                TextRenderer.DrawText(g, Teacher.Name ?? "", AppTheme.BodyFont, nameRect,
                    colors.TextPrimary,
                    TextFormatFlags.Left | TextFormatFlags.Bottom |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                var infoRect = new Rectangle(textLeft, rect.Y + pad + avatarSize / 2, textWidth, avatarSize / 2);
                TextRenderer.DrawText(g, Teacher.Info ?? "", AppTheme.SmallFont, infoRect,
                    colors.TextSecondary,
                    TextFormatFlags.Left | TextFormatFlags.Top |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }

            private void DrawAvatar(Graphics g, Rectangle rect)
            {
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
                    using var bg = new SolidBrush(AppTheme.Colors.HoverBg);
                    g.FillPath(bg, path);

                    string initial = string.IsNullOrEmpty(Teacher.Name) ? "?" : Teacher.Name.Substring(0, 1);
                    using var font = new Font(AppTheme.BodyFont.FontFamily, rect.Height * 0.4f, FontStyle.Bold);
                    TextRenderer.DrawText(g, initial, font, rect, AppTheme.Colors.TextSecondary,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                }
            }

            public void Dispose()
            {
                _photo?.Dispose();
                _photo = null;
            }
        }
    }
}