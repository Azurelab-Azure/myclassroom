using System.Drawing;
using System.Windows.Forms;
using CourseApp.Controls;
using CourseApp.Localization;
using CourseApp.Models;
using CourseApp.Theme;

namespace CourseApp.Dialogs
{
    /// <summary>
    /// 学生档案详情弹窗（只读）。
    /// 显示基本信息、等级、积分、徽章、健康、学籍、家长信息。
    /// </summary>
    public class StudentArchiveDialog : FlatDialogBase
    {
        /// <summary>要展示的学生</summary>
        private readonly Student _student;

        /// <summary>
        /// 构造学生档案弹窗。
        /// </summary>
        /// <param name="student">要展示的学生</param>
        public StudentArchiveDialog(Student student)
            : base(I18n.T("student.archive"), 560, 700)
        {
            _student = student;

            // 启用滚动模式
            EnableScroll();

            BuildUI();
        }

        /// <summary>
        /// 构建界面。
        /// </summary>
        private void BuildUI()
        {
            int x = 20;
            int y = 10;

            // 头像
            var avatar = new WinUI3Avatar
            {
                Left = x, Top = y,
                Width = 100, Height = 100,
                PhotoPath = _student.Photo ?? "",
                Initial = _student.Name ?? "?",
            };
            ScrollPanel!.Controls.Add(avatar);

            // 姓名
            var nameLbl = new Label
            {
                Text = _student.Name ?? "",
                Font = new Font(AppTheme.BodyFont.FontFamily, 22f, FontStyle.Bold),
                ForeColor = AppTheme.Colors.TextPrimary,
                Left = x + 120, Top = y + 10,
                Width = 380, Height = 36,
                BackColor = Color.Transparent,
                AutoSize = false,
            };
            ScrollPanel.Controls.Add(nameLbl);

            // 学号 + 等级
            var subLbl = new Label
            {
                Text = (_student.Id ?? "") + "  ·  " + (_student.Level ?? ""),
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.Colors.Accent,
                Left = x + 120, Top = y + 52,
                Width = 380, Height = 24,
                BackColor = Color.Transparent,
                AutoSize = false,
            };
            ScrollPanel.Controls.Add(subLbl);

            y += 120;

            // 基本行
            y = AddRow(y, I18n.T("student.gender"), GenderText(_student.Gender));
            y = AddRow(y, I18n.T("student.seat"), _student.Seat);
            y = AddRow(y, I18n.T("student.birth"), _student.BirthDate);
            y = AddRow(y, I18n.T("student.duty"), _student.Duty);

            // 积分 / 徽章
            y = AddSection(y, I18n.T("student.section.points"));
            y = AddRow(y, I18n.T("student.totalPoints"), _student.TotalPoints.ToString());
            y = AddRow(y, I18n.T("student.badge"), BadgeText(_student.Badge));

            // 综合评价
            y = AddSection(y, I18n.T("student.section.overall"));
            y = AddRow(y, I18n.T("student.levelScore"), _student.LevelScore.ToString());
            y = AddRow(y, I18n.T("student.overallScore"), _student.OverallScore.ToString());
            y = AddRow(y, I18n.T("student.overallComment"), _student.OverallComment);

            // 健康
            y = AddSection(y, I18n.T("student.section.health"));
            y = AddRow(y, I18n.T("student.healthStatus"), _student.HealthStatus);
            y = AddRow(y, I18n.T("student.healthNote"), _student.HealthNote);

            // 学籍
            y = AddSection(y, I18n.T("student.section.archive"));
            y = AddRow(y, I18n.T("student.archiveNo"), _student.ArchiveNo);
            y = AddRow(y, I18n.T("student.enrollment"), _student.EnrollmentDate);
            y = AddRow(y, I18n.T("student.political"), _student.PoliticalStatus);
            y = AddRow(y, I18n.T("student.hometown"), _student.Hometown);
            y = AddRow(y, I18n.T("student.ethnicity"), _student.Ethnicity);

            // 家长
            y = AddSection(y, I18n.T("student.section.parent"));
            y = AddRow(y, I18n.T("student.parent"), _student.ParentName);
            y = AddRow(y, I18n.T("student.parentPhone"), _student.ParentPhone);
            y = AddRow(y, I18n.T("student.address"), _student.Address);
            y = AddRow(y, I18n.T("student.remark"), _student.Remark);

            y += 10;

            // 关闭按钮
            var btnClose = new FlatButton
            {
                Text = I18n.T("common.close"),
                ButtonStyle = FlatButtonStyle.Primary,
                Width = 100, Height = 36,
                Left = 420, Top = y,
            };
            btnClose.Click += (s, e) => Close();
            ScrollPanel.Controls.Add(btnClose);

            // 告诉滚动宿主内容实际高度
            SetScrollContentHeight(y + btnClose.Height + 40);
        }

        /// <summary>
        /// 添加分区标题。
        /// </summary>
        private int AddSection(int y, string title)
        {
            y += 10;
            var lbl = new Label
            {
                Text = title,
                Font = new Font(AppTheme.BodyFont.FontFamily, 13f, FontStyle.Bold),
                ForeColor = AppTheme.Colors.Accent,
                Left = 20, Top = y,
                Width = 500, Height = 24,
                BackColor = Color.Transparent,
                AutoSize = false,
            };
            ScrollPanel!.Controls.Add(lbl);
            return y + 30;
        }

        /// <summary>
        /// 添加一行键值。
        /// </summary>
        private int AddRow(int y, string label, string? value)
        {
            var lbl = new Label
            {
                Text = label,
                Font = AppTheme.SmallFont,
                ForeColor = AppTheme.Colors.TextSecondary,
                Left = 20, Top = y, Width = 100, Height = 22,
                BackColor = Color.Transparent,
                AutoSize = false,
            };
            ScrollPanel!.Controls.Add(lbl);

            var val = new Label
            {
                Text = string.IsNullOrEmpty(value) ? "—" : value,
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.Colors.TextPrimary,
                Left = 130, Top = y, Width = 390, Height = 22,
                BackColor = Color.Transparent,
                AutoSize = false,
            };
            ScrollPanel.Controls.Add(val);

            return y + 28;
        }

        /// <summary>
        /// 性别翻译。
        /// </summary>
        private string GenderText(string gender)
        {
            if (gender == "female") return I18n.T("student.gender.female");
            if (gender == "male") return I18n.T("student.gender.male");
            return "";
        }

        /// <summary>
        /// 徽章翻译。
        /// </summary>
        private string BadgeText(string badge)
        {
            if (string.IsNullOrEmpty(badge)) return I18n.T("badge.none");
            return badge;
        }
    }
}