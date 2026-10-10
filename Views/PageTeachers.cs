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
    /// 教师页：左侧列表 + 右侧详情 + 课代表管理。
    /// </summary>
    public class PageTeachers : Panel
    {
        private readonly Form1 _owner;

        private Panel _leftPane = null!;
        private Panel _rightPane = null!;
        private WinUI3SearchBox _searchBox = null!;
        private FlatScrollBar _listScrollBar = null!;
        private Panel _listHost = null!;
        private FlatButton _btnAdd = null!;
        private FlatButton _btnRep = null!;

        private List<Teacher> _filtered = new();
        private int _selectedIndex = -1;
        private int _hoverIndex = -1;
        private int _scrollY = 0;
        private const int CardH = 72;
        private const int CardGap = 6;

        public PageTeachers(Form1 owner)
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

        private void BuildUI()
        {
            // ---------- 左侧 ----------
            _leftPane = new Panel
            {
                Dock = DockStyle.Left,
                Width = 320,
                BackColor = AppTheme.Colors.WindowBg,
            };
            Controls.Add(_leftPane);

            _searchBox = new WinUI3SearchBox
            {
                Left = 16, Top = 16,
                Width = 288,
                Placeholder = I18n.T("teacher.search"),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            _searchBox.TextChanged += (s, e) => ApplyFilter();
            _leftPane.Controls.Add(_searchBox);

            _listHost = new Panel
            {
                Left = 16, Top = 60,
                Width = 280,
                Height = _leftPane.Height - 60 - 100,
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
                Left = 300, Top = 60,
                Width = 8,
                Height = _leftPane.Height - 60 - 100,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right,
            };
            _listScrollBar.ValueChanged += (s, e) => { _scrollY = _listScrollBar.Value; _listHost.Invalidate(); };
            _leftPane.Controls.Add(_listScrollBar);

            _btnAdd = new FlatButton
            {
                Text = I18n.T("teacher.add"),
                ButtonStyle = FlatButtonStyle.Primary,
                Left = 16, Width = 288, Height = 36,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            };
            _btnAdd.Click += (s, e) => AddTeacher();
            _leftPane.Controls.Add(_btnAdd);

            _btnRep = new FlatButton
            {
                Text = I18n.T("courseRep.manage"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Left = 16, Width = 288, Height = 36,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            };
            _btnRep.Click += (s, e) => ManageRepresentatives();
            _leftPane.Controls.Add(_btnRep);

            _leftPane.Resize += (s, e) =>
            {
                _btnRep.Top = _leftPane.Height - 96;
                _btnAdd.Top = _leftPane.Height - 52;
                _listHost.Height = _leftPane.Height - 60 - 100;
                _listScrollBar.Height = _listHost.Height;
            };

            // ---------- 右侧 ----------
            _rightPane = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Colors.WindowBg,
                AutoScroll = true,
            };
            Controls.Add(_rightPane);
            _rightPane.BringToFront();
        }

        // =====================================================
        // 数据
        // =====================================================
        public void SetData(List<Teacher> teachers) => ApplyFilter();

        private void ApplyFilter()
        {
            string keyword = _searchBox.Text.Trim();
            _filtered = string.IsNullOrEmpty(keyword)
                ? _owner.Teachers.ToList()
                : _owner.Teachers.Where(t =>
                    (t.Name ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    (t.Subject ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    (t.Phone ?? "").Contains(keyword) ||
                    (t.Email ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase))
                  .ToList();

            _scrollY = 0;
            int contentH = _filtered.Count * (CardH + CardGap);
            _listScrollBar.Maximum = Math.Max(contentH, _listHost.Height);
            _listScrollBar.LargeChange = _listHost.Height;
            _listScrollBar.Value = 0;

            _listHost.Invalidate();

            if (_filtered.Count > 0 && (_selectedIndex < 0 || _selectedIndex >= _filtered.Count))
            {
                _selectedIndex = 0;
                ShowDetail(_filtered[0]);
            }
            else if (_filtered.Count == 0)
            {
                _selectedIndex = -1;
                ShowEmpty();
            }
            else ShowDetail(_filtered[_selectedIndex]);
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
                    DrawTeacherCard(g, _filtered[i], i, rect);
                y += CardH + CardGap;
            }

            if (_filtered.Count == 0)
            {
                TextRenderer.DrawText(g, I18n.T("teacher.empty"), AppTheme.BodyFont,
                    new Rectangle(0, 40, _listHost.Width, 24), colors.TextSecondary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix);
            }
        }

        private void DrawTeacherCard(Graphics g, Teacher t, int index, Rectangle rect)
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

            int avatarSize = 48;
            var avatarRect = new Rectangle(rect.X + 10, rect.Y + (rect.Height - avatarSize) / 2, avatarSize, avatarSize);
            DrawAvatar(g, t, avatarRect);

            var nameRect = new Rectangle(avatarRect.Right + 10, rect.Y + 10, rect.Width - avatarRect.Right - 20, 22);
            TextRenderer.DrawText(g, t.Name ?? "", AppTheme.BodyFont, nameRect, colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            var sub = "";
            if (!string.IsNullOrEmpty(t.Subject)) sub += t.Subject;
            if (!string.IsNullOrEmpty(t.Title)) sub += (sub.Length > 0 ? " · " : "") + t.Title;

            var subRect = new Rectangle(avatarRect.Right + 10, rect.Y + 34, rect.Width - avatarRect.Right - 20, 20);
            TextRenderer.DrawText(g, sub, AppTheme.SmallFont, subRect, colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            // 课代表数量徽章
            var reps = GetRepresentativesForTeacher(t);
            if (reps.Count > 0)
            {
                using var badgeFont = new Font(AppTheme.BodyFont.FontFamily, 10f, FontStyle.Bold);
                string text = reps.Count.ToString();
                var size = TextRenderer.MeasureText(g, text, badgeFont);
                int bw = Math.Max(18, size.Width + 10);
                var badgeRect = new Rectangle(rect.Right - bw - 10, rect.Y + 10, bw, 18);

                using var brush = new SolidBrush(colors.Accent);
                using var path = GraphicsExtensions.GetRoundPath(badgeRect, 9);
                g.FillPath(brush, path);

                TextRenderer.DrawText(g, text, badgeFont, badgeRect, colors.AccentForeground,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
        }

        private void DrawAvatar(Graphics g, Teacher t, Rectangle rect)
        {
            using var path = GraphicsExtensions.GetRoundPath(rect, WinUI3Tokens.ControlRadius);

            if (!string.IsNullOrEmpty(t.Photo) && File.Exists(t.Photo))
            {
                try
                {
                    using var fs = new FileStream(t.Photo, FileMode.Open, FileAccess.Read, FileShare.Read);
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
                catch { }
            }

            using var bg = new SolidBrush(AppTheme.Colors.HoverBg);
            g.FillPath(bg, path);

            string initial = string.IsNullOrEmpty(t.Name) ? "?" : t.Name.Substring(0, 1);
            using var font = new Font(AppTheme.BodyFont.FontFamily, rect.Height * 0.4f, FontStyle.Bold);
            TextRenderer.DrawText(g, initial, font, rect, AppTheme.Colors.TextSecondary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        /// <summary>找该教师所授课程的课代表。</summary>
        private List<(string Course, string Student)> GetRepresentativesForTeacher(Teacher t)
        {
            var result = new List<(string, string)>();
            var courses = _owner.Courses.Where(c => c.Teacher == t.Name).Select(c => c.Name).Distinct();
            foreach (var course in courses)
            {
                var rep = _owner.CourseRepresentatives.FirstOrDefault(r => r.CourseName == course);
                if (rep != null)
                    result.Add((course ?? "", rep.StudentName ?? ""));
            }
            return result;
        }

        // =====================================================
        // 右侧详情
        // =====================================================
        private void ShowEmpty()
        {
            _rightPane.Controls.Clear();
            var lbl = new Label
            {
                Text = I18n.T("teacher.emptyDetail"),
                Font = new Font(AppTheme.BodyFont.FontFamily, 12f),
                ForeColor = AppTheme.Colors.TextSecondary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
            };
            _rightPane.Controls.Add(lbl);
        }

        private void ShowDetail(Teacher t)
        {
            _rightPane.Controls.Clear();
            _rightPane.AutoScrollPosition = new Point(0, 0);

            int x = 32;
            int y = 32;

            var avatar = new Panel
            {
                Left = x, Top = y,
                Width = 120, Height = 120,
                BackColor = Color.Transparent,
            };
            avatar.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                DrawAvatar(g, t, new Rectangle(0, 0, 120, 120));
            };
            _rightPane.Controls.Add(avatar);

            _rightPane.Controls.Add(new Label
            {
                Text = t.Name ?? "",
                Font = new Font(AppTheme.BodyFont.FontFamily, 22f, FontStyle.Bold),
                ForeColor = AppTheme.Colors.TextPrimary,
                Left = x + 140, Top = y + 10,
                Width = _rightPane.Width - x - 160,
                Height = 40,
                BackColor = Color.Transparent,
                AutoSize = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            });

            var sub = "";
            if (!string.IsNullOrEmpty(t.Subject)) sub += t.Subject;
            if (!string.IsNullOrEmpty(t.Title)) sub += (sub.Length > 0 ? " · " : "") + t.Title;

            _rightPane.Controls.Add(new Label
            {
                Text = sub,
                Font = new Font(AppTheme.BodyFont.FontFamily, 12f),
                ForeColor = AppTheme.Colors.Accent,
                Left = x + 140, Top = y + 52,
                Width = _rightPane.Width - x - 160,
                Height = 24,
                BackColor = Color.Transparent,
                AutoSize = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            });

            y += 150;

            y = AddDetailRow(I18n.T("teacher.field.phone"), t.Phone, x, y);
            y = AddDetailRow(I18n.T("teacher.field.email"), t.Email, x, y);
            y = AddDetailRow(I18n.T("teacher.field.office"), t.Office, x, y);
            y = AddDetailRow(I18n.T("teacher.field.info"), t.Info, x, y);
            y = AddDetailRow(I18n.T("teacher.field.remark"), t.Remark, x, y);

            // ---------- 课代表 ----------
            y += 12;
            _rightPane.Controls.Add(new Label
            {
                Text = I18n.T("courseRep.section"),
                Font = new Font(AppTheme.BodyFont.FontFamily, 14f, FontStyle.Bold),
                ForeColor = AppTheme.Colors.TextPrimary,
                Left = x, Top = y, Width = 400, Height = 24,
                BackColor = Color.Transparent,
                AutoSize = false,
            });
            y += 32;

            var reps = GetRepresentativesForTeacher(t);
            if (reps.Count == 0)
            {
                _rightPane.Controls.Add(new Label
                {
                    Text = I18n.T("courseRep.empty"),
                    Font = AppTheme.SmallFont,
                    ForeColor = AppTheme.Colors.TextDisabled,
                    Left = x, Top = y, Width = 400, Height = 22,
                    BackColor = Color.Transparent,
                    AutoSize = false,
                });
                y += 30;
            }
            else
            {
                foreach (var (course, student) in reps)
                {
                    _rightPane.Controls.Add(new Label
                    {
                        Text = $"{course}  ·  {student}",
                        Font = AppTheme.BodyFont,
                        ForeColor = AppTheme.Colors.TextPrimary,
                        Left = x, Top = y, Width = 400, Height = 22,
                        BackColor = Color.Transparent,
                        AutoSize = false,
                    });
                    y += 28;
                }
            }

            var btnEdit = new FlatButton
            {
                Text = I18n.T("common.edit"),
                ButtonStyle = FlatButtonStyle.Primary,
                Left = x, Top = y + 20,
                Width = 100, Height = 36,
            };
            btnEdit.Click += (s, e) => EditTeacher(t);
            _rightPane.Controls.Add(btnEdit);

            var btnDelete = new FlatButton
            {
                Text = I18n.T("common.delete"),
                ButtonStyle = FlatButtonStyle.Danger,
                Left = x + 112, Top = y + 20,
                Width = 100, Height = 36,
            };
            btnDelete.Click += (s, e) => DeleteTeacher(t);
            _rightPane.Controls.Add(btnDelete);
        }

        private int AddDetailRow(string label, string? value, int x, int y)
        {
            _rightPane.Controls.Add(new Label
            {
                Text = label,
                Font = AppTheme.SmallFont,
                ForeColor = AppTheme.Colors.TextSecondary,
                Left = x, Top = y, Width = 80, Height = 22,
                BackColor = Color.Transparent,
                AutoSize = false,
            });

            _rightPane.Controls.Add(new Label
            {
                Text = string.IsNullOrEmpty(value) ? "—" : value,
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.Colors.TextPrimary,
                Left = x + 88, Top = y,
                Width = _rightPane.Width - x - 120,
                Height = 22,
                BackColor = Color.Transparent,
                AutoSize = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            });

            return y + 32;
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
            ShowDetail(_filtered[idx]);
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
        private void AddTeacher()
        {
            using var dlg = new TeacherEditorDialog();
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Result != null)
            {
                _owner.Teachers.Add(dlg.Result);
                _owner.SaveAll();
                _owner.RefreshAll();
                ApplyFilter();
            }
        }

        private void EditTeacher(Teacher t)
        {
            using var dlg = new TeacherEditorDialog(t);
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Result != null)
            {
                _owner.SaveAll();
                _owner.RefreshAll();
                ApplyFilter();
            }
        }

        private void DeleteTeacher(Teacher t)
        {
            if (!MessageDialog.Confirm(I18n.T("common.confirm"),
                string.Format(I18n.T("msg.confirmDeleteTeacher"), t.Name)))
                return;
            _owner.Teachers.Remove(t);
            _owner.SaveAll();
            _owner.RefreshAll();
            _selectedIndex = -1;
            ApplyFilter();
        }

        private void ManageRepresentatives()
        {
            using var dlg = new CourseRepresentativeDialog(
                _owner.Courses, _owner.Students, _owner.CourseRepresentatives);

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _owner.CourseRepresentatives = dlg.Result;
                _owner.SaveAll();
                _owner.RefreshAll();
                ApplyFilter();
            }
        }
    }
}