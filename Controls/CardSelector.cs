using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// 卡片选择器：一排卡片，点击选中。
    /// 支持纯文字 / 代码画预览图两种模式。
    /// 主题切换由顶层 Form1 统一触发 Invalidate。
    /// </summary>
    public class CardSelector : Control
    {
        // =====================================================
        // 项
        // =====================================================
        public class Item
        {
            /// <summary>显示文字（无 PreviewDrawer 时用）</summary>
            public string Text = "";

            /// <summary>是否启用</summary>
            public bool Enabled = true;

            /// <summary>预览图绘制委托（有则代替文字）</summary>
            public Action<Graphics, Rectangle>? PreviewDrawer;
        }

        // =====================================================
        // 尺寸常量
        // =====================================================
        private const int ItemW = 128;
        private const int ItemH = 88;
        private const int Gap = 8;
        private const int PreviewPad = 6;

        // =====================================================
        // 状态
        // =====================================================
        private readonly List<Item> _items = new();
        private int _selectedIndex = -1;
        private int _hoverIndex = -1;
        private bool _focused = false;

        public event EventHandler? SelectedIndexChanged;

        public CardSelector()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.Selectable, true);

            BackColor = Color.Transparent;
            Height = ItemH;
            TabStop = true;
        }

        // =====================================================
        // 数据
        // =====================================================
        public void SetItems(IEnumerable<Item>? items)
        {
            _items.Clear();
            if (items != null) _items.AddRange(items);

            if (_selectedIndex >= _items.Count) _selectedIndex = -1;
            Invalidate();
        }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (value < -1 || value >= _items.Count) value = -1;
                if (_selectedIndex == value) return;
                _selectedIndex = value;
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }

        public Item? SelectedItem =>
            (_selectedIndex >= 0 && _selectedIndex < _items.Count) ? _items[_selectedIndex] : null;

        public int ItemCount => _items.Count;

        // =====================================================
        // 每项矩形
        // =====================================================
        private Rectangle GetItemRect(int index)
        {
            if (_items.Count == 0) return Rectangle.Empty;

            int cols = ColumnsPerRow;
            int row = index / cols;
            int col = index % cols;

            return new Rectangle(col * (ItemW + Gap), row * (ItemH + Gap), ItemW, ItemH);
        }

        private int ColumnsPerRow
        {
            get
            {
                if (Width <= 0) return 1;
                int cols = (Width + Gap) / (ItemW + Gap);
                return cols < 1 ? 1 : cols;
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

            for (int i = 0; i < _items.Count; i++)
            {
                var rect = GetItemRect(i);
                if (rect.Width <= 0 || rect.Height <= 0) continue;
                DrawItem(g, i, rect, colors);
            }
        }

        private void DrawItem(Graphics g, int index, Rectangle rect, ThemeColors colors)
        {
            var item = _items[index];
            bool selected = (index == _selectedIndex);
            bool hover = (index == _hoverIndex) && item.Enabled;
            bool enabled = item.Enabled;

            // ---------- 背景 ----------
            Color bg;
            if (!enabled) bg = colors.WindowBg;
            else if (selected) bg = colors.SelectedBg;
            else if (hover) bg = colors.HoverBg;
            else bg = colors.CardBg;

            using (var path = GraphicsExtensions.GetRoundPath(rect, 6))
            using (var brush = new SolidBrush(bg))
                g.FillPath(brush, path);

            // ---------- 边框 ----------
            Color border = !enabled ? colors.Divider
                         : selected ? colors.Accent
                                    : colors.CardBorder;
            float bw = (selected && enabled) ? 2f : 1f;
            using (var path = GraphicsExtensions.GetRoundPath(rect, 6))
            using (var pen = new Pen(border, bw))
                g.DrawPath(pen, path);

            // ---------- 焦点虚线 ----------
            if (selected && _focused && enabled)
            {
                var fr = new Rectangle(rect.X + 4, rect.Y + 4, rect.Width - 9, rect.Height - 9);
                using var fpath = GraphicsExtensions.GetRoundPath(fr, 4);
                using var fpen = new Pen(colors.Accent, 1f) { DashStyle = DashStyle.Dot };
                g.DrawPath(fpen, fpath);
            }

            // ---------- 内容：预览图 / 文字 ----------
            if (item.PreviewDrawer != null)
            {
                var previewRect = new Rectangle(
                    rect.X + PreviewPad,
                    rect.Y + PreviewPad,
                    rect.Width - PreviewPad * 2,
                    rect.Height - PreviewPad * 2);

                item.PreviewDrawer(g, previewRect);
            }
            else
            {
                Color fg = !enabled ? colors.TextDisabled
                         : selected ? colors.Accent
                                    : colors.TextPrimary;

                var textRect = new Rectangle(
                    rect.X + 4, rect.Y + 4,
                    rect.Width - 8, rect.Height - 8);

                TextRenderer.DrawText(g, item.Text ?? "", AppTheme.BodyFont, textRect, fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis |
                    TextFormatFlags.NoPrefix);
            }
        }

        // =====================================================
        // 鼠标
        // =====================================================
        private int HitTest(Point p)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                var rect = GetItemRect(i);
                if (rect.Contains(p)) return i;
            }
            return -1;
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
                Focus();
                int idx = HitTest(e.Location);
                if (idx >= 0 && _items[idx].Enabled)
                    SelectedIndex = idx;
            }
            base.OnMouseDown(e);
        }

        // =====================================================
        // 焦点
        // =====================================================
        protected override void OnGotFocus(EventArgs e)
        {
            _focused = true;
            Invalidate();
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            _focused = false;
            Invalidate();
            base.OnLostFocus(e);
        }

        // =====================================================
        // 键盘
        // =====================================================
        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData & Keys.KeyCode)
            {
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:
                case Keys.Space:
                case Keys.Enter:
                    return true;
            }
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_items.Count == 0) { base.OnKeyDown(e); return; }

            int dir = 0;
            switch (e.KeyCode)
            {
                case Keys.Left:
                case Keys.Up: dir = -1; break;
                case Keys.Right:
                case Keys.Down: dir = +1; break;
                case Keys.Home: SelectFirstEnabled(); e.Handled = true; return;
                case Keys.End: SelectLastEnabled(); e.Handled = true; return;
                case Keys.Space:
                case Keys.Enter:
                    e.Handled = true;
                    return;
            }

            if (dir != 0)
            {
                int next = _selectedIndex;
                for (int step = 0; step < _items.Count; step++)
                {
                    next += dir;
                    if (next < 0) next = _items.Count - 1;
                    if (next >= _items.Count) next = 0;
                    if (_items[next].Enabled) { SelectedIndex = next; break; }
                }
                e.Handled = true;
            }

            base.OnKeyDown(e);
        }

        private void SelectFirstEnabled()
        {
            for (int i = 0; i < _items.Count; i++)
                if (_items[i].Enabled) { SelectedIndex = i; return; }
        }

        private void SelectLastEnabled()
        {
            for (int i = _items.Count - 1; i >= 0; i--)
                if (_items[i].Enabled) { SelectedIndex = i; return; }
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            Invalidate();
            base.OnEnabledChanged(e);
        }
    }
}