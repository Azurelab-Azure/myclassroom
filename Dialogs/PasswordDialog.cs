using System;
using System.Drawing;
using System.Windows.Forms;
using CourseApp.Controls;
using CourseApp.Localization;
using CourseApp.Theme;

namespace CourseApp.Dialogs
{
    /// <summary>
    /// 密码确认弹窗。
    /// </summary>
    public class PasswordDialog : FlatDialogBase
    {
        private readonly FlatTextBox _pwdBox;
        public bool Confirmed { get; private set; } = false;

        public PasswordDialog()
            : base(I18n.T("dialog.confirm"), 380, 220)
        {
            // 提示
            var lbl = new Label
            {
                Text = I18n.T("dialog.enterPassword"),
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.Colors.TextPrimary,
                Left = 20, Top = 20, Width = 320, Height = 24,
                BackColor = Color.Transparent,
                AutoSize = false,
            };
            ContentPanel.Controls.Add(lbl);

            // 密码框
            _pwdBox = new FlatTextBox
            {
                Left = 20, Top = 56, Width = 320, Height = 32,
                PasswordChar = '*',
            };
            _pwdBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) Confirm();
            };
            ContentPanel.Controls.Add(_pwdBox);

            // 确定
            var btnOk = new FlatButton
            {
                Text = I18n.T("common.ok"),
                ButtonStyle = FlatButtonStyle.Primary,
                Width = 100, Height = 36,
                Left = 130, Top = 120,
            };
            btnOk.Click += (s, e) => Confirm();
            ContentPanel.Controls.Add(btnOk);

            // 取消
            var btnCancel = new FlatButton
            {
                Text = I18n.T("common.cancel"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Width = 100, Height = 36,
                Left = 240, Top = 120,
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            ContentPanel.Controls.Add(btnCancel);

            // 焦点
            Load += (s, e) => _pwdBox.Focus();
        }

        private void Confirm()
        {
            if (_pwdBox.Text == "12345678")
            {
                Confirmed = true;
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageDialog.ShowError(I18n.T("dialog.error"), I18n.T("dialog.passwordWrong"));
                _pwdBox.Text = "";
                _pwdBox.Focus();
            }
        }
    }
}