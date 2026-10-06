using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CourseApp.Localization;
using CourseApp.Theme;

namespace CourseApp.Views
{
    /// <summary>
    /// 新闻资讯轮播：顶部灵动岛扩展显示。
    /// 主题切换由 FloatingIslandForm 转发（通过 ApplyTheme）。
    /// </summary>
    public class NewsIslandView : Panel
    {
        private readonly System.Windows.Forms.Timer _rotateTimer;
        private readonly List<string> _news = new();
        private int _currentIndex = 0;
        private string _url = "";
        private int _intervalSec = 10;
        private DateTime _lastFetch = DateTime.MinValue;
        private bool _fetching = false;

        private static readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(8),
        };

        public NewsIslandView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            BackColor = Color.FromArgb(0x1A, 0x1A, 0x1A);

            _rotateTimer = new System.Windows.Forms.Timer { Interval = 5000 };
            _rotateTimer.Tick += (s, e) => NextNews();
            _rotateTimer.Start();

            // 独立灵动岛内的自绘：主题变化由 FloatingIslandForm 触发自身 Invalidate，
            // 本控件不订阅 ThemeChanged（自绘时从 AppTheme.Colors 取色即可）。
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _rotateTimer?.Dispose();
            base.Dispose(disposing);
        }

        // =====================================================
        // 配置
        // =====================================================
        public void Configure(string url, int intervalSec)
        {
            _url = url ?? "";
            _intervalSec = Math.Max(3, intervalSec);
            _rotateTimer.Interval = _intervalSec * 1000;

            _lastFetch = DateTime.MinValue;
            _ = FetchAsync();
        }

        // =====================================================
        // 拉取 RSS
        // =====================================================
        private async System.Threading.Tasks.Task FetchAsync()
        {
            if (_fetching) return;
            if (string.IsNullOrWhiteSpace(_url)) return;
            if ((DateTime.Now - _lastFetch).TotalMinutes < 5) return;

            _fetching = true;
            try
            {
                var xml = await _http.GetStringAsync(_url);
                var items = ParseRss(xml);
                if (items.Count > 0)
                {
                    lock (_news)
                    {
                        _news.Clear();
                        _news.AddRange(items);
                        _currentIndex = 0;
                    }
                    if (InvokeRequired) BeginInvoke(new Action(Invalidate));
                    else Invalidate();
                }
                _lastFetch = DateTime.Now;
            }
            catch { /* 网络失败静默 */ }
            finally
            {
                _fetching = false;
            }
        }

        private static List<string> ParseRss(string xml)
        {
            var result = new List<string>();
            try
            {
                var matches = Regex.Matches(xml,
                    @"<item>.*?<title>(.*?)</title>.*?</item>",
                    RegexOptions.Singleline | RegexOptions.IgnoreCase);

                foreach (Match m in matches)
                {
                    var t = Regex.Replace(m.Groups[1].Value, "<.*?>", "").Trim();
                    t = System.Net.WebUtility.HtmlDecode(t);
                    if (!string.IsNullOrEmpty(t))
                        result.Add(t);
                    if (result.Count >= 20) break;
                }
            }
            catch { }
            return result;
        }

        private void NextNews()
        {
            lock (_news)
            {
                if (_news.Count == 0)
                {
                    _ = FetchAsync();
                    return;
                }
                _currentIndex = (_currentIndex + 1) % _news.Count;
            }
            Invalidate();
        }

        // =====================================================
        // 绘制
        // =====================================================
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var bg = new SolidBrush(BackColor))
                g.FillRectangle(bg, ClientRectangle);

            // 左侧圆点
            int left = 14;
            int midY = Height / 2;

            var dotRect = new Rectangle(left, midY - 4, 8, 8);
            using (var dotBrush = new SolidBrush(Color.FromArgb(0x4C, 0xC2, 0xFF)))
                g.FillEllipse(dotBrush, dotRect);

            left += 16;

            // 右侧时间
            string timeText = DateTime.Now.ToString("HH:mm");
            var timeSize = TextRenderer.MeasureText(g, timeText, AppTheme.SmallFont);
            int right = Width - 14;
            var timeRect = new Rectangle(right - timeSize.Width, 0, timeSize.Width, Height);
            TextRenderer.DrawText(g, timeText, AppTheme.SmallFont, timeRect,
                Color.FromArgb(0xB0, 0xB0, 0xB0),
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            // 中间新闻标题
            string text;
            lock (_news)
            {
                if (_news.Count == 0)
                    text = I18n.T("common.loading");
                else
                    text = _news[_currentIndex % _news.Count];
            }

            var textRect = new Rectangle(left, 0, timeRect.Left - left - 10, Height);
            TextRenderer.DrawText(g, text, AppTheme.SmallFont, textRect,
                Color.FromArgb(0xF0, 0xF0, 0xF0),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }
}