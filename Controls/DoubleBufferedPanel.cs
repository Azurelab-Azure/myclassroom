using System.Drawing;
using System.Windows.Forms;

namespace CourseApp.Controls
{
    /// <summary>
    /// 开启双缓冲的 Panel，消除自绘时的闪烁。
    /// 支持透明背景。
    /// </summary>
    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;
        }
    }
}