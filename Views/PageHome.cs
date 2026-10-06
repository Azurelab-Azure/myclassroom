using System;
using System.Drawing;
using System.Windows.Forms;
using CourseApp.Theme;

namespace CourseApp.Views
{
    /// <summary>
    /// 主页：工具栏 + 课表。
    /// 主题切换由 Form1 统一触发 Invalidate。
    /// </summary>
    public class PageHome : Panel
    {
        public ToolbarView Toolbar { get; private set; } = null!;
        public ScheduleGridView ScheduleGrid { get; private set; } = null!;

        public PageHome()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Colors.WindowBg;

            Toolbar = new ToolbarView { Dock = DockStyle.Top };
            ScheduleGrid = new ScheduleGridView { Dock = DockStyle.Fill };

            Controls.Add(ScheduleGrid);
            Controls.Add(Toolbar);
        }
    }
}