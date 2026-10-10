using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// 自绘右键菜单，主题切换由顶层 Form1 统一触发重绘
    /// </summary>
    public class FlatContextMenu : IDisposable
    {
        private readonly List<FlatMenuItem> items = new();
        private int hoverIndex = -1;

        private FlatPopup? popup;
        private FlatMenuHost? menuHost;

        private const int ItemPaddingLeft = 12;
        private const int IconSize = 16;
        private const int TextGap = 12;
        private const int ShortcutGapRight = 12;
        private const int MinWidth = 160;

        /// <summary>添加菜单项</summary>
        public FlatContextMenu AddItem(FlatMenuItem item)
        {
            items.Add(item);
            return this;
        }

        /// <summary>添加分隔线</summary>
        public FlatContextMenu AddSeparator()
        {
            items.Add(FlatMenuItem.CreateSeparator());
            return this;
        }

        /// <summary>清空所有菜单项</summary>
        public FlatContextMenu Clear()
        {
            items.Clear();
            return this;
        }

        /// <summary>所有菜单项</summary>
        public IReadOnlyList<FlatMenuItem> Items => items;

        /// <summary>在指定屏幕坐标处显示菜单</summary>
        public void ShowAt(Control owner, Point screenPoint)
        {
            if (items.Count == 0) return;

            int width = MeasureWidth();
            int height = MeasureHeight();

            menuHost = new FlatMenuHost(this);
            menuHost.SetBounds(0, 0, width, height);

            popup = new FlatPopup();
            popup.SetContent(menuHost, width, height);
            popup.FormClosed += (s, e) =>
            {
                popup = null;
                menuHost = null;
                hoverIndex = -1;
            };

            popup.ShowAt(screenPoint);
        }

        /// <summary>关闭菜单</summary>
        public void Close()
        {
            popup?.Close();
            popup = null;
            menuHost = null;
        }

        /// <summary>释放资源</summary>
        public void Dispose() => Close();

        /// <summary>计算菜单宽度</summary>
        private int MeasureWidth()
        {
            int width = MinWidth;

            using var bitmap = new Bitmap(1, 1);
            using var g = Graphics.FromImage(bitmap);

            var font = AppTheme.BodyFont;
            var smallFont = AppTheme.SmallFont;

            foreach (var item in items)
            {
                if (item.Type == FlatMenuItem.ItemType.Separator) continue;

                int rowWidth = ItemPaddingLeft + IconSize + TextGap;
                rowWidth += TextRenderer.MeasureText(g, item.Text ?? "", font).Width;

                if (!string.IsNullOrEmpty(item.Shortcut))
                {
                    rowWidth += 24;
                    rowWidth += TextRenderer.MeasureText(g, item.Shortcut, smallFont).Width;
                }
                rowWidth += ShortcutGapRight;

                if (rowWidth > width) width = rowWidth;
            }
            return width;
        }

        /// <summary>计算菜单高度</summary>
        private int MeasureHeight()
        {
            int height = 6;
            foreach (var item in items) height += item.GetHeight();
            height += 6;
            return height;
        }

        /// <summary>
        /// 菜单绘制宿主控件，负责菜单的绘制和交互
        /// </summary>
        private class FlatMenuHost : Control
        {
            private readonly FlatContextMenu owner;

            /// <summary>构造函数，设置双缓冲绘制</summary>
            public FlatMenuHost(FlatContextMenu menuOwner)
            {
                owner = menuOwner;
                SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.ResizeRedraw, true);
            }

            /// <summary>鼠标移动时更新悬停项</summary>
            protected override void OnMouseMove(MouseEventArgs e)
            {
                int index = HitTest(e.Location);
                if (index != owner.hoverIndex)
                {
                    owner.hoverIndex = index;
                    Invalidate();
                }
                base.OnMouseMove(e);
            }

            /// <summary>鼠标离开时清除悬停状态</summary>
            protected override void OnMouseLeave(EventArgs e)
            {
                owner.hoverIndex = -1;
                Invalidate();
                base.OnMouseLeave(e);
            }

            /// <summary>鼠标点击时执行菜单项</summary>
            protected override void OnMouseDown(MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    int index = HitTest(e.Location);
                    if (index >= 0)
                    {
                        var item = owner.items[index];
                        if (item.Enabled && item.Type == FlatMenuItem.ItemType.Normal)
                        {
                            owner.Close();
                            item.OnClick?.Invoke();
                        }
                    }
                }
                base.OnMouseDown(e);
            }

            /// <summary>绘制菜单内容</summary>
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var colors = AppTheme.Colors;

                using (var background = new SolidBrush(colors.CardBg))
                    g.FillRectangle(background, ClientRectangle);

                using (var pen = new Pen(colors.Divider, 1f))
                {
                    g.DrawLine(pen, 0, 0, Width, 0);
                    g.DrawLine(pen, 0, Height - 1, Width, Height - 1);
                }

                int y = 6;
                for (int i = 0; i < owner.items.Count; i++)
                {
                    var item = owner.items[i];
                    int height = item.GetHeight();
                    var rowRect = new Rectangle(0, y, Width, height);

                    if (item.Type == FlatMenuItem.ItemType.Separator)
                    {
                        using var pen = new Pen(colors.Divider, 1f);
                        int middleY = y + height / 2;
                        g.DrawLine(pen, ItemPaddingLeft, middleY, Width - ItemPaddingLeft, middleY);
                    }
                    else
                    {
                        bool isHover = (i == owner.hoverIndex) && item.Enabled;
                        DrawItem(g, item, rowRect, isHover);
                    }
                    y += height;
                }
            }

            /// <summary>绘制单个菜单项</summary>
            private void DrawItem(Graphics g, FlatMenuItem item, Rectangle rect, bool isHover)
            {
                var colors = AppTheme.Colors;

                if (isHover)
                {
                    using var brush = new SolidBrush(colors.HoverBg);
                    var innerRect = new Rectangle(4, rect.Y + 2, rect.Width - 8, rect.Height - 4);
                    using var path = GraphicsExtensions.GetRoundPath(innerRect, 4);
                    g.FillPath(brush, path);
                }

                Color foreground;
                if (!item.Enabled) foreground = colors.TextDisabled;
                else if (item.IsDanger) foreground = colors.Danger;
                else foreground = colors.TextPrimary;

                int x = ItemPaddingLeft;

                if (!string.IsNullOrEmpty(item.Icon))
                {
                    var iconRect = new Rectangle(
                        x, rect.Y + (rect.Height - IconSize) / 2,
                        IconSize, IconSize);
                    IconRenderer.Draw(g, item.Icon, iconRect, foreground, IconSize);
                }
                x += IconSize + TextGap;

                int shortcutWidth = 0;
                if (!string.IsNullOrEmpty(item.Shortcut))
                {
                    var size = TextRenderer.MeasureText(g, item.Shortcut, AppTheme.SmallFont);
                    shortcutWidth = size.Width + 24;
                }

                var textRect = new Rectangle(
                    x, rect.Y,
                    rect.Right - x - ShortcutGapRight - shortcutWidth,
                    rect.Height);
                TextRenderer.DrawText(g, item.Text ?? "", AppTheme.BodyFont, textRect, foreground,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                if (!string.IsNullOrEmpty(item.Shortcut))
                {
                    var shortcutRect = new Rectangle(
                        rect.Right - ShortcutGapRight - shortcutWidth, rect.Y,
                        shortcutWidth, rect.Height);
                    TextRenderer.DrawText(g, item.Shortcut, AppTheme.SmallFont, shortcutRect,
                        colors.TextSecondary,
                        TextFormatFlags.Right | TextFormatFlags.VerticalCenter |
                        TextFormatFlags.NoPrefix);
                }
            }

            /// <summary>根据鼠标位置命中测试菜单项索引</summary>
            private int HitTest(Point point)
            {
                int y = 6;
                for (int i = 0; i < owner.items.Count; i++)
                {
                    var item = owner.items[i];
                    int height = item.GetHeight();
                    if (item.Type != FlatMenuItem.ItemType.Separator)
                    {
                        var rowRect = new Rectangle(0, y, Width, height);
                        if (rowRect.Contains(point)) return i;
                    }
                    y += height;
                }
                return -1;
            }
        }
    }
}