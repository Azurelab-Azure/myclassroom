using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CourseApp.Controls;
using CourseApp.Localization;
using CourseApp.Services;
using CourseApp.Theme;

namespace CourseApp.Dialogs
{
    /// <summary>
    /// 软件更新弹窗。
    /// </summary>
    public class UpdateDialog : FlatDialogBase
    {
        private readonly ReleaseInfo _release;

        private Label _statusLabel = null!;
        private FlatButton _btnUpdate = null!;
        private FlatButton _btnLater = null!;
        private FlatButton _btnGitHub = null!;

        private bool _downloading = false;

        public UpdateDialog(ReleaseInfo release)
            : base(I18n.T("update.title"), 520, 460)
        {
            _release = release ?? throw new ArgumentNullException(nameof(release));
            BuildUI();
        }

        private void BuildUI()
        {
            int x = 24;
            int y = 16;
            int w = ClientSize.Width - 48;

            // ---------- 版本信息 ----------
            using var titleFont = new Font(AppTheme.BodyFont.FontFamily, 18f, FontStyle.Bold);
            ContentPanel.Controls.Add(new Label
            {
                Text = string.Format(I18n.T("update.newVersion"), _release.TagName),
                Font = titleFont,
                ForeColor = AppTheme.Colors.TextPrimary,
                Left = x, Top = y, Width = w, Height = 30,
                BackColor = Color.Transparent,
                AutoSize = false,
            });
            y += 36;

            // 当前版本
            ContentPanel.Controls.Add(new Label
            {
                Text = string.Format(I18n.T("update.currentVersion"), AppInfo.Version),
                Font = AppTheme.SmallFont,
                ForeColor = AppTheme.Colors.TextSecondary,
                Left = x, Top = y, Width = w, Height = 20,
                BackColor = Color.Transparent,
                AutoSize = false,
            });
            y += 26;

            // 发布日期
            if (!string.IsNullOrEmpty(_release.PublishedAt))
            {
                ContentPanel.Controls.Add(new Label
                {
                    Text = I18n.T("update.publishedAt") + " " + _release.PublishedAt.Substring(0, Math.Min(10, _release.PublishedAt.Length)),
                    Font = AppTheme.SmallFont,
                    ForeColor = AppTheme.Colors.TextSecondary,
                    Left = x, Top = y, Width = w, Height = 20,
                    BackColor = Color.Transparent,
                    AutoSize = false,
                });
                y += 26;
            }

            // 更新说明
            var body = _release.Body ?? "";
            if (body.Length > 800) body = body.Substring(0, 800) + "…";

            var bodyBox = new FlatTextBox
            {
                Left = x, Top = y,
                Width = w, Height = 180,
                ReadOnly = true,
            };
            bodyBox.Text = body;
            ContentPanel.Controls.Add(bodyBox);
            y += 190;

            // ---------- 状态 ----------
            _statusLabel = new Label
            {
                Text = "",
                Font = AppTheme.SmallFont,
                ForeColor = AppTheme.Colors.TextSecondary,
                Left = x, Top = y, Width = w, Height = 22,
                BackColor = Color.Transparent,
                AutoSize = false,
            };
            ContentPanel.Controls.Add(_statusLabel);

            // ---------- 按钮 ----------
            int btnY = ClientSize.Height - 56;

            _btnUpdate = new FlatButton
            {
                Text = I18n.T("update.downloadAndInstall"),
                ButtonStyle = FlatButtonStyle.Primary,
                Width = 180, Height = 36,
                Left = x, Top = btnY,
            };
            _btnUpdate.Click += async (s, e) => await StartDownloadAsync();
            ContentPanel.Controls.Add(_btnUpdate);

            _btnLater = new FlatButton
            {
                Text = I18n.T("update.later"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Width = 100, Height = 36,
                Left = x + 190, Top = btnY,
            };
            _btnLater.Click += (s, e) => Close();
            ContentPanel.Controls.Add(_btnLater);

            _btnGitHub = new FlatButton
            {
                Text = I18n.T("update.openGitHub"),
                ButtonStyle = FlatButtonStyle.Subtle,
                Width = 120, Height = 36,
                Left = ContentPanel.Width - 140, Top = btnY,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            _btnGitHub.Click += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = _release.HtmlUrl,
                        UseShellExecute = true,
                    });
                }
                catch { }
            };
            ContentPanel.Controls.Add(_btnGitHub);
        }

        // =====================================================
        // 下载
        // =====================================================
        private async Task StartDownloadAsync()
        {
            if (_downloading) return;
            _downloading = true;

            _btnUpdate.Enabled = false;
            _btnLater.Enabled = false;
            _btnGitHub.Enabled = false;

            _statusLabel.Text = I18n.T("update.downloading") + "  0%";

            var path = await UpdateService.DownloadUpdateAsync(_release, (downloaded, total) =>
            {
                if (total > 0)
                {
                    int percent = (int)(downloaded * 100 / total);
                    if (InvokeRequired)
                        BeginInvoke(new Action(() => _statusLabel.Text = I18n.T("update.downloading") + $"  {percent}%"));
                    else
                        _statusLabel.Text = I18n.T("update.downloading") + $"  {percent}%";
                }
            });

            if (string.IsNullOrEmpty(path))
            {
                _statusLabel.Text = I18n.T("update.downloadFailed");
                _btnUpdate.Enabled = true;
                _btnLater.Enabled = true;
                _btnGitHub.Enabled = true;
                _downloading = false;
                return;
            }

            _statusLabel.Text = I18n.T("update.installing");

            await Task.Delay(500);

            UpdateService.RunInstallerAndExit(path);
        }
    }
}