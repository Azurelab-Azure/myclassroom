using System;
using System.Drawing;
using System.Windows.Forms;
using CourseApp.Controls;
using CourseApp.Localization;
using CourseApp.Services;
using CourseApp.Theme;

namespace CourseApp.Dialogs
{
    /// <summary>
    /// 下课提醒弹窗。
    /// - 30 秒倒计时
    /// - 点击"结束放映" → Kill 放映进程
    /// - 30 秒到 → 自动 Kill
    /// </summary>
    public class ClassEndDialog : FlatDialogBase
    {
        private readonly System.Windows.Forms.Timer _timer;
        private readonly Label _countdownLabel;
        private readonly FlatButton _killButton;
        private readonly string _courseName;

        private int _remainingSeconds = 30;

        public ClassEndDialog(string courseName)
            : base(I18n.T("classEnd.title"), 420, 260)
        {
            _courseName = courseName ?? "";

            var msgLabel = new Label
            {
                Text = string.Format(I18n.T("classEnd.message"), _courseName),
                Font = new Font(AppTheme.BodyFont.FontFamily, 12f, FontStyle.Bold),
                ForeColor = AppTheme.Colors.TextPrimary,
                Left = 20, Top = 30,
                Width = ClientSize.Width - 40, Height = 80,
                AutoSize = false,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
            };
            ContentPanel.Controls.Add(msgLabel);

            _countdownLabel = new Label
            {
                Text = string.Format(I18n.T("classEnd.countdown"), _remainingSeconds),
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.Colors.TextSecondary,
                Left = 20, Top = 120,
                Width = ClientSize.Width - 40, Height = 24,
                AutoSize = false,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
            };
            ContentPanel.Controls.Add(_countdownLabel);

            _killButton = new FlatButton
            {
                Text = I18n.T("classEnd.end"),
                ButtonStyle = FlatButtonStyle.Danger,
                Width = 120, Height = 40,
                Left = 60, Top = 160,
            };
            _killButton.Click += (s, e) => EndPresentation();
            ContentPanel.Controls.Add(_killButton);

            var ignoreButton = new FlatButton
            {
                Text = I18n.T("classEnd.continue"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Width = 120, Height = 40,
                Left = 200, Top = 160,
            };
            ignoreButton.Click += (s, e) =>
            {
                _timer.Stop();
                Close();
            };
            ContentPanel.Controls.Add(ignoreButton);

            _timer = new System.Windows.Forms.Timer { Interval = 1000 };
            _timer.Tick += (s, e) =>
            {
                _remainingSeconds--;
                _countdownLabel.Text = string.Format(I18n.T("classEnd.countdown"), _remainingSeconds);

                if (_remainingSeconds <= 0)
                {
                    _timer.Stop();
                    EndPresentation();
                }
            };
            _timer.Start();
        }

        private void EndPresentation()
        {
            try { PresentationDetector.KillPresentingProcess(); }
            catch { }

            _timer.Stop();
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _timer?.Stop();
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _timer?.Dispose();
            base.Dispose(disposing);
        }
    }
}