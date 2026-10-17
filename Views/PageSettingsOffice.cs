using System;
using System.Drawing;
using System.Windows.Forms;
using CourseApp.Services;
using CourseApp.Theme;

namespace CourseApp.Views
{
    /// <summary>
    /// 办公模式的设置页外壳。
    /// 左侧导航复用，右侧内容用办公专用视图。
    /// </summary>
    public class PageSettingsOffice : Panel
    {
        /// <summary>主窗口接口引用</summary>
        private readonly IMainForm _owner;

        /// <summary>左侧导航</summary>
        private SettingsNavView _nav = null!;

        /// <summary>右侧内容</summary>
        private SettingsOfficeView _content = null!;

        /// <summary>
        /// 构建办公模式设置页。
        /// </summary>
        /// <param name="owner">主窗口（Form2）</param>
        public PageSettingsOffice(IMainForm owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Colors.WindowBg;

            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            _content = new SettingsOfficeView(_owner);
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