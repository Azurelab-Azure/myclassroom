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
    /// 学生页：左侧列表 + 右侧（座位表 / 个人档案）。
    /// </summary>
    public class PageStudents : Panel
    {
        private readonly Form1 _owner;

        // 左侧
        private Panel _leftPane = null!;
        private FlatTextBox _searchBox = null!;
        private FlatButton _btnImport = null!;
        private FlatButton _btnExport = null!;
        private FlatButton _btnAdd = null!;
        private Panel _listHost = null!;
        private FlatScrollBar _listScrollBar = null!;

        // 右侧
        private Panel _rightPane = null!;
        private FlatButton _tabSeat = null!;
        private FlatButton _tabProfile = null!;
        private Panel _contentHost = null!;

        // 数据
        private List<Student> _filtered = new();
        private int _selectedIndex = -1;
        private int _hoverIndex = -1;
        private int _scrollY = 0;
        private const int CardH = 56;
        private const int CardGap = 6;

        // 视图
        private enum ViewMode { Seat, Profile }
        private ViewMode _viewMode = ViewMode.Seat;

        // 座位网格
        private int _seatRows = 6;
        private int _seatCols = 8;

        public PageStudents(Form1 owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Colors.WindowBg;

            BuildUI();
        }

        // =====================================================
        // UI
        // =====================================================
        private void BuildUI()
        {
            // ---------- 左侧 ----------
            _leftPane = new Panel
            {
                Dock = DockStyle.Left,
                Width = 340,
                BackColor = AppTheme.Colors.WindowBg,
            };
            Controls.Add(_leftPane);

            // 搜索
            _searchBox = new FlatTextBox
            {
                Left = 16, Top = 16,
                Width = 308, Height = 32,
                Placeholder = I18n.T("student.search"),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            _searchBox.TextChanged += (s, e) => ApplyFilter();
            _leftPane.Controls.Add(_searchBox);

            // 导入 / 导出
            _btnImport = new FlatButton
            {
                Text = I18n.T("student.import"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Left = 16, Top = 56,
                Width = 150, Height = 30,
            };
            _btnImport.Click += (s, e) => ImportStudents();
            _leftPane.Controls.Add(_btnImport);

            _btnExport = new FlatButton
            {
                Text = I18n.T("student.export"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Left = 174, Top = 56,
                Width = 150, Height = 30,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };
            _btnExport.Click += (s, e) => ExportStudents();
            _leftPane.Controls.Add(_btnExport);

            // 列表
            _listHost = new Panel
            {
                Left = 16, Top = 96,
                Width = 300,
                Height = _leftPane.Height - 96 - 60,
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
                Left = 320, Top = 96,
                Width = 8,
                Height = _leftPane.Height - 96 - 60,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right,
            };
            _listScrollBar.ValueChanged += (s, e) =>
            {
                _scrollY = _listScrollBar.Value;
                _listHost.Invalidate();
            };
            _leftPane.Controls.Add(_listScrollBar);

            // 添加
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
                _listHost.Height = _leftPane.Height - 96 - 60;
                _listScrollBar.Height = _listHost.Height;
            };

            // ---------- 右侧 ----------
            _rightPane = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Colors.WindowBg,
            };
            Controls.Add(_rightPane);
            _rightPane.BringToFront();

            // Tab 栏
            var tabBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = AppTheme.Colors.WindowBg,
            };
            _rightPane.Controls.Add(tabBar);

            _tabSeat = new FlatButton
            {
                Text = I18n.T("student.tabSeat"),
                ButtonStyle = FlatButtonStyle.Primary,
                Left = 16, Top = 8,
                Width = 100, Height = 32,
            };
            _tabSeat.Click += (s, e) => SwitchView(ViewMode.Seat);
            tabBar.Controls.Add(_tabSeat);

            _tabProfile = new FlatButton
            {
                Text = I18n.T("student.tabProfile"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Left = 124, Top = 8,
                Width = 100, Height = 32,
            };
            _tabProfile.Click += (s, e) => SwitchView(ViewMode.Profile);
            tabBar.Controls.Add(_tabProfile);

            // 内容容器
            _contentHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Colors.WindowBg,
                AutoScroll = true,
            };
            _contentHost.Paint += ContentHost_Paint;
            _contentHost.MouseDown += ContentHost_MouseDown;
            _rightPane.Controls.Add(_contentHost);
        }

        private void SwitchView(ViewMode mode)
        {
            _viewMode = mode;

            _tabSeat.ButtonStyle = (mode == ViewMode.Seat) ? FlatButtonStyle.Primary : FlatButtonStyle.Secondary;
            _tabProfile.ButtonStyle = (mode == ViewMode.Profile) ? FlatButtonStyle.Primary : FlatButtonStyle.Secondary;

            _contentHost.Invalidate();
        }

        // =====================================================
        // 数据
        // =====================================================
        public void SetData(List<Student> students)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            string keyword = _searchBox.Text.Trim();
            _filtered = string.IsNullOrEmpty(keyword)
                ? _owner.Students.ToList()
                : _owner.Students.Where(s =>
                    (s.Name ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    (s.Id ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase))
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

        private void DrawStudentCard(Graphics g, Student s, int index, Rectangle rect)
        {
            var colors = AppTheme.Colors;
            bool selected = (index == _selectedIndex);
            bool hover = (index == _hoverIndex);

            Color bg = selected ? colors.SelectedBg
                     : hover ? colors.HoverBg
                             : colors.CardBg;

            using (var path = GraphicsExtensions.GetRoundPath(rect, 6))
            using (var brush = new SolidBrush(bg))
                g.FillPath(brush, path);

            if (selected)
            {
                using var pen = new Pen(colors.Accent, 2f);
                using var path = GraphicsExtensions.GetRoundPath(rect, 6);
                g.DrawPath(pen, path);
            }

            // 名字
            var nameRect = new Rectangle(12, rect.Y + 8, rect.Width - 24, 22);
            TextRenderer.DrawText(g, s.Name ?? "", AppTheme.BodyFont, nameRect,
                colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            // 学号 + 座位
            var sub = (s.Id ?? "") + "  " + (s.Seat ?? "");
            var subRect = new Rectangle(12, rect.Y + 30, rect.Width - 24, 18);
            TextRenderer.DrawText(g, sub, AppTheme.SmallFont, subRect,
                colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        // =====================================================
        // 右侧内容
        // =====================================================
        private void ContentHost_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var colors = AppTheme.Colors;

            using (var bg = new SolidBrush(colors.WindowBg))
                g.FillRectangle(bg, _contentHost.ClientRectangle);

            if (_viewMode == ViewMode.Seat)
                DrawSeatMap(g);
            else
                DrawProfile(g);
        }

        // ---------- 座位表 ----------
        private void DrawSeatMap(Graphics g)
        {
            var colors = AppTheme.Colors;

            int startX = 32;
            int startY = 32;
            int seatW = 80;
            int seatH = 56;
            int gap = 8;

            // 讲台
            int totalW = _seatCols * (seatW + gap) - gap;
            var podiumRect = new Rectangle(
                startX + (totalW - 120) / 2,
                startY, 120, 24);
            using (var brush = new SolidBrush(colors.Divider))
                g.FillRectangle(brush, podiumRect);
            TextRenderer.DrawText(g, I18n.T("student.podium"), AppTheme.SmallFont, podiumRect,
                colors.TextSecondary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            int gridY = startY + 44;

            for (int r = 0; r < _seatRows; r++)
            {
                for (int c = 0; c < _seatCols; c++)
                {
                    int x = startX + c * (seatW + gap);
                    int y = gridY + r * (seatH + gap);

                    var seatRect = new Rectangle(x, y, seatW, seatH);

                    // 座位号（行列）
                    string seatKey = (r + 1) + "," + (c + 1);

                    // 找这个座位上的学生
                    var student = _owner.Students.FirstOrDefault(s => s.Seat == seatKey);

                    Color bg = student != null ? colors.CardBg : colors.WindowBg;
                    Color border = student != null ? colors.Accent : colors.Divider;

                    using (var path = GraphicsExtensions.GetRoundPath(seatRect, 6))
                    using (var brush = new SolidBrush(bg))
                        g.FillPath(brush, path);

                    using (var path = GraphicsExtensions.GetRoundPath(seatRect, 6))
                    using (var pen = new Pen(border, 1f))
                        g.DrawPath(pen, path);

                    string text = student != null ? (student.Name ?? "") : I18n.T("student.emptySeat");
                    Color textColor = student != null ? colors.TextPrimary : colors.TextDisabled;

                    TextRenderer.DrawText(g, text, AppTheme.SmallFont, seatRect,
                        textColor,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                        TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                }
            }
        }

        // ---------- 个人档案 ----------
        private void DrawProfile(Graphics g)
        {
            var colors = AppTheme.Colors;

            if (_selectedIndex < 0 || _selectedIndex >= _filtered.Count)
            {
                TextRenderer.DrawText(g, I18n.T("student.selectStudent"), AppTheme.BodyFont,
                    _contentHost.ClientRectangle, colors.TextSecondary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix);
                return;
            }

            var s = _filtered[_selectedIndex];

            int x = 32;
            int y = 32;

            // 头像
            var avatarRect = new Rectangle(x, y, 120, 120);
            using (var path = GraphicsExtensions.GetRoundPath(avatarRect, 12))
            using (var brush = new SolidBrush(colors.HoverBg))
                g.FillPath(brush, path);

            // 头像——照片或首字母
            DrawStudentAvatar(g, s, avatarRect);

            // 姓名
            using var nameFont = new Font(AppTheme.BodyFont.FontFamily, 22f, FontStyle.Bold);
            var nameRect = new Rectangle(x + 140, y + 10, 400, 36);
            TextRenderer.DrawText(g, s.Name ?? "", nameFont, nameRect, colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            // 学号 + 性别
            string sub = (s.Id ?? "") + "  ·  " + GenderText(s.Gender);
            var subRect = new Rectangle(x + 140, y + 52, 400, 24);
            TextRenderer.DrawText(g, sub, AppTheme.BodyFont, subRect, colors.Accent,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            y += 150;

            // 详情行
            y = AddDetailRow(g, I18n.T("student.seat"), s.Seat, x, y);
            y = AddDetailRow(g, I18n.T("student.birth"), s.BirthDate, x, y);
            y = AddDetailRow(g, I18n.T("student.parent"), s.ParentName, x, y);
            y = AddDetailRow(g, I18n.T("student.parentPhone"), s.ParentPhone, x, y);
            y = AddDetailRow(g, I18n.T("student.address"), s.Address, x, y);
            y = AddDetailRow(g, I18n.T("student.remark"), s.Remark, x, y);
        }

        private void DrawStudentAvatar(Graphics g, Student s, Rectangle rect)
        {
            try
            {
                if (!string.IsNullOrEmpty(s.Photo) && File.Exists(s.Photo))
                {
                    using var fs = new FileStream(s.Photo, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using var img = Image.FromStream(fs);

                    using var path = GraphicsExtensions.GetRoundPath(rect, 12);
                    var old = g.Clip;
                    g.SetClip(path);
                    try
                    {
                        float scale = Math.Max((float)rect.Width / img.Width, (float)rect.Height / img.Height);
                        int w = (int)(img.Width * scale);
                        int h = (int)(img.Height * scale);
                        int x = rect.X + (rect.Width - w) / 2;
                        int y = rect.Y + (rect.Height - h) / 2;
                        g.DrawImage(img, new Rectangle(x, y, w, h));
                    }
                    finally { g.Clip = old; }
                    return;
                }
            }
            catch { }

            // 首字母
            string initial = string.IsNullOrEmpty(s.Name) ? "?" : s.Name.Substring(0, 1);
            using var font = new Font(AppTheme.BodyFont.FontFamily, rect.Height * 0.4f, FontStyle.Bold);
            TextRenderer.DrawText(g, initial, font, rect, AppTheme.Colors.TextSecondary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPrefix);
        }

        private int AddDetailRow(Graphics g, string label, string? value, int x, int y)
        {
            var colors = AppTheme.Colors;

            var labelRect = new Rectangle(x, y, 100, 24);
            TextRenderer.DrawText(g, label, AppTheme.SmallFont, labelRect, colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            var valueRect = new Rectangle(x + 110, y, 400, 24);
            TextRenderer.DrawText(g, string.IsNullOrEmpty(value) ? "—" : value, AppTheme.BodyFont,
                valueRect, colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            return y + 30;
        }

        private string GenderText(string gender)
        {
            if (gender == "female") return I18n.T("student.gender.female");
            if (gender == "male") return I18n.T("student.gender.male");
            return "";
        }

        private void ContentHost_MouseDown(object? sender, MouseEventArgs e)
        {
            // 座位表：双击座位分配学生
            if (_viewMode == ViewMode.Seat && e.Button == MouseButtons.Left && e.Clicks == 2)
            {
                int startX = 32;
                int startY = 76;   // 44 + 32
                int seatW = 80;
                int seatH = 56;
                int gap = 8;

                for (int r = 0; r < _seatRows; r++)
                {
                    for (int c = 0; c < _seatCols; c++)
                    {
                        int x = startX + c * (seatW + gap);
                        int y = startY + r * (seatH + gap);
                        var seatRect = new Rectangle(x, y, seatW, seatH);

                        if (seatRect.Contains(e.Location))
                        {
                            string seatKey = (r + 1) + "," + (c + 1);
                            AssignSeat(seatKey);
                            return;
                        }
                    }
                }
            }
        }

        private void AssignSeat(string seatKey)
        {
            if (_selectedIndex < 0 || _selectedIndex >= _filtered.Count)
            {
                MessageDialog.ShowInfo(I18n.T("common.info"), I18n.T("student.selectStudent"));
                return;
            }

            var s = _filtered[_selectedIndex];

            // 清掉这个座位原来的学生
            var existing = _owner.Students.FirstOrDefault(x => x.Seat == seatKey);
            if (existing != null) existing.Seat = "";

            // 清掉这个学生原来的座位
            s.Seat = seatKey;

            _owner.SaveAll();
            _contentHost.Invalidate();
            _listHost.Invalidate();
        }

        // =====================================================
        // 鼠标
        // =====================================================
        private int HitTest(Point p)
        {
            if (p.Y < 0) return -1;
            int idx = (p.Y + _scrollY) / (CardH + CardGap);
            if (idx < 0 || idx >= _filtered.Count) return -1;
            return idx;
        }

        private void ListHost_MouseMove(object? sender, MouseEventArgs e)
        {
            int idx = HitTest(e.Location);
            if (idx != _hoverIndex) { _hoverIndex = idx; _listHost.Invalidate(); }
        }

        private void ListHost_MouseDown(object? sender, MouseEventArgs e)
        {
            int idx = HitTest(e.Location);
            if (idx < 0) return;
            _selectedIndex = idx;
            _listHost.Invalidate();
            _contentHost.Invalidate();

            if (e.Clicks == 2)
                EditStudent(_filtered[idx]);
        }

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

        // =====================================================
        // 动作
        // =====================================================
        private void AddStudent()
        {
            using var dlg = new StudentEditorDialog();
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Result != null)
            {
                _owner.Students.Add(dlg.Result);
                _owner.SaveAll();
                ApplyFilter();
            }
        }

        private void EditStudent(Student s)
        {
            using var dlg = new StudentEditorDialog(s);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _owner.SaveAll();
                ApplyFilter();
            }
        }

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
                if (existing == null)
                {
                    _owner.Students.Add(s);
                    added++;
                }
            }

            _owner.SaveAll();
            ApplyFilter();
            MessageDialog.ShowInfo(I18n.T("common.success"),
                string.Format(I18n.T("student.imported"), added));
        }

        private void ExportStudents()
        {
            string path = FileBrowserDialog.PickFile(AppPaths.BaseDir, "CSV 文件|*.csv");
            if (string.IsNullOrEmpty(path)) return;

            StudentRepository.ExportCsv(path, _owner.Students);
            MessageDialog.ShowInfo(I18n.T("common.success"), I18n.T("student.exported"));
        }
    }
}