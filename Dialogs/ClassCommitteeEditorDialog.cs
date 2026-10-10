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
    /// 班委公告编辑弹窗。
    /// </summary>
    public class ClassCommitteeEditorDialog : FlatDialogBase
    {
        private readonly ClassCommittee? _editing;

        private FlatTextBox _titleBox = null!;
        private FlatTextBox _contentBox = null!;
        private FlatTextBox _dateBox = null!;
        private FlatTextBox _publisherBox = null!;
        private FlatComboBox _typeBox = null!;
        private WinUI3Toggle _pinnedToggle = null!;

        public ClassCommittee? Result { get; private set; }

        public ClassCommitteeEditorDialog() : this(null) { }

        public ClassCommitteeEditorDialog(ClassCommittee? existing)
            : base(existing == null ? I18n.T("committee.add") : I18n.T("committee.edit"), 520, 460)
        {
            _editing = existing;
            BuildUI();
            if (existing != null) LoadFrom(existing);
        }

        private void BuildUI()
        {
            int x = 20;
            int y = 10;
            int fieldX = 130;
            int fieldW = 340;
            int rowH = 36;
            int gap = 8;

            AddLabel(I18n.T("committee.title"), x, y);
            _titleBox = new FlatTextBox { Left = fieldX, Top = y, Width = fieldW, Height = 32 };
            ContentPanel.Controls.Add(_titleBox);
            y += rowH + gap;

            AddLabel(I18n.T("committee.type"), x, y);
            _typeBox = new FlatComboBox { Left = fieldX, Top = y, Width = 200, Height = 32 };
            _typeBox.SetItems(new IFlatComboItem[]
            {
                new FlatTextItem(I18n.T("committee.type.notice")),
                new FlatTextItem(I18n.T("committee.type.committee")),
                new FlatTextItem(I18n.T("committee.type.duty")),
            });
            _typeBox.SelectedIndex = 0;
            ContentPanel.Controls.Add(_typeBox);
            y += rowH + gap;

            AddLabel(I18n.T("committee.content"), x, y);
            _contentBox = new FlatTextBox { Left = fieldX, Top = y, Width = fieldW, Height = 32 };
            ContentPanel.Controls.Add(_contentBox);
            y += rowH + gap;

            AddLabel(I18n.T("committee.date"), x, y);
            _dateBox = new FlatTextBox
            {
                Left = fieldX, Top = y, Width = fieldW, Height = 32,
                Text = DateTime.Now.ToString("yyyy-MM-dd"),
            };
            ContentPanel.Controls.Add(_dateBox);
            y += rowH + gap;

            AddLabel(I18n.T("committee.publisher"), x, y);
            _publisherBox = new FlatTextBox { Left = fieldX, Top = y, Width = fieldW, Height = 32 };
            ContentPanel.Controls.Add(_publisherBox);
            y += rowH + gap;

            AddLabel(I18n.T("committee.pinned"), x, y);
            _pinnedToggle = new WinUI3Toggle { Left = fieldX, Top = y + 4 };
            ContentPanel.Controls.Add(_pinnedToggle);
            y += rowH + gap + 10;

            var btnOk = new FlatButton
            {
                Text = I18n.T("dialog.save"),
                ButtonStyle = FlatButtonStyle.Primary,
                Width = 100, Height = 36,
                Left = 220, Top = y,
            };
            btnOk.Click += BtnOk_Click;
            ContentPanel.Controls.Add(btnOk);

            var btnCancel = new FlatButton
            {
                Text = I18n.T("dialog.cancel"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Width = 100, Height = 36,
                Left = 330, Top = y,
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

        private void LoadFrom(ClassCommittee c)
        {
            _titleBox.Text = c.Title ?? "";
            _contentBox.Text = c.Content ?? "";
            _dateBox.Text = c.Date ?? "";
            _publisherBox.Text = c.Publisher ?? "";
            _typeBox.SelectedIndex = c.Type switch
            {
                "committee" => 1, "duty" => 2, _ => 0
            };
            _pinnedToggle.Checked = c.Pinned;
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            string title = _titleBox.Text.Trim();
            if (string.IsNullOrEmpty(title))
            {
                MessageDialog.ShowInfo(I18n.T("common.info"), I18n.T("committee.needTitle"));
                return;
            }

            var target = _editing ?? new ClassCommittee();
            target.Title = title;
            target.Content = _contentBox.Text.Trim();
            target.Date = _dateBox.Text.Trim();
            target.Publisher = _publisherBox.Text.Trim();
            target.Type = _typeBox.SelectedIndex switch
            {
                1 => "committee", 2 => "duty", _ => "notice"
            };
            target.Pinned = _pinnedToggle.Checked;

            Result = target;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}