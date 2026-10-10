using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using CourseApp.Controls;
using CourseApp.Localization;
using CourseApp.Models;
using CourseApp.Theme;

namespace CourseApp.Dialogs
{
    /// <summary>
    /// 加分 / 减分弹窗。
    /// 支持多选学生、选择分类、输入分值和原因。
    /// </summary>
    public class ScoreEntryDialog : FlatDialogBase
    {
        /// <summary>可操作的学生列表</summary>
        private readonly List<Student> _students;

        /// <summary>分类列表</summary>
        private readonly List<ScoreCategory> _categories;

        /// <summary>学生勾选状态：学号 → 是否勾选</summary>
        private readonly Dictionary<string, bool> _checked = new();

        /// <summary>分类下拉框</summary>
        private FlatComboBox _categoryBox = null!;

        /// <summary>分值输入框</summary>
        private FlatNumberBox _deltaBox = null!;

        /// <summary>原因输入框</summary>
        private FlatTextBox _reasonBox = null!;

        /// <summary>操作人输入框</summary>
        private FlatTextBox _operatorBox = null!;

        /// <summary>加分按钮</summary>
        private FlatButton _btnPositive = null!;

        /// <summary>减分按钮</summary>
        private FlatButton _btnNegative = null!;

        /// <summary>学生列表宿主</summary>
        private Panel _listHost = null!;

        /// <summary>学生列表滚动条</summary>
        private FlatScrollBar _scrollBar = null!;

        /// <summary>学生列表滚动偏移</summary>
        private int _scrollY = 0;

        /// <summary>学生行高</summary>
        private const int RowH = 36;

        /// <summary>当前是否加分</summary>
        private bool _positive = true;

        /// <summary>结果记录列表</summary>
        public List<ScoreRecord> Result { get; } = new();

        /// <summary>
        /// 构造加分 / 减分弹窗。
        /// </summary>
        /// <param name="students">可操作的学生</param>
        /// <param name="categories">分类列表</param>
        /// <param name="defaultPositive">默认是否加分</param>
        public ScoreEntryDialog(
            List<Student> students,
            List<ScoreCategory> categories,
            bool defaultPositive)
            : base(I18n.T("points.entryTitle"), 560, 640)
        {
            _students = students ?? new List<Student>();
            _categories = categories ?? new List<ScoreCategory>();
            _positive = defaultPositive;

            foreach (var s in _students)
                _checked[s.Id] = false;

            BuildUI();
        }

