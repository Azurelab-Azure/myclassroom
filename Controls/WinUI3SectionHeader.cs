using System;
using System.Drawing;
using System.Windows.Forms;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// WinUI 3 风格分区标题：标题 + 描述 + 右侧操作。
    /// </summary>
    public class WinUI3SectionHeader : Panel
    {
        private readonly Label _title;
        private readonly Label _desc;

        public string Title
        {
            get => _title.Text;
            set => _title.Text = value ?? "";
        }

        public string Description
        {
            get => _desc.Text;
            set => _desc.Text = value ?? "";
        }

        public WinUI3SectionHeader()
        {
            Height = 48;
            BackColor = Color.Transparent;

            _title = new Label
            {
                Font = new Font(AppTheme.BodyFont.FontFamily, WinUI3Tokens.FontSubtitle, FontStyle.Bold),
                ForeColor = AppTheme.Colors.TextPrimary,
                Left = 0, Top = 0,
                AutoSize = true,
                BackColor = Color.Transparent,
            };
            Controls.Add(_title);

            _desc = new Label
            {
                Font = AppTheme.SmallFont,
                ForeColor = AppTheme.Colors.TextSecondary,
                Left = 0, Top = 26,
                AutoSize = true,
                BackColor = Color.Transparent,
            };
            Controls.Add(_desc);
        }
    }
}