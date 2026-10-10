using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// 卡片选择器控件
    /// 用于在多个选项之间进行可视化选择，支持纯文本或自定义预览绘制
    /// </summary>
    public class CardSelector : Control
    {
        /// <summary>
        /// 选择项数据结构
        /// </summary>
        public class Item
        {
            /// <summary>显示文本（无预览绘制时使用）</summary>
            public string Text = "";

            /// <summary>是否可选中</summary>
            public bool Enabled = true;

            /// <summary>预览绘制委托，非空时替代文本显示</summary>
            public Action<Graphics, Rectangle>? PreviewDrawer;
        }

        private const int ItemWidth = 128;
        private const int ItemHeight = 88;
        private const int ItemGap = 8;
        private const int PreviewPadding = 6;

        private readonly List<Item> items = new();
        private int selectedIndex = -1;
        private int hoverIndex = -1;
        private bool isFocused = false;

        /// <summary>选中项变化时触发</summary>
        public event EventHandler? SelectedIndexChanged;

        /// <summary>
        /// 构造函数，初始化控件样式和默认属性
        /// </summary>
        public CardSelector()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.Selectable, true);

            BackColor = Color.Transparent;
            Height = ItemHeight;
            TabStop = true;
        }

        /// <summary>设置所有选择项</summary>
        public void SetItems(IEnumerable<Item>? newItems)
        {
            items.Clear();
            if (newItems != null) items.AddRange(newItems);
            if (selectedIndex >= items.Count) selectedIndex = -1;
            Invalidate();
        }

        /// <summary>当前选中项索引</summary>
        public int SelectedIndex
        {
            get => selectedIndex;
            set
            {
                if (value < -1 || value >= items.Count) value = -1;
                if (selectedIndex == value) return;
                selectedIndex = value;
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }

        /// <summary>当前选中项</summary>
        public Item? SelectedItem =>
            (selectedIndex >= 0 && selectedIndex < items.Count) ? items[selectedIndex] : null;

        /// <summary>选择项数量</summary>
        public int ItemCount => items.Count;

        /// <summary>根据索引计算项所在的矩形区域</summary>
        private Rectangle GetItemRect(int index)
        {
            if (items.Count == 0) return Rectangle.Empty;

            int columns = ColumnsPerRow;
            int row = index / columns;
            int col = index % columns;

            return new Rectangle(
                col * (ItemWidth + ItemGap),
                row * (ItemHeight + ItemGap),
                ItemWidth,
                ItemHeight);
        }

        /// <summary>根据当前宽度计算每行可容纳的列数</summary>
        private int ColumnsPerRow
        {
            get
            {
                if (Width <= 0) return 1;
                int cols = (Width + ItemGap) / (ItemWidth + ItemGap);
                return cols < 1 ? 1 : cols;
            }
        }

        /// <summary>绘制所有选择项</summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var colors = AppTheme.Colors;

            for (int i = 0; i < items.Count; i++)
            {
                var rect = GetItemRect(i);
                if (rect.Width <= 0 || rect.Height <= 0) continue;
                DrawItem(g, i, rect, colors);
            }
        }

        /// <summary>绘制单个选择项</summary>
        private void DrawItem(Graphics g, int index, Rectangle rect, ThemeColors colors)
        {
            var item = items[index];
            bool isSelected = (index == selectedIndex);
            bool isHover = (index == hoverIndex) && item.Enabled;
            bool isEnabled = item.Enabled;

            Color bg;
            if (!isEnabled) bg = colors.WindowBg;
            else if (isSelected) bg = colors.SelectedBg;
            else if (isHover) bg = colors.HoverBg;
            else bg = colors.CardBg;

            using (var path = GraphicsExtensions.GetRoundPath(rect, 6))
            using (var brush = new SolidBrush(bg))
                g.FillPath(brush, path);

            Color borderColor = !isEnabled ? colors.Divider
                              : isSelected ? colors.Accent
                                           : colors.CardBorder;
            float borderWidth = (isSelected && isEnabled) ? 2f : 1f;

            using (var path = GraphicsExtensions.GetRoundPath(rect, 6))
            using (var pen = new Pen(borderColor, borderWidth))
                g.DrawPath(pen, path);

            if (isSelected && isFocused && isEnabled)
            {
                var focusRect = new Rectangle(rect.X + 4, rect.Y + 4, rect.Width - 9, rect.Height - 9);
                using var focusPath = GraphicsExtensions.GetRoundPath(focusRect, 4);
                using var focusPen = new Pen(colors.Accent, 1f) { DashStyle = DashStyle.Dot };
                g.DrawPath(focusPen, focusPath);
            }

            if (item.PreviewDrawer != null)
            {
                var previewRect = new Rectangle(
                    rect.X + PreviewPadding,
                    rect.Y + PreviewPadding,
                    rect.Width - PreviewPadding * 2,
                    rect.Height - PreviewPadding * 2);
                item.PreviewDrawer(g, previewRect);
            }
            else
            {
                Color foreground = !isEnabled ? colors.TextDisabled
                                 : isSelected ? colors.Accent
                                              : colors.TextPrimary;

                var textRect = new Rectangle(rect.X + 4, rect.Y + 4, rect.Width - 8, rect.Height - 8);

                TextRenderer.DrawText(g, item.Text ?? "", AppTheme.BodyFont, textRect, foreground,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis |
                    TextFormatFlags.NoPrefix);
            }
        }

        /// <summary>根据鼠标位置命中测试，返回项索引</summary>
        private int HitTest(Point point)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var rect = GetItemRect(i);
                if (rect.Contains(point)) return i;
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

        /// <summary>鼠标点击时选中对应项</summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Focus();
                int index = HitTest(e.Location);
                if (index >= 0 && items[index].Enabled)
                    SelectedIndex = index;
            }
            base.OnMouseDown(e);
        }

        /// <summary>获得焦点时重绘</summary>
        protected override void OnGotFocus(EventArgs e)
        {
            isFocused = true;
            Invalidate();
            base.OnGotFocus(e);
        }

        /// <summary>失去焦点时重绘</summary>
        protected override void OnLostFocus(EventArgs e)
        {
            isFocused = false;
            Invalidate();
            base.OnLostFocus(e);
        }

        /// <summary>声明方向键和空格回车为输入键</summary>
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

        /// <summary>键盘导航处理</summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (items.Count == 0) { base.OnKeyDown(e); return; }

            int direction = 0;
            switch (e.KeyCode)
            {
                case Keys.Left:
                case Keys.Up: direction = -1; break;
                case Keys.Right:
                case Keys.Down: direction = +1; break;
                case Keys.Home: SelectFirstEnabled(); e.Handled = true; return;
                case Keys.End: SelectLastEnabled(); e.Handled = true; return;
                case Keys.Space:
                case Keys.Enter:
                    e.Handled = true;
                    return;
            }

            if (direction != 0)
            {
                int next = selectedIndex;
                for (int step = 0; step < items.Count; step++)
                {
                    next += direction;
                    if (next < 0) next = items.Count - 1;
                    if (next >= items.Count) next = 0;
                    if (items[next].Enabled) { SelectedIndex = next; break; }
                }
                e.Handled = true;
            }

            base.OnKeyDown(e);
        }

        /// <summary>选中第一个可用项</summary>
        private void SelectFirstEnabled()
        {
            for (int i = 0; i < items.Count; i++)
                if (items[i].Enabled) { SelectedIndex = i; return; }
        }

        /// <summary>选中最后一个可用项</summary>
        private void SelectLastEnabled()
        {
            for (int i = items.Count - 1; i >= 0; i--)
                if (items[i].Enabled) { SelectedIndex = i; return; }
        }

        /// <summary>可用状态变化时重绘</summary>
        protected override void OnEnabledChanged(EventArgs e)
        {
            Invalidate();
            base.OnEnabledChanged(e);
        }
    }
}