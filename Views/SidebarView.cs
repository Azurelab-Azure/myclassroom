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
    /// 侧边栏：位置（上下左右）+ 对齐（主/交叉）自定义，尺寸固定 72。
    /// 主题切换由 Form1 统一触发 Invalidate。
    /// </summary>
    public class SidebarView : Panel
    {
        public event Action<int>? ItemClicked;

        // ============ 固定尺寸 ============
        private const int FixedSize = 72;
        private const int ItemSize = 52;
        private const int ItemGap = 4;
        private const int PadEdge = 6;

        // ============ 布局配置 ============
        private string _position = "left";
        private string _alignPrimary = "start";
        private string _alignSecondary = "center";

        // ============ 项（文字走 I18n key） ============
private readonly (string Icon, string Key)[] _items =
{
    (Icons.App,      "nav.dashboard"),   // ← 新增：首页
    (Icons.Calendar, "nav.home"),        // 主页（课表）
    (Icons.Person,   "nav.teachers"),
    (Icons.Person,   "nav.students"),
    (Icons.Calendar, "nav.scores"),
    (Icons.Settings, "nav.settings"),
};

        private readonly List<Rectangle> _rects = new();
        private int _selectedIndex = 0;
        private int _hoverIndex = -1;

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

        // =====================================================
        // 配置
        // =====================================================
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
                    case "right":  Dock = DockStyle.Right;  break;
                    case "top":    Dock = DockStyle.Top;    break;
                    case "bottom": Dock = DockStyle.Bottom; break;
                    case "left":
                    default:       Dock = DockStyle.Left;   break;
                }

                RecalcRects();
            }
            finally
            {
                ResumeLayout(true);
            }

            Invalidate();
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

        // =====================================================
        // 布局计算
        // =====================================================
        private bool IsHorizontal => _position == "top" || _position == "bottom";

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
                case "end":    mainStart = mainLength - totalSize - PadEdge; break;
                default:       mainStart = PadEdge; break;
            }

            int crossStart;
            switch (_alignSecondary)
            {
                case "start":  crossStart = PadEdge; break;
                case "end":    crossStart = crossLength - ItemSize - PadEdge; break;
                default:       crossStart = (crossLength - ItemSize) / 2; break;
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

        // =====================================================
        // 绘制
        // =====================================================
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
                    using var path = GraphicsExtensions.GetRoundPath(rect, 8);
                    using var brush = new SolidBrush(bg);
                    g.FillPath(brush, path);
                }

                // 图标
                int iconSize = 22;
                var iconRect = new Rectangle(
                    rect.X + (rect.Width - iconSize) / 2,
                    rect.Y + 8,
                    iconSize,
                    iconSize);
                IconRenderer.Draw(g, _items[i].Icon, iconRect, Color.Empty, iconSize);

                // 文字
                var textRect = new Rectangle(rect.X, rect.Y + 32, rect.Width, rect.Height - 34);
                Color fg = selected ? colors.Accent : colors.TextSecondary;
                TextRenderer.DrawText(g, I18n.T(_items[i].Key), AppTheme.SmallFont, textRect, fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix);
            }
        }

        // =====================================================
        // 鼠标
        // =====================================================
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