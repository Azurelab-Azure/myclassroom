using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CourseApp.Controls;
using CourseApp.Localization;
using CourseApp.Services;
using CourseApp.Theme;

namespace CourseApp.Dialogs
{
    /// <summary>
    /// "关于"弹窗——含隐私政策 / Cookie 说明。
    /// </summary>
    public class AboutDialog : FlatDialogBase
    {
        private Panel _contentHost = null!;
        private FlatButton _tabAbout = null!;
        private FlatButton _tabPrivacy = null!;
        private FlatButton _tabCookie = null!;

        private enum Page { About, Privacy, Cookie }
        private Page _currentPage = Page.About;

        public AboutDialog()
            : base(I18n.T("about.title"), 640, 560)
        {
            BuildUI();
        }

        // =====================================================
        // UI
        // =====================================================
        private void BuildUI()
        {
            // Tab 栏
            var tabBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = AppTheme.Colors.WindowBg,
            };
            ContentPanel.Controls.Add(tabBar);

            _tabAbout = MakeTab(I18n.T("about.tabAbout"), 16, tabBar);
            _tabAbout.Click += (s, e) => SwitchPage(Page.About);

            _tabPrivacy = MakeTab(I18n.T("about.tabPrivacy"), 124, tabBar);
            _tabPrivacy.Click += (s, e) => SwitchPage(Page.Privacy);

            _tabCookie = MakeTab(I18n.T("about.tabCookie"), 232, tabBar);
            _tabCookie.Click += (s, e) => SwitchPage(Page.Cookie);

            // 内容
            _contentHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Colors.WindowBg,
                AutoScroll = true,
            };
            _contentHost.Paint += ContentHost_Paint;
            ContentPanel.Controls.Add(_contentHost);
            _contentHost.BringToFront();

            // 关闭按钮
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

