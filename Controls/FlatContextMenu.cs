using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// 自绘右键菜单。
    /// 主题切换由顶层 Form1 统一触发 Invalidate。
    /// </summary>
    public class FlatContextMenu : IDisposable
    {
        private readonly List<FlatMenuItem> _items = new();
        private int _hoverIndex = -1;

        private FlatPopup? _popup;
        private FlatMenuHost? _host;

        private const int ItemPadLeft = 12;
        private const int IconSize = 16;
        private const int TextGap = 12;
        private const int ShortcutGapRight = 12;
        private const int MinWidth = 160;

        // =====================================================
        // 构建
        // =====================================================
        public FlatContextMenu AddItem(FlatMenuItem item)
        {
            _items.Add(item);
            return this;
        }

        public FlatContextMenu AddSeparator()
        {
            _items.Add(FlatMenuItem.CreateSeparator());
            return this;
        }

        public FlatContextMenu Clear()
        {
            _items.Clear();
            return this;
        }

        public IReadOnlyList<FlatMenuItem> Items => _items;

        // =====================================================
        // 显示
        // =====================================================
        public void ShowAt(Control owner, Point screenPt)
        {
            if (_items.Count == 0) return;

            int width = MeasureWidth();
            int height = MeasureHeight();

            _host = new FlatMenuHost(this);
            _host.SetBounds(0, 0, width, height);

            _popup = new FlatPopup();
            _popup.SetContent(_host, width, height);
            _popup.FormClosed += (s, e) =>
            {
                _popup = null;
                _host = null;
                _hoverIndex = -1;
            };

            _popup.ShowAt(screenPt);
        }

        public void Close()
        {
            _popup?.Close();
            _popup = null;
            _host = null;
        }

        public void Dispose() => Close();

        // =====================================================
        // 尺寸
        // =====================================================
        private int MeasureWidth()
        {
            int w = MinWidth;

            using var bmp = new Bitmap(1, 1);
            using var g = Graphics.FromImage(bmp);

            var font = AppTheme.BodyFont;
            var smallFont = AppTheme.SmallFont;

            foreach (var it in _items)
            {
                if (it.Type == FlatMenuItem.ItemType.Separator) continue;

                int rowW = ItemPadLeft + IconSize + TextGap;
                rowW += TextRenderer.MeasureText(g, it.Text ?? "", font).Width;

                if (!string.IsNullOrEmpty(it.Shortcut))
                {
                    rowW += 24;
                    rowW += TextRenderer.MeasureText(g, it.Shortcut, smallFont).Width;
                }
                rowW += ShortcutGapRight;

                if (rowW > w) w = rowW;
            }
            return w;
        }

        private int MeasureHeight()
        {
            int h = 6;
            foreach (var it in _items) h += it.GetHeight();
            h += 6;
            return h;
        }

        // =====================================================
        // 菜单宿主
        // =====================================================
        private class FlatMenuHost : Control
        {
            private readonly FlatContextMenu _owner;

            public FlatMenuHost(FlatContextMenu owner)
            {
                _owner = owner;
                SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.ResizeRedraw, true);
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                int idx = HitTest(e.Location);
                if (idx != _owner._hoverIndex)
                {
                    _owner._hoverIndex = idx;
                    Invalidate();
                }
                base.OnMouseMove(e);
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                _owner._hoverIndex = -1;
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
                        var it = _owner._items[idx];
                        if (it.Enabled && it.Type == FlatMenuItem.ItemType.Normal)
                        {
                            _owner.Close();
                            it.OnClick?.Invoke();
                        }
                    }
                }
                base.OnMouseDown(e);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var colors = AppTheme.Colors;

                using (var bg = new SolidBrush(colors.CardBg))
                    g.FillRectangle(bg, ClientRectangle);

                using (var pen = new Pen(colors.Divider, 1f))
                {
                    g.DrawLine(pen, 0, 0, Width, 0);
                    g.DrawLine(pen, 0, Height - 1, Width, Height - 1);
                }

                int y = 6;
                for (int i = 0; i < _owner._items.Count; i++)
                {
                    var it = _owner._items[i];
                    int h = it.GetHeight();
                    var rowRect = new Rectangle(0, y, Width, h);

                    if (it.Type == FlatMenuItem.ItemType.Separator)
                    {
                        using var pen = new Pen(colors.Divider, 1f);
                        int my = y + h / 2;
                        g.DrawLine(pen, ItemPadLeft, my, Width - ItemPadLeft, my);
                    }
                    else
                    {
                        bool hover = (i == _owner._hoverIndex) && it.Enabled;
                        DrawItem(g, it, rowRect, hover);
                    }
                    y += h;
                }
            }

            private void DrawItem(Graphics g, FlatMenuItem it, Rectangle rect, bool hover)
            {
                var colors = AppTheme.Colors;

                if (hover)
                {
                    using var brush = new SolidBrush(colors.HoverBg);
                    var inner = new Rectangle(4, rect.Y + 2, rect.Width - 8, rect.Height - 4);
                    using var path = GraphicsExtensions.GetRoundPath(inner, 4);
                    g.FillPath(brush, path);
                }

                Color fg;
                if (!it.Enabled) fg = colors.TextDisabled;
                else if (it.IsDanger) fg = colors.Danger;   // ← 从主题取
                else fg = colors.TextPrimary;

                int x = ItemPadLeft;

                if (!string.IsNullOrEmpty(it.Icon))
                {
                    var iconRect = new Rectangle(
                        x, rect.Y + (rect.Height - IconSize) / 2,
                        IconSize, IconSize);
                    IconRenderer.Draw(g, it.Icon, iconRect, fg, IconSize);
                }
                x += IconSize + TextGap;

                int shortcutWidth = 0;
                if (!string.IsNullOrEmpty(it.Shortcut))
                {
                    var sz = TextRenderer.MeasureText(g, it.Shortcut, AppTheme.SmallFont);
                    shortcutWidth = sz.Width + 24;
                }

                var textRect = new Rectangle(
                    x, rect.Y,
                    rect.Right - x - ShortcutGapRight - shortcutWidth,
                    rect.Height);
                TextRenderer.DrawText(g, it.Text ?? "", AppTheme.BodyFont, textRect, fg,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                if (!string.IsNullOrEmpty(it.Shortcut))
                {
                    var scRect = new Rectangle(
                        rect.Right - ShortcutGapRight - shortcutWidth, rect.Y,
                        shortcutWidth, rect.Height);
                    TextRenderer.DrawText(g, it.Shortcut, AppTheme.SmallFont, scRect,
                        colors.TextSecondary,
                        TextFormatFlags.Right | TextFormatFlags.VerticalCenter |
                        TextFormatFlags.NoPrefix);
                }
            }

            private int HitTest(Point p)
            {
                int y = 6;
                for (int i = 0; i < _owner._items.Count; i++)
                {
                    var it = _owner._items[i];
                    int h = it.GetHeight();
                    if (it.Type != FlatMenuItem.ItemType.Separator)
                    {
                        var rowRect = new Rectangle(0, y, Width, h);
                        if (rowRect.Contains(p)) return i;
                    }
                    y += h;
                }
                return -1;
            }
        }
    }
}