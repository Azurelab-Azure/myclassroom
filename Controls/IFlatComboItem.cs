using System.Drawing;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// 下拉框 / 列表里的"项"接口。
    /// 每项自己负责画自己。
    /// </summary>
    public interface IFlatComboItem
    {
        /// <summary>项在列表中的高度（像素）</summary>
        int Height { get; }

        /// <summary>纯文本摘要，用于 FlatComboBox 显示在收起状态</summary>
        string DisplayText { get; }

        /// <summary>绘制自己</summary>
        void Draw(Graphics g, Rectangle rect, bool hover, bool selected);
    }

    /// <summary>
    /// 纯文本项（用于课程名下拉等）。
    /// </summary>
    public class FlatTextItem : IFlatComboItem
    {
        public string Text { get; set; } = "";
        public int Height { get; set; } = 32;

        public string DisplayText => Text;

        public FlatTextItem() { }
        public FlatTextItem(string text) { Text = text ?? ""; }

        public void Draw(Graphics g, Rectangle rect, bool hover, bool selected)
        {
            var colors = AppTheme.Colors;

            Color bg;
            if (selected) bg = colors.SelectedBg;
            else if (hover) bg = colors.HoverBg;
            else bg = Color.Transparent;

            if (bg.A > 0)
            {
                using var brush = new SolidBrush(bg);
                g.FillRectangle(brush, rect);
            }

            var textRect = new Rectangle(rect.X + 12, rect.Y, rect.Width - 24, rect.Height);
            GraphicsExtensions.DrawTextLeft(g, Text, AppTheme.BodyFont, colors.TextPrimary, textRect);
        }

        public override string ToString() => Text;
    }
}