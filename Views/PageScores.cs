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
    /// 成绩看板。
    /// </summary>
    public class PageScores : Panel
    {
        private readonly Form1 _owner;

        private FlatComboBox _examBox = null!;
        private FlatButton _btnAddExam = null!;
        private FlatButton _btnEditScores = null!;

        private Panel _statsPanel = null!;
        private Panel _chartPanel = null!;
        private Panel _listPanel = null!;
        private FlatScrollBar _listScrollBar = null!;

        private List<ExamRecord> _exams = new();
        private ExamRecord? _currentExam;
        private int _scrollY = 0;

        private const int RowH = 36;

        public PageScores(Form1 owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Colors.WindowBg;

            BuildUI();
        }

        private void BuildUI()
        {
            int top = 16;
            int left = 16;

            // 考试选择
            var lblExam = new Label
            {
                Text = I18n.T("score.exam"),
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.Colors.TextPrimary,
                Left = left, Top = top + 6, Width = 60,
                BackColor = Color.Transparent,
                AutoSize = false,
            };
            Controls.Add(lblExam);

            _examBox = new FlatComboBox
            {
                Left = left + 70, Top = top,
                Width = 260, Height = 32,
            };
            _examBox.SelectedIndexChanged += (s, e) => OnExamChanged();
            Controls.Add(_examBox);

            _btnAddExam = new FlatButton
            {
                Text = I18n.T("score.addExam"),
                ButtonStyle = FlatButtonStyle.Primary,
                Left = left + 340, Top = top,
                Width = 100, Height = 32,
            };
            _btnAddExam.Click += (s, e) => AddExam();
            Controls.Add(_btnAddExam);

            _btnEditScores = new FlatButton
            {
                Text = I18n.T("score.editScores"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Left = left + 448, Top = top,
                Width = 100, Height = 32,
            };
            _btnEditScores.Click += (s, e) => EditScores();
            Controls.Add(_btnEditScores);

            top += 48;

            // 统计卡片
            _statsPanel = new Panel
            {
                Left = left, Top = top,
                Width = Width - left * 2,
                Height = 80,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            _statsPanel.Paint += StatsPanel_Paint;
            Controls.Add(_statsPanel);

            top += 92;

            // 图表
            _chartPanel = new Panel
            {
                Left = left, Top = top,
                Width = Width - left * 2,
                Height = 200,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            _chartPanel.Paint += ChartPanel_Paint;
            Controls.Add(_chartPanel);

            top += 212;

            // 列表
            _listPanel = new Panel
            {
                Left = left, Top = top,
                Width = Width - left * 2 - 12,
                Height = Height - top - 16,
                BackColor = AppTheme.Colors.CardBg,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            };
            _listPanel.Paint += ListPanel_Paint;
            _listPanel.MouseWheel += ListPanel_MouseWheel;
            Controls.Add(_listPanel);

            _listScrollBar = new FlatScrollBar
            {
                Left = Width - left - 8, Top = top,
                Width = 8,
                Height = _listPanel.Height,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right,
            };
            _listScrollBar.ValueChanged += (s, e) =>
            {
                _scrollY = _listScrollBar.Value;
                _listPanel.Invalidate();
            };
            Controls.Add(_listScrollBar);
        }

        // =====================================================
        // 数据
        // =====================================================
        public void SetData(List<ExamRecord> exams)
        {
            _exams = exams ?? new List<ExamRecord>();

            // 刷新下拉
            var items = new List<IFlatComboItem>();
            foreach (var e in _exams)
                items.Add(new FlatTextItem($"{e.Name} · {e.Subject} ({e.Date})"));

            _examBox.SetItems(items);

            if (_exams.Count > 0)
                _examBox.SelectedIndex = 0;

            Invalidate();
            _statsPanel.Invalidate();
            _chartPanel.Invalidate();
            _listPanel.Invalidate();
        }

        private void OnExamChanged()
        {
            int idx = _examBox.SelectedIndex;
            _currentExam = (idx >= 0 && idx < _exams.Count) ? _exams[idx] : null;

            UpdateScrollBar();
            _statsPanel.Invalidate();
            _chartPanel.Invalidate();
            _listPanel.Invalidate();
        }

        private void UpdateScrollBar()
        {
            if (_currentExam == null)
            {
                _listScrollBar.Visible = false;
                return;
            }

            int count = _currentExam.Scores?.Count ?? 0;
            int contentH = count * RowH + 40;

            if (contentH > _listPanel.Height)
            {
                _listScrollBar.Visible = true;
                _listScrollBar.Maximum = contentH;
                _listScrollBar.LargeChange = _listPanel.Height;
                _listScrollBar.Value = 0;
                _scrollY = 0;
            }
            else
            {
                _listScrollBar.Visible = false;
                _scrollY = 0;
            }
        }

        // =====================================================
        // 统计卡片
        // =====================================================
        private void StatsPanel_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var colors = AppTheme.Colors;

            if (_currentExam == null)
            {
                TextRenderer.DrawText(g, I18n.T("score.noExam"), AppTheme.BodyFont,
                    _statsPanel.ClientRectangle, colors.TextSecondary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix);
                return;
            }

            var stats = ComputeStats(_currentExam);

            int cardW = (_statsPanel.Width - 36) / 4;

            DrawStatCard(g, 0, cardW, I18n.T("score.average"), stats.Average.ToString("F1"), colors.Accent);
            DrawStatCard(g, 1, cardW, I18n.T("score.max"), stats.Max.ToString("F1"), Color.FromArgb(0x4C, 0xAF, 0x50));
            DrawStatCard(g, 2, cardW, I18n.T("score.min"), stats.Min.ToString("F1"), Color.FromArgb(0xE8, 0x1B, 0x1B));
            DrawStatCard(g, 3, cardW, I18n.T("score.passRate"), (stats.PassRate * 100).ToString("F0") + "%",
                Color.FromArgb(0xFF, 0xB9, 0x00));
        }

        private void DrawStatCard(Graphics g, int index, int cardW, string label, string value, Color accent)
        {
            int x = index * (cardW + 12);
            var rect = new Rectangle(x, 0, cardW, 80);

            using (var path = GraphicsExtensions.GetRoundPath(rect, 8))
            using (var brush = new SolidBrush(AppTheme.Colors.CardBg))
                g.FillPath(brush, path);

            using (var path = GraphicsExtensions.GetRoundPath(rect, 8))
            using (var pen = new Pen(AppTheme.Colors.CardBorder, 1f))
                g.DrawPath(pen, path);

            // 左侧色条
            var bar = new Rectangle(rect.X + 12, rect.Y + 20, 3, 40);
            using (var brush = new SolidBrush(accent))
            using (var path = GraphicsExtensions.GetRoundPath(bar, 2))
                g.FillPath(brush, path);

            // 标签
            var labelRect = new Rectangle(rect.X + 24, rect.Y + 16, rect.Width - 32, 20);
            TextRenderer.DrawText(g, label, AppTheme.SmallFont, labelRect, AppTheme.Colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            // 值
            using var valueFont = new Font(AppTheme.BodyFont.FontFamily, 18f, FontStyle.Bold);
            var valueRect = new Rectangle(rect.X + 24, rect.Y + 38, rect.Width - 32, 32);
            TextRenderer.DrawText(g, value, valueFont, valueRect, accent,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        // =====================================================
        // 图表
        // =====================================================
        private void ChartPanel_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var colors = AppTheme.Colors;

            // 背景
            using (var path = GraphicsExtensions.GetRoundPath(
                new Rectangle(0, 0, _chartPanel.Width - 1, _chartPanel.Height - 1), 8))
            using (var brush = new SolidBrush(colors.CardBg))
                g.FillPath(brush, path);

            using (var path = GraphicsExtensions.GetRoundPath(
                new Rectangle(0, 0, _chartPanel.Width - 1, _chartPanel.Height - 1), 8))
            using (var pen = new Pen(colors.CardBorder, 1f))
                g.DrawPath(pen, path);

            if (_currentExam == null || _currentExam.Scores == null || _currentExam.Scores.Count == 0)
            {
                TextRenderer.DrawText(g, I18n.T("score.noData"), AppTheme.BodyFont,
                    _chartPanel.ClientRectangle, colors.TextSecondary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix);
                return;
            }

            // 柱状图
            var scores = _currentExam.Scores.Values.OrderByDescending(v => v).ToList();
            double maxScore = _currentExam.FullScore > 0 ? _currentExam.FullScore : 100;

            int padLeft = 40;
            int padRight = 20;
            int padTop = 20;
            int padBottom = 30;

            int chartW = _chartPanel.Width - padLeft - padRight;
            int chartH = _chartPanel.Height - padTop - padBottom;

            // 基线
            using (var pen = new Pen(colors.Divider, 1f))
                g.DrawLine(pen, padLeft, padTop + chartH, padLeft + chartW, padTop + chartH);

            // 柱
            int barW = Math.Max(4, chartW / Math.Max(1, scores.Count) - 2);
            for (int i = 0; i < scores.Count; i++)
            {
                double v = scores[i];
                double ratio = v / maxScore;
                int barH = (int)(chartH * ratio);

                int x = padLeft + i * (barW + 2);
                int y = padTop + chartH - barH;

                var barRect = new Rectangle(x, y, barW, barH);

                Color barColor = v >= _currentExam.PassScore
                    ? Color.FromArgb(0x4C, 0xAF, 0x50)   // 及格——绿
                    : Color.FromArgb(0xE8, 0x1B, 0x1B);   // 不及格——红

                using var brush = new SolidBrush(barColor);
                using var path = GraphicsExtensions.GetRoundPath(barRect, 2);
                g.FillPath(brush, path);
            }

            // Y 轴标签
            using var font = AppTheme.SmallFont;
            TextRenderer.DrawText(g, maxScore.ToString("F0"), font,
                new Rectangle(0, padTop - 8, padLeft - 4, 20), colors.TextSecondary,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            TextRenderer.DrawText(g, "0", font,
                new Rectangle(0, padTop + chartH - 10, padLeft - 4, 20), colors.TextSecondary,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        // =====================================================
        // 列表
        // =====================================================
        private void ListPanel_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var colors = AppTheme.Colors;

            using (var bg = new SolidBrush(colors.CardBg))
                g.FillRectangle(bg, _listPanel.ClientRectangle);

            if (_currentExam == null || _currentExam.Scores == null || _currentExam.Scores.Count == 0)
            {
                TextRenderer.DrawText(g, I18n.T("score.noData"), AppTheme.BodyFont,
                    _listPanel.ClientRectangle, colors.TextSecondary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix);
                return;
            }

            // 表头
            var headerRect = new Rectangle(0, 0, _listPanel.Width, 34);
            using (var brush = new SolidBrush(colors.GridHeaderBg))
                g.FillRectangle(brush, headerRect);

            using (var pen = new Pen(colors.Divider, 1f))
                g.DrawLine(pen, 0, headerRect.Bottom - 1, _listPanel.Width, headerRect.Bottom - 1);

            int nameW = _listPanel.Width - 100 - 80;
            int scoreX = nameW + 20;
            int rankX = scoreX + 100;

            TextRenderer.DrawText(g, I18n.T("student.name"), AppTheme.BodyFont,
                new Rectangle(16, 0, nameW, 34), colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(g, I18n.T("score.score"), AppTheme.BodyFont,
                new Rectangle(scoreX, 0, 100, 34), colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(g, I18n.T("score.rank"), AppTheme.BodyFont,
                new Rectangle(rankX, 0, 80, 34), colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            // 学生列表（按分数降序）
            var list = _currentExam.Scores
                .Select(kv => new { Id = kv.Key, Score = kv.Value })
                .OrderByDescending(x => x.Score)
                .ToList();

            int y = 34 - _scrollY;
            for (int i = 0; i < list.Count; i++)
            {
                var row = new Rectangle(0, y, _listPanel.Width, RowH);
                if (row.Bottom > 34 && row.Top < _listPanel.Height)
                {
                    DrawScoreRow(g, i + 1, list[i].Id, list[i].Score, row, nameW, scoreX, rankX);
                }
                y += RowH;
            }
        }

        private void DrawScoreRow(Graphics g, int rank, string id, double score,
            Rectangle row, int nameW, int scoreX, int rankX)
        {
            var colors = AppTheme.Colors;

            // 找学生名
            var student = _owner.Students.FirstOrDefault(s => s.Id == id);
            string name = student?.Name ?? id;

            TextRenderer.DrawText(g, name, AppTheme.BodyFont,
                new Rectangle(16, row.Y, nameW, row.Height), colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            // 分数（不及格红色）
            Color scoreColor = score >= (_currentExam?.PassScore ?? 60)
                ? colors.TextPrimary
                : Color.FromArgb(0xE8, 0x1B, 0x1B);

            TextRenderer.DrawText(g, score.ToString("F1"), AppTheme.BodyFont,
                new Rectangle(scoreX, row.Y, 100, row.Height), scoreColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            // 排名
            TextRenderer.DrawText(g, rank.ToString(), AppTheme.BodyFont,
                new Rectangle(rankX, row.Y, 80, row.Height), colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            // 分隔线
            using var pen = new Pen(colors.Divider, 1f);
            g.DrawLine(pen, 16, row.Bottom - 1, _listPanel.Width - 16, row.Bottom - 1);
        }

        private void ListPanel_MouseWheel(object? sender, MouseEventArgs e)
        {
            if (!_listScrollBar.Visible) return;

            _scrollY -= Math.Sign(e.Delta) * 60;
            int maxScroll = Math.Max(0, _listScrollBar.Maximum - _listScrollBar.LargeChange);
            if (_scrollY < 0) _scrollY = 0;
            if (_scrollY > maxScroll) _scrollY = maxScroll;

            _listScrollBar.Value = _scrollY;
            _listScrollBar.Wake();
            _listPanel.Invalidate();
        }

        // =====================================================
        // 统计计算
        // =====================================================
        private class ExamStats
        {
            public double Average;
            public double Max;
            public double Min;
            public double PassRate;
        }

        private ExamStats ComputeStats(ExamRecord exam)
        {
            var stats = new ExamStats();
            if (exam.Scores == null || exam.Scores.Count == 0) return stats;

            var values = exam.Scores.Values.ToList();
            stats.Average = values.Average();
            stats.Max = values.Max();
            stats.Min = values.Min();
            int passCount = values.Count(v => v >= exam.PassScore);
            stats.PassRate = (double)passCount / values.Count;
            return stats;
        }

        // =====================================================
        // 动作
        // =====================================================
        private void AddExam()
        {
            using var dlg = new ExamEditorDialog(null, _owner.Students);
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Result != null)
            {
                _exams.Add(dlg.Result);
                _owner.Exams = _exams;
                _owner.SaveAll();
                SetData(_exams);
            }
        }

        private void EditScores()
        {
            if (_currentExam == null)
            {
                MessageDialog.ShowInfo(I18n.T("common.info"), I18n.T("score.noExam"));
                return;
            }

            using var dlg = new ScoreEntryDialog(_currentExam, _owner.Students);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _owner.SaveAll();
                SetData(_exams);
            }
        }
    }
}