        /// <summary>
        /// 构建界面。
        /// </summary>
        private void BuildUI()
        {
            int x = 20;
            int y = 10;
            int fieldX = 130;
            int fieldW = 380;
            int rowH = 36;
            int gap = 8;

            // 加分 / 减分切换
            AddLabel(I18n.T("points.mode"), x, y);
            _btnPositive = new FlatButton
            {
                Text = I18n.T("points.add"),
                ButtonStyle = _positive ? FlatButtonStyle.Primary : FlatButtonStyle.Secondary,
                Left = fieldX, Top = y, Width = 90, Height = 32,
            };
            _btnPositive.Click += (s, e) => SetPositive(true);
            ContentPanel.Controls.Add(_btnPositive);

            _btnNegative = new FlatButton
            {
                Text = I18n.T("points.subtract"),
                ButtonStyle = !_positive ? FlatButtonStyle.Danger : FlatButtonStyle.Secondary,
                Left = fieldX + 100, Top = y, Width = 90, Height = 32,
            };
            _btnNegative.Click += (s, e) => SetPositive(false);
            ContentPanel.Controls.Add(_btnNegative);
            y += rowH + gap;

            // 分类
            AddLabel(I18n.T("points.category"), x, y);
            _categoryBox = new FlatComboBox { Left = fieldX, Top = y, Width = fieldW, Height = 32 };
            var items = _categories.Select(c => (IFlatComboItem)new FlatTextItem(c.Name)).ToList();
            _categoryBox.SetItems(items);
            _categoryBox.SelectedIndex = 0;
            _categoryBox.SelectedIndexChanged += (s, e) => SyncCategoryDefault();
            ContentPanel.Controls.Add(_categoryBox);
            y += rowH + gap;

            // 分值
            AddLabel(I18n.T("points.delta"), x, y);
            _deltaBox = new FlatNumberBox
            {
                Left = fieldX, Top = y, Width = 120, Height = 32,
                Minimum = 1, Maximum = 100, Value = 1,
            };
            ContentPanel.Controls.Add(_deltaBox);
            y += rowH + gap;

            // 原因
            AddLabel(I18n.T("points.reason"), x, y);
            _reasonBox = new FlatTextBox
            {
                Left = fieldX, Top = y, Width = fieldW, Height = 32,
                Placeholder = I18n.T("points.reasonPlaceholder"),
            };
            ContentPanel.Controls.Add(_reasonBox);
            y += rowH + gap;

            // 操作人
            AddLabel(I18n.T("points.operator"), x, y);
            _operatorBox = new FlatTextBox
            {
                Left = fieldX, Top = y, Width = fieldW, Height = 32,
                Placeholder = I18n.T("points.operatorPlaceholder"),
            };
            ContentPanel.Controls.Add(_operatorBox);
            y += rowH + gap + 8;

            // 学生列表
            AddLabel(I18n.T("points.students"), x, y);
            y += rowH;

            _listHost = new Panel
            {
                Left = x, Top = y,
                Width = ClientSize.Width - x * 2 - 12,
                Height = ClientSize.Height - y - 80,
                BackColor = AppTheme.Colors.CardBg,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            };
            _listHost.Paint += ListHost_Paint;
            _listHost.MouseDown += ListHost_MouseDown;
            _listHost.MouseWheel += ListHost_MouseWheel;
            ContentPanel.Controls.Add(_listHost);

            _scrollBar = new FlatScrollBar
            {
                Left = ClientSize.Width - x - 8,
                Top = y,
                Width = 8,
                Height = _listHost.Height,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right,
            };
            _scrollBar.ValueChanged += (s, e) =>
            {
                _scrollY = _scrollBar.Value;
                _listHost.Invalidate();
            };
            ContentPanel.Controls.Add(_scrollBar);

            UpdateScrollBar();

            // 按钮
            var btnOk = new FlatButton
            {
                Text = I18n.T("dialog.save"),
                ButtonStyle = FlatButtonStyle.Primary,
                Width = 100, Height = 36,
                Left = ClientSize.Width - 240,
                Top = ClientSize.Height - 52,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            btnOk.Click += (s, e) => Save();
            ContentPanel.Controls.Add(btnOk);

            var btnCancel = new FlatButton
            {
                Text = I18n.T("dialog.cancel"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Width = 100, Height = 36,
                Left = ClientSize.Width - 130,
                Top = ClientSize.Height - 52,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            ContentPanel.Controls.Add(btnCancel);

            SyncCategoryDefault();
        }

        /// <summary>
        /// 添加字段标签。
        /// </summary>
        private void AddLabel(string text, int x, int y)
        {
            ContentPanel.Controls.Add(new Label
            {
                Text = text,
                Left = x, Top = y + 6, Width = 100,
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.Colors.TextPrimary,
                BackColor = Color.Transparent,
                AutoSize = false,
            });
        }

        /// <summary>
        /// 切换加分 / 减分状态。
        /// </summary>
        private void SetPositive(bool positive)
        {
            _positive = positive;
            _btnPositive.ButtonStyle = positive ? FlatButtonStyle.Primary : FlatButtonStyle.Secondary;
            _btnNegative.ButtonStyle = !positive ? FlatButtonStyle.Danger : FlatButtonStyle.Secondary;
        }

        /// <summary>
        /// 根据当前分类同步默认分值和正负状态。
        /// </summary>
        private void SyncCategoryDefault()
        {
            if (_categoryBox.SelectedIndex < 0) return;
            if (_categoryBox.SelectedIndex >= _categories.Count) return;

            var cat = _categories[_categoryBox.SelectedIndex];
            _deltaBox.Value = Math.Max(1, Math.Abs(cat.DefaultDelta));
            SetPositive(cat.IsPositive);
        }

        /// <summary>
        /// 绘制学生列表。
        /// </summary>
        private void ListHost_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var colors = AppTheme.Colors;

            using (var bg = new SolidBrush(colors.CardBg))
                g.FillRectangle(bg, _listHost.ClientRectangle);

            int y = -_scrollY;
            foreach (var s in _students)
            {
                var rowRect = new Rectangle(0, y, _listHost.Width, RowH);
                if (rowRect.Bottom > 0 && rowRect.Top < _listHost.Height)
                    DrawStudentRow(g, s, rowRect);
                y += RowH;
            }
        }

        /// <summary>
        /// 绘制单个学生行。
        /// </summary>
        private void DrawStudentRow(Graphics g, Student s, Rectangle rect)
        {
            var colors = AppTheme.Colors;
            bool isChecked = _checked.TryGetValue(s.Id, out var v) && v;

            if (isChecked)
            {
                using var bg = new SolidBrush(colors.SelectedBg);
                g.FillRectangle(bg, rect);
            }

            var boxRect = new Rectangle(12, rect.Y + (rect.Height - 16) / 2, 16, 16);
            using (var pen = new Pen(isChecked ? colors.Accent : colors.ButtonBorder, 1.5f))
                g.DrawRectangle(pen, boxRect);

            if (isChecked)
            {
                using var pen = new Pen(colors.Accent, 2f)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                };
                g.DrawLines(pen, new[]
                {
                    new Point(boxRect.X + 4, boxRect.Y + 8),
                    new Point(boxRect.X + 7, boxRect.Y + 11),
                    new Point(boxRect.X + 12, boxRect.Y + 5),
                });
            }

            TextRenderer.DrawText(g, s.Name ?? "", AppTheme.BodyFont,
                new Rectangle(40, rect.Y, rect.Width - 180, rect.Height),
                colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            TextRenderer.DrawText(g, s.TotalPoints.ToString(), AppTheme.BodyFont,
                new Rectangle(rect.Right - 120, rect.Y, 100, rect.Height),
                colors.TextSecondary,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            using var pen2 = new Pen(colors.Divider, 1f);
            g.DrawLine(pen2, 12, rect.Bottom - 1, rect.Right - 12, rect.Bottom - 1);
        }

        /// <summary>
        /// 鼠标点击切换学生勾选。
        /// </summary>
        private void ListHost_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            int idx = (e.Y + _scrollY) / RowH;
            if (idx < 0 || idx >= _students.Count) return;

            var s = _students[idx];
            _checked[s.Id] = !_checked[s.Id];
            _listHost.Invalidate();
        }

        /// <summary>
        /// 鼠标滚轮滚动学生列表。
        /// </summary>
        private void ListHost_MouseWheel(object? sender, MouseEventArgs e)
        {
            if (!_scrollBar.Visible) return;

            int maxScroll = Math.Max(0, _scrollBar.Maximum - _scrollBar.LargeChange);
            _scrollY -= Math.Sign(e.Delta) * 60;
            if (_scrollY < 0) _scrollY = 0;
            if (_scrollY > maxScroll) _scrollY = maxScroll;

            _scrollBar.Value = _scrollY;
            _scrollBar.Wake();
            _listHost.Invalidate();
        }

        /// <summary>
        /// 更新滚动条状态。
        /// </summary>
        private void UpdateScrollBar()
        {
            int contentH = _students.Count * RowH;
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

        /// <summary>
        /// 保存加分 / 减分记录。
        /// </summary>
        private void Save()
        {
            var selected = _students.Where(s => _checked.TryGetValue(s.Id, out var v) && v).ToList();
            if (selected.Count == 0)
            {
                MessageDialog.ShowInfo(I18n.T("common.info"), I18n.T("points.needStudent"));
                return;
            }

            string category = _categoryBox.SelectedText ?? "";
            int delta = _deltaBox.Value;
            if (!_positive) delta = -delta;

            string reason = _reasonBox.Text.Trim();
            string op = _operatorBox.Text.Trim();
            string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            Result.Clear();
            foreach (var s in selected)
            {
                Result.Add(new ScoreRecord
                {
                    Id = Guid.NewGuid().ToString("N"),
                    StudentId = s.Id ?? "",
                    StudentName = s.Name ?? "",
                    Delta = delta,
                    Reason = reason,
                    Category = category,
                    Date = now,
                    Operator = op,
                });
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}