using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using CourseApp.Controls;
using CourseApp.Localization;
using CourseApp.Theme;

namespace CourseApp.Dialogs
{
    /// <summary>
    /// 导出 CSV 弹窗。
    /// </summary>
    public class ExportCsvDialog : FlatDialogBase
    {
        private FlatTextBox _pathBox = null!;
        private FlatTextBox _nameBox = null!;

        public string ResultPath { get; private set; } = "";

        public ExportCsvDialog(string defaultPath, string defaultFileName)
            : base(I18n.T("fileDialog.titleExport"), 540, 300)
        {
            BuildUI(defaultPath, defaultFileName);
        }

        private void BuildUI(string defaultPath, string defaultFileName)
        {
            // 保存位置
            AddLabel(I18n.T("fileDialog.exportLocation"), 10);
            _pathBox = new FlatTextBox
            {
                Left = 110, Top = 10, Width = 320, Height = 32,
                Text = defaultPath ?? "",
            };
            ContentPanel.Controls.Add(_pathBox);

            var btnBrowse = new FlatButton
            {
                Text = I18n.T("fileDialog.browse"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Left = 440, Top = 10, Width = 70, Height = 32,
            };
            btnBrowse.Click += (s, e) =>
            {
                var folder = FileBrowserDialog.PickFolder(_pathBox.Text);
                if (!string.IsNullOrEmpty(folder)) _pathBox.Text = folder;
            };
            ContentPanel.Controls.Add(btnBrowse);

            // 文件名
            AddLabel(I18n.T("fileDialog.exportFileName"), 54);
            _nameBox = new FlatTextBox
            {
                Left = 110, Top = 54, Width = 400, Height = 32,
                Text = defaultFileName ?? "courses.csv",
            };
            ContentPanel.Controls.Add(_nameBox);

            // 按钮
            var btnSave = new FlatButton
            {
                Text = I18n.T("dialog.save"),
                ButtonStyle = FlatButtonStyle.Primary,
                Width = 100, Height = 36, Left = 290, Top = 200,
            };
            btnSave.Click += BtnSave_Click;
            ContentPanel.Controls.Add(btnSave);

            var btnCancel = new FlatButton
            {
                Text = I18n.T("dialog.cancel"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Width = 100, Height = 36, Left = 400, Top = 200,
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            ContentPanel.Controls.Add(btnCancel);
        }

        private void AddLabel(string text, int top)
        {
            ContentPanel.Controls.Add(new Label
            {
                Text = text, Left = 20, Top = top + 6, Width = 90,
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.Colors.TextPrimary,
                BackColor = Color.Transparent,
                AutoSize = false,
            });
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            string folder = _pathBox.Text.Trim();
            string fileName = _nameBox.Text.Trim();

            if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(fileName)) return;
            if (!Directory.Exists(folder)) return;

            ResultPath = Path.Combine(folder, fileName);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}