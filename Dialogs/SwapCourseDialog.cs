using System;
using System.Drawing;
using System.Windows.Forms;
using CourseApp.Controls;
using CourseApp.Localization;
using CourseApp.Models;
using CourseApp.Theme;

namespace CourseApp.Dialogs
{
    /// <summary>
    /// 交换课程弹窗。
    /// 拖动 A 到 B 后：
    ///   - A 移到输入的新位置
    ///   - B 移到 A 的原位置
    /// </summary>
    public class SwapCourseDialog : FlatDialogBase
    {
        private readonly Course _courseA;      // 被拖动的课程
        private readonly Course _courseB;      // 目标位置已有的课程（可空）

        private FlatNumberBox _dayBox = null!;
        private FlatNumberBox _startBox = null!;
        private FlatNumberBox _endBox = null!;

        /// <summary>确定后——A 的新位置</summary>
        public int NewDay { get; private set; }
        public int NewStart { get; private set; }
        public int NewEnd { get; private set; }

        /// <summary>是否确认</summary>
        public bool Confirmed { get; private set; } = false;

        public SwapCourseDialog(Course courseA, Course? courseB, int targetDay, int targetSection)
            : base(I18n.T("swap.title"), 420, 380)
        {
            _courseA = courseA ?? throw new ArgumentNullException(nameof(courseA));
            _courseB = courseB;

            // 默认位置：目标位置
            NewDay = targetDay;
            NewStart = targetSection;
            NewEnd = targetSection + (courseA.TimeEnd - courseA.TimeStart);

            BuildUI();
        }

        private void BuildUI()
        {
            int x = 20;
            int y = 10;
            int labelW = 100;
            int fieldX = 130;
            int fieldW = 240;
            int rowH = 40;
            int gap = 12;

            // ---------- 提示 ----------
            string tip;
            if (_courseB != null)
            {
                tip = string.Format(I18n.T("swap.hint"), _courseA.Name, _courseB.Name);
            }
            else
            {
                tip = string.Format(I18n.T("swap.hintEmpty"), _courseA.Name);
            }

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

            // ---------- 星期 ----------
            AddLabel(I18n.T("swap.day"), x, y);
            _dayBox = new FlatNumberBox
            {
                Left = fieldX, Top = y, Width = 80, Height = 32,
                Minimum = 1, Maximum = 7, Value = NewDay,
            };
            ContentPanel.Controls.Add(_dayBox);
            y += rowH + gap;

            // ---------- 起始节 ----------
            AddLabel(I18n.T("swap.start"), x, y);
            _startBox = new FlatNumberBox
            {
                Left = fieldX, Top = y, Width = 80, Height = 32,
                Minimum = 1, Maximum = 30, Value = NewStart,
            };
            ContentPanel.Controls.Add(_startBox);
            y += rowH + gap;

            // ---------- 结束节 ----------
            AddLabel(I18n.T("swap.end"), x, y);
            _endBox = new FlatNumberBox
            {
                Left = fieldX, Top = y, Width = 80, Height = 32,
                Minimum = 1, Maximum = 30, Value = NewEnd,
            };
            ContentPanel.Controls.Add(_endBox);
            y += rowH + gap + 10;

            // ---------- 按钮 ----------
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

            // 密码验证
            using var pwdDlg = new PasswordDialog();
            if (pwdDlg.ShowDialog(this) != DialogResult.OK) return;
            if (!pwdDlg.Confirmed) return;

            NewDay = day;
            NewStart = start;
            NewEnd = end;
            Confirmed = true;

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}