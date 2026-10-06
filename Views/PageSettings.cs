using System;
using System.Drawing;
using System.Windows.Forms;
using CourseApp.Theme;

namespace CourseApp.Views
{
    /// <summary>
    /// 设置页：左侧导航 + 右侧内容（嵌套式）。
    /// 主题切换由 Form1 统一触发 Invalidate。
    /// </summary>
    public class PageSettings : Panel
    {
        private readonly Form1 _owner;
        private SettingsNavView _nav = null!;
        private SettingsContentView _content = null!;

        public PageSettings(Form1 owner)
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