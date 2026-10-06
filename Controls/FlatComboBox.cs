using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// 自绘下拉框。
    /// 收起态：自绘外框 + 当前项摘要 + 飞镖头。
    /// 展开态：FlatPopup 内嵌 FlatListBox。
    /// 主题切换由 Form1 统一触发 Invalidate。
    /// </summary>
    public class FlatComboBox : Control
    {
        private readonly List<IFlatComboItem> _items = new();
        private int _selectedIndex = -1;
        private bool _hover;
        private bool _open;

        private FlatPopup? _popup;
        private FlatListBox? _listBox;

        private int _cornerRadius = 6;

        public event EventHandler? SelectedIndexChanged;

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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                ClosePopup();
            base.Dispose(disposing);
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
                    Invalidate();
                }
            }
        }

        public IFlatComboItem? SelectedItem =>
            (_selectedIndex >= 0 && _selectedIndex < _items.Count) ? _items[_selectedIndex] : null;

        public string SelectedText => SelectedItem?.DisplayText ?? "";

        public int CornerRadius
        {
            get => _cornerRadius;
            set { _cornerRadius = value; Invalidate(); }
        }

        // =====================================================
        // 数据
        // =====================================================
        public void SetItems(IEnumerable<IFlatComboItem> items)
        {
            _items.Clear();
            if (items != null) _items.AddRange(items);
            if (_selectedIndex >= _items.Count) _selectedIndex = -1;
            _listBox?.SetItems(_items);
            Invalidate();
        }

        public void ClearItems() => SetItems(null);

        // =====================================================
        // 鼠标
        // =====================================================
        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true; Invalidate(); base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false; Invalidate(); base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) TogglePopup();
            base.OnMouseDown(e);
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
                case Keys.Space:
                    return true;
            }
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Down:
                    if (!_open) OpenPopup();
                    else if (_listBox != null && _listBox.SelectedIndex < _items.Count - 1)
                        _listBox.SelectedIndex++;
                    e.Handled = true;
                    break;

                case Keys.Up:
                    if (_open && _listBox != null && _listBox.SelectedIndex > 0)
                        _listBox.SelectedIndex--;
                    e.Handled = true;
                    break;

                case Keys.Enter:
                    if (_open) CommitSelection();
                    else OpenPopup();
                    e.Handled = true;
                    break;

                case Keys.Escape:
                    if (_open) ClosePopup();
                    e.Handled = true;
                    break;
            }
            base.OnKeyDown(e);
        }

        // =====================================================
        // Popup
        // =====================================================
        private void TogglePopup()
        {
            if (_open) ClosePopup();
            else OpenPopup();
        }

        private void OpenPopup()
        {
            if (_items.Count == 0) return;

            _popup = new FlatPopup();

            _listBox = new FlatListBox();
            _listBox.SetItems(_items);
            _listBox.SelectedIndex = _selectedIndex >= 0 ? _selectedIndex : 0;
            _listBox.ItemDoubleClicked += (s, e) => CommitSelection();

            // 计算弹出高度
            int contentH = 0;
            foreach (var it in _items) contentH += it.Height;
            int popupH = Math.Min(contentH, 300);
            int popupW = Width;

            _popup.SetContent(_listBox, popupW, popupH);
            _popup.FormClosed += (s, e) =>
            {
                _open = false;
                _popup = null;
                _listBox = null;
                Invalidate();
            };

            var screenPt = PointToScreen(new Point(0, Height));
            _popup.ShowAt(screenPt);

            _listBox.Focus();
            _open = true;
            Invalidate();
        }

        private void ClosePopup()
        {
            _popup?.Close();
            _popup = null;
            _listBox = null;
            _open = false;
            Invalidate();
        }

        private void CommitSelection()
        {
            if (_listBox != null)
                SelectedIndex = _listBox.SelectedIndex;
            ClosePopup();
        }

        // =====================================================
        // 绘制
        // =====================================================
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var colors = AppTheme.Colors;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            using (var path = GraphicsExtensions.GetRoundPath(rect, _cornerRadius))
            {
                using (var bg = new SolidBrush(colors.CardBg))
                    g.FillPath(bg, path);

                Color border = _open ? colors.Accent
                             : _hover ? colors.TextSecondary
                             : colors.ButtonBorder;
                float bw = _open ? 2f : 1f;

                using var pen = new Pen(border, bw);
                g.DrawPath(pen, path);
            }

            // 当前项文字
            int arrowArea = 24;
            var textRect = new Rectangle(10, 0, Width - 10 - arrowArea, Height);
            TextRenderer.DrawText(g, SelectedText, AppTheme.BodyFont, textRect, colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            // 飞镖头
            DrawChevron(g, colors.TextSecondary, _open);
        }

        private void DrawChevron(Graphics g, Color color, bool up)
        {
            int cx = Width - 14;
            int cy = Height / 2;
            const int half = 4;
            const int h = 3;

            Point p1, p2, p3;
            if (up)
            {
                p1 = new Point(cx - half, cy + h);
                p2 = new Point(cx, cy - h);
                p3 = new Point(cx + half, cy + h);
            }
            else
            {
                p1 = new Point(cx - half, cy - h);
                p2 = new Point(cx, cy + h);
                p3 = new Point(cx + half, cy - h);
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