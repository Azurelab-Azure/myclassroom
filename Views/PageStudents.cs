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
    /// 学生页。
    /// 左侧列表 + 右侧（座位表 / 个人档案 / 花名册 / 综合评价）。
    /// 学生卡片显示总分，个人档案显示积分与徽章，综合评价支持加减分。
    /// </summary>
    public class PageStudents : Panel
    {
        /// <summary>主窗口引用</summary>
        private readonly Form1 _owner;

        // ---------- 左侧 ----------
        /// <summary>左侧面板</summary>
        private Panel _leftPane = null!;

        /// <summary>搜索框</summary>
        private WinUI3SearchBox _searchBox = null!;

        /// <summary>导入按钮</summary>
        private FlatButton _btnImport = null!;

        /// <summary>导出按钮</summary>
        private FlatButton _btnExport = null!;

        /// <summary>添加学生按钮</summary>
        private FlatButton _btnAdd = null!;

        /// <summary>学生列表宿主</summary>
        private Panel _listHost = null!;

        /// <summary>学生列表滚动条</summary>
        private FlatScrollBar _listScrollBar = null!;

        // ---------- 右侧 ----------
        /// <summary>右侧面板</summary>
        private Panel _rightPane = null!;

        /// <summary>分段控件</summary>
        private WinUI3Segmented _tabBar = null!;

        /// <summary>右侧内容宿主</summary>
        private Panel _contentHost = null!;

        // ---------- 数据 ----------
        /// <summary>过滤后的学生列表</summary>
        private List<Student> _filtered = new();

        /// <summary>积分记录</summary>
        private List<ScoreRecord> _records = new();

        /// <summary>积分分类</summary>
        private List<ScoreCategory> _categories = new();

        /// <summary>当前选中索引</summary>
        private int _selectedIndex = -1;

        /// <summary>当前悬停索引</summary>
        private int _hoverIndex = -1;

        /// <summary>左侧滚动偏移</summary>
        private int _scrollY = 0;

        /// <summary>学生卡片高度</summary>
        private const int CardH = 68;

        /// <summary>学生卡片间距</summary>
        private const int CardGap = 6;

        // ---------- 视图 ----------
        /// <summary>视图模式</summary>
        private enum ViewMode { Seat, Profile, Roster, Overall }

        /// <summary>当前视图</summary>
        private ViewMode _viewMode = ViewMode.Seat;

        /// <summary>座位行数</summary>
        private int _seatRows = 6;

        /// <summary>座位列数</summary>
        private int _seatCols = 8;

        /// <summary>"综合评价" tab 加分按钮纵向位置</summary>
        private int _overallAddY = 0;

        /// <summary>"综合评价" tab 减分按钮纵向位置</summary>
        private int _overallSubY = 0;

        /// <summary>
        /// 构造学生页。
        /// </summary>
        /// <param name="owner">主窗口</param>
        public PageStudents(Form1 owner)
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
                Placeholder = I18n.T("student.search"),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            _searchBox.TextChanged += (s, e) => ApplyFilter();
            _leftPane.Controls.Add(_searchBox);

            _btnImport = new FlatButton
            {
                Text = I18n.T("student.import"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Left = 16, Top = 60,
                Width = 150, Height = 30,
            };
            _btnImport.Click += (s, e) => ImportStudents();
            _leftPane.Controls.Add(_btnImport);

            _btnExport = new FlatButton
            {
                Text = I18n.T("student.export"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Left = 174, Top = 60,
                Width = 150, Height = 30,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };
            _btnExport.Click += (s, e) => ExportStudents();
            _leftPane.Controls.Add(_btnExport);

            _listHost = new Panel
            {
                Left = 16, Top = 100,
                Width = 300,
                Height = _leftPane.Height - 100 - 60,
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
                Left = 320, Top = 100,
                Width = 8,
                Height = _leftPane.Height - 100 - 60,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right,
            };
            _listScrollBar.ValueChanged += (s, e) =>
            {
                _scrollY = _listScrollBar.Value;
                _listHost.Invalidate();
            };
            _leftPane.Controls.Add(_listScrollBar);

            _btnAdd = new FlatButton
            {
                Text = I18n.T("student.add"),
                ButtonStyle = FlatButtonStyle.Primary,
                Left = 16, Width = 308, Height = 36,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            };
            _btnAdd.Click += (s, e) => AddStudent();
            _leftPane.Controls.Add(_btnAdd);

            _leftPane.Resize += (s, e) =>
            {
                _btnAdd.Top = _leftPane.Height - 52;
                _listHost.Height = _leftPane.Height - 100 - 60;
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

            _tabBar = new WinUI3Segmented
            {
                Left = 16, Top = 16,
                Width = 480, Height = 36,
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
            };
            _tabBar.SetItems(new[]
            {
                I18n.T("student.tabSeat"),
                I18n.T("student.tabProfile"),
                I18n.T("student.tabRoster"),
                I18n.T("student.tabOverall"),
            });
            _tabBar.SelectedIndex = 0;
            _tabBar.SelectedIndexChanged += (s, e) =>
            {
                _viewMode = (ViewMode)_tabBar.SelectedIndex;
                _contentHost.Invalidate();
            };
            _rightPane.Controls.Add(_tabBar);

            _contentHost = new Panel
            {
                Left = 0, Top = 64,
                Width = _rightPane.Width,
                Height = _rightPane.Height - 64,
                BackColor = AppTheme.Colors.WindowBg,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            };
            _contentHost.Paint += ContentHost_Paint;
            _contentHost.MouseDown += ContentHost_MouseDown;
            _rightPane.Controls.Add(_contentHost);

            _rightPane.Resize += (s, e) =>
            {
                _contentHost.Width = _rightPane.Width;
                _contentHost.Height = _rightPane.Height - 64;
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
        /// <param name="categories">积分分类</param>
        public void SetData(List<Student> students, List<ScoreRecord> records, List<ScoreCategory> categories)
        {
            _records = records ?? new List<ScoreRecord>();
            _categories = categories ?? new List<ScoreCategory>();
            ScoreRepository.RecalculateStudentPoints(_owner.Students, _records);
            ApplyFilter();
        }

        /// <summary>
        /// 应用搜索过滤。
        /// </summary>
        private void ApplyFilter()
        {
            string keyword = _searchBox.Text.Trim();
            _filtered = string.IsNullOrEmpty(keyword)
                ? _owner.Students.ToList()
                : _owner.Students.Where(s =>
                    (s.Name ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    (s.Id ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    (s.Duty ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase))
                  .ToList();

            _scrollY = 0;
            int contentH = _filtered.Count * (CardH + CardGap);
            _listScrollBar.Maximum = Math.Max(contentH, _listHost.Height);
            _listScrollBar.LargeChange = _listHost.Height;
            _listScrollBar.Value = 0;

            _listHost.Invalidate();
            _contentHost.Invalidate();
        }

        // =====================================================
        // 左侧列表
        // =====================================================
        /// <summary>
        /// 绘制学生列表。
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
                new Rectangle(avatarRect.Right + 10, rect.Y + 10, rect.Width - avatarRect.Right - 90, 22),
                colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            // 学号 + 职务
            string sub = (s.Id ?? "") + (string.IsNullOrEmpty(s.Duty) ? "" : "  ·  " + s.Duty);
            TextRenderer.DrawText(g, sub, AppTheme.SmallFont,
                new Rectangle(avatarRect.Right + 10, rect.Y + 34, rect.Width - avatarRect.Right - 90, 18),
                colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            // 等级徽章
            if (!string.IsNullOrEmpty(s.Level))
            {
                var badge = LevelToKind(s.Level);
                using var badgeFont = new Font(AppTheme.BodyFont.FontFamily, 10f, FontStyle.Bold);
                var size = TextRenderer.MeasureText(g, s.Level, badgeFont);
                int bw = size.Width + 12;
                var badgeRect = new Rectangle(rect.Right - bw - 70, rect.Y + 10, bw, 20);

                using var brush = new SolidBrush(badge.bg);
                using var path = GraphicsExtensions.GetRoundPath(badgeRect, 10);
                g.FillPath(brush, path);

                TextRenderer.DrawText(g, s.Level, badgeFont, badgeRect, badge.fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix);
            }

            // 总分
            using var pointFont = new Font(AppTheme.BodyFont.FontFamily, 15f, FontStyle.Bold);
            TextRenderer.DrawText(g, s.TotalPoints.ToString(), pointFont,
                new Rectangle(rect.Right - 62, rect.Y + 8, 54, 24),
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

        /// <summary>
        /// 等级配色。
        /// </summary>
        private static (Color bg, Color fg) LevelToKind(string level)
        {
            return level switch
            {
                "优" => (Color.FromArgb(40, 0x4C, 0xAF, 0x50), Color.FromArgb(0x2E, 0x7D, 0x32)),
                "良" => (Color.FromArgb(40, 0x2E, 0x86, 0xE8), Color.FromArgb(0x1F, 0x6F, 0xC4)),
                "中" => (Color.FromArgb(40, 0xFF, 0xB9, 0x00), Color.FromArgb(0x8A, 0x6D, 0x00)),
                "待提高" => (Color.FromArgb(40, 0xE8, 0x1B, 0x1B), Color.FromArgb(0xC4, 0x2B, 0x1C)),
                _ => (Color.FromArgb(40, 0x80, 0x80, 0x80), Color.FromArgb(0x60, 0x60, 0x60)),
            };
        }

        // =====================================================
        // 右侧内容
        // =====================================================
        /// <summary>
        /// 根据视图模式绘制内容。
        /// </summary>
        private void ContentHost_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var colors = AppTheme.Colors;

            using (var bg = new SolidBrush(colors.WindowBg))
                g.FillRectangle(bg, _contentHost.ClientRectangle);

            switch (_viewMode)
            {
                case ViewMode.Seat: DrawSeatMap(g); break;
                case ViewMode.Profile: DrawProfile(g); break;
                case ViewMode.Roster: DrawRoster(g); break;
                case ViewMode.Overall: DrawOverall(g); break;
            }
        }

        // ---------- 座位表 ----------
        /// <summary>
        /// 绘制座位表。
        /// </summary>
        private void DrawSeatMap(Graphics g)
        {
            var colors = AppTheme.Colors;

            int startX = 24;
            int startY = 24;
            int seatW = 88;
            int seatH = 60;
            int gap = 8;

            int totalW = _seatCols * (seatW + gap) - gap;
            var podiumRect = new Rectangle(
                startX + (totalW - 140) / 2,
                startY, 140, 26);
            using (var brush = new SolidBrush(colors.Divider))
                g.FillRectangle(brush, podiumRect);
            TextRenderer.DrawText(g, I18n.T("student.podium"), AppTheme.SmallFont, podiumRect,
                colors.TextSecondary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            int gridY = startY + 46;

            for (int r = 0; r < _seatRows; r++)
            {
                for (int c = 0; c < _seatCols; c++)
                {
                    int x = startX + c * (seatW + gap);
                    int y = gridY + r * (seatH + gap);
                    var seatRect = new Rectangle(x, y, seatW, seatH);

                    string seatKey = (r + 1) + "," + (c + 1);
                    var student = _owner.Students.FirstOrDefault(s => s.Seat == seatKey);

                    Color bg = student != null ? colors.CardBg : colors.WindowBg;
                    Color border = student != null ? colors.Accent : colors.Divider;

                    using (var path = GraphicsExtensions.GetRoundPath(seatRect, WinUI3Tokens.CardRadius))
                    using (var brush = new SolidBrush(bg))
                        g.FillPath(brush, path);

                    using (var path = GraphicsExtensions.GetRoundPath(seatRect, WinUI3Tokens.CardRadius))
                    using (var pen = new Pen(border, 1f))
                        g.DrawPath(pen, path);

                    string text = student != null ? (student.Name ?? "") : I18n.T("student.emptySeat");
                    Color textColor = student != null ? colors.TextPrimary : colors.TextDisabled;

                    TextRenderer.DrawText(g, text, AppTheme.SmallFont, seatRect, textColor,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                        TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                }
            }
        }

        // ---------- 个人档案 ----------
        /// <summary>
        /// 绘制个人档案。
        /// </summary>
        private void DrawProfile(Graphics g)
        {
            var colors = AppTheme.Colors;

            if (_selectedIndex < 0 || _selectedIndex >= _filtered.Count)
            {
                DrawEmpty(g, I18n.T("student.selectStudent"));
                return;
            }

            var s = _filtered[_selectedIndex];

            int x = 32;
            int y = 32;

            var avatarRect = new Rectangle(x, y, 120, 120);
            using (var path = GraphicsExtensions.GetRoundPath(avatarRect, WinUI3Tokens.CardRadius))
            using (var brush = new SolidBrush(colors.HoverBg))
                g.FillPath(brush, path);
            DrawProfileAvatar(g, s, avatarRect);

            using var nameFont = new Font(AppTheme.BodyFont.FontFamily, 22f, FontStyle.Bold);
            TextRenderer.DrawText(g, s.Name ?? "", nameFont,
                new Rectangle(x + 140, y + 10, 420, 36), colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            string sub = (s.Id ?? "") + "  ·  " + GenderText(s.Gender) + "  ·  " + (s.Duty ?? "");
            TextRenderer.DrawText(g, sub, AppTheme.BodyFont,
                new Rectangle(x + 140, y + 52, 420, 24), colors.Accent,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            // 总分大字
            using var pointFont = new Font(AppTheme.BodyFont.FontFamily, 32f, FontStyle.Bold);
            TextRenderer.DrawText(g, s.TotalPoints.ToString(), pointFont,
                new Rectangle(x + 140, y + 78, 200, 40),
                s.TotalPoints >= ScoreRepository.BasePoints
                    ? colors.Accent
                    : Color.FromArgb(0xE8, 0x1B, 0x1B),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            y += 150;

            y = AddDetailRow(g, I18n.T("student.seat"), s.Seat, x, y);
            y = AddDetailRow(g, I18n.T("student.birth"), s.BirthDate, x, y);
            y = AddDetailRow(g, I18n.T("student.level"), s.Level, x, y);
            y = AddDetailRow(g, I18n.T("student.healthStatus"), s.HealthStatus, x, y);
            y = AddDetailRow(g, I18n.T("student.parent"), s.ParentName, x, y);
            y = AddDetailRow(g, I18n.T("student.parentPhone"), s.ParentPhone, x, y);
            y = AddDetailRow(g, I18n.T("student.address"), s.Address, x, y);
            y = AddDetailRow(g, I18n.T("student.remark"), s.Remark, x, y);

            // ---------- 积分 / 徽章 ----------
            y += 12;
            using (var sectionFont = new Font(AppTheme.BodyFont.FontFamily, 14f, FontStyle.Bold))
                TextRenderer.DrawText(g, I18n.T("student.section.points"), sectionFont,
                    new Rectangle(x, y, 400, 24), colors.Accent,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            y += 30;

            y = AddDetailRow(g, I18n.T("student.totalPoints"), s.TotalPoints.ToString(), x, y);
            y = AddDetailRow(g, I18n.T("student.badge"),
                string.IsNullOrEmpty(s.Badge) ? I18n.T("badge.none") : s.Badge, x, y);

            // ---------- 最近记录 ----------
            var recent = _records
                .Where(r => r.StudentId == s.Id)
                .OrderByDescending(r => r.Date)
                .Take(5)
                .ToList();

            if (recent.Count > 0)
            {
                y += 8;
                using var recFont = new Font(AppTheme.BodyFont.FontFamily, 12f, FontStyle.Bold);
                TextRenderer.DrawText(g, I18n.T("points.recentLog"), recFont,
                    new Rectangle(x, y, 400, 22), colors.TextPrimary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                y += 26;

                foreach (var r in recent)
                {
                    if (y > _contentHost.Height - 30) break;

                    string time = r.Date ?? "";
                    if (time.Length > 16) time = time.Substring(5, 11);

                    TextRenderer.DrawText(g, time, AppTheme.SmallFont,
                        new Rectangle(x, y, 100, 22), colors.TextSecondary,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

                    Color dc = r.Delta >= 0
                        ? Color.FromArgb(0x4C, 0xAF, 0x50)
                        : Color.FromArgb(0xE8, 0x1B, 0x1B);
                    string ds = r.Delta >= 0 ? "+" + r.Delta : r.Delta.ToString();
                    TextRenderer.DrawText(g, ds, AppTheme.BodyFont,
                        new Rectangle(x + 110, y, 50, 22), dc,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

                    TextRenderer.DrawText(g, r.Category ?? "", AppTheme.SmallFont,
                        new Rectangle(x + 170, y, 80, 22), colors.TextPrimary,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

                    TextRenderer.DrawText(g, r.Reason ?? "", AppTheme.SmallFont,
                        new Rectangle(x + 260, y, 260, 22), colors.TextSecondary,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                        TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                    y += 26;
                }
            }
        }

        /// <summary>
        /// 绘制个人档案头像。
        /// </summary>
        private void DrawProfileAvatar(Graphics g, Student s, Rectangle rect)
        {
            try
            {
                if (!string.IsNullOrEmpty(s.Photo) && File.Exists(s.Photo))
                {
                    using var fs = new FileStream(s.Photo, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using var img = Image.FromStream(fs);
                    using var path = GraphicsExtensions.GetRoundPath(rect, WinUI3Tokens.CardRadius);
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

            string initial = string.IsNullOrEmpty(s.Name) ? "?" : s.Name.Substring(0, 1);
            using var font = new Font(AppTheme.BodyFont.FontFamily, rect.Height * 0.4f, FontStyle.Bold);
            TextRenderer.DrawText(g, initial, font, rect, AppTheme.Colors.TextSecondary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        // ---------- 花名册 ----------
        /// <summary>
        /// 绘制花名册。
        /// </summary>
        private void DrawRoster(Graphics g)
        {
            var colors = AppTheme.Colors;

            int x = 24;
            int y = 24;
            int w = _contentHost.Width - 48;

            var headerRect = new Rectangle(x, y, w, 34);
            using (var brush = new SolidBrush(colors.GridHeaderBg))
                g.FillRectangle(brush, headerRect);

            string[] headers = { "学号", "姓名", "性别", "等级", "积分", "职务", "家长电话" };
            int[] widths = { 90, 90, 60, 70, 70, 90, 130 };
            int cx = x + 12;
            for (int i = 0; i < headers.Length; i++)
            {
                TextRenderer.DrawText(g, headers[i], AppTheme.BodyFont,
                    new Rectangle(cx, y, widths[i], 34), colors.TextSecondary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                cx += widths[i];
            }
            y += 34;

            int rowH = 36;
            for (int i = 0; i < _filtered.Count; i++)
            {
                var s = _filtered[i];
                var rowRect = new Rectangle(x, y, w, rowH);

                if (i % 2 == 1)
                {
                    using var brush = new SolidBrush(colors.GridStripeBg);
                    g.FillRectangle(brush, rowRect);
                }

                string[] values = {
                    s.Id ?? "", s.Name ?? "", GenderText(s.Gender),
                    s.Level ?? "", s.TotalPoints.ToString(), s.Duty ?? "", s.ParentPhone ?? ""
                };

                cx = x + 12;
                for (int j = 0; j < values.Length; j++)
                {
                    Color fg = (j == 4 && s.TotalPoints < ScoreRepository.BasePoints)
                        ? Color.FromArgb(0xE8, 0x1B, 0x1B)
                        : colors.TextPrimary;

                    TextRenderer.DrawText(g, values[j], AppTheme.BodyFont,
                        new Rectangle(cx, y, widths[j], rowH), fg,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                        TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                    cx += widths[j];
                }

                using var pen = new Pen(colors.Divider, 1f);
                g.DrawLine(pen, x, y + rowH - 1, x + w, y + rowH - 1);

                y += rowH;
                if (y > _contentHost.Height - 40) break;
            }
        }

        // ---------- 综合评价 ----------
        /// <summary>
        /// 绘制综合评价。
        /// </summary>
        private void DrawOverall(Graphics g)
        {
            var colors = AppTheme.Colors;

            if (_selectedIndex < 0 || _selectedIndex >= _filtered.Count)
            {
                DrawEmpty(g, I18n.T("student.selectStudent"));
                return;
            }

            var s = _filtered[_selectedIndex];

            int x = 32;
            int y = 32;

            using var titleFont = new Font(AppTheme.BodyFont.FontFamily, 20f, FontStyle.Bold);
            TextRenderer.DrawText(g, s.Name ?? "", titleFont,
                new Rectangle(x, y, 500, 36), colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            y += 50;

            // 总分卡片
            var totalCard = new Rectangle(x, y, 220, 100);
            using (var path = GraphicsExtensions.GetRoundPath(totalCard, WinUI3Tokens.CardRadius))
            using (var brush = new SolidBrush(colors.CardBg))
                g.FillPath(brush, path);

            using (var path = GraphicsExtensions.GetRoundPath(totalCard, WinUI3Tokens.CardRadius))
            using (var pen = new Pen(colors.CardBorder, 1f))
                g.DrawPath(pen, path);

            using (var labelFont = new Font(AppTheme.BodyFont.FontFamily, 12f))
                TextRenderer.DrawText(g, I18n.T("points.current"), labelFont,
                    new Rectangle(totalCard.X + 16, totalCard.Y + 16, 180, 20),
                    colors.TextSecondary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            using (var valueFont = new Font(AppTheme.BodyFont.FontFamily, 32f, FontStyle.Bold))
                TextRenderer.DrawText(g, s.TotalPoints.ToString(), valueFont,
                    new Rectangle(totalCard.X + 16, totalCard.Y + 40, 180, 44),
                    s.TotalPoints >= ScoreRepository.BasePoints
                        ? colors.Accent
                        : Color.FromArgb(0xE8, 0x1B, 0x1B),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            // 徽章卡片
            var badgeCard = new Rectangle(x + 240, y, 220, 100);
            using (var path = GraphicsExtensions.GetRoundPath(badgeCard, WinUI3Tokens.CardRadius))
            using (var brush = new SolidBrush(colors.CardBg))
                g.FillPath(brush, path);

            using (var path = GraphicsExtensions.GetRoundPath(badgeCard, WinUI3Tokens.CardRadius))
            using (var pen = new Pen(colors.CardBorder, 1f))
                g.DrawPath(pen, path);

            using (var labelFont = new Font(AppTheme.BodyFont.FontFamily, 12f))
                TextRenderer.DrawText(g, I18n.T("student.badge"), labelFont,
                    new Rectangle(badgeCard.X + 16, badgeCard.Y + 16, 180, 20),
                    colors.TextSecondary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            using (var valueFont = new Font(AppTheme.BodyFont.FontFamily, 22f, FontStyle.Bold))
                TextRenderer.DrawText(g,
                    string.IsNullOrEmpty(s.Badge) ? I18n.T("badge.none") : s.Badge,
                    valueFont,
                    new Rectangle(badgeCard.X + 16, badgeCard.Y + 40, 180, 44),
                    colors.TextPrimary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            y += 120;

            // 评语
            using (var labelFont = new Font(AppTheme.BodyFont.FontFamily, 14f, FontStyle.Bold))
                TextRenderer.DrawText(g, I18n.T("student.overallComment"), labelFont,
                    new Rectangle(x, y, 460, 24), colors.TextPrimary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            y += 30;

            var commentRect = new Rectangle(x, y, 460, 80);
            using (var path = GraphicsExtensions.GetRoundPath(commentRect, WinUI3Tokens.CardRadius))
            using (var brush = new SolidBrush(colors.CardBg))
                g.FillPath(brush, path);

            TextRenderer.DrawText(g, string.IsNullOrEmpty(s.OverallComment) ? "—" : s.OverallComment,
                AppTheme.BodyFont,
                new Rectangle(commentRect.X + 16, commentRect.Y + 16, commentRect.Width - 32, commentRect.Height - 32),
                colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);

            y += 100;

            // 加减分按钮
            _overallAddY = y;
            _overallSubY = y;

            DrawButton(g, new Rectangle(x, y, 100, 36), I18n.T("points.add"), true);
            DrawButton(g, new Rectangle(x + 112, y, 100, 36), I18n.T("points.subtract"), false);
        }

        /// <summary>
        /// 绘制自绘按钮。
        /// </summary>
        private void DrawButton(Graphics g, Rectangle rect, string text, bool primary)
        {
            var colors = AppTheme.Colors;
            Color bg = primary ? colors.Accent : colors.Danger;
            Color fg = primary ? colors.AccentForeground : colors.DangerForeground;

            using (var path = GraphicsExtensions.GetRoundPath(rect, WinUI3Tokens.ControlRadius))
            using (var brush = new SolidBrush(bg))
                g.FillPath(brush, path);

            TextRenderer.DrawText(g, text, AppTheme.BodyFont, rect, fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        // ---------- 工具 ----------
        /// <summary>
        /// 绘制空状态。
        /// </summary>
        private void DrawEmpty(Graphics g, string text)
        {
            TextRenderer.DrawText(g, text, AppTheme.BodyFont,
                _contentHost.ClientRectangle, AppTheme.Colors.TextSecondary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        /// <summary>
        /// 绘制一行详情。
        /// </summary>
        private int AddDetailRow(Graphics g, string label, string? value, int x, int y)
        {
            var colors = AppTheme.Colors;

            TextRenderer.DrawText(g, label, AppTheme.SmallFont,
                new Rectangle(x, y, 100, 24), colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            TextRenderer.DrawText(g, string.IsNullOrEmpty(value) ? "—" : value, AppTheme.BodyFont,
                new Rectangle(x + 110, y, 420, 24), colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            return y + 30;
        }

        /// <summary>
        /// 性别翻译。
        /// </summary>
        private string GenderText(string gender)
        {
            if (gender == "female") return I18n.T("student.gender.female");
            if (gender == "male") return I18n.T("student.gender.male");
            return "";
        }

        // =====================================================
        // 鼠标
        // =====================================================
        /// <summary>
        /// 左侧列表命中测试。
        /// </summary>
        private int HitTest(Point p)
        {
            if (p.Y < 0) return -1;
            int idx = (p.Y + _scrollY) / (CardH + CardGap);
            if (idx < 0 || idx >= _filtered.Count) return -1;
            return idx;
        }

        /// <summary>
        /// 鼠标移动更新悬停。
        /// </summary>
        private void ListHost_MouseMove(object? sender, MouseEventArgs e)
        {
            int idx = HitTest(e.Location);
            if (idx != _hoverIndex) { _hoverIndex = idx; _listHost.Invalidate(); }
        }

        /// <summary>
        /// 鼠标点击切换选中。
        /// </summary>
        private void ListHost_MouseDown(object? sender, MouseEventArgs e)
        {
            int idx = HitTest(e.Location);
            if (idx < 0) return;
            _selectedIndex = idx;
            _listHost.Invalidate();
            _contentHost.Invalidate();

            if (e.Clicks == 2) EditStudent(_filtered[idx]);
        }

        /// <summary>
        /// 左侧列表滚轮。
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
        /// 右侧内容鼠标点击。
        /// </summary>
        private void ContentHost_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            // 座位表：双击分配
            if (_viewMode == ViewMode.Seat && e.Clicks == 2)
            {
                int startX = 24;
                int startY = 70;
                int seatW = 88;
                int seatH = 60;
                int gap = 8;

                for (int r = 0; r < _seatRows; r++)
                {
                    for (int c = 0; c < _seatCols; c++)
                    {
                        int x = startX + c * (seatW + gap);
                        int y = startY + r * (seatH + gap);
                        if (new Rectangle(x, y, seatW, seatH).Contains(e.Location))
                        {
                            AssignSeat((r + 1) + "," + (c + 1));
                            return;
                        }
                    }
                }
            }

            // 综合评价：加减分按钮
            if (_viewMode == ViewMode.Overall)
            {
                if (_selectedIndex < 0 || _selectedIndex >= _filtered.Count) return;
                var s = _filtered[_selectedIndex];

                var addRect = new Rectangle(32, _overallAddY, 100, 36);
                var subRect = new Rectangle(144, _overallSubY, 100, 36);

                if (addRect.Contains(e.Location)) { OpenScoreEntry(s, true); return; }
                if (subRect.Contains(e.Location)) { OpenScoreEntry(s, false); return; }
            }
        }

        /// <summary>
        /// 分配座位。
        /// </summary>
        private void AssignSeat(string seatKey)
        {
            if (_selectedIndex < 0 || _selectedIndex >= _filtered.Count)
            {
                MessageDialog.ShowInfo(I18n.T("common.info"), I18n.T("student.selectStudent"));
                return;
            }

            var s = _filtered[_selectedIndex];
            var existing = _owner.Students.FirstOrDefault(x => x.Seat == seatKey);
            if (existing != null) existing.Seat = "";
            s.Seat = seatKey;

            _owner.SaveAll();
            _contentHost.Invalidate();
            _listHost.Invalidate();
        }

        // =====================================================
        // 动作
        // =====================================================
        /// <summary>
        /// 添加学生。
        /// </summary>
        private void AddStudent()
        {
            using var dlg = new StudentEditorDialog();
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Result != null)
            {
                _owner.Students.Add(dlg.Result);
                ScoreRepository.RecalculateStudentPoints(_owner.Students, _owner.ScoreRecords);
                _owner.SaveAll();
                ApplyFilter();
            }
        }

        /// <summary>
        /// 编辑学生。
        /// </summary>
        private void EditStudent(Student s)
        {
            using var dlg = new StudentEditorDialog(s);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                ScoreRepository.RecalculateStudentPoints(_owner.Students, _owner.ScoreRecords);
                _owner.SaveAll();
                ApplyFilter();
            }
        }

        /// <summary>
        /// 针对单个学生打开加减分弹窗。
        /// </summary>
        private void OpenScoreEntry(Student s, bool positive)
        {
            var tempList = new List<Student> { s };
            using var dlg = new ScoreEntryDialog(tempList, _categories, positive);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            _owner.ScoreRecords.AddRange(dlg.Result);
            ScoreRepository.RecalculateStudentPoints(_owner.Students, _owner.ScoreRecords);
            _owner.SaveAll();
            _owner.RefreshAll();
        }

        /// <summary>
        /// 导入学生 CSV。
        /// </summary>
        private void ImportStudents()
        {
            string path = FileBrowserDialog.PickFile(AppPaths.BaseDir, "CSV 文件|*.csv");
            if (string.IsNullOrEmpty(path)) return;

            var imported = StudentRepository.ImportCsv(path);
            if (imported.Count == 0)
            {
                MessageDialog.ShowInfo(I18n.T("common.info"), I18n.T("student.importEmpty"));
                return;
            }

            int added = 0;
            foreach (var s in imported)
            {
                var existing = _owner.Students.FirstOrDefault(x => x.Id == s.Id && !string.IsNullOrEmpty(s.Id));
                if (existing == null) { _owner.Students.Add(s); added++; }
            }

            _owner.SaveAll();
            ApplyFilter();
            MessageDialog.ShowInfo(I18n.T("common.success"),
                string.Format(I18n.T("student.imported"), added));
        }

        /// <summary>
        /// 导出学生 CSV。
        /// </summary>
        private void ExportStudents()
        {
            string path = FileBrowserDialog.PickFile(AppPaths.BaseDir, "CSV 文件|*.csv");
            if (string.IsNullOrEmpty(path)) return;

            StudentRepository.ExportCsv(path, _owner.Students);
            MessageDialog.ShowInfo(I18n.T("common.success"), I18n.T("student.exported"));
        }
    }
}