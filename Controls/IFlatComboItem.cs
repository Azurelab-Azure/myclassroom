using System.Drawing;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// 下拉框和列表中的项接口，每项负责绘制自身
    /// </summary>
    public interface IFlatComboItem
    {
        /// <summary>项在列表中的高度</summary>
        int Height { get; }

        /// <summary>用于收起状态显示的纯文本摘要</summary>
        string DisplayText { get; }

        /// <summary>绘制该项内容</summary>
        void Draw(Graphics g, Rectangle rect, bool hover, bool selected);
    }

    /// <summary>
    /// 纯文本项实现，用于简单的下拉列表
    /// </summary>
    public class FlatTextItem : IFlatComboItem
    {
        /// <summary>显示文本</summary>
        public string Text { get; set; } = "";

        /// <summary>项高度</summary>
        public int Height { get; set; } = 32;

        /// <summary>显示文本</summary>
        public string DisplayText => Text;

        /// <summary>默认构造函数</summary>
        public FlatTextItem() { }

        /// <summary>带文本参数的构造函数</summary>
        public FlatTextItem(string text) { Text = text ?? ""; }

        /// <summary>绘制纯文本项</summary>
        public void Draw(Graphics g, Rectangle rect, bool hover, bool selected)
        {
            var colors = AppTheme.Colors;

            Color background;
            if (selected) background = colors.SelectedBg;
            else if (hover) background = colors.HoverBg;
            else background = Color.Transparent;

            if (background.A > 0)
            {
                using var brush = new SolidBrush(background);
                g.FillRectangle(brush, rect);
            }

            var textRect = new Rectangle(rect.X + 12, rect.Y, rect.Width - 24, rect.Height);
            GraphicsExtensions.DrawTextLeft(g, Text, AppTheme.BodyFont, colors.TextPrimary, textRect);
        }

        /// <summary>返回显示文本</summary>
        public override string ToString() => Text;
    }
}