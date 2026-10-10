using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
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
    /// 综合评价页。
    /// 班级优化大师风格：左侧学生卡片，右侧选中学生的加减分记录。
    /// 基础分 80，加减分累积在基础分之上。
    /// </summary>
    public class PagePoints : Panel
    {
        /// <summary>主窗口引用</summary>
        private readonly Form1 _owner;

        // ---------- 数据 ----------
        /// <summary>学生列表</summary>
        private List<Student> _students = new();

        /// <summary>积分记录</summary>
        private List<ScoreRecord> _records = new();

        /// <summary>分类列表</summary>
        private List<ScoreCategory> _categories = new();

        // ---------- 左侧 ----------
        /// <summary>左侧面板</summary>
        private Panel _leftPane = null!;

        /// <summary>搜索框</summary>
        private WinUI3SearchBox _searchBox = null!;

        /// <summary>学生卡片列表宿主</summary>
        private Panel _listHost = null!;

        /// <summary>学生卡片滚动条</summary>
        private FlatScrollBar _listScrollBar = null!;

        /// <summary>当前过滤后的学生列表</summary>
        private List<Student> _filtered = new();

        /// <summary>当前选中学生索引</summary>
        private int _selectedIndex = -1;

        /// <summary>当前悬停学生索引</summary>
        private int _hoverIndex = -1;

        /// <summary>列表滚动偏移</summary>
        private int _scrollY = 0;

        /// <summary>卡片高度</summary>
        private const int CardH = 72;

        /// <summary>卡片间距</summary>
        private const int CardGap = 6;

        // ---------- 右侧 ----------
        /// <summary>右侧面板</summary>
        private Panel _rightPane = null!;

        /// <summary>加分按钮</summary>
        private FlatButton _btnAdd = null!;

        /// <summary>减分按钮</summary>
        private FlatButton _btnSubtract = null!;

        /// <summary>导出按钮</summary>
        private FlatButton _btnExport = null!;

        /// <summary>右侧顶部统计面板</summary>
        private Panel _statsPanel = null!;

        /// <summary>右侧记录列表宿主</summary>
        private Panel _logHost = null!;

        /// <summary>右侧记录滚动条</summary>
        private FlatScrollBar _logScrollBar = null!;

        /// <summary>右侧滚动偏移</summary>
        private int _logScrollY = 0;

        /// <summary>记录行高</summary>
        private const int LogRowH = 40;

        /// <summary>
        /// 构造综合评价页。
        /// </summary>
        /// <param name="owner">主窗口</param>
        public PagePoints(Form1 owner)
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

        // =====================================================
        // 界面
        // =====================================================
        /// <summary>
        /// 构建界面。
        /// </summary>
        private void BuildUI()
        {
            // 左侧面板
            _leftPane = new Panel
            {
                Dock = DockStyle.Left,
                Width = 340,
                BackColor = AppTheme.Colors.WindowBg,
            };
            Controls.Add(_leftPane);

            _searchBox = new WinUI3SearchBox
            {
                Left = 16, Top = 16,
                Width = 308,
                Placeholder = I18n.T("points.search"),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            _searchBox.TextChanged += (s, e) => ApplyFilter();
            _leftPane.Controls.Add(_searchBox);

            _listHost = new Panel
            {
                Left = 16, Top = 60,
                Width = 300,
                Height = _leftPane.Height - 60 - 16,
                BackColor = AppTheme.Colors.WindowBg,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            };
            _listHost.Paint += ListHost_Paint;
            _listHost.MouseDown += ListHost_MouseDown;
            _listHost.MouseMove += ListHost_MouseMove;
            _listHost.MouseLeave += (s, e) => { _hoverIndex = -1; _listHost.Invalidate(); };
            _listHost.MouseWheel += ListHost_MouseWheel;
            _leftPane.Controls.Add(_listHost);

            _listScrollBar = new FlatScrollBar
            {
                Left = 320, Top = 60,
                Width = 8,
                Height = _leftPane.Height - 60 - 16,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right,
            };
            _listScrollBar.ValueChanged += (s, e) =>
            {
                _scrollY = _listScrollBar.Value;
                _listHost.Invalidate();
            };
            _leftPane.Controls.Add(_listScrollBar);

            _leftPane.Resize += (s, e) =>
            {
                _listHost.Height = _leftPane.Height - 60 - 16;
                _listScrollBar.Height = _listHost.Height;
            };

            // 右侧面板
            _rightPane = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Colors.WindowBg,
            };
            Controls.Add(_rightPane);
            _rightPane.BringToFront();

            // 操作栏
            var actionBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = AppTheme.Colors.WindowBg,
            };
            _rightPane.Controls.Add(actionBar);

            _btnAdd = new FlatButton
            {
                Text = I18n.T("points.add"),
                ButtonStyle = FlatButtonStyle.Primary,
                Left = 16, Top = 8,
                Width = 100, Height = 32,
            };
            _btnAdd.Click += (s, e) => OpenEntry(true);
            actionBar.Controls.Add(_btnAdd);

            _btnSubtract = new FlatButton
            {
                Text = I18n.T("points.subtract"),
                ButtonStyle = FlatButtonStyle.Danger,
                Left = 124, Top = 8,
                Width = 100, Height = 32,
            };
            _btnSubtract.Click += (s, e) => OpenEntry(false);
            actionBar.Controls.Add(_btnSubtract);

            _btnExport = new FlatButton
            {
                Text = I18n.T("points.export"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Left = 232, Top = 8,
                Width = 100, Height = 32,
            };
            _btnExport.Click += (s, e) => ExportCsv();
            actionBar.Controls.Add(_btnExport);

            // 统计面板
            _statsPanel = new Panel
            {
                Left = 16, Top = 56,
                Width = _rightPane.Width - 32,
                Height = 100,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            _statsPanel.Paint += StatsPanel_Paint;
            _rightPane.Controls.Add(_statsPanel);

            // 记录列表
            _logHost = new Panel
            {
                Left = 16, Top = 168,
                Width = _rightPane.Width - 32 - 12,
                Height = _rightPane.Height - 168 - 16,
                BackColor = AppTheme.Colors.CardBg,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            };
            _logHost.Paint += LogHost_Paint;
            _logHost.MouseWheel += LogHost_MouseWheel;
            _rightPane.Controls.Add(_logHost);

            _logScrollBar = new FlatScrollBar
            {
                Left = _rightPane.Width - 20,
                Top = 168,
                Width = 8,
                Height = _logHost.Height,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right,
            };
            _logScrollBar.ValueChanged += (s, e) =>
            {
                _logScrollY = _logScrollBar.Value;
                _logHost.Invalidate();
            };
            _rightPane.Controls.Add(_logScrollBar);

            _rightPane.Resize += (s, e) =>
            {
                _statsPanel.Width = _rightPane.Width - 32;
                _logHost.Width = _rightPane.Width - 32 - 12;
                _logHost.Height = _rightPane.Height - 168 - 16;
                _logScrollBar.Left = _rightPane.Width - 20;
                _logScrollBar.Height = _logHost.Height;
            };
        }

        // =====================================================
        // 数据
        // =====================================================
        /// <summary>
        /// 设置数据。
        /// </summary>
        /// <param name="students">学生列表</param>
        /// <param name="records">积分记录</param>
        /// <param name="categories">分类列表</param>
        public void SetData(List<Student> students, List<ScoreRecord> records, List<ScoreCategory> categories)
        {
            _students = students ?? new List<Student>();
            _records = records ?? new List<ScoreRecord>();
            _categories = categories ?? new List<ScoreCategory>();

            ScoreRepository.RecalculateStudentPoints(_students, _records);

            ApplyFilter();
            _statsPanel.Invalidate();
            _logHost.Invalidate();
        }

        /// <summary>
        /// 应用搜索过滤。
        /// </summary>
        private void ApplyFilter()
        {
            string keyword = _searchBox.Text.Trim();
            _filtered = string.IsNullOrEmpty(keyword)
                ? _students.ToList()
                : _students.Where(s =>
                    (s.Name ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    (s.Id ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase))
                  .ToList();

            _scrollY = 0;
            int contentH = _filtered.Count * (CardH + CardGap);
            _listScrollBar.Maximum = Math.Max(contentH, _listHost.Height);
            _listScrollBar.LargeChange = _listHost.Height;
            _listScrollBar.Value = 0;

            if (_filtered.Count > 0 && (_selectedIndex < 0 || _selectedIndex >= _filtered.Count))
                _selectedIndex = 0;

            _listHost.Invalidate();
            UpdateLogScrollBar();
        }

        /// <summary>
        /// 更新记录列表滚动条。
        /// </summary>
        private void UpdateLogScrollBar()
        {
            var current = GetCurrentStudent();
            if (current == null)
            {
                _logScrollBar.Visible = false;
                return;
            }

            int count = _records.Count(r => r.StudentId == current.Id);
            int contentH = count * LogRowH + 44;
            bool need = contentH > _logHost.Height;

            _logScrollBar.Visible = need;
            if (need)
            {
                _logScrollBar.Maximum = contentH;
                _logScrollBar.LargeChange = _logHost.Height;
                _logScrollBar.Value = 0;
                _logScrollY = 0;
            }
            else _logScrollY = 0;
        }

        /// <summary>
        /// 取当前选中学生。
        /// </summary>
        private Student? GetCurrentStudent()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _filtered.Count) return null;
            return _filtered[_selectedIndex];
        }

        // =====================================================
        // 左侧绘制
        // =====================================================
        /// <summary>
        /// 绘制学生卡片列表。
        /// </summary>
        private void ListHost_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var colors = AppTheme.Colors;

            using (var bg = new SolidBrush(colors.WindowBg))
                g.FillRectangle(bg, _listHost.ClientRectangle);

            int y = -_scrollY;
            for (int i = 0; i < _filtered.Count; i++)
            {
                var rect = new Rectangle(0, y, _listHost.Width, CardH);
                if (rect.Bottom > 0 && rect.Top < _listHost.Height)
                    DrawStudentCard(g, _filtered[i], i, rect);
                y += CardH + CardGap;
            }

            if (_filtered.Count == 0)
            {
                TextRenderer.DrawText(g, I18n.T("student.empty"), AppTheme.BodyFont,
                    new Rectangle(0, 40, _listHost.Width, 24),
                    colors.TextSecondary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix);
            }
        }

        /// <summary>
        /// 绘制单个学生卡片。
        /// </summary>
        private void DrawStudentCard(Graphics g, Student s, int index, Rectangle rect)
        {
            var colors = AppTheme.Colors;
            bool selected = (index == _selectedIndex);
            bool hover = (index == _hoverIndex);

            Color bg = selected ? colors.SelectedBg
                     : hover ? colors.HoverBg
                             : colors.CardBg;

            using (var path = GraphicsExtensions.GetRoundPath(rect, WinUI3Tokens.CardRadius))
            using (var brush = new SolidBrush(bg))
                g.FillPath(brush, path);

            if (selected)
            {
                using var pen = new Pen(colors.Accent, 2f);
                using var path = GraphicsExtensions.GetRoundPath(rect, WinUI3Tokens.CardRadius);
                g.DrawPath(pen, path);
            }

            // 头像
            var avatarRect = new Rectangle(rect.X + 10, rect.Y + (rect.Height - 44) / 2, 44, 44);
            DrawAvatar(g, s, avatarRect);

            // 姓名
            TextRenderer.DrawText(g, s.Name ?? "", AppTheme.BodyFont,
                new Rectangle(avatarRect.Right + 10, rect.Y + 12, rect.Width - avatarRect.Right - 90, 22),
                colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            // 学号 + 徽章
            string sub = (s.Id ?? "") + (string.IsNullOrEmpty(s.Badge) ? "" : "  ·  " + s.Badge);
            TextRenderer.DrawText(g, sub, AppTheme.SmallFont,
                new Rectangle(avatarRect.Right + 10, rect.Y + 36, rect.Width - avatarRect.Right - 90, 18),
                colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            // 总分
            using var pointFont = new Font(AppTheme.BodyFont.FontFamily, 16f, FontStyle.Bold);
            TextRenderer.DrawText(g, s.TotalPoints.ToString(), pointFont,
                new Rectangle(rect.Right - 70, rect.Y + 20, 60, 30),
                s.TotalPoints >= ScoreRepository.BasePoints
                    ? colors.Accent
                    : Color.FromArgb(0xE8, 0x1B, 0x1B),
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        /// <summary>
        /// 绘制头像（照片或首字母）。
        /// </summary>
        private void DrawAvatar(Graphics g, Student s, Rectangle rect)
        {
            using var path = GraphicsExtensions.GetRoundPath(rect, rect.Width / 2);

            try
            {
                if (!string.IsNullOrEmpty(s.Photo) && File.Exists(s.Photo))
                {
                    using var fs = new FileStream(s.Photo, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using var img = Image.FromStream(fs);
                    var old = g.Clip;
                    g.SetClip(path);
                    float scale = Math.Max((float)rect.Width / img.Width, (float)rect.Height / img.Height);
                    int w = (int)(img.Width * scale);
                    int h = (int)(img.Height * scale);
                    int x = rect.X + (rect.Width - w) / 2;
                    int y = rect.Y + (rect.Height - h) / 2;
                    g.DrawImage(img, new Rectangle(x, y, w, h));
                    g.Clip = old;
                    return;
                }
            }
            catch { }

            using var bg = new SolidBrush(AppTheme.Colors.HoverBg);
            g.FillPath(bg, path);

            string initial = string.IsNullOrEmpty(s.Name) ? "?" : s.Name.Substring(0, 1);
            using var font = new Font(AppTheme.BodyFont.FontFamily, rect.Height * 0.4f, FontStyle.Bold);
            TextRenderer.DrawText(g, initial, font, rect, AppTheme.Colors.TextSecondary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        // =====================================================
        // 右侧统计
        // =====================================================
        /// <summary>
        /// 绘制右侧统计卡片。
        /// </summary>
        private void StatsPanel_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var colors = AppTheme.Colors;

            var current = GetCurrentStudent();
            if (current == null)
            {
                using var font = new Font(AppTheme.BodyFont.FontFamily, 12f);
                TextRenderer.DrawText(g, I18n.T("student.selectStudent"), font,
                    _statsPanel.ClientRectangle, colors.TextSecondary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                return;
            }

            int totalPoints = current.TotalPoints;
            int addTotal = _records.Where(r => r.StudentId == current.Id && r.Delta > 0).Sum(r => r.Delta);
            int subTotal = _records.Where(r => r.StudentId == current.Id && r.Delta < 0).Sum(r => r.Delta);
            int recordCount = _records.Count(r => r.StudentId == current.Id);

            int gap = 12;
            int cardW = (_statsPanel.Width - gap * 3) / 4;
            int cardH = 100;

            DrawStatCard(g, 0, 0, cardW, cardH, totalPoints.ToString(), I18n.T("points.current"), colors.Accent);
            DrawStatCard(g, cardW + gap, 0, cardW, cardH, "+" + addTotal, I18n.T("points.added"), Color.FromArgb(0x4C, 0xAF, 0x50));
            DrawStatCard(g, (cardW + gap) * 2, 0, cardW, cardH, subTotal.ToString(), I18n.T("points.subtracted"), Color.FromArgb(0xE8, 0x1B, 0x1B));
            DrawStatCard(g, (cardW + gap) * 3, 0, cardW, cardH, recordCount.ToString(), I18n.T("points.recordCount"), colors.TextPrimary);
        }

        /// <summary>
        /// 绘制单个统计卡片。
        /// </summary>
        private void DrawStatCard(Graphics g, int x, int y, int w, int h, string value, string label, Color accent)
        {
            var colors = AppTheme.Colors;
            var rect = new Rectangle(x, y, w, h);

            using (var path = GraphicsExtensions.GetRoundPath(rect, WinUI3Tokens.CardRadius))
            using (var brush = new SolidBrush(colors.CardBg))
                g.FillPath(brush, path);

            using (var path = GraphicsExtensions.GetRoundPath(rect, WinUI3Tokens.CardRadius))
            using (var pen = new Pen(colors.CardBorder, 1f))
                g.DrawPath(pen, path);

            using var valueFont = new Font(AppTheme.BodyFont.FontFamily, 24f, FontStyle.Bold);
            TextRenderer.DrawText(g, value, valueFont,
                new Rectangle(rect.X + 16, rect.Y + 16, rect.Width - 32, 36), accent,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            using var labelFont = new Font(AppTheme.BodyFont.FontFamily, 11f);
            TextRenderer.DrawText(g, label, labelFont,
                new Rectangle(rect.X + 16, rect.Y + 58, rect.Width - 32, 20), colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        // =====================================================
        // 右侧记录
        // =====================================================
        /// <summary>
        /// 绘制选中学生的加减分记录。
        /// </summary>
        private void LogHost_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var colors = AppTheme.Colors;

            using (var bg = new SolidBrush(colors.CardBg))
                g.FillRectangle(bg, _logHost.ClientRectangle);

            var current = GetCurrentStudent();
            if (current == null)
            {
                using var font = new Font(AppTheme.BodyFont.FontFamily, 12f);
                TextRenderer.DrawText(g, I18n.T("student.selectStudent"), font,
                    _logHost.ClientRectangle, colors.TextDisabled,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                return;
            }

            // 表头
            var headerRect = new Rectangle(0, 0, _logHost.Width, 44);
            using (var brush = new SolidBrush(colors.GridHeaderBg))
                g.FillRectangle(brush, headerRect);

            TextRenderer.DrawText(g, I18n.T("points.time"), AppTheme.BodyFont,
                new Rectangle(16, 0, 140, 44), colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(g, I18n.T("points.delta"), AppTheme.BodyFont,
                new Rectangle(160, 0, 80, 44), colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(g, I18n.T("points.category"), AppTheme.BodyFont,
                new Rectangle(240, 0, 100, 44), colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(g, I18n.T("points.reason"), AppTheme.BodyFont,
                new Rectangle(340, 0, _logHost.Width - 356, 44), colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            using (var pen = new Pen(colors.Divider, 1f))
                g.DrawLine(pen, 0, 44, _logHost.Width, 44);

            var records = _records
                .Where(r => r.StudentId == current.Id)
                .OrderByDescending(r => r.Date)
                .ToList();

            int y = 44 - _logScrollY;
            foreach (var r in records)
            {
                var rowRect = new Rectangle(0, y, _logHost.Width, LogRowH);
                if (rowRect.Bottom > 44 && rowRect.Top < _logHost.Height)
                    DrawLogRow(g, r, rowRect);
                y += LogRowH;
            }

            if (records.Count == 0)
            {
                using var font = new Font(AppTheme.BodyFont.FontFamily, 12f);
                TextRenderer.DrawText(g, I18n.T("points.noLog"), font,
                    new Rectangle(16, 60, _logHost.Width - 32, 30), colors.TextDisabled,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
        }

        /// <summary>
        /// 绘制单条记录。
        /// </summary>
        private void DrawLogRow(Graphics g, ScoreRecord r, Rectangle rect)
        {
            var colors = AppTheme.Colors;

            string time = r.Date ?? "";
            if (time.Length > 16) time = time.Substring(5, 11);

            TextRenderer.DrawText(g, time, AppTheme.SmallFont,
                new Rectangle(16, rect.Y, 140, rect.Height), colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            Color deltaColor = r.Delta >= 0
                ? Color.FromArgb(0x4C, 0xAF, 0x50)
                : Color.FromArgb(0xE8, 0x1B, 0x1B);

            using var deltaFont = new Font(AppTheme.BodyFont.FontFamily, 12f, FontStyle.Bold);
            string deltaStr = r.Delta >= 0 ? "+" + r.Delta : r.Delta.ToString();
            TextRenderer.DrawText(g, deltaStr, deltaFont,
                new Rectangle(160, rect.Y, 80, rect.Height), deltaColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            TextRenderer.DrawText(g, r.Category ?? "", AppTheme.SmallFont,
                new Rectangle(240, rect.Y, 100, rect.Height), colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            string reason = (r.Reason ?? "") + (string.IsNullOrEmpty(r.Operator) ? "" : "  ·  " + r.Operator);
            TextRenderer.DrawText(g, reason, AppTheme.SmallFont,
                new Rectangle(340, rect.Y, rect.Width - 356, rect.Height), colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            using var pen = new Pen(colors.Divider, 1f);
            g.DrawLine(pen, 16, rect.Bottom - 1, rect.Right - 16, rect.Bottom - 1);
        }

        // =====================================================
        // 鼠标
        // =====================================================
        /// <summary>
        /// 命中测试：返回鼠标所在学生卡片索引。
        /// </summary>
        private int HitTest(Point p)
        {
            if (p.Y < 0) return -1;
            int idx = (p.Y + _scrollY) / (CardH + CardGap);
            if (idx < 0 || idx >= _filtered.Count) return -1;
            return idx;
        }

        /// <summary>
        /// 鼠标移动时更新悬停。
        /// </summary>
        private void ListHost_MouseMove(object? sender, MouseEventArgs e)
        {
            int idx = HitTest(e.Location);
            if (idx != _hoverIndex) { _hoverIndex = idx; _listHost.Invalidate(); }
        }

        /// <summary>
        /// 鼠标点击时切换选中。
        /// </summary>
        private void ListHost_MouseDown(object? sender, MouseEventArgs e)
        {
            int idx = HitTest(e.Location);
            if (idx < 0) return;
            _selectedIndex = idx;
            _listHost.Invalidate();
            _statsPanel.Invalidate();
            UpdateLogScrollBar();
            _logHost.Invalidate();
        }

        /// <summary>
        /// 学生列表滚轮。
        /// </summary>
        private void ListHost_MouseWheel(object? sender, MouseEventArgs e)
        {
            int contentH = _filtered.Count * (CardH + CardGap);
            int maxScroll = Math.Max(0, contentH - _listHost.Height);
            _scrollY -= Math.Sign(e.Delta) * 60;
            if (_scrollY < 0) _scrollY = 0;
            if (_scrollY > maxScroll) _scrollY = maxScroll;
            _listScrollBar.Value = _scrollY;
            _listScrollBar.Wake();
            _listHost.Invalidate();
        }

        /// <summary>
        /// 记录列表滚轮。
        /// </summary>
        private void LogHost_MouseWheel(object? sender, MouseEventArgs e)
        {
            if (!_logScrollBar.Visible) return;

            int maxScroll = Math.Max(0, _logScrollBar.Maximum - _logScrollBar.LargeChange);
            _logScrollY -= Math.Sign(e.Delta) * 60;
            if (_logScrollY < 0) _logScrollY = 0;
            if (_logScrollY > maxScroll) _logScrollY = maxScroll;
            _logScrollBar.Value = _logScrollY;
            _logScrollBar.Wake();
            _logHost.Invalidate();
        }

        // =====================================================
        // 动作
        // =====================================================
        /// <summary>
        /// 打开加分 / 减分弹窗。
        /// </summary>
        private void OpenEntry(bool positive)
        {
            using var dlg = new ScoreEntryDialog(_students, _categories, positive);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            _owner.ScoreRecords.AddRange(dlg.Result);
            _owner.SaveAll();
            _owner.RefreshAll();
        }

        /// <summary>
        /// 导出积分记录到 CSV。
        /// </summary>
        private void ExportCsv()
        {
            string path = FileBrowserDialog.PickFile(AppPaths.BaseDir, "CSV 文件|*.csv");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                var lines = new List<string>
                {
                    "时间,学号,姓名,分值,分类,原因,操作人"
                };

                foreach (var r in _records.OrderByDescending(r => r.Date))
                {
                    lines.Add($"{r.Date},{r.StudentId},{r.StudentName},{r.Delta},{r.Category},{r.Reason},{r.Operator}");
                }

                File.WriteAllLines(path, lines, System.Text.Encoding.UTF8);
                MessageDialog.ShowInfo(I18n.T("common.success"), I18n.T("points.exported"));
            }
            catch (Exception ex)
            {
                MessageDialog.ShowError(I18n.T("common.error"), ex.Message);
            }
        }
    }
}