using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// 自绘下拉框，收起态显示当前项摘要，展开态弹出列表
    /// </summary>
    public class FlatComboBox : Control
    {
        private readonly List<IFlatComboItem> items = new();
        private int selectedIndex = -1;
        private bool isHover;
        private bool isOpen;

        private FlatPopup? popup;
        private FlatListBox? listBox;

        private int cornerRadius = 6;

        /// <summary>选中项变化时触发</summary>
        public event EventHandler? SelectedIndexChanged;

        /// <summary>
        /// 构造函数，初始化下拉框基本属性
        /// </summary>
        public FlatComboBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Size = new Size(200, 32);
        }

        /// <summary>释放资源并关闭弹出层</summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing) ClosePopup();
            base.Dispose(disposing);
        }

        /// <summary>所有选项</summary>
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
                    Invalidate();
                }
            }
        }

        /// <summary>当前选中项</summary>
        public IFlatComboItem? SelectedItem =>
            (selectedIndex >= 0 && selectedIndex < items.Count) ? items[selectedIndex] : null;

        /// <summary>当前选中项的显示文本</summary>
        public string SelectedText => SelectedItem?.DisplayText ?? "";

        /// <summary>圆角半径</summary>
        public int CornerRadius
        {
            get => cornerRadius;
            set { cornerRadius = value; Invalidate(); }
        }

        /// <summary>设置所有选项</summary>
        public void SetItems(IEnumerable<IFlatComboItem> newItems)
        {
            items.Clear();
            if (newItems != null) items.AddRange(newItems);
            if (selectedIndex >= items.Count) selectedIndex = -1;
            listBox?.SetItems(items);
            Invalidate();
        }

        /// <summary>清空所有选项</summary>
        public void ClearItems() => SetItems(null);

        /// <summary>鼠标进入时标记悬停状态</summary>
        protected override void OnMouseEnter(EventArgs e)
        {
            isHover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        /// <summary>鼠标离开时清除悬停状态</summary>
        protected override void OnMouseLeave(EventArgs e)
        {
            isHover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        /// <summary>鼠标按下时切换弹出层</summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) TogglePopup();
            base.OnMouseDown(e);
        }

        /// <summary>声明方向键回车空格为输入键</summary>
        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData & Keys.KeyCode)
            {
                case Keys.Up:
                case Keys.Down:
                case Keys.Enter:
                case Keys.Space:
                    return true;
            }
            return base.IsInputKey(keyData);
        }

        /// <summary>键盘导航处理</summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Down:
                    if (!isOpen) OpenPopup();
                    else if (listBox != null && listBox.SelectedIndex < items.Count - 1)
                        listBox.SelectedIndex++;
                    e.Handled = true;
                    break;

                case Keys.Up:
                    if (isOpen && listBox != null && listBox.SelectedIndex > 0)
                        listBox.SelectedIndex--;
                    e.Handled = true;
                    break;

                case Keys.Enter:
                    if (isOpen) CommitSelection();
                    else OpenPopup();
                    e.Handled = true;
                    break;

                case Keys.Escape:
                    if (isOpen) ClosePopup();
                    e.Handled = true;
                    break;
            }
            base.OnKeyDown(e);
        }

        /// <summary>切换弹出层显示状态</summary>
        private void TogglePopup()
        {
            if (isOpen) ClosePopup();
            else OpenPopup();
        }

        /// <summary>打开下拉弹出层</summary>
        private void OpenPopup()
        {
            if (items.Count == 0) return;

            popup = new FlatPopup();
            listBox = new FlatListBox();
            listBox.SetItems(items);
            listBox.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
            listBox.ItemDoubleClicked += (s, e) => CommitSelection();

            int contentHeight = 0;
            foreach (var item in items) contentHeight += item.Height;
            int popupHeight = Math.Min(contentHeight, 300);
            int popupWidth = Width;

            popup.SetContent(listBox, popupWidth, popupHeight);
            popup.FormClosed += (s, e) =>
            {
                isOpen = false;
                popup = null;
                listBox = null;
                Invalidate();
            };

            var screenPoint = PointToScreen(new Point(0, Height));
            popup.ShowAt(screenPoint);

            listBox.Focus();
            isOpen = true;
            Invalidate();
        }

        /// <summary>关闭下拉弹出层</summary>
        private void ClosePopup()
        {
            popup?.Close();
            popup = null;
            listBox = null;
            isOpen = false;
            Invalidate();
        }

        /// <summary>提交当前选择</summary>
        private void CommitSelection()
        {
            if (listBox != null)
                SelectedIndex = listBox.SelectedIndex;
            ClosePopup();
        }

        /// <summary>绘制下拉框外观</summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var colors = AppTheme.Colors;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            using (var path = GraphicsExtensions.GetRoundPath(rect, cornerRadius))
            {
                using (var background = new SolidBrush(colors.CardBg))
                    g.FillPath(background, path);

                Color borderColor = isOpen ? colors.Accent
                                  : isHover ? colors.TextSecondary
                                            : colors.ButtonBorder;
                float borderWidth = isOpen ? 2f : 1f;

                using var pen = new Pen(borderColor, borderWidth);
                g.DrawPath(pen, path);
            }

            int arrowArea = 24;
            var textRect = new Rectangle(10, 0, Width - 10 - arrowArea, Height);
            TextRenderer.DrawText(g, SelectedText, AppTheme.BodyFont, textRect, colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            DrawChevron(g, colors.TextSecondary, isOpen);
        }

        /// <summary>绘制下拉箭头</summary>
        private void DrawChevron(Graphics g, Color color, bool pointUp)
        {
            int centerX = Width - 14;
            int centerY = Height / 2;
            const int halfWidth = 4;
            const int halfHeight = 3;

            Point p1, p2, p3;
            if (pointUp)
            {
                p1 = new Point(centerX - halfWidth, centerY + halfHeight);
                p2 = new Point(centerX, centerY - halfHeight);
                p3 = new Point(centerX + halfWidth, centerY + halfHeight);
            }
            else
            {
                p1 = new Point(centerX - halfWidth, centerY - halfHeight);
                p2 = new Point(centerX, centerY + halfHeight);
                p3 = new Point(centerX + halfWidth, centerY - halfHeight);
            }

            using var pen = new Pen(color, 1.6f)
            {
                LineJoin = LineJoin.Round,
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
            };
            g.DrawLines(pen, new[] { p1, p2, p3 });
        }
    }
}