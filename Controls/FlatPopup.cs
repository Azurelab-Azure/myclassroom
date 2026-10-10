using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// 自绘弹出层，用于下拉框和右键菜单
    /// </summary>
    public class FlatPopup : Form
    {
        private readonly Panel contentHost;
        private Control? content;

        /// <summary>
        /// 构造函数，初始化弹出层属性
        /// </summary>
        public FlatPopup()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = AppTheme.Colors.CardBg;
            DoubleBuffered = true;
            Padding = new Padding(1);

            contentHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Colors.CardBg,
                Padding = new Padding(0),
            };
            Controls.Add(contentHost);

            Deactivate += (s, e) => Close();
        }

        /// <summary>设置弹出层内容控件和尺寸</summary>
        public void SetContent(Control newContent, int width, int height)
        {
            contentHost.Controls.Clear();
            content = newContent;
            content.Dock = DockStyle.Fill;
            contentHost.Controls.Add(content);
            ClientSize = new Size(width + Padding.Horizontal, height + Padding.Vertical);
        }

        /// <summary>在屏幕坐标处显示弹出层，自动适配屏幕边界</summary>
        public void ShowAt(Point screenPoint)
        {
            Location = screenPoint;

            var screen = Screen.FromPoint(screenPoint).WorkingArea;

            if (Right > screen.Right)
                Location = new Point(screen.Right - Width, Location.Y);

            if (Bottom > screen.Bottom)
            {
                int aboveTop = screenPoint.Y - Height;
                if (aboveTop >= screen.Top)
                    Location = new Point(Location.X, aboveTop);
                else
                    Location = new Point(Location.X, screen.Bottom - Height);
            }

            Show();
            BringToFront();
            Activate();
        }

        /// <summary>绘制弹出层边框和背景</summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var colors = AppTheme.Colors;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            using var path = GraphicsExtensions.GetRoundPath(rect, 8);
            using var background = new SolidBrush(colors.CardBg);
            g.FillPath(background, path);

            using var pen = new Pen(colors.CardBorder, 1f);
            g.DrawPath(pen, path);
        }
    }
}