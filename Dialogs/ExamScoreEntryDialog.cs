using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CourseApp.Controls;
using CourseApp.Localization;
using CourseApp.Models;
using CourseApp.Theme;

namespace CourseApp.Dialogs
{
    /// <summary>
    /// 考试成绩录入弹窗。
    /// 为一次考试批量录入学生分数，支持上下滚动。
    /// </summary>
    public class ExamScoreEntryDialog : FlatDialogBase
    {
        /// <summary>要录入的考试</summary>
        private readonly ExamRecord _exam;

        /// <summary>学生列表</summary>
        private readonly List<Student> _students;

        /// <summary>每个学生的分数输入框：学号 → 输入框</summary>
        private readonly Dictionary<string, FlatTextBox> _scoreBoxes = new();

        /// <summary>滚动宿主</summary>
        private Panel _listHost = null!;

        /// <summary>滚动条</summary>
        private FlatScrollBar _scrollBar = null!;

        /// <summary>滚动偏移</summary>
        private int _scrollY = 0;

        /// <summary>行高</summary>
        private const int RowH = 40;

        /// <summary>
        /// 构造考试成绩录入弹窗。
        /// </summary>
        /// <param name="exam">要录入的考试</param>
        /// <param name="students">学生列表</param>
        public ExamScoreEntryDialog(ExamRecord exam, List<Student> students)
            : base(I18n.T("score.editScores"), 520, 620)
        {
            _exam = exam ?? throw new ArgumentNullException(nameof(exam));
            _students = students ?? new List<Student>();

            BuildUI();
        }

        /// <summary>
        /// 构建界面。
        /// </summary>
        private void BuildUI()
        {
            // 顶部信息
            var info = new Label
            {
                Text = $"{_exam.Name}  ·  {_exam.Subject}  ·  满分 {_exam.FullScore}",
                Font = new Font(AppTheme.BodyFont.FontFamily, 12f, FontStyle.Bold),
                ForeColor = AppTheme.Colors.TextPrimary,
                Left = 20, Top = 10, Width = 460, Height = 24,
                BackColor = Color.Transparent,
                AutoSize = false,
            };
            ContentPanel.Controls.Add(info);

            // 列表宿主
            _listHost = new Panel
            {
                Left = 20, Top = 44,
                Width = ContentPanel.Width - 40 - 12,
                Height = ContentPanel.Height - 44 - 80,
                BackColor = AppTheme.Colors.CardBg,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            };
            _listHost.Paint += ListHost_Paint;
            _listHost.MouseWheel += ListHost_MouseWheel;
            ContentPanel.Controls.Add(_listHost);

            _scrollBar = new FlatScrollBar
            {
                Left = ContentPanel.Width - 20 - 8,
                Top = 44,
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

            // 创建分数输入框
            CreateScoreBoxes();
            UpdateScrollBar();

            // 保存 / 取消
            var btnOk = new FlatButton
            {
                Text = I18n.T("dialog.save"),
                ButtonStyle = FlatButtonStyle.Primary,
                Width = 100, Height = 36,
                Left = ContentPanel.Width - 240,
                Top = ContentPanel.Height - 52,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            btnOk.Click += (s, e) => { Save(); Close(); };
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

        /// <summary>
        /// 为每个学生创建分数输入框。
        /// </summary>
        private void CreateScoreBoxes()
        {
            foreach (var s in _students)
            {
                if (_scoreBoxes.ContainsKey(s.Id)) continue;

                var box = new FlatTextBox
                {
                    Width = 100,
                    Height = 32,
                    Text = _exam.Scores != null && _exam.Scores.TryGetValue(s.Id, out var v)
                        ? v.ToString("F1")
                        : "",
                };
                _scoreBoxes[s.Id] = box;
                _listHost.Controls.Add(box);
            }

            LayoutBoxes();
        }

        /// <summary>
        /// 布局分数输入框位置。
        /// </summary>
        private void LayoutBoxes()
        {
            int y = 44 - _scrollY;
            foreach (var s in _students)
            {
                if (!_scoreBoxes.TryGetValue(s.Id, out var box)) continue;

                box.Top = y + 4;
                box.Left = _listHost.Width - 120;

                y += RowH;
            }
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

            // 表头
            var headerRect = new Rectangle(0, 0, _listHost.Width, 44);
            using (var brush = new SolidBrush(colors.GridHeaderBg))
                g.FillRectangle(brush, headerRect);

            using (var pen = new Pen(colors.Divider, 1f))
                g.DrawLine(pen, 0, 44, _listHost.Width, 44);

            TextRenderer.DrawText(g, I18n.T("student.name"), AppTheme.BodyFont,
                new Rectangle(16, 0, _listHost.Width - 140, 44), colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(g, I18n.T("score.score"), AppTheme.BodyFont,
                new Rectangle(_listHost.Width - 120, 0, 100, 44), colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            int y = 44 - _scrollY;
            foreach (var s in _students)
            {
                var rect = new Rectangle(0, y, _listHost.Width, RowH);
                if (rect.Bottom > 44 && rect.Top < _listHost.Height)
                {
                    TextRenderer.DrawText(g, $"{s.Name} ({s.Id})", AppTheme.BodyFont,
                        new Rectangle(16, y, _listHost.Width - 140, RowH), colors.TextPrimary,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

                    using var pen = new Pen(colors.Divider, 1f);
                    g.DrawLine(pen, 16, rect.Bottom - 1, _listHost.Width - 16, rect.Bottom - 1);
                }
                y += RowH;
            }
        }

        /// <summary>
        /// 更新滚动条。
        /// </summary>
        private void UpdateScrollBar()
        {
            int contentH = _students.Count * RowH + 44;
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
        /// 鼠标滚轮滚动列表。
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
            LayoutBoxes();
            _listHost.Invalidate();
        }

        /// <summary>
        /// 保存分数。
        /// </summary>
        private void Save()
        {
            if (_exam.Scores == null)
                _exam.Scores = new Dictionary<string, double>();

            _exam.Scores.Clear();
            foreach (var kv in _scoreBoxes)
            {
                if (double.TryParse(kv.Value.Text.Trim(), out var v))
                    _exam.Scores[kv.Key] = v;
            }
        }
    }
}