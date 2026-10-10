using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// 自绘列表控件，每项实现 IFlatComboItem 接口
    /// </summary>
    public class FlatListBox : Control
    {
        private readonly List<IFlatComboItem> items = new();
        private int selectedIndex = -1;
        private int hoverIndex = -1;
        private int scrollY = 0;
        private readonly FlatScrollBar scrollBar;

        /// <summary>选中项变化时触发</summary>
        public event EventHandler? SelectedIndexChanged;

        /// <summary>双击列表项时触发</summary>
        public event EventHandler? ItemDoubleClicked;

        /// <summary>
        /// 构造函数，初始化列表和滚动条
        /// </summary>
        public FlatListBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;

            scrollBar = new FlatScrollBar
            {
                Dock = DockStyle.Right,
                Width = 8,
                SmallChange = 30,
            };
            scrollBar.ValueChanged += (s, e) =>
            {
                scrollY = scrollBar.Value;
                Invalidate();
            };
            Controls.Add(scrollBar);
        }

        /// <summary>所有列表项</summary>
        public IReadOnlyList<IFlatComboItem> Items => items;

        /// <summary>当前选中项索引</summary>
        public int SelectedIndex
        {
            get => selectedIndex;
            set
            {
                if (value < -1 || value >= items.Count) value = -1;
                if (selectedIndex != value)
                {
                    selectedIndex = value;
                    SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
                    EnsureVisible(selectedIndex);
                    Invalidate();
                }
            }
        }

        /// <summary>当前选中项</summary>
        public IFlatComboItem? SelectedItem =>
            (selectedIndex >= 0 && selectedIndex < items.Count) ? items[selectedIndex] : null;

        /// <summary>设置所有列表项</summary>
        public void SetItems(IEnumerable<IFlatComboItem>? newItems)
        {
            items.Clear();
            if (newItems != null) items.AddRange(newItems);
            selectedIndex = -1;
            hoverIndex = -1;
            scrollY = 0;
            UpdateScrollBar();
            Invalidate();
        }

        /// <summary>清空所有列表项</summary>
        public void ClearItems() => SetItems(null);

        /// <summary>绘制列表内容</summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var colors = AppTheme.Colors;

            using (var brush = new SolidBrush(colors.CardBg))
                g.FillRectangle(brush, ClientRectangle);

            int rightEdge = scrollBar.Visible ? Width - scrollBar.Width : Width;
            g.SetClip(new Rectangle(0, 0, rightEdge, Height));

            int y = -scrollY;
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null) continue;

                var itemRect = new Rectangle(0, y, rightEdge, item.Height);
                if (itemRect.Bottom > 0 && itemRect.Top < Height)
                {
                    bool isHover = (i == hoverIndex);
                    bool isSelected = (i == selectedIndex);
                    item.Draw(g, itemRect, isHover, isSelected);
                }
                y += item.Height;
            }
        }

        /// <summary>计算列表总内容高度</summary>
        private int GetContentHeight()
        {
            int height = 0;
            foreach (var item in items)
                if (item != null) height += item.Height;
            return height;
        }

        /// <summary>根据内容高度更新滚动条状态</summary>
        private void UpdateScrollBar()
        {
            int contentHeight = GetContentHeight();
            bool needScrollBar = contentHeight > Height;
            scrollBar.Visible = needScrollBar;

            if (needScrollBar)
            {
                scrollBar.Maximum = contentHeight;
                scrollBar.LargeChange = Height;
                scrollBar.Value = scrollY;
            }
            else
            {
                scrollY = 0;
            }
        }

        /// <summary>尺寸变化时更新滚动条</summary>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateScrollBar();
        }

        /// <summary>根据鼠标位置命中测试列表项索引</summary>
        private int HitTest(Point point)
        {
            int y = -scrollY;
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null) continue;
                var rect = new Rectangle(0, y, Width, item.Height);
                if (rect.Contains(point)) return i;
                y += item.Height;
            }
            return -1;
        }

        /// <summary>鼠标移动时更新悬停项</summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            int index = HitTest(e.Location);
            if (index != hoverIndex)
            {
                hoverIndex = index;
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        /// <summary>鼠标离开时清除悬停状态</summary>
        protected override void OnMouseLeave(EventArgs e)
        {
            hoverIndex = -1;
            Invalidate();
            base.OnMouseLeave(e);
        }

        /// <summary>鼠标点击时选中项</summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int index = HitTest(e.Location);
                if (index >= 0) SelectedIndex = index;
            }
            base.OnMouseDown(e);
        }

        /// <summary>鼠标双击时选中项并触发事件</summary>
        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            int index = HitTest(e.Location);
            if (index >= 0)
            {
                SelectedIndex = index;
                ItemDoubleClicked?.Invoke(this, EventArgs.Empty);
            }
            base.OnMouseDoubleClick(e);
        }

        /// <summary>鼠标滚轮滚动列表</summary>
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (GetContentHeight() <= Height) return;
            scrollY -= Math.Sign(e.Delta) * 60;
            ClampScroll();
            scrollBar.Value = scrollY;
            Invalidate();
            base.OnMouseWheel(e);
        }

        /// <summary>限制滚动范围</summary>
        private void ClampScroll()
        {
            int contentHeight = GetContentHeight();
            int maxScroll = Math.Max(0, contentHeight - Height);
            if (scrollY < 0) scrollY = 0;
            if (scrollY > maxScroll) scrollY = maxScroll;
        }

        /// <summary>确保指定索引项可见</summary>
        private void EnsureVisible(int index)
        {
            if (index < 0 || index >= items.Count) return;
            int y = 0;
            for (int i = 0; i < index; i++) y += items[i]?.Height ?? 0;
            int itemHeight = items[index]?.Height ?? 0;

            if (y < scrollY) scrollY = y;
            else if (y + itemHeight > scrollY + Height) scrollY = y + itemHeight - Height;

            ClampScroll();
            if (scrollBar.Visible) scrollBar.Value = scrollY;
        }

        /// <summary>声明方向键回车为输入键</summary>
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

        /// <summary>键盘导航处理</summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Up:
                    if (selectedIndex > 0) SelectedIndex--;
                    e.Handled = true;
                    break;
                case Keys.Down:
                    if (selectedIndex < items.Count - 1) SelectedIndex++;
                    e.Handled = true;
                    break;
                case Keys.Home:
                    if (items.Count > 0) SelectedIndex = 0;
                    e.Handled = true;
                    break;
                case Keys.End:
                    if (items.Count > 0) SelectedIndex = items.Count - 1;
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