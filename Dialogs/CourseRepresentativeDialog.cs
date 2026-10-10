using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CourseApp.Controls;
using CourseApp.Localization;
using CourseApp.Models;
using CourseApp.Theme;

namespace CourseApp.Dialogs
{
    /// <summary>
    /// 课代表分配弹窗。
    /// 为每门课程选择一名课代表学生。
    /// </summary>
    public class CourseRepresentativeDialog : FlatDialogBase
    {
        private readonly List<Course> _courses;
        private readonly List<Student> _students;
        private readonly List<CourseRepresentative> _existing;

        private readonly List<CourseRepresentative> _result = new();
        private readonly Dictionary<string, FlatComboBox> _boxes = new();

        private FlatScrollBar _scrollBar = null!;
        private Panel _listHost = null!;
        private int _scrollY = 0;
        private const int RowH = 44;

        public List<CourseRepresentative> Result => _result;

        public CourseRepresentativeDialog(
            List<Course> courses,
            List<Student> students,
            List<CourseRepresentative> existing)
            : base(I18n.T("courseRep.title"), 620, 600)
        {
            _courses = courses ?? new List<Course>();
            _students = students ?? new List<Student>();
            _existing = existing ?? new List<CourseRepresentative>();

            BuildUI();
        }

        private void BuildUI()
        {
            // 说明
            var desc = new Label
            {
                Text = I18n.T("courseRep.desc"),
                Font = AppTheme.SmallFont,
                ForeColor = AppTheme.Colors.TextSecondary,
                Left = 20, Top = 10, Width = 560, Height = 24,
                BackColor = Color.Transparent,
                AutoSize = false,
            };
            ContentPanel.Controls.Add(desc);

            // 列表宿主
            _listHost = new Panel
            {
                Left = 20, Top = 42,
                Width = ContentPanel.Width - 40 - 12,
                Height = ContentPanel.Height - 42 - 80,
                BackColor = AppTheme.Colors.CardBg,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            };
            _listHost.Paint += ListHost_Paint;
            _listHost.MouseWheel += ListHost_MouseWheel;
            ContentPanel.Controls.Add(_listHost);

            _scrollBar = new FlatScrollBar
            {
                Left = ContentPanel.Width - 20 - 8,
                Top = 42,
                Width = 8,
                Height = _listHost.Height,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right,
            };
            _scrollBar.ValueChanged += (s, e) =>
            {
                _scrollY = _scrollBar.Value;
                LayoutBoxes();
                _listHost.Invalidate();
            };
            ContentPanel.Controls.Add(_scrollBar);

            // 去重课程
            var uniqueCourses = _courses
                .GroupBy(c => c.Name ?? "")
                .Select(g => g.First())
                .Where(c => !string.IsNullOrEmpty(c.Name))
                .OrderBy(c => c.Name)
                .ToList();

            // 创建下拉框
            foreach (var c in uniqueCourses)
            {
                var items = new List<IFlatComboItem>
                {
                    new FlatTextItem(I18n.T("courseRep.none"))
                };
                foreach (var s in _students.OrderBy(s => s.Name))
                    items.Add(new FlatTextItem($"{s.Name} ({s.Id})"));

                var box = new FlatComboBox
                {
                    Width = 220,
                    Height = 32,
                };
                box.SetItems(items);

                var existingRep = _existing.FirstOrDefault(r => r.CourseName == c.Name);
                if (existingRep != null)
                {
                    int idx = _students.FindIndex(s => s.Id == existingRep.StudentId);
                    box.SelectedIndex = idx >= 0 ? idx + 1 : 0;
                }
                else box.SelectedIndex = 0;

                _boxes[c.Name ?? ""] = box;
                _listHost.Controls.Add(box);
            }

            LayoutBoxes();
            UpdateScrollBar();

            // 按钮
            var btnOk = new FlatButton
            {
                Text = I18n.T("dialog.save"),
                ButtonStyle = FlatButtonStyle.Primary,
                Width = 100, Height = 36,
                Left = ContentPanel.Width - 240,
                Top = ContentPanel.Height - 52,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            btnOk.Click += (s, e) => Save();
            ContentPanel.Controls.Add(btnOk);

            var btnCancel = new FlatButton
            {
                Text = I18n.T("dialog.cancel"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Width = 100, Height = 36,
                Left = ContentPanel.Width - 130,
                Top = ContentPanel.Height - 52,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            ContentPanel.Controls.Add(btnCancel);
        }

        private void LayoutBoxes()
        {
            int y = 6 - _scrollY;
            foreach (var kv in _boxes)
            {
                kv.Value.Top = y + 6;
                kv.Value.Left = _listHost.Width - kv.Value.Width - 12;
                y += RowH;
            }
        }

        private void ListHost_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var colors = AppTheme.Colors;

            using (var bg = new SolidBrush(colors.CardBg))
                g.FillRectangle(bg, _listHost.ClientRectangle);

            int y = 6 - _scrollY;
            foreach (var kv in _boxes)
            {
                var rowRect = new Rectangle(0, y, _listHost.Width, RowH);
                if (rowRect.Bottom > 0 && rowRect.Top < _listHost.Height)
                {
                    TextRenderer.DrawText(g, kv.Key, AppTheme.BodyFont,
                        new Rectangle(16, y, _listHost.Width - 260, RowH),
                        colors.TextPrimary,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                        TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                    using var pen = new Pen(colors.Divider, 1f);
                    g.DrawLine(pen, 16, rowRect.Bottom - 1, _listHost.Width - 16, rowRect.Bottom - 1);
                }
                y += RowH;
            }
        }

        private void UpdateScrollBar()
        {
            int contentH = _boxes.Count * RowH + 12;
            bool need = contentH > _listHost.Height;
            _scrollBar.Visible = need;
            if (need)
            {
                _scrollBar.Maximum = contentH;
                _scrollBar.LargeChange = _listHost.Height;
                _scrollBar.Value = 0;
                _scrollY = 0;
            }
            else _scrollY = 0;
        }

        private void ListHost_MouseWheel(object? sender, MouseEventArgs e)
        {
            if (!_scrollBar.Visible) return;
            _scrollY -= Math.Sign(e.Delta) * 60;
            int maxScroll = Math.Max(0, _scrollBar.Maximum - _scrollBar.LargeChange);
            if (_scrollY < 0) _scrollY = 0;
            if (_scrollY > maxScroll) _scrollY = maxScroll;
            _scrollBar.Value = _scrollY;
            _scrollBar.Wake();
            LayoutBoxes();
            _listHost.Invalidate();
        }

        private void Save()
        {
            _result.Clear();
            foreach (var kv in _boxes)
            {
                int idx = kv.Value.SelectedIndex;
                if (idx <= 0) continue;

                var s = _students.OrderBy(x => x.Name).ElementAtOrDefault(idx - 1);
                if (s == null) continue;

                _result.Add(new CourseRepresentative
                {
                    CourseName = kv.Key,
                    StudentId = s.Id ?? "",
                    StudentName = s.Name ?? "",
                });
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}