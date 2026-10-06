using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// 自绘列表。每项是 IFlatComboItem。
    /// 用于 FlatComboBox 的下拉。
    /// 主题切换由顶层 Form1 统一触发 Invalidate。
    /// </summary>
    public class FlatListBox : Control
    {
        private readonly List<IFlatComboItem> _items = new();
        private int _selectedIndex = -1;
        private int _hoverIndex = -1;
        private int _scrollY = 0;
        private readonly FlatScrollBar _scrollBar;

        public event EventHandler? SelectedIndexChanged;
        public event EventHandler? ItemDoubleClicked;

        public FlatListBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;

            _scrollBar = new FlatScrollBar
            {
                Dock = DockStyle.Right,
                Width = 8,
                SmallChange = 30,
            };
            _scrollBar.ValueChanged += (s, e) =>
            {
                _scrollY = _scrollBar.Value;
                Invalidate();
            };
            Controls.Add(_scrollBar);
        }

        // =====================================================
        // 属性
        // =====================================================
        public IReadOnlyList<IFlatComboItem> Items => _items;

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (value < -1 || value >= _items.Count) value = -1;
                if (_selectedIndex != value)
                {
                    _selectedIndex = value;
                    SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
                    EnsureVisible(_selectedIndex);
                    Invalidate();
                }
            }
        }

        public IFlatComboItem? SelectedItem =>
            (_selectedIndex >= 0 && _selectedIndex < _items.Count) ? _items[_selectedIndex] : null;

        // =====================================================
        // 数据
        // =====================================================
        public void SetItems(IEnumerable<IFlatComboItem>? items)
        {
            _items.Clear();
            if (items != null) _items.AddRange(items);
            _selectedIndex = -1;
            _hoverIndex = -1;
            _scrollY = 0;
            UpdateScrollBar();
            Invalidate();
        }

        public void ClearItems() => SetItems(null);

        // =====================================================
        // 绘制
        // =====================================================
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var colors = AppTheme.Colors;

            using (var brush = new SolidBrush(colors.CardBg))
                g.FillRectangle(brush, ClientRectangle);

            // 裁剪（跳过滚动条）
            int rightEdge = _scrollBar.Visible ? Width - _scrollBar.Width : Width;
            g.SetClip(new Rectangle(0, 0, rightEdge, Height));

            int y = -_scrollY;
            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item == null) continue;

                var itemRect = new Rectangle(0, y, rightEdge, item.Height);
                if (itemRect.Bottom > 0 && itemRect.Top < Height)
                {
                    bool hover = (i == _hoverIndex);
                    bool selected = (i == _selectedIndex);
                    item.Draw(g, itemRect, hover, selected);
                }
                y += item.Height;
            }
        }

        // =====================================================
        // 尺寸
        // =====================================================
        private int GetContentHeight()
        {
            int h = 0;
            foreach (var it in _items) if (it != null) h += it.Height;
            return h;
        }

        private void UpdateScrollBar()
        {
            int contentH = GetContentHeight();
            bool needBar = contentH > Height;
            _scrollBar.Visible = needBar;

            if (needBar)
            {
                _scrollBar.Maximum = contentH;
                _scrollBar.LargeChange = Height;
                _scrollBar.Value = _scrollY;
            }
            else
            {
                _scrollY = 0;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateScrollBar();
        }

        // =====================================================
        // 命中
        // =====================================================
        private int HitTest(Point p)
        {
            int y = -_scrollY;
            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item == null) continue;
                var rect = new Rectangle(0, y, Width, item.Height);
                if (rect.Contains(p)) return i;
                y += item.Height;
            }
            return -1;
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
                if (idx >= 0) SelectedIndex = idx;
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            int idx = HitTest(e.Location);
            if (idx >= 0)
            {
                SelectedIndex = idx;
                ItemDoubleClicked?.Invoke(this, EventArgs.Empty);
            }
            base.OnMouseDoubleClick(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (GetContentHeight() <= Height) return;
            _scrollY -= Math.Sign(e.Delta) * 60;
            ClampScroll();
            _scrollBar.Value = _scrollY;
            Invalidate();
            base.OnMouseWheel(e);
        }

        private void ClampScroll()
        {
            int contentH = GetContentHeight();
            int maxScroll = Math.Max(0, contentH - Height);
            if (_scrollY < 0) _scrollY = 0;
            if (_scrollY > maxScroll) _scrollY = maxScroll;
        }

        private void EnsureVisible(int index)
        {
            if (index < 0 || index >= _items.Count) return;
            int y = 0;
            for (int i = 0; i < index; i++) y += _items[i]?.Height ?? 0;
            int itemH = _items[index]?.Height ?? 0;

            if (y < _scrollY) _scrollY = y;
            else if (y + itemH > _scrollY + Height) _scrollY = y + itemH - Height;

            ClampScroll();
            if (_scrollBar.Visible) _scrollBar.Value = _scrollY;
        }

        // =====================================================
        // 键盘
        // =====================================================
        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData & Keys.KeyCode)
            {
                case Keys.Up:
                case Keys.Down:
                case Keys.Enter:
                    return true;
            }
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Up:
                    if (_selectedIndex > 0) SelectedIndex--;
                    e.Handled = true;
                    break;
                case Keys.Down:
                    if (_selectedIndex < _items.Count - 1) SelectedIndex++;
                    e.Handled = true;
                    break;
                case Keys.Home:
                    if (_items.Count > 0) SelectedIndex = 0;
                    e.Handled = true;
                    break;
                case Keys.End:
                    if (_items.Count > 0) SelectedIndex = _items.Count - 1;
                    e.Handled = true;
                    break;
                case Keys.Enter:
                    ItemDoubleClicked?.Invoke(this, EventArgs.Empty);
                    e.Handled = true;
                    break;
            }
            base.OnKeyDown(e);
        }
    }
}