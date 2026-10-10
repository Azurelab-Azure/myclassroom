using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// WinUI 3 风格分段控件。
    /// </summary>
    public class WinUI3Segmented : Control
    {
        private readonly List<string> _items = new();
        private readonly List<Rectangle> _rects = new();
        private int _selectedIndex = 0;
        private int _hoverIndex = -1;

        public event EventHandler? SelectedIndexChanged;

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (value < 0 || value >= _items.Count) return;
                if (_selectedIndex == value) return;
                _selectedIndex = value;
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }

        public WinUI3Segmented()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Height = 36;
            Cursor = Cursors.Hand;
        }

        public void SetItems(IEnumerable<string> items)
        {
            _items.Clear();
            _items.AddRange(items);
            if (_selectedIndex >= _items.Count) _selectedIndex = 0;
            RecalcRects();
            Invalidate();
        }

        protected override void OnResize(EventArgs e) { base.OnResize(e); RecalcRects(); }

        private void RecalcRects()
        {
            _rects.Clear();
            if (_items.Count == 0) return;
            int w = Width / _items.Count;
            for (int i = 0; i < _items.Count; i++)
                _rects.Add(new Rectangle(i * w, 0, w, Height));
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int idx = HitTest(e.Location);
            if (idx != _hoverIndex) { _hoverIndex = idx; Invalidate(); }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e) { _hoverIndex = -1; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int idx = HitTest(e.Location);
                if (idx >= 0) SelectedIndex = idx;
            }
            base.OnMouseDown(e);
        }

        private int HitTest(Point p)
        {
            for (int i = 0; i < _rects.Count; i++)
                if (_rects[i].Contains(p)) return i;
            return -1;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var colors = AppTheme.Colors;

            var outer = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var outerPath = GraphicsExtensions.GetRoundPath(outer, WinUI3Tokens.ControlRadius))
            using (var outerBrush = new SolidBrush(colors.WindowBg))
                g.FillPath(outerBrush, outerPath);

            for (int i = 0; i < _items.Count && i < _rects.Count; i++)
            {
                var rect = _rects[i];
                var inner = new Rectangle(rect.X + 2, 2, rect.Width - 4, rect.Height - 4);

                bool selected = (i == _selectedIndex);
                bool hover = (i == _hoverIndex);

                if (selected)
                {
                    using var path = GraphicsExtensions.GetRoundPath(inner, WinUI3Tokens.ControlRadius);
                    using var brush = new SolidBrush(colors.CardBg);
                    g.FillPath(brush, path);
                    using var pen = new Pen(colors.CardBorder, 1f);
                    g.DrawPath(pen, path);
                }
                else if (hover)
                {
                    using var path = GraphicsExtensions.GetRoundPath(inner, WinUI3Tokens.ControlRadius);
                    using var brush = new SolidBrush(colors.HoverBg);
                    g.FillPath(brush, path);
                }

                Color fg = selected ? colors.TextPrimary : colors.TextSecondary;
                TextRenderer.DrawText(g, _items[i], AppTheme.BodyFont, rect, fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix);
            }
        }
    }
}