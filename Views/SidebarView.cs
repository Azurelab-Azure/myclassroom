using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CourseApp.Localization;
using CourseApp.Services;
using CourseApp.Theme;

namespace CourseApp.Views
{
    /// <summary>
    /// 左侧导航栏。
    /// 7 项：首页 / 主页 / 教师 / 学生 / 评价 / 成绩 / 设置。
    /// 支持上下左右四个位置和主轴 / 交叉轴对齐。
    /// 尺寸固定，主题切换由 Form1 统一触发重绘。
    /// </summary>
    public class SidebarView : Panel
    {
        /// <summary>导航项点击事件，参数为项索引</summary>
        public event Action<int>? ItemClicked;

        /// <summary>固定宽度 / 高度</summary>
        private const int FixedSize = 72;

        /// <summary>单个导航项尺寸</summary>
        private const int ItemSize = 52;

        /// <summary>导航项之间的间距</summary>
        private const int ItemGap = 4;

        /// <summary>边缘留白</summary>
        private const int PadEdge = 6;

        /// <summary>当前位置：left / right / top / bottom</summary>
        private string _position = "left";

        /// <summary>主轴对齐：start / center / end</summary>
        private string _alignPrimary = "start";

        /// <summary>交叉轴对齐：start / center / end</summary>
        private string _alignSecondary = "center";

        /// <summary>导航项定义：图标短名 + 翻译 key</summary>
        private readonly (string Icon, string Key)[] _items =
        {
            (Icons.App,      "nav.dashboard"),
            (Icons.Calendar, "nav.home"),
            (Icons.Person,   "nav.teachers"),
            (Icons.Person,   "nav.students"),
            (Icons.Ok,       "nav.points"),
            (Icons.Book,     "nav.exam"),
            (Icons.Settings, "nav.settings"),
        };

        /// <summary>每项的命中矩形</summary>
        private readonly List<Rectangle> _rects = new();

        /// <summary>当前选中项索引</summary>
        private int _selectedIndex = 0;

        /// <summary>当前悬停项索引</summary>
        private int _hoverIndex = -1;

        /// <summary>
        /// 构造侧边栏。
        /// </summary>
        public SidebarView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            BackColor = AppTheme.Colors.WindowBg;
            Resize += (s, e) => RecalcRects();

            ApplyConfig();
        }

        /// <summary>
        /// 从 config.json 读取位置和对齐配置并应用。
        /// </summary>
        public void ApplyConfig()
        {
            var cfg = ConfigService.Load();
            _position = (cfg.SidebarPosition ?? "left").ToLowerInvariant();
            _alignPrimary = (cfg.SidebarAlignPrimary ?? "start").ToLowerInvariant();
            _alignSecondary = (cfg.SidebarAlignSecondary ?? "center").ToLowerInvariant();

            SuspendLayout();
            try
            {
                Dock = DockStyle.None;

                if (_position == "top" || _position == "bottom")
                    Height = FixedSize;
                else
                    Width = FixedSize;

                switch (_position)
                {
                    case "right": Dock = DockStyle.Right; break;
                    case "top": Dock = DockStyle.Top; break;
                    case "bottom": Dock = DockStyle.Bottom; break;
                    default: Dock = DockStyle.Left; break;
                }

                RecalcRects();
            }
            finally
            {
                ResumeLayout(true);
            }

            Invalidate();
        }

        /// <summary>
        /// 当前选中项索引。
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
        /// 是否为水平方向（顶部 / 底部）。
        /// </summary>
        private bool IsHorizontal => _position == "top" || _position == "bottom";

        /// <summary>
        /// 根据当前位置和大小重新计算每项的命中矩形。
        /// </summary>
        private void RecalcRects()
        {
            _rects.Clear();

            int totalItems = _items.Length;
            int totalSize = totalItems * ItemSize + (totalItems - 1) * ItemGap;

            int mainLength = IsHorizontal ? Width : Height;
            int crossLength = IsHorizontal ? Height : Width;

            int mainStart;
            switch (_alignPrimary)
            {
                case "center": mainStart = (mainLength - totalSize) / 2; break;
                case "end": mainStart = mainLength - totalSize - PadEdge; break;
                default: mainStart = PadEdge; break;
            }

            int crossStart;
            switch (_alignSecondary)
            {
                case "start": crossStart = PadEdge; break;
                case "end": crossStart = crossLength - ItemSize - PadEdge; break;
                default: crossStart = (crossLength - ItemSize) / 2; break;
            }

            for (int i = 0; i < totalItems; i++)
            {
                int mainPos = mainStart + i * (ItemSize + ItemGap);

                Rectangle r;
                if (IsHorizontal)
                    r = new Rectangle(mainPos, crossStart, ItemSize, ItemSize);
                else
                    r = new Rectangle(crossStart, mainPos, ItemSize, ItemSize);

                _rects.Add(r);
            }
        }

        /// <summary>
        /// 绘制所有导航项。
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
                if (rect.Width <= 0 || rect.Height <= 0) continue;

                bool selected = (i == _selectedIndex);
                bool hover = (i == _hoverIndex);

                if (selected || hover)
                {
                    Color bg = selected ? colors.SelectedBg : colors.HoverBg;
                    using var path = GraphicsExtensions.GetRoundPath(rect, WinUI3Tokens.CardRadius);
                    using var brush = new SolidBrush(bg);
                    g.FillPath(brush, path);
                }

                int iconSize = 22;
                var iconRect = new Rectangle(
                    rect.X + (rect.Width - iconSize) / 2,
                    rect.Y + 8,
                    iconSize,
                    iconSize);
                IconRenderer.Draw(g, _items[i].Icon, iconRect, Color.Empty, iconSize);

                var textRect = new Rectangle(rect.X, rect.Y + 32, rect.Width, rect.Height - 34);
                Color fg = selected ? colors.Accent : colors.TextSecondary;
                TextRenderer.DrawText(g, I18n.T(_items[i].Key), AppTheme.SmallFont, textRect, fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix);
            }
        }

        /// <summary>
        /// 鼠标移动时更新悬停项。
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            int idx = HitTest(e.Location);
            if (idx != _hoverIndex) { _hoverIndex = idx; Invalidate(); }
            base.OnMouseMove(e);
        }

        /// <summary>
        /// 鼠标离开时清除悬停。
        /// </summary>
        protected override void OnMouseLeave(EventArgs e)
        {
            _hoverIndex = -1;
            Invalidate();
            base.OnMouseLeave(e);
        }

        /// <summary>
        /// 鼠标点击时切换选中项并触发事件。
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
        /// 命中测试：返回鼠标所在项索引，未命中返回 -1。
        /// </summary>
        private int HitTest(Point p)
        {
            for (int i = 0; i < _rects.Count; i++)
                if (_rects[i].Contains(p)) return i;
            return -1;
        }
    }
}