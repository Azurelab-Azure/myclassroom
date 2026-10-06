using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CourseApp.Controls;
using CourseApp.Localization;
using CourseApp.Models;
using CourseApp.Theme;

namespace CourseApp.Dialogs
{
    /// <summary>
    /// 添加 / 修改课程弹窗。
    /// 课程名：可打字，也可点预设按钮填入。
    /// </summary>
    public class CourseEditorDialog : FlatDialogBase
    {
        private readonly Course? _existing;
        private readonly int _day;
        private readonly int _section;
        private readonly int _currentWeek;
        private readonly List<Teacher> _teachers;

        // 课程名：文本框 + 预设按钮
        private FlatTextBox _courseNameBox = null!;
        private FlatComboBox _teacherBox = null!;
        private FlatTextBox _roomBox = null!;
        private FlatComboBox _loopBox = null!;
        private FlatNumberBox _weekStartBox = null!;
        private FlatNumberBox _weekEndBox = null!;
        private FlatTextBox _pwdBox = null!;

        private static readonly string[] PresetCourses =
        {
            "语文", "数学", "英语", "物理", "化学", "生物",
            "政治", "历史", "地理", "体育", "音乐", "美术",
            "信息技术", "自习", "班会",
        };

        public Course? ResultCourse { get; private set; }

        public CourseEditorDialog(Course? existing, int day, int section,
            List<Teacher> teachers, int currentWeek)
            : base(existing == null ? I18n.T("course.add") : I18n.T("course.edit"), 520, 620)
        {
            _existing = existing;
            _day = day;
            _section = section;
            _teachers = teachers ?? new List<Teacher>();
            _currentWeek = currentWeek;

            BuildUI();
            FillValues();
        }

