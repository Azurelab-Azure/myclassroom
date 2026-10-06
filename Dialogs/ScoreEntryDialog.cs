using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CourseApp.Controls;
using CourseApp.Localization;
using CourseApp.Models;
using CourseApp.Theme;

namespace CourseApp.Dialogs
{
    /// <summary>
    /// 批量录入成绩。
    /// </summary>
    public class ScoreEntryDialog : FlatDialogBase
    {
        private readonly ExamRecord _exam;
        private readonly List<Student> _students;
        private readonly Dictionary<string, FlatTextBox> _scoreBoxes = new();

        private Panel _listHost = null!;
        private FlatScrollBar _scrollBar = null!;
        private int _scrollY = 0;
        private const int RowH = 40;

        public ScoreEntryDialog(ExamRecord exam, List<Student> students)
            : base(I18n.T("score.editScores"), 500, 600)
        {
            _exam = exam ?? throw new ArgumentNullException(nameof(exam));
            _students = students ?? new List<Student>();

            BuildUI();
        }

        private void BuildUI()
        {
            _listHost = new Panel
            {
                Left = 20, Top = 10,
                Width = ClientSize.Width - 40 - 12,
                Height = ClientSize.Height - 10 - 80,
                BackColor = AppTheme.Colors.CardBg,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            };
            _listHost.Paint += ListHost_Paint;
            _listHost.MouseWheel += ListHost_MouseWheel;
            ContentPanel.Controls.Add(_listHost);

            _scrollBar = new FlatScrollBar
            {
                Left = ClientSize.Width - 20 - 8,
                Top = 10,
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

            // 保存
            var btnOk = new FlatButton
            {
                Text = I18n.T("dialog.save"),
                ButtonStyle = FlatButtonStyle.Primary,
                Width = 100, Height = 36,
                Left = ClientSize.Width - 240,
                Top = _listHost.Bottom + 12,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            btnOk.Click += (s, e) => { Save(); Close(); };
            ContentPanel.Controls.Add(btnOk);

            // 取消
            var btnCancel = new FlatButton
            {
                Text = I18n.T("dialog.cancel"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Width = 100, Height = 36,
                Left = ClientSize.Width - 130,
                Top = _listHost.Bottom + 12,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            ContentPanel.Controls.Add(btnCancel);

            // 创建分数输入框
            CreateScoreBoxes();
            UpdateScrollBar();
        }

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

        private void LayoutBoxes()
        {
            int y = 34 - _scrollY;
            foreach (var s in _students)
            {
                if (!_scoreBoxes.TryGetValue(s.Id, out var box)) continue;

                box.Top = y + 4;
                box.Left = _listHost.Width - 120;

                y += RowH;
            }
        }

        private void ListHost_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var colors = AppTheme.Colors;

            using (var bg = new SolidBrush(colors.CardBg))
                g.FillRectangle(bg, _listHost.ClientRectangle);

            // 表头
            var headerRect = new Rectangle(0, 0, _listHost.Width, 34);
            using (var brush = new SolidBrush(colors.GridHeaderBg))
                g.FillRectangle(brush, headerRect);

            using (var pen = new Pen(colors.Divider, 1f))
                g.DrawLine(pen, 0, 34, _listHost.Width, 34);

            TextRenderer.DrawText(g, I18n.T("student.name"), AppTheme.BodyFont,
                new Rectangle(16, 0, _listHost.Width - 140, 34), colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(g, I18n.T("score.score"), AppTheme.BodyFont,
                new Rectangle(_listHost.Width - 120, 0, 100, 34), colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            // 学生行
            int y = 34 - _scrollY;
            foreach (var s in _students)
            {
                var rect = new Rectangle(0, y, _listHost.Width, RowH);
                if (rect.Bottom > 34 && rect.Top < _listHost.Height)
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

        private void UpdateScrollBar()
        {
            int contentH = _students.Count * RowH + 40;
            bool need = contentH > _listHost.Height;

            _scrollBar.Visible = need;
            if (need)
            {
                _scrollBar.Maximum = contentH;
                _scrollBar.LargeChange = _listHost.Height;
                _scrollBar.Value = 0;
                _scrollY = 0;
            }
            else
            {
                _scrollY = 0;
            }
        }

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