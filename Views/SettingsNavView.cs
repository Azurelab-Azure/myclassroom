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
    /// 7 项：系统 / 个性化 / 应用 / 悬浮组件 / 时间和语言 / 关于 / 危险区。
    /// </summary>
    public class SettingsNavView : Panel
    {
        /// <summary>导航项点击事件，参数为项索引</summary>
        public event Action<int>? ItemClicked;

        /// <summary>导航项定义：图标短名 + 翻译 key</summary>
        private readonly (string Icon, string Key)[] _items =
        {
            (Icons.Settings, "settings.section.system"),
            (Icons.Info,     "settings.section.personalization"),
            (Icons.Import,   "settings.section.apps"),
            (Icons.Info,     "settings.section.overlay"),
            (Icons.Calendar, "settings.section.timelanguage"),
            (Icons.Info,     "settings.section.about"),
            (Icons.Delete,   "settings.section.danger"),
        };

        /// <summary>每项的命中矩形</summary>
        private readonly List<Rectangle> _rects = new();

        /// <summary>当前选中索引</summary>
        private int _selectedIndex = 1;

        /// <summary>当前悬停索引</summary>
        private int _hoverIndex = -1;

        /// <summary>单项高度</summary>
        private const int ItemH = 40;

        /// <summary>顶部留白</summary>
        private const int PadTop = 12;

        /// <summary>左侧留白</summary>
        private const int PadLeft = 8;

        /// <summary>右侧留白</summary>
        private const int PadRight = 8;

        /// <summary>
        /// 构建设置导航栏。
        /// </summary>
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

        /// <summary>
        /// 当前选中索引。
        /// </summary>
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

        /// <summary>
        /// 导航项数量。
        /// </summary>
        public int ItemCount => _items.Length;

        /// <summary>
        /// 重新计算每项矩形。
        /// </summary>
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

        /// <summary>
        /// 绘制导航项。
        /// </summary>
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

                int iconSize = 18;
                var iconRect = new Rectangle(rect.X + 12, rect.Y + (rect.Height - iconSize) / 2, iconSize, iconSize);
                IconRenderer.Draw(g, _items[i].Icon, iconRect, Color.Empty, iconSize);

                var textRect = new Rectangle(iconRect.Right + 12, rect.Y, rect.Width - iconRect.Right - 16, rect.Height);
                Color fg = selected ? colors.TextPrimary : colors.TextSecondary;

                TextRenderer.DrawText(g, I18n.T(_items[i].Key), AppTheme.BodyFont, textRect, fg,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
        }

        /// <summary>
        /// 鼠标移动更新悬停。
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            int idx = HitTest(e.Location);
            if (idx != _hoverIndex) { _hoverIndex = idx; Invalidate(); }
            base.OnMouseMove(e);
        }

        /// <summary>
        /// 鼠标离开清除悬停。
        /// </summary>
        protected override void OnMouseLeave(EventArgs e)
        {
            _hoverIndex = -1;
            Invalidate();
            base.OnMouseLeave(e);
        }

        /// <summary>
        /// 鼠标点击切换选中。
        /// </summary>
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

        /// <summary>
        /// 命中测试。
        /// </summary>
        private int HitTest(Point p)
        {
            for (int i = 0; i < _rects.Count; i++)
                if (_rects[i].Contains(p)) return i;
            return -1;
        }
    }
}