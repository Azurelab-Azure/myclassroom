using System;
using System.Drawing;
using System.Windows.Forms;
using CourseApp.Controls;
using CourseApp.Localization;
using CourseApp.Theme;

namespace CourseApp.Dialogs
{
    /// <summary>
    /// 预设新闻源选择弹窗。
    /// </summary>
    public class NewsSourceDialog : FlatDialogBase
    {
        private readonly (string Name, string Url)[] _sources =
        {
            ("知乎热榜",        "https://rsshub.app/zhihu/hotlist"),
            ("人民网 · 时政",   "http://www.people.com.cn/rss/politics.xml"),
            ("BBC News",        "https://feeds.bbci.co.uk/news/rss.xml"),
            ("36Kr",            "https://rsshub.app/36kr/newsflashes"),
            ("Solidot",         "https://www.solidot.org/index.rss"),
        };

        public string ResultUrl { get; private set; } = "";

        public NewsSourceDialog(string currentUrl)
            : base(I18n.T("newsSource.title"), 520, 460)
        {
            int y = 10;
            foreach (var (name, url) in _sources)
            {
                var btn = new FlatButton
                {
                    Text = name + "  ·  " + url,
                    ButtonStyle = FlatButtonStyle.Secondary,
                    Left = 20, Top = y,
                    Width = ContentPanel.Width - 40,
                    Height = 40,
                };
                string u = url;
                btn.Click += (s, e) =>
                {
                    ResultUrl = u;
                    DialogResult = DialogResult.OK;
                    Close();
                };
                ContentPanel.Controls.Add(btn);
                y += 48;
            }

            var btnCancel = new FlatButton
            {
                Text = I18n.T("dialog.cancel"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Width = 100, Height = 36,
                Left = ContentPanel.Width - 120,
                Top = y + 10,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            ContentPanel.Controls.Add(btnCancel);
        }
    }
}