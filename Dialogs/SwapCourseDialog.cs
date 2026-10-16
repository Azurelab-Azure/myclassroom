using System;
using System.Drawing;
using System.Windows.Forms;
using CourseApp.Controls;
using CourseApp.Localization;
using CourseApp.Models;
using CourseApp.Services;
using CourseApp.Theme;

namespace CourseApp.Dialogs
{
    /// <summary>
    /// 交换课程弹窗。
    /// 拖动 A 到 B 后，A 移到新位置，B 移到 A 的原位置。
    /// 是否校验密码由配置决定。
    /// </summary>
    public class SwapCourseDialog : FlatDialogBase
    {
        /// <summary>被拖动的课程</summary>
        private readonly Course _courseA;

        /// <summary>目标位置已有的课程（可空）</summary>
        private readonly Course? _courseB;

        /// <summary>星期输入框</summary>
        private FlatNumberBox _dayBox = null!;

        /// <summary>起始节输入框</summary>
        private FlatNumberBox _startBox = null!;

        /// <summary>结束节输入框</summary>
        private FlatNumberBox _endBox = null!;

        /// <summary>确定后 A 的新星期</summary>
        public int NewDay { get; private set; }

        /// <summary>确定后 A 的新起始节</summary>
        public int NewStart { get; private set; }

        /// <summary>确定后 A 的新结束节</summary>
        public int NewEnd { get; private set; }

        /// <summary>是否确认</summary>
        public bool Confirmed { get; private set; } = false;

        /// <summary>
        /// 构造交换课程弹窗。
        /// </summary>
        /// <param name="courseA">被拖动的课程</param>
        /// <param name="courseB">目标位置已有的课程，可为空</param>
        /// <param name="targetDay">目标星期</param>
        /// <param name="targetSection">目标起始节</param>
        public SwapCourseDialog(Course courseA, Course? courseB, int targetDay, int targetSection)
            : base(I18n.T("swap.title"), 420, 400)
        {
            _courseA = courseA ?? throw new ArgumentNullException(nameof(courseA));
            _courseB = courseB;

            NewDay = targetDay;
            NewStart = targetSection;
            NewEnd = targetSection + (courseA.TimeEnd - courseA.TimeStart);

            BuildUI();
        }

        /// <summary>
        /// 构建界面。
        /// </summary>
        private void BuildUI()
        {
            int x = 20;
            int y = 10;
            int fieldX = 130;
            int rowH = 40;
            int gap = 12;

            // 提示文字
            string tip = _courseB != null
                ? string.Format(I18n.T("swap.hint"), _courseA.Name, _courseB.Name)
                : string.Format(I18n.T("swap.hintEmpty"), _courseA.Name);

            ContentPanel.Controls.Add(new Label
            {
                Text = tip,
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.Colors.TextPrimary,
                Left = x, Top = y, Width = 380, Height = 60,
                BackColor = Color.Transparent,
                AutoSize = false,
            });
            y += 60;

            // 星期
            AddLabel(I18n.T("swap.day"), x, y);
            _dayBox = new FlatNumberBox
            {
                Left = fieldX, Top = y, Width = 80, Height = 32,
                Minimum = 1, Maximum = 7, Value = NewDay,
            };
            ContentPanel.Controls.Add(_dayBox);
            y += rowH + gap;

            // 起始节
            AddLabel(I18n.T("swap.start"), x, y);
            _startBox = new FlatNumberBox
            {
                Left = fieldX, Top = y, Width = 80, Height = 32,
                Minimum = 1, Maximum = 30, Value = NewStart,
            };
            ContentPanel.Controls.Add(_startBox);
            y += rowH + gap;

            // 结束节
            AddLabel(I18n.T("swap.end"), x, y);
            _endBox = new FlatNumberBox
            {
                Left = fieldX, Top = y, Width = 80, Height = 32,
                Minimum = 1, Maximum = 30, Value = NewEnd,
            };
            ContentPanel.Controls.Add(_endBox);
            y += rowH + gap + 10;

            // 按钮
            var btnOk = new FlatButton
            {
                Text = I18n.T("common.ok"),
                ButtonStyle = FlatButtonStyle.Primary,
                Width = 100, Height = 36,
                Left = 130, Top = y,
            };
            btnOk.Click += (s, e) => Confirm();
            ContentPanel.Controls.Add(btnOk);

            var btnCancel = new FlatButton
            {
                Text = I18n.T("common.cancel"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Width = 100, Height = 36,
                Left = 240, Top = y,
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            ContentPanel.Controls.Add(btnCancel);
        }

        /// <summary>
        /// 添加字段标签。
        /// </summary>
        private void AddLabel(string text, int x, int y)
        {
            ContentPanel.Controls.Add(new Label
            {
                Text = text,
                Left = x, Top = y + 6, Width = 100,
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.Colors.TextPrimary,
                BackColor = Color.Transparent,
                AutoSize = false,
            });
        }

        /// <summary>
        /// 确认：校验 + 密码。
        /// </summary>
        private void Confirm()
        {
            int day = _dayBox.Value;
            int start = _startBox.Value;
            int end = _endBox.Value;

            if (end < start)
            {
                MessageDialog.ShowInfo(I18n.T("common.info"), I18n.T("swap.endBeforeStart"));
                return;
            }

            // 密码校验（仅配置开启时）
            if (ConfigService.Load().RequirePasswordOnCourseEdit)
            {
                using var pwdDlg = new PasswordDialog();
                if (pwdDlg.ShowDialog(this) != DialogResult.OK) return;
                if (!pwdDlg.Confirmed) return;
            }

            NewDay = day;
            NewStart = start;
            NewEnd = end;
            Confirmed = true;

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}