            SwitchPage(Page.About);
        }

        private FlatButton MakeTab(string text, int left, Panel parent)
        {
            var btn = new FlatButton
            {
                Text = text,
                ButtonStyle = FlatButtonStyle.Secondary,
                Left = left, Top = 8,
                Width = 100, Height = 32,
            };
            parent.Controls.Add(btn);
            return btn;
        }

        private void SwitchPage(Page page)
        {
            _currentPage = page;

            _tabAbout.ButtonStyle = (page == Page.About) ? FlatButtonStyle.Primary : FlatButtonStyle.Secondary;
            _tabPrivacy.ButtonStyle = (page == Page.Privacy) ? FlatButtonStyle.Primary : FlatButtonStyle.Secondary;
            _tabCookie.ButtonStyle = (page == Page.Cookie) ? FlatButtonStyle.Primary : FlatButtonStyle.Secondary;

            _contentHost.Invalidate();
        }

        // =====================================================
        // 内容绘制
        // =====================================================
        private void ContentHost_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var colors = AppTheme.Colors;

            using (var bg = new SolidBrush(colors.WindowBg))
                g.FillRectangle(bg, _contentHost.ClientRectangle);

            switch (_currentPage)
            {
                case Page.About:
                    DrawAbout(g);
                    break;
                case Page.Privacy:
                    DrawPrivacy(g);
                    break;
                case Page.Cookie:
                    DrawCookie(g);
                    break;
            }
        }

        // ---------- 关于 ----------
        private void DrawAbout(Graphics g)
        {
            var colors = AppTheme.Colors;
            int x = 32;
            int y = 16;
            int w = _contentHost.Width - x * 2;

            // 应用名
            using var nameFont = new Font(AppTheme.BodyFont.FontFamily, 26f, FontStyle.Bold);
            var nameRect = new Rectangle(x, y, w, 44);
            TextRenderer.DrawText(g, AppInfo.AppName, nameFont, nameRect, colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            y += 46;

            // 英文名
            using var subFont = new Font(AppTheme.BodyFont.FontFamily, 14f);
            var subRect = new Rectangle(x, y, w, 24);
            TextRenderer.DrawText(g, AppInfo.AppNameEn, subFont, subRect, colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            y += 32;

            // 版本
            using var infoFont = new Font(AppTheme.BodyFont.FontFamily, 12f);
            var versionRect = new Rectangle(x, y, w, 22);
            TextRenderer.DrawText(g,
                string.Format(I18n.T("about.version"), AppInfo.RuntimeVersion) + "  ·  " + AppInfo.BuildDate,
                infoFont, versionRect, colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            y += 36;

            // 分隔线
            using (var pen = new Pen(colors.Divider, 1f))
                g.DrawLine(pen, x, y, x + w, y);
            y += 20;

            // 简介
            var descRect = new Rectangle(x, y, w, 110);
            TextRenderer.DrawText(g, AppInfo.Description, infoFont, descRect, colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.Top |
                TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
            y += 120;

            // 信息
            y = AddInfoLine(g, I18n.T("about.author"), AppInfo.Author, x, y, w);
            y = AddInfoLine(g, I18n.T("about.license"), AppInfo.License, x, y, w);
            y = AddInfoLine(g, I18n.T("about.copyright"), AppInfo.Copyright, x, y, w);
        }

        private int AddInfoLine(Graphics g, string label, string value, int x, int y, int w)
        {
            var colors = AppTheme.Colors;
            using var font = new Font(AppTheme.BodyFont.FontFamily, 12f);

            var labelRect = new Rectangle(x, y, 80, 22);
            TextRenderer.DrawText(g, label, font, labelRect, colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            var valueRect = new Rectangle(x + 90, y, w - 90, 22);
            TextRenderer.DrawText(g, value, font, valueRect, colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            return y + 28;
        }

        // ---------- 隐私政策 ----------
        private void DrawPrivacy(Graphics g)
        {
            var colors = AppTheme.Colors;
            int x = 32;
            int y = 16;
            int w = _contentHost.Width - x * 2;

            // 标题
            using var titleFont = new Font(AppTheme.BodyFont.FontFamily, 20f, FontStyle.Bold);
            var titleRect = new Rectangle(x, y, w, 36);
            TextRenderer.DrawText(g, I18n.T("about.privacyTitle"), titleFont, titleRect,
                colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            y += 44;

            // 更新时间
            using var smallFont = new Font(AppTheme.BodyFont.FontFamily, 11f);
            var updatedRect = new Rectangle(x, y, w, 20);
            TextRenderer.DrawText(g, I18n.T("about.privacyUpdated") + " 2026-10-06",
                smallFont, updatedRect, colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            y += 32;

            // 正文
            string[] sections = new[]
            {
                I18n.T("about.privacy.s1"),
                I18n.T("about.privacy.s2"),
                I18n.T("about.privacy.s3"),
                I18n.T("about.privacy.s4"),
                I18n.T("about.privacy.s5"),
                I18n.T("about.privacy.s6"),
            };

            using var bodyFont = new Font(AppTheme.BodyFont.FontFamily, 12f);

            foreach (var section in sections)
            {
                if (string.IsNullOrEmpty(section)) continue;

                var size = TextRenderer.MeasureText(g, section, bodyFont,
                    new Size(w, int.MaxValue), TextFormatFlags.WordBreak);
                var rect = new Rectangle(x, y, w, size.Height + 4);

                TextRenderer.DrawText(g, section, bodyFont, rect, colors.TextPrimary,
                    TextFormatFlags.Left | TextFormatFlags.Top |
                    TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);

                y += size.Height + 20;
            }
        }

        // ---------- Cookie ----------
        private void DrawCookie(Graphics g)
        {
            var colors = AppTheme.Colors;
            int x = 32;
            int y = 16;
            int w = _contentHost.Width - x * 2;

            // 标题
            using var titleFont = new Font(AppTheme.BodyFont.FontFamily, 20f, FontStyle.Bold);
            var titleRect = new Rectangle(x, y, w, 36);
            TextRenderer.DrawText(g, I18n.T("about.cookieTitle"), titleFont, titleRect,
                colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            y += 44;

            // 正文
            string[] sections = new[]
            {
                I18n.T("about.cookie.s1"),
                I18n.T("about.cookie.s2"),
                I18n.T("about.cookie.s3"),
                I18n.T("about.cookie.s4"),
            };

            using var bodyFont = new Font(AppTheme.BodyFont.FontFamily, 12f);

            foreach (var section in sections)
            {
                if (string.IsNullOrEmpty(section)) continue;

                var size = TextRenderer.MeasureText(g, section, bodyFont,
                    new Size(w, int.MaxValue), TextFormatFlags.WordBreak);
                var rect = new Rectangle(x, y, w, size.Height + 4);

                TextRenderer.DrawText(g, section, bodyFont, rect, colors.TextPrimary,
                    TextFormatFlags.Left | TextFormatFlags.Top |
                    TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);

                y += size.Height + 20;
            }
        }
    }
}