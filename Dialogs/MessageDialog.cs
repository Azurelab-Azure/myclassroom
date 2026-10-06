using System.Drawing;
using System.Windows.Forms;
using CourseApp.Controls;
using CourseApp.Localization;
using CourseApp.Theme;

namespace CourseApp.Dialogs
{
    /// <summary>
    /// 统一消息框。
    /// </summary>
    public class MessageDialog : FlatDialogBase
    {
        private readonly Label _messageLabel;
        private bool _confirmed;

        private MessageDialog(string title, string message, bool showCancel)
            : base(title, 420, showCancel ? 200 : 180)
        {
            _messageLabel = new Label
            {
                Text = message ?? "",
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.Colors.TextSecondary,
                Left = 20,
                Top = 20,
                Width = ClientSize.Width - 40,
                Height = 70,
                AutoSize = false,
                BackColor = Color.Transparent,
            };
            ContentPanel.Controls.Add(_messageLabel);

            var btnOk = new FlatButton
            {
                Text = I18n.T("dialog.ok"),
                ButtonStyle = FlatButtonStyle.Primary,
                Width = 96,
                Height = 36,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            btnOk.Click += (s, e) =>
            {
                _confirmed = true;
                DialogResult = DialogResult.OK;
                Close();
            };
            ContentPanel.Controls.Add(btnOk);

            if (showCancel)
            {
                var btnCancel = new FlatButton
                {
                    Text = I18n.T("dialog.cancel"),
                    ButtonStyle = FlatButtonStyle.Secondary,
                    Width = 96,
                    Height = 36,
                    Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                };
                btnCancel.Click += (s, e) =>
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                };
                ContentPanel.Controls.Add(btnCancel);

                ContentPanel.Resize += (s, e) =>
                {
                    btnCancel.Left = ContentPanel.Width - btnCancel.Width - 20;
                    btnCancel.Top = ContentPanel.Height - btnCancel.Height - 20;
                    btnOk.Left = btnCancel.Left - btnOk.Width - 12;
                    btnOk.Top = btnCancel.Top;
                };
            }
            else
            {
                ContentPanel.Resize += (s, e) =>
                {
                    btnOk.Left = ContentPanel.Width - btnOk.Width - 20;
                    btnOk.Top = ContentPanel.Height - btnOk.Height - 20;
                };
            }
        }

        // =====================================================
        // 静态入口
        // =====================================================
        public static void ShowInfo(string title, string message)
        {
            using var dlg = new MessageDialog(title, message, false);
            dlg.ShowDialog();
        }

        public static void ShowError(string title, string message)
        {
            using var dlg = new MessageDialog(title, message, false);
            dlg.ShowDialog();
        }

        public static bool Confirm(string title, string message)
        {
            using var dlg = new MessageDialog(title, message, true);
            dlg.ShowDialog();
            return dlg._confirmed;
        }
    }
}