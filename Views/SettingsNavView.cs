using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CourseApp.Localization;
using CourseApp.Theme;

namespace CourseApp.Views
{
    /// <summary>
    /// 设置左侧导航栏（Win11 风格）。
    /// 主题切换由 Form1 统一触发 Invalidate。
    /// </summary>
    public class SettingsNavView : Panel
    {
        public event Action<int>? ItemClicked;

        private readonly (string Icon, string Key)[] _items =
        {
            (Icons.Settings, "settings.section.system"),
            (Icons.Info,     "settings.section.personalization"),
            (Icons.Import,   "settings.section.apps"),
            (Icons.Info,     "settings.section.island"),
            (Icons.Settings, "settings.section.sidebar"),
            (Icons.Calendar, "settings.section.timelanguage"),
            (Icons.Info,     "settings.section.about"),
            (Icons.Delete,   "settings.section.danger"),
        };

        private readonly List<Rectangle> _rects = new();
        private int _selectedIndex = 1;
        private int _hoverIndex = -1;

        private const int ItemH = 40;
        private const int PadTop = 12;
        private const int PadLeft = 8;
        private const int PadRight = 8;

        public SettingsNavView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            Dock = DockStyle.Left;
            Width = 220;
            BackColor = AppTheme.Colors.WindowBg;

            Resize += (s, e) => RecalcRects();
            RecalcRects();
        }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (value < 0 || value >= _items.Length) return;
                if (_selectedIndex == value) return;
                _selectedIndex = value;
                Invalidate();
            }
        }

        public int ItemCount => _items.Length;

        private void RecalcRects()
        {
            _rects.Clear();
            int y = PadTop;
            int w = Width - PadLeft - PadRight;
            for (int i = 0; i < _items.Length; i++)
            {
                _rects.Add(new Rectangle(PadLeft, y, w, ItemH));
                y += ItemH + 2;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var colors = AppTheme.Colors;

            using (var bg = new SolidBrush(colors.WindowBg))
                g.FillRectangle(bg, ClientRectangle);

            for (int i = 0; i < _items.Length && i < _rects.Count; i++)
            {
                var rect = _rects[i];
                bool selected = (i == _selectedIndex);
                bool hover = (i == _hoverIndex);

                if (selected)
                {
                    var barRect = new Rectangle(rect.X - 3, rect.Y + 8, 3, rect.Height - 16);
                    using var barBrush = new SolidBrush(colors.Accent);
                    using var barPath = GraphicsExtensions.GetRoundPath(barRect, 2);
                    g.FillPath(barBrush, barPath);
                }

                if (selected || hover)
                {
                    Color bg = selected ? colors.SelectedBg : colors.HoverBg;
                    using var path = GraphicsExtensions.GetRoundPath(rect, 5);
                    using var brush = new SolidBrush(bg);
                    g.FillPath(brush, path);
                }

                // 图标
                int iconSize = 18;
                var iconRect = new Rectangle(rect.X + 12, rect.Y + (rect.Height - iconSize) / 2, iconSize, iconSize);
                IconRenderer.Draw(g, _items[i].Icon, iconRect, Color.Empty, iconSize);

                // 文字
                var textRect = new Rectangle(iconRect.Right + 12, rect.Y, rect.Width - iconRect.Right - 16, rect.Height);
                Color fg = selected ? colors.TextPrimary : colors.TextSecondary;

                TextRenderer.DrawText(g, I18n.T(_items[i].Key), AppTheme.BodyFont, textRect, fg,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int idx = HitTest(e.Location);
            if (idx != _hoverIndex) { _hoverIndex = idx; Invalidate(); }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hoverIndex = -1;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int idx = HitTest(e.Location);
                if (idx >= 0)
                {
                    _selectedIndex = idx;
                    Invalidate();
                    ItemClicked?.Invoke(idx);
                }
            }
            base.OnMouseDown(e);
        }

        private int HitTest(Point p)
        {
            for (int i = 0; i < _rects.Count; i++)
                if (_rects[i].Contains(p)) return i;
            return -1;
        }
    }
}