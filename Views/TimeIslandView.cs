using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using CourseApp.Localization;
using CourseApp.Models;
using CourseApp.Services;
using CourseApp.Theme;

namespace CourseApp.Views
{
    /// <summary>
    /// 灵动岛内容视图。
    /// 完全由配置的模块列表决定显示内容。
    /// </summary>
    public class TimeIslandView : Panel
    {
        private readonly System.Windows.Forms.Timer _timer;
        private readonly OverlayContext _ctx = new();
        private OverlayConfig _config = new();

        private const int PadLeft = 16;
        private const int PadRight = 16;

        // 网络请求客户端
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };
        
        private DateTime _lastNewsFetch = DateTime.MinValue;
        private DateTime _lastWeatherFetch = DateTime.MinValue;
        private bool _isFetchingNews = false;
        private bool _isFetchingWeather = false;

        private int _lastReportedWidth = 0;

        public event Action<int>? ContentWidthChanged;

        public TimeIslandView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            BackColor = Color.FromArgb(0x20, 0x20, 0x20);

            _timer = new System.Windows.Forms.Timer { Interval = 1000 };
            _timer.Tick += (s, e) => OnTick();
            _timer.Start();

            // 启动时拉取一次
            var initTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            initTimer.Tick += (s, e) =>
            {
                initTimer.Stop();
                initTimer.Dispose();
                if (!IsDisposed)
                {
                    _ = FetchNewsAsync();
                    _ = FetchWeatherAsync();
                }
            };
            initTimer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _timer?.Dispose();
            base.Dispose(disposing);
        }

        public void SetData(List<SectionTime> sections, List<Course> courses, int currentWeek,
            DutyRoster duty, List<ClassCommittee> committees)
        {
            _ctx.Sections = sections ?? new List<SectionTime>();
            _ctx.Courses = courses ?? new List<Course>();
            _ctx.CurrentWeek = currentWeek;
            _ctx.Duty = duty ?? new DutyRoster();
            _ctx.Committees = committees ?? new List<ClassCommittee>();
            SafeRecalculateWidth();
            Invalidate();
        }

        public void SetOfficeData(List<ScheduleItem> schedules)
        {
            _ctx.Schedules = schedules ?? new List<ScheduleItem>();
            SafeRecalculateWidth();
            Invalidate();
        }

        public void SetConfig(OverlayConfig config)
        {
            _config = config ?? new OverlayConfig();
            SafeRecalculateWidth();
            Invalidate();
            
            _lastNewsFetch = DateTime.MinValue;
            _lastWeatherFetch = DateTime.MinValue;

            _ = FetchNewsAsync();
            _ = FetchWeatherAsync();
        }

        public void SetNews(string news)
        {
            _ctx.CurrentNews = news ?? "";
            SafeRecalculateWidth();
            Invalidate();
        }

        public void SetCurrentApp(string app)
        {
            _ctx.CurrentApp = app ?? "";
            SafeRecalculateWidth();
            Invalidate();
        }

        private void OnTick()
        {
            _ctx.Now = DateTime.Now;

            if (_config.IslandModules.Contains("App"))
                _ctx.CurrentApp = GetForegroundProcessName();

            if ((_ctx.Now - _lastNewsFetch).TotalMinutes >= 10 && _config.IslandModules.Contains("News"))
            {
                _ = FetchNewsAsync();
            }

            if ((_ctx.Now - _lastWeatherFetch).TotalMinutes >= 30 && _config.IslandModules.Contains("Weather"))
            {
                _ = FetchWeatherAsync();
            }

            SafeRecalculateWidth();
            Invalidate();
        }

        private void SafeRecalculateWidth()
        {
            if (_config == null || IsDisposed || !IsHandleCreated) return;

            try
            {
                using var bmp = new Bitmap(1, 1);
                using var g = Graphics.FromImage(bmp);

                int contentW = 0;
                var modules = _config.IslandModules;
                if (modules != null)
                {
                    for (int i = 0; i < modules.Count; i++)
                    {
                        string text = OverlayRenderer.RenderText(modules[i], _ctx);
                        if (string.IsNullOrEmpty(text)) continue;

                        if (i > 0) contentW += 12;
                        contentW += 8;
                        contentW += TextRenderer.MeasureText(g, text, AppTheme.BodyFont).Width + 6;
                    }
                }

                string timeText = _ctx.Now.ToString("HH:mm");
                int timeW = TextRenderer.MeasureText(g, timeText, AppTheme.SmallFont).Width;
                
                int totalWidth = PadLeft + 8 + 8 + contentW + 12 + timeW + PadRight;
                
                if (totalWidth < 200) totalWidth = 200;
                if (totalWidth > 800) totalWidth = 800;

                if (Math.Abs(_lastReportedWidth - totalWidth) > 5)
                {
                    _lastReportedWidth = totalWidth;
                    ContentWidthChanged?.Invoke(totalWidth);
                }
            }
            catch { }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            DrawBackground(g);

            int left = PadLeft;
            int midY = Height / 2;
            
            Color dotColor = GetStatusDotColor();
            using (var dotBrush = new SolidBrush(dotColor))
                g.FillEllipse(dotBrush, left, midY - 4, 8, 8);
            left += 14;

            string timeText = _ctx.Now.ToString("HH:mm");
            var timeFont = new Font(AppTheme.BodyFont.FontFamily, 11f, FontStyle.Bold);
            var timeSize = TextRenderer.MeasureText(g, timeText, timeFont);
            int right = Width - PadRight;
            TextRenderer.DrawText(g, timeText, timeFont,
                new Rectangle(right - timeSize.Width, 0, timeSize.Width, Height),
                Color.FromArgb(0xE0, 0xE0, 0xE0),
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            int textRight = right - timeSize.Width - 12;
            var mainRect = new Rectangle(left, 0, Math.Max(10, textRight - left), Height);

            // 统一按模块列表渲染
            DrawModules(g, mainRect);
        }

        private void DrawBackground(Graphics g)
        {
            var rect = new Rectangle(0, 0, Width, Height);
            int radius = _config.IslandMode == "topbar" ? 0 : Height / 2;

            using var path = GraphicsExtensions.GetRoundPath(rect, radius);
            
            using (var brush = new LinearGradientBrush(
                rect,
                Color.FromArgb(0x33, 0x33, 0x33),
                Color.FromArgb(0x1A, 0x1A, 0x1A),
                90f))
            {
                g.FillPath(brush, path);
            }

            using (var pen = new Pen(Color.FromArgb(30, 255, 255, 255), 1f))
            {
                g.DrawPath(pen, path);
            }
        }

        private void DrawModules(Graphics g, Rectangle rect)
        {
            var modules = _config.IslandModules;
            if (modules == null || modules.Count == 0) return;

            int currentX = rect.X;
            var font = AppTheme.BodyFont;

            for (int i = 0; i < modules.Count; i++)
            {
                string m = modules[i];
                string text = OverlayRenderer.RenderText(m, _ctx);
                if (string.IsNullOrEmpty(text)) continue;

                Color themeColor = OverlayRenderer.GetModuleColor(m, _ctx);

                if (i > 0)
                {
                    using var dotBrush = new SolidBrush(Color.FromArgb(0x60, 0x60, 0x60));
                    g.FillEllipse(dotBrush, currentX, rect.Y + rect.Height / 2 - 2, 4, 4);
                    currentX += 12;
                }

                var tagRect = new Rectangle(currentX, rect.Y + (rect.Height - 14) / 2, 4, 14);
                using (var path = GraphicsExtensions.GetRoundPath(tagRect, 2))
                using (var brush = new SolidBrush(themeColor))
                    g.FillPath(brush, path);
                
                currentX += 8;

                var size = TextRenderer.MeasureText(g, text, font);
                var textRect = new Rectangle(currentX, rect.Y, size.Width + 10, rect.Height);
                
                Color textColor = ControlPaint.Light(themeColor, 0.5f);
                if (m == "App" || m == "News") textColor = Color.FromArgb(0xE0, 0xE0, 0xE0);

                TextRenderer.DrawText(g, text, font, textRect, textColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

                currentX += size.Width + 6;

                if (currentX > rect.Right) break;
            }
        }

        private Color GetStatusDotColor()
        {
            // 如果有正在进行的日程，显示绿色
            var todaySchedules = ScheduleRepository.GetForDay(_ctx.Schedules, _ctx.Now);
            var currentSchedule = todaySchedules.FirstOrDefault(s => 
                !s.IsTodo && !string.IsNullOrEmpty(s.StartTime) && !string.IsNullOrEmpty(s.EndTime) && 
                TimeSpan.TryParse(s.StartTime, out var st) && TimeSpan.TryParse(s.EndTime, out var et) &&
                _ctx.Now.TimeOfDay >= st && _ctx.Now.TimeOfDay < et);

            if (currentSchedule != null) return Color.FromArgb(0x4C, 0xAF, 0x50);

            // 否则检查课程
            var now = _ctx.Now;
            foreach (var st in _ctx.Sections.Where(s => s.Type == "normal").OrderBy(s => s.Section))
            {
                if (string.IsNullOrEmpty(st.StartTime) || string.IsNullOrEmpty(st.EndTime)) continue;
                if (!TimeSpan.TryParse(st.StartTime, out var startTs)) continue;
                if (!TimeSpan.TryParse(st.EndTime, out var endTs)) continue;

                var startDt = now.Date + startTs;
                var endDt = now.Date + endTs;

                if (now >= startDt && now < endDt)
                {
                    var remain = endDt - now;
                    if (remain.TotalMinutes <= 5) return Color.FromArgb(0xFF, 0x6B, 0x00);
                    return Color.FromArgb(0x4C, 0xAF, 0x50);
                }
                if (now < startDt) return Color.FromArgb(0xFF, 0xB9, 0x00);
            }
            return Color.FromArgb(0x60, 0x60, 0x60);
        }

        private async Task FetchNewsAsync()
        {
            if (_isFetchingNews || string.IsNullOrEmpty(_config.NewsUrl)) return;
            _isFetchingNews = true;
            _lastNewsFetch = DateTime.Now;

            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, _config.NewsUrl);
                req.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                req.Headers.Add("Accept", "application/rss+xml, application/xml, text/xml, */*");

                using var res = await _http.SendAsync(req);
                res.EnsureSuccessStatusCode();
                var xml = await res.Content.ReadAsStringAsync();

                string firstTitle = "";

                try
                {
                    var doc = XDocument.Parse(xml);
                    var item = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "item" || x.Name.LocalName == "entry");
                    if (item != null)
                    {
                        var titleNode = item.Elements().FirstOrDefault(x => x.Name.LocalName == "title");
                        if (titleNode != null) firstTitle = titleNode.Value.Trim();
                    }
                }
                catch 
                {
                    var matches = Regex.Matches(xml, @"<title>(.*?)</title>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                    if (matches.Count > 1) firstTitle = matches[1].Groups[1].Value;
                    else if (matches.Count == 1) firstTitle = matches[0].Groups[1].Value;
                }

                if (!string.IsNullOrEmpty(firstTitle))
                {
                    firstTitle = Regex.Replace(firstTitle, "<.*?>", "").Trim();
                    firstTitle = System.Net.WebUtility.HtmlDecode(firstTitle);

                    if (IsHandleCreated && !IsDisposed)
                    {
                        if (InvokeRequired) BeginInvoke(new Action(() => { SetNews(firstTitle); }));
                        else SetNews(firstTitle);
                    }
                }
            }
            catch 
            {
                // 拉取失败，尝试备用百度热榜
                try
                {
                    var backupXml = await _http.GetStringAsync("http://news.baidu.com/ns?word=热门&tn=newsrss&sr=0&cl=2&rn=20&ct=0");
                    var matches = Regex.Matches(backupXml, @"<title>(.*?)</title>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                    if (matches.Count > 1) 
                    {
                        var t = matches[1].Groups[1].Value;
                        if (IsHandleCreated && !IsDisposed)
                        {
                            if (InvokeRequired) BeginInvoke(new Action(() => SetNews(t)));
                            else SetNews(t);
                        }
                    }
                } catch {}
            }
            finally { _isFetchingNews = false; }
        }

        private async Task FetchWeatherAsync()
        {
            if (_isFetchingWeather) return;
            _isFetchingWeather = true;
            _lastWeatherFetch = DateTime.Now;

            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, "https://wttr.in/?format=%C+%t");
                req.Headers.Add("User-Agent", "curl/7.68.0");
                
                using var res = await _http.SendAsync(req);
                res.EnsureSuccessStatusCode();
                var text = await res.Content.ReadAsStringAsync();
                
                if (!string.IsNullOrEmpty(text) && text.Length < 20)
                {
                    text = text.Trim();
                    if (IsHandleCreated && !IsDisposed)
                    {
                        if (InvokeRequired) BeginInvoke(new Action(() => { _ctx.WeatherText = text; Invalidate(); }));
                        else { _ctx.WeatherText = text; Invalidate(); }
                    }
                }
            }
            catch { }
            finally { _isFetchingWeather = false; }
        }

        private static string GetForegroundProcessName()
        {
            try
            {
                var hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero) return "";
                GetWindowThreadProcessId(hwnd, out uint pid);
                if (pid == 0) return "";
                var proc = System.Diagnostics.Process.GetProcessById((int)pid);
                return proc.ProcessName ?? "";
            }
            catch { return ""; }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    }
}