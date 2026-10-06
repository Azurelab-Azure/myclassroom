using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// 自绘弹出层。用于 FlatComboBox 的下拉、右键菜单等。
    /// 主题切换由顶层 Form1 统一触发 Invalidate。
    /// </summary>
    public class FlatPopup : Form
    {
        private readonly Panel _contentHost;
        private Control? _content;

        public FlatPopup()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = AppTheme.Colors.CardBg;
            DoubleBuffered = true;
            Padding = new Padding(1);

            _contentHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Colors.CardBg,
                Padding = new Padding(0),
            };
            Controls.Add(_contentHost);

            Deactivate += (s, e) => Close();
        }

        /// <summary>设置内容控件和弹出尺寸。</summary>
        public void SetContent(Control content, int width, int height)
        {
            _contentHost.Controls.Clear();
            _content = content;
            _content.Dock = DockStyle.Fill;
            _contentHost.Controls.Add(_content);
            ClientSize = new Size(width + Padding.Horizontal, height + Padding.Vertical);
        }

        /// <summary>在屏幕坐标处显示。若超出屏幕底部，则在上方显示。</summary>
        public void ShowAt(Point screenPt)
        {
            Location = screenPt;

            var screen = Screen.FromPoint(screenPt).WorkingArea;

            // 右侧超出
            if (Right > screen.Right)
                Location = new Point(screen.Right - Width, Location.Y);

            // 底部超出 → 尝试向上弹出
            if (Bottom > screen.Bottom)
            {
                int aboveTop = screenPt.Y - Height;
                if (aboveTop >= screen.Top)
                    Location = new Point(Location.X, aboveTop);
                else
                    Location = new Point(Location.X, screen.Bottom - Height);
            }

            Show();
            BringToFront();
            Activate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var colors = AppTheme.Colors;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            using var path = GraphicsExtensions.GetRoundPath(rect, 8);
            using var bg = new SolidBrush(colors.CardBg);
            g.FillPath(bg, path);

            using var pen = new Pen(colors.CardBorder, 1f);
            g.DrawPath(pen, path);
        }
    }
}