        // =====================================================
        // UI
        // =====================================================
        private void BuildUI()
        {
            int fieldX = 110;
            int fieldW = 370;
            int rowH = 36;
            int gap = 10;
            int y = 10;

            // ============ 课程名：文本框 ============
            AddLabel(I18n.T("course.name"), y);
            _courseNameBox = new FlatTextBox
            {
                Left = fieldX, Top = y, Width = fieldW, Height = 32,
                Placeholder = I18n.T("course.namePlaceholder"),
            };
            ContentPanel.Controls.Add(_courseNameBox);
            y += rowH;

            // ============ 预设课程：小按钮一排 ============
            AddLabel(I18n.T("course.presets"), y);
            var presetPanel = new Panel
            {
                Left = fieldX, Top = y, Width = fieldW, Height = 28,
                BackColor = Color.Transparent,
            };
            ContentPanel.Controls.Add(presetPanel);

            int px = 0;
            int py = 0;
            const int btnH = 26;
            const int btnGap = 4;

            foreach (var preset in PresetCourses)
            {
                // 估算宽度
                int textW;
                try
                {
                    using var bmp = new Bitmap(1, 1);
                    using var g = Graphics.FromImage(bmp);
                    textW = TextRenderer.MeasureText(g, preset, AppTheme.SmallFont).Width;
                }
                catch { textW = preset.Length * 12; }

                int btnW = textW + 16;

                // 换行
                if (px + btnW > fieldW)
                {
                    px = 0;
                    py += btnH + btnGap;
                }

                var btn = new FlatButton
                {
                    Text = preset,
                    ButtonStyle = FlatButtonStyle.Secondary,
                    Left = px, Top = py,
                    Width = btnW, Height = btnH,
                };
                var p = preset;
                btn.Click += (s, e) =>
                {
                    _courseNameBox.Text = p;
                    _courseNameBox.Focus();
                };
                presetPanel.Controls.Add(btn);

                px += btnW + btnGap;
            }
            // 预设面板高度
            presetPanel.Height = py + btnH;

            y += presetPanel.Height + gap;

            // ============ 教师 ============
            AddLabel(I18n.T("course.teacher"), y);
            _teacherBox = new FlatComboBox { Left = fieldX, Top = y, Width = fieldW, Height = 32 };
            _teacherBox.SetItems(_teachers.Select(t => (IFlatComboItem)new TeacherItem(t)));
            ContentPanel.Controls.Add(_teacherBox);
            y += rowH + gap;

            // ============ 教室 ============
            AddLabel(I18n.T("course.classroom"), y);
            _roomBox = new FlatTextBox { Left = fieldX, Top = y, Width = fieldW, Height = 32 };
            ContentPanel.Controls.Add(_roomBox);
            y += rowH + gap;

            // ============ 循环 ============
            AddLabel(I18n.T("course.loop"), y);
            _loopBox = new FlatComboBox { Left = fieldX, Top = y, Width = 160, Height = 32 };
            _loopBox.SetItems(new IFlatComboItem[]
            {
                new FlatTextItem(I18n.T("course.loopEvery")),
                new FlatTextItem(I18n.T("course.loopOdd")),
                new FlatTextItem(I18n.T("course.loopEven")),
            });
            _loopBox.SelectedIndex = 0;
            ContentPanel.Controls.Add(_loopBox);
            y += rowH + gap;

            // ============ 周次范围 ============
            AddLabel(I18n.T("course.weekRange"), y);
            _weekStartBox = new FlatNumberBox
            {
                Left = fieldX, Top = y, Width = 80, Height = 32,
                Minimum = 1, Maximum = 30, Value = 1,
            };
            ContentPanel.Controls.Add(_weekStartBox);

            ContentPanel.Controls.Add(new Label
            {
                Text = "~",
                Left = fieldX + 90, Top = y + 6, Width = 20,
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.Colors.TextPrimary,
                BackColor = Color.Transparent,
                AutoSize = false,
            });

            _weekEndBox = new FlatNumberBox
            {
                Left = fieldX + 120, Top = y, Width = 80, Height = 32,
                Minimum = 1, Maximum = 30, Value = 20,
            };
            ContentPanel.Controls.Add(_weekEndBox);
            y += rowH + gap;

            // ============ 密码 ============
            AddLabel(I18n.T("course.password"), y);
            _pwdBox = new FlatTextBox
            {
                Left = fieldX, Top = y, Width = fieldW, Height = 32,
                PasswordChar = '*',
            };
            ContentPanel.Controls.Add(_pwdBox);
            y += rowH + gap + 10;

            // ============ 按钮 ============
            var btnOk = new FlatButton
            {
                Text = I18n.T("dialog.ok"),
                ButtonStyle = FlatButtonStyle.Primary,
                Width = 100, Height = 36,
                Left = 240, Top = y,
            };
            btnOk.Click += BtnOk_Click;
            ContentPanel.Controls.Add(btnOk);

            var btnCancel = new FlatButton
            {
                Text = I18n.T("dialog.cancel"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Width = 100, Height = 36,
                Left = 350, Top = y,
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            ContentPanel.Controls.Add(btnCancel);
        }

        private void AddLabel(string text, int top)
        {
            ContentPanel.Controls.Add(new Label
            {
                Text = text,
                Left = 20, Top = top + 6, Width = 80,
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.Colors.TextPrimary,
                BackColor = Color.Transparent,
                AutoSize = false,
            });
        }

        // =====================================================
        // 填值（编辑模式）
        // =====================================================
        private void FillValues()
        {
            if (_existing == null) return;

            // 课程名
            _courseNameBox.Text = _existing.Name ?? "";

            // 教师
            for (int i = 0; i < _teacherBox.Items.Count; i++)
            {
                if (_teacherBox.Items[i].DisplayText == _existing.Teacher)
                {
                    _teacherBox.SelectedIndex = i;
                    break;
                }
            }

            // 教室
            _roomBox.Text = _existing.Classroom ?? "";

            // 周次
            if (_existing.Weeks != null && _existing.Weeks.Count > 0)
            {
                var sorted = _existing.Weeks.OrderBy(w => w).ToList();
                _weekStartBox.Value = sorted.First();
                _weekEndBox.Value = sorted.Last();

                bool allOdd = sorted.All(w => w % 2 == 1);
                bool allEven = sorted.All(w => w % 2 == 0);
                if (allOdd && !allEven && sorted.Count > 1) _loopBox.SelectedIndex = 1;
                else if (allEven && !allOdd && sorted.Count > 1) _loopBox.SelectedIndex = 2;
                else _loopBox.SelectedIndex = 0;
            }
        }

        // =====================================================
        // 确定
        // =====================================================
        private void BtnOk_Click(object? sender, EventArgs e)
        {
            // 密码校验
            if (_pwdBox.Text != "12345678")
            {
                MessageDialog.ShowError(I18n.T("dialog.error"), I18n.T("dialog.passwordWrong"));
                return;
            }

            string courseName = _courseNameBox.Text.Trim();
            if (string.IsNullOrEmpty(courseName))
            {
                MessageDialog.ShowInfo(I18n.T("dialog.info"), I18n.T("dialog.needCourseName"));
                return;
            }

            string teacherName = _teacherBox.SelectedText?.Trim() ?? "";
            string classroom = _roomBox.Text.Trim();
            string loopType = _loopBox.SelectedText ?? "";
            int wStart = _weekStartBox.Value;
            int wEnd = _weekEndBox.Value;

            // 生成 weeks
            var weeks = new List<int>();
            for (int w = wStart; w <= wEnd; w++)
            {
                if (loopType == I18n.T("course.loopEvery")) weeks.Add(w);
                else if (loopType == I18n.T("course.loopOdd") && w % 2 == 1) weeks.Add(w);
                else if (loopType == I18n.T("course.loopEven") && w % 2 == 0) weeks.Add(w);
            }
            if (!weeks.Contains(_currentWeek)) weeks.Add(_currentWeek);

            if (_existing == null)
            {
                ResultCourse = new Course
                {
                    Name = courseName,
                    Teacher = teacherName,
                    Classroom = classroom,
                    Weeks = weeks,
                    WeekDay = _day,
                    TimeStart = _section,
                    TimeEnd = _section,
                };
            }
            else
            {
                _existing.Name = courseName;
                _existing.Teacher = teacherName;
                _existing.Classroom = classroom;
                _existing.Weeks = weeks;
                ResultCourse = _existing;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}