using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using CourseApp.Controls;
using CourseApp.Localization;
using CourseApp.Services;
using CourseApp.Theme;

namespace CourseApp.Dialogs
{
    /// <summary>
    /// "关于"弹窗。
    /// </summary>
    public class AboutDialog : FlatDialogBase
    {
        public AboutDialog()
            : base(I18n.T("about.title"), 480, 480)
        {
            BuildUI();
        }

        private void BuildUI()
        {
            int y = 10;

            // ---------- 应用名 ----------
            ContentPanel.Controls.Add(new Label
            {
                Text = AppInfo.AppName,
                Font = new Font(AppTheme.BodyFont.FontFamily, 20f, FontStyle.Bold),
                ForeColor = AppTheme.Colors.TextPrimary,
                Left = 0, Top = y,
                Width = ClientSize.Width - 40, Height = 36,
                AutoSize = false,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
            });
            y += 44;

            // ---------- 副标题 ----------
            ContentPanel.Controls.Add(new Label
            {
                Text = AppInfo.AppNameEn,
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.Colors.TextSecondary,
                Left = 0, Top = y,
                Width = ClientSize.Width - 40, Height = 20,
                AutoSize = false,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
            });
            y += 32;

            // ---------- 版本 ----------
            ContentPanel.Controls.Add(new Label
            {
                Text = string.Format(I18n.T("about.version"), AppInfo.RuntimeVersion)
                       + "   ·   " + AppInfo.BuildDate,
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.Colors.TextSecondary,
                Left = 0, Top = y,
                Width = ClientSize.Width - 40, Height = 20,
                AutoSize = false,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
            });
            y += 28;

            // ---------- 分隔线 ----------
            ContentPanel.Controls.Add(new Panel
            {
                Left = 40, Top = y,
                Width = ClientSize.Width - 80, Height = 1,
                BackColor = AppTheme.Colors.Divider,
            });
            y += 16;

            // ---------- 简介 ----------
            ContentPanel.Controls.Add(new Label
            {
                Text = AppInfo.Description,
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.Colors.TextPrimary,
                Left = 0, Top = y,
                Width = ClientSize.Width - 40, Height = 90,
                AutoSize = false,
                BackColor = Color.Transparent,
            });
            y += 100;

            // ---------- 信息列表 ----------
            y = AddInfoLine(I18n.T("about.author"), AppInfo.Author, y);
            y = AddInfoLine(I18n.T("about.license"), AppInfo.License, y);
            y = AddInfoLine(I18n.T("about.copyright"), AppInfo.Copyright, y);

            // ---------- 链接按钮 ----------
            int btnLeft = 20;
            if (!string.IsNullOrEmpty(AppInfo.Homepage))
            {
                var linkBtn = new FlatButton
                {
                    Text = I18n.T("about.homepage"),
                    ButtonStyle = FlatButtonStyle.Secondary,
                    Width = 100, Height = 32,
                    Left = btnLeft, Top = y + 8,
                };
                linkBtn.Click += (s, e) =>
                {
                    try { Process.Start(new ProcessStartInfo(AppInfo.Homepage) { UseShellExecute = true }); }
                    catch { }
                };
                ContentPanel.Controls.Add(linkBtn);
                btnLeft += 112;
            }

            if (!string.IsNullOrEmpty(AppInfo.ContactEmail))
            {
                var mailBtn = new FlatButton
                {
                    Text = I18n.T("about.contact"),
                    ButtonStyle = FlatButtonStyle.Secondary,
                    Width = 100, Height = 32,
                    Left = btnLeft, Top = y + 8,
                };
                mailBtn.Click += (s, e) =>
                {
                    try { Process.Start(new ProcessStartInfo("mailto:" + AppInfo.ContactEmail) { UseShellExecute = true }); }
                    catch { }
                };
                ContentPanel.Controls.Add(mailBtn);
            }

            // ---------- 关闭按钮 ----------
            var btnClose = new FlatButton
            {
                Text = I18n.T("common.ok"),
                ButtonStyle = FlatButtonStyle.Primary,
                Width = 100, Height = 36,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            btnClose.Click += (s, e) => Close();
            ContentPanel.Controls.Add(btnClose);

            ContentPanel.Resize += (s, e) =>
            {
                btnClose.Left = ContentPanel.Width - btnClose.Width - 20;
                btnClose.Top = ContentPanel.Height - btnClose.Height - 20;
            };
        }

        private int AddInfoLine(string label, string value, int y)
        {
            ContentPanel.Controls.Add(new Label
            {
                Text = label,
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.Colors.TextSecondary,
                Left = 0, Top = y,
                Width = 60, Height = 22,
                AutoSize = false,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight,
            });

            ContentPanel.Controls.Add(new Label
            {
                Text = value,
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.Colors.TextPrimary,
                Left = 70, Top = y,
                Width = ClientSize.Width - 110, Height = 22,
                AutoSize = false,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
            });

            return y + 24;
        }
    }
}