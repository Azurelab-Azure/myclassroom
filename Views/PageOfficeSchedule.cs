using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using CourseApp.Controls;
using CourseApp.Dialogs;
using CourseApp.Localization;
using CourseApp.Models;
using CourseApp.Services;
using CourseApp.Theme;

namespace CourseApp.Views
{
    /// <summary>
    /// 办公模式日程页。
    /// 月历 + 当天日程列表 + 新建 / 编辑 / 删除 + 导入导出。
    /// </summary>
    public class PageOfficeSchedule : Panel
    {
        /// <summary>主窗口引用</summary>
        private readonly Form2 _owner;

        /// <summary>日程数据</summary>
        private List<ScheduleItem> _schedules = new();

        /// <summary>当前显示月份</summary>
        private DateTime _currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

        /// <summary>当前选中日期</summary>
        private DateTime _selectedDate = DateTime.Today;

        /// <summary>月历单元格矩形缓存</summary>
        private readonly List<(DateTime Date, Rectangle Rect)> _dayRects = new();

        // 顶部
        /// <summary>上一月按钮</summary>
        private FlatButton _btnPrevMonth = null!;

        /// <summary>下一月按钮</summary>
        private FlatButton _btnNextMonth = null!;

        /// <summary>月份标签</summary>
        private Label _monthLabel = null!;

        /// <summary>新建按钮</summary>
        private FlatButton _btnNew = null!;

        /// <summary>导出按钮</summary>
        private FlatButton _btnExport = null!;

        /// <summary>导入按钮</summary>
        private FlatButton _btnImport = null!;

        /// <summary>月历宿主</summary>
        private Panel _calendarHost = null!;

        /// <summary>当天日程列表宿主</summary>
        private Panel _listHost = null!;

        /// <summary>列表滚动条</summary>
        private FlatScrollBar _listScrollBar = null!;

        /// <summary>列表滚动偏移</summary>
        private int _scrollY = 0;

        /// <summary>行高</summary>
        private const int RowH = 56;

        /// <summary>
        /// 构造办公日程页。
        /// </summary>
        /// <param name="owner">办公模式主窗口</param>
        public PageOfficeSchedule(Form2 owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Colors.WindowBg;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            BuildUI();
        }

