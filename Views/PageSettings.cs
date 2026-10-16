using System;
using System.Drawing;
using System.Windows.Forms;
using CourseApp.Services;
using CourseApp.Theme;

namespace CourseApp.Views
{
    /// <summary>
    /// 设置页：左侧导航 + 右侧内容（嵌套式）。
    /// 主题切换由主窗口统一触发 Invalidate。
    /// </summary>
    public class PageSettings : Panel
    {
        /// <summary>主窗口接口引用</summary>
        private readonly IMainForm _owner;

        /// <summary>左侧导航</summary>
        private SettingsNavView _nav = null!;

        /// <summary>右侧内容</summary>
        private SettingsContentView _content = null!;

        /// <summary>
        /// 构建设置页。
        /// </summary>
        /// <param name="owner">主窗口（Form1 或 Form2）</param>
        public PageSettings(IMainForm owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Colors.WindowBg;

            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            _content = new SettingsContentView(_owner);
            Controls.Add(_content);

            _nav = new SettingsNavView();
            _nav.ItemClicked += idx => _content.SetPage(idx);
            Controls.Add(_nav);

            // 默认选中"个性化"
            _nav.SelectedIndex = 1;
            _content.SetPage(1);
        }
    }
}