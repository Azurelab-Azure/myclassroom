using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CourseApp.Controls;
using CourseApp.Localization;
using CourseApp.Models;
using CourseApp.Theme;

namespace CourseApp.Dialogs
{
    /// <summary>新建 / 编辑考试。</summary>
    public class ExamEditorDialog : FlatDialogBase
    {
        private readonly ExamRecord? _editing;

        private FlatTextBox _nameBox = null!;
        private FlatTextBox _subjectBox = null!;
        private FlatTextBox _dateBox = null!;
        private FlatNumberBox _fullBox = null!;
        private FlatNumberBox _passBox = null!;

        public ExamRecord? Result { get; private set; }

        public ExamEditorDialog(ExamRecord? existing, List<Student> students)
            : base(existing == null ? I18n.T("score.addExam") : I18n.T("score.editExam"), 460, 380)
        {
            _editing = existing;
            BuildUI();
            if (existing != null) LoadFrom(existing);
        }

        private void BuildUI()
        {
            int x = 20, y = 10;
            int fieldX = 130, fieldW = 280, rowH = 36, gap = 10;

            AddLabel(I18n.T("score.examName"), x, y);
            _nameBox = new FlatTextBox { Left = fieldX, Top = y, Width = fieldW, Height = 32 };
            ContentPanel.Controls.Add(_nameBox);
            y += rowH + gap;

            AddLabel(I18n.T("score.subject"), x, y);
            _subjectBox = new FlatTextBox { Left = fieldX, Top = y, Width = fieldW, Height = 32 };
            ContentPanel.Controls.Add(_subjectBox);
            y += rowH + gap;

            AddLabel(I18n.T("score.date"), x, y);
            _dateBox = new FlatTextBox
            {
                Left = fieldX, Top = y, Width = fieldW, Height = 32,
                Placeholder = "yyyy-MM-dd",
                Text = DateTime.Now.ToString("yyyy-MM-dd"),
            };
            ContentPanel.Controls.Add(_dateBox);
            y += rowH + gap;

            AddLabel(I18n.T("score.fullScore"), x, y);
            _fullBox = new FlatNumberBox
            {
                Left = fieldX, Top = y, Width = 120, Height = 32,
                Minimum = 1, Maximum = 1000, Value = 100,
            };
            ContentPanel.Controls.Add(_fullBox);
            y += rowH + gap;

            AddLabel(I18n.T("score.passScore"), x, y);
            _passBox = new FlatNumberBox
            {
                Left = fieldX, Top = y, Width = 120, Height = 32,
                Minimum = 1, Maximum = 1000, Value = 60,
            };
            ContentPanel.Controls.Add(_passBox);
            y += rowH + gap + 10;

            var btnOk = new FlatButton
            {
                Text = I18n.T("dialog.save"),
                ButtonStyle = FlatButtonStyle.Primary,
                Width = 100, Height = 36,
                Left = 200, Top = y,
            };
            btnOk.Click += BtnOk_Click;
            ContentPanel.Controls.Add(btnOk);

            var btnCancel = new FlatButton
            {
                Text = I18n.T("dialog.cancel"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Width = 100, Height = 36,
                Left = 310, Top = y,
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

        private void LoadFrom(ExamRecord e)
        {
            _nameBox.Text = e.Name ?? "";
            _subjectBox.Text = e.Subject ?? "";
            _dateBox.Text = e.Date ?? "";
            _fullBox.Value = (int)e.FullScore;
            _passBox.Value = (int)e.PassScore;
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            string name = _nameBox.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageDialog.ShowInfo(I18n.T("common.info"), I18n.T("score.needExamName"));
                return;
            }

            if (_editing == null)
            {
                Result = new ExamRecord
                {
                    Name = name,
                    Subject = _subjectBox.Text.Trim(),
                    Date = _dateBox.Text.Trim(),
                    FullScore = _fullBox.Value,
                    PassScore = _passBox.Value,
                };
            }
            else
            {
                _editing.Name = name;
                _editing.Subject = _subjectBox.Text.Trim();
                _editing.Date = _dateBox.Text.Trim();
                _editing.FullScore = _fullBox.Value;
                _editing.PassScore = _passBox.Value;
                Result = _editing;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}