        /// <summary>
        /// 构建界面。
        /// </summary>
        private void BuildUI()
        {
            int top = 16;
            int left = 16;

            // 上一月
            _btnPrevMonth = new FlatButton
            {
                Text = "◀",
                ButtonStyle = FlatButtonStyle.Secondary,
                Left = left, Top = top,
                Width = 36, Height = 32,
            };
            _btnPrevMonth.Click += (s, e) =>
            {
                _currentMonth = _currentMonth.AddMonths(-1);
                UpdateMonthLabel();
                _calendarHost.Invalidate();
            };
            Controls.Add(_btnPrevMonth);

            // 月份标签
            _monthLabel = new Label
            {
                Text = _currentMonth.ToString("yyyy 年 M 月"),
                Font = new Font(AppTheme.BodyFont.FontFamily, 16f, FontStyle.Bold),
                ForeColor = AppTheme.Colors.TextPrimary,
                Left = left + 48, Top = top + 4,
                Width = 200, Height = 28,
                BackColor = Color.Transparent,
                AutoSize = false,
            };
            Controls.Add(_monthLabel);

            // 下一月
            _btnNextMonth = new FlatButton
            {
                Text = "▶",
                ButtonStyle = FlatButtonStyle.Secondary,
                Left = left + 260, Top = top,
                Width = 36, Height = 32,
            };
            _btnNextMonth.Click += (s, e) =>
            {
                _currentMonth = _currentMonth.AddMonths(1);
                UpdateMonthLabel();
                _calendarHost.Invalidate();
            };
            Controls.Add(_btnNextMonth);

            // 新建
            _btnNew = new FlatButton
            {
                Text = I18n.T("schedule.new"),
                ButtonStyle = FlatButtonStyle.Primary,
                Left = left + 312, Top = top,
                Width = 100, Height = 32,
            };
            _btnNew.Click += (s, e) => _owner.OpenScheduleEditor(null);
            Controls.Add(_btnNew);

            // 导出 / 导入（右侧）
            _btnExport = new FlatButton
            {
                Text = I18n.T("schedule.export"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Top = top, Width = 100, Height = 32,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };
            _btnExport.Click += (s, e) => ExportSchedules();
            Controls.Add(_btnExport);

            _btnImport = new FlatButton
            {
                Text = I18n.T("schedule.import"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Top = top, Width = 100, Height = 32,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };
            _btnImport.Click += (s, e) => ImportSchedules();
            Controls.Add(_btnImport);

            Resize += (s, e) =>
            {
                _btnExport.Left = Width - _btnExport.Width - 16;
                _btnImport.Left = _btnExport.Left - _btnImport.Width - 8;
            };
            _btnExport.Left = Width - _btnExport.Width - 16;
            _btnImport.Left = _btnExport.Left - _btnImport.Width - 8;

            top += 48;

            // 月历宿主
            _calendarHost = new Panel
            {
                Left = left, Top = top,
                Width = Width - left * 2,
                Height = 380,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            _calendarHost.Paint += CalendarHost_Paint;
            _calendarHost.MouseDown += CalendarHost_MouseDown;
            Controls.Add(_calendarHost);

            top += 400;

            // 当天日程列表
            _listHost = new Panel
            {
                Left = left, Top = top,
                Width = Width - left * 2 - 12,
                Height = Height - top - 16,
                BackColor = AppTheme.Colors.CardBg,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            };
            _listHost.Paint += ListHost_Paint;
            _listHost.MouseDown += ListHost_MouseDown;
            _listHost.MouseWheel += ListHost_MouseWheel;
            Controls.Add(_listHost);

            _listScrollBar = new FlatScrollBar
            {
                Left = Width - left - 8,
                Top = top,
                Width = 8,
                Height = _listHost.Height,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right,
            };
            _listScrollBar.ValueChanged += (s, e) =>
            {
                _scrollY = _listScrollBar.Value;
                _listHost.Invalidate();
            };
            Controls.Add(_listScrollBar);

            Resize += (s, e) =>
            {
                _calendarHost.Width = Width - left * 2;
                _listHost.Width = Width - left * 2 - 12;
                _listHost.Height = Height - _listHost.Top - 16;
                _listScrollBar.Left = Width - left - 8;
                _listScrollBar.Height = _listHost.Height;
            };
        }

        /// <summary>
        /// 更新月份标签。
        /// </summary>
        private void UpdateMonthLabel()
        {
            _monthLabel.Text = _currentMonth.ToString("yyyy 年 M 月");
        }

        /// <summary>
        /// 设置数据。
        /// </summary>
        public void SetData(List<ScheduleItem> schedules)
        {
            _schedules = schedules ?? new List<ScheduleItem>();
            _calendarHost.Invalidate();
            _listHost.Invalidate();
            UpdateScrollBar();
        }

        // =====================================================
        // 月历绘制
        // =====================================================
        /// <summary>
        /// 绘制月历。
        /// </summary>
        private void CalendarHost_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var colors = AppTheme.Colors;

            int cellW = _calendarHost.Width / 7;
            int headerH = 32;
            int cellH = (_calendarHost.Height - headerH) / 6;

            _dayRects.Clear();

            // 星期表头
            string[] weekdays = { "一", "二", "三", "四", "五", "六", "日" };
            for (int i = 0; i < 7; i++)
            {
                var rect = new Rectangle(i * cellW, 0, cellW, headerH);
                Color fg = (i >= 5) ? Color.FromArgb(0xE8, 0x1B, 0x1B) : colors.TextSecondary;
                TextRenderer.DrawText(g, weekdays[i], AppTheme.SmallFont, rect, fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix);
            }

            // 第一天是周几
            var first = _currentMonth;
            int firstDow = (int)first.DayOfWeek;
            if (firstDow == 0) firstDow = 7;
            firstDow--; // 0 = 周一

            // 上月补位
            var startDate = first.AddDays(-firstDow);

            int todayDay = DateTime.Today.Day;
            int todayMonth = DateTime.Today.Month;
            int todayYear = DateTime.Today.Year;

            for (int i = 0; i < 42; i++)
            {
                var date = startDate.AddDays(i);
                int row = i / 7;
                int col = i % 7;
                var rect = new Rectangle(col * cellW, headerH + row * cellH, cellW, cellH);
                _dayRects.Add((date, rect));

                bool isCurrentMonth = (date.Month == _currentMonth.Month && date.Year == _currentMonth.Year);
                bool isToday = (date.Year == todayYear && date.Month == todayMonth && date.Day == todayDay);
                bool isSelected = (date.Date == _selectedDate.Date);

                Color bg = colors.WindowBg;
                if (isSelected) bg = colors.SelectedBg;
                else if (isToday) bg = Color.FromArgb(30, colors.Accent);

                using (var brush = new SolidBrush(bg))
                    g.FillRectangle(brush, rect);

                using (var pen = new Pen(colors.Divider, 1f))
                    g.DrawRectangle(pen, rect);

                // 日期数字
                Color dayColor = !isCurrentMonth ? colors.TextDisabled
                              : isToday ? colors.Accent
                                        : colors.TextPrimary;
                var dayFont = isToday
                    ? new Font(AppTheme.BodyFont.FontFamily, 12f, FontStyle.Bold)
                    : AppTheme.BodyFont;

                TextRenderer.DrawText(g, date.Day.ToString(), dayFont,
                    new Rectangle(rect.X + 4, rect.Y + 2, rect.Width - 8, 20), dayColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

                // 该天日程小点
                var dayItems = ScheduleRepository.GetForDay(_schedules, date);
                int dotY = rect.Y + 24;
                foreach (var item in dayItems.Take(3))
                {
                    var dotColor = ParseColor(item.Color, colors.Accent);
                    var dotRect = new Rectangle(rect.X + 6, dotY, 6, 6);
                    using var dotBrush = new SolidBrush(dotColor);
                    g.FillEllipse(dotBrush, dotRect);

                    string title = item.Title ?? "";
                    if (title.Length > 6) title = title.Substring(0, 6);
                    TextRenderer.DrawText(g, title, AppTheme.SmallFont,
                        new Rectangle(rect.X + 16, dotY - 4, rect.Width - 20, 14), colors.TextSecondary,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                        TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                    dotY += 14;
                    if (dotY > rect.Bottom - 12) break;
                }
            }
        }

        /// <summary>
        /// 月历点击。
        /// </summary>
        private void CalendarHost_MouseDown(object? sender, MouseEventArgs e)
        {
            foreach (var (date, rect) in _dayRects)
            {
                if (rect.Contains(e.Location))
                {
                    _selectedDate = date.Date;
                    _calendarHost.Invalidate();
                    _listHost.Invalidate();
                    UpdateScrollBar();
                    return;
                }
            }
        }

        // =====================================================
        // 列表绘制
        // =====================================================
        /// <summary>
        /// 绘制当天日程列表。
        /// </summary>
        private void ListHost_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var colors = AppTheme.Colors;

            using (var bg = new SolidBrush(colors.CardBg))
                g.FillRectangle(bg, _listHost.ClientRectangle);

            // 表头
            var headerRect = new Rectangle(0, 0, _listHost.Width, 34);
            using (var brush = new SolidBrush(colors.GridHeaderBg))
                g.FillRectangle(brush, headerRect);

            using (var pen = new Pen(colors.Divider, 1f))
                g.DrawLine(pen, 0, 34, _listHost.Width, 34);

            string dateText = _selectedDate.ToString("yyyy-MM-dd dddd");
            TextRenderer.DrawText(g, dateText, AppTheme.BodyFont,
                new Rectangle(16, 0, _listHost.Width - 32, 34), colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            var items = ScheduleRepository.GetForDay(_schedules, _selectedDate)
                .OrderBy(s => s.StartTime)
                .ToList();

            int y = 34 - _scrollY;
            foreach (var item in items)
            {
                var rect = new Rectangle(0, y, _listHost.Width, RowH);
                if (rect.Bottom > 34 && rect.Top < _listHost.Height)
                    DrawListItem(g, item, rect);
                y += RowH;
            }

            if (items.Count == 0)
            {
                using var font = new Font(AppTheme.BodyFont.FontFamily, 12f);
                TextRenderer.DrawText(g, I18n.T("office.noSchedule"), font,
                    new Rectangle(16, 60, _listHost.Width - 32, 30), colors.TextDisabled,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
        }

        /// <summary>
        /// 绘制列表项。
        /// </summary>
        private void DrawListItem(Graphics g, ScheduleItem item, Rectangle rect)
        {
            var colors = AppTheme.Colors;

            var barColor = ParseColor(item.Color, colors.Accent);
            var barRect = new Rectangle(12, rect.Y + 12, 4, rect.Height - 24);
            using (var brush = new SolidBrush(barColor))
            using (var path = GraphicsExtensions.GetRoundPath(barRect, 2))
                g.FillPath(brush, path);

            string timeStr = item.StartTime ?? "";
            if (!string.IsNullOrEmpty(item.EndTime)) timeStr += " - " + item.EndTime;
            TextRenderer.DrawText(g, timeStr, AppTheme.SmallFont,
                new Rectangle(28, rect.Y + 10, 160, 18), colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            TextRenderer.DrawText(g, item.Title ?? "", AppTheme.BodyFont,
                new Rectangle(28, rect.Y + 28, rect.Width - 200, 22), colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            string sub = (item.Location ?? "") + (string.IsNullOrEmpty(item.Location) ? "" : "  ·  ") + CategoryText(item.Category);
            TextRenderer.DrawText(g, sub, AppTheme.SmallFont,
                new Rectangle(rect.Right - 300, rect.Y + 18, 280, 20), colors.TextSecondary,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            using var pen = new Pen(colors.Divider, 1f);
            g.DrawLine(pen, 12, rect.Bottom - 1, rect.Right - 12, rect.Bottom - 1);
        }

        /// <summary>
        /// 列表点击。
        /// </summary>
        private void ListHost_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Y < 34) return;

            int idx = (e.Y - 34 + _scrollY) / RowH;
            var items = ScheduleRepository.GetForDay(_schedules, _selectedDate)
                .OrderBy(s => s.StartTime)
                .ToList();
            if (idx < 0 || idx >= items.Count) return;

            if (e.Button == MouseButtons.Left && e.Clicks == 2)
            {
                var target = _schedules.Find(x => x.Id == items[idx].Id);
                if (target != null)
                    _owner.OpenScheduleEditor(target);
            }
            else if (e.Button == MouseButtons.Right)
            {
                var target = _schedules.Find(x => x.Id == items[idx].Id);
                if (target == null) return;

                var menu = new FlatContextMenu();
                menu.AddItem(FlatMenuItem.Create(I18n.T("menu.edit"),
                    () => _owner.OpenScheduleEditor(target), Icons.Edit));
                menu.AddItem(FlatMenuItem.Create(
                    target.Done ? I18n.T("schedule.markUndone") : I18n.T("schedule.markDone"),
                    () => _owner.ToggleScheduleDone(target), Icons.Ok));
                menu.AddSeparator();
                menu.AddItem(FlatMenuItem.CreateDanger(I18n.T("menu.delete"),
                    () => _owner.DeleteSchedule(target), Icons.Delete));
                menu.ShowAt(_listHost, _listHost.PointToScreen(e.Location));
            }
        }

        /// <summary>
        /// 列表滚轮。
        /// </summary>
        private void ListHost_MouseWheel(object? sender, MouseEventArgs e)
        {
            if (!_listScrollBar.Visible) return;

            int maxScroll = Math.Max(0, _listScrollBar.Maximum - _listScrollBar.LargeChange);
            _scrollY -= Math.Sign(e.Delta) * 60;
            if (_scrollY < 0) _scrollY = 0;
            if (_scrollY > maxScroll) _scrollY = maxScroll;
            _listScrollBar.Value = _scrollY;
            _listScrollBar.Wake();
            _listHost.Invalidate();
        }

        /// <summary>
        /// 更新滚动条。
        /// </summary>
        private void UpdateScrollBar()
        {
            int count = ScheduleRepository.GetForDay(_schedules, _selectedDate).Count;
            int contentH = count * RowH + 34;
            bool need = contentH > _listHost.Height;

            _listScrollBar.Visible = need;
            if (need)
            {
                _listScrollBar.Maximum = contentH;
                _listScrollBar.LargeChange = _listHost.Height;
                _listScrollBar.Value = 0;
                _scrollY = 0;
            }
            else _scrollY = 0;
        }

        // =====================================================
        // 导入导出
        // =====================================================
        /// <summary>
        /// 导出日程。
        /// </summary>
        private void ExportSchedules()
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "JSON|*.json",
                FileName = "schedules.json",
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;

            if (ScheduleRepository.Export(sfd.FileName, _schedules))
                MessageDialog.ShowInfo(I18n.T("common.success"), I18n.T("schedule.exported"));
        }

        /// <summary>
        /// 导入日程。
        /// </summary>
        private void ImportSchedules()
        {
            using var ofd = new OpenFileDialog { Filter = "JSON|*.json" };
            if (ofd.ShowDialog() != DialogResult.OK) return;

            var imported = ScheduleRepository.Import(ofd.FileName);
            if (imported == null)
            {
                MessageDialog.ShowError(I18n.T("common.error"), I18n.T("schedule.importFailed"));
                return;
            }

            _owner.Schedules.AddRange(imported);
            _owner.SaveAll();
            _owner.RefreshAll();

            MessageDialog.ShowInfo(I18n.T("common.success"),
                string.Format(I18n.T("schedule.imported"), imported.Count));
        }

        /// <summary>
        /// 分类翻译。
        /// </summary>
        private static string CategoryText(string category)
        {
            return category switch
            {
                "meeting" => I18n.T("schedule.category.meeting"),
                "task" => I18n.T("schedule.category.task"),
                "reminder" => I18n.T("schedule.category.reminder"),
                "other" => I18n.T("schedule.category.other"),
                _ => "",
            };
        }

        /// <summary>
        /// hex 转 Color。
        /// </summary>
        private static Color ParseColor(string hex, Color fallback)
        {
            if (string.IsNullOrEmpty(hex)) return fallback;
            try
            {
                if (hex.StartsWith("#")) hex = hex.Substring(1);
                if (hex.Length != 6) return fallback;
                int r = Convert.ToInt32(hex.Substring(0, 2), 16);
                int g = Convert.ToInt32(hex.Substring(2, 2), 16);
                int b = Convert.ToInt32(hex.Substring(4, 2), 16);
                return Color.FromArgb(r, g, b);
            }
            catch { return fallback; }
        }
    }
}