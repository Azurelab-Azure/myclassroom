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
    /// 考试成绩分析页。
    /// 包含：考试选择、班级概览、分数段分布、学生成绩列表、学生个人趋势、多次考试对比。
    /// </summary>
    public class PageExam : Panel
    {
        /// <summary>主窗口引用</summary>
        private readonly Form1 _owner;

        // ---------- 顶部 ----------
        /// <summary>考试下拉框</summary>
        private FlatComboBox _examBox = null!;

        /// <summary>新建考试按钮</summary>
        private FlatButton _btnAddExam = null!;

        /// <summary>录入成绩按钮</summary>
        private FlatButton _btnEditScores = null!;

        /// <summary>导出按钮</summary>
        private FlatButton _btnExport = null!;

        // ---------- 数据 ----------
        /// <summary>考试列表</summary>
        private List<ExamRecord> _exams = new();

        /// <summary>学生列表</summary>
        private List<Student> _students = new();

        /// <summary>当前选中的考试</summary>
        private ExamRecord? _currentExam;

        // ---------- 视图切换 ----------
        /// <summary>分段控件：班级概览 / 学生趋势 / 考试对比</summary>
        private WinUI3Segmented _segmented = null!;

        /// <summary>视图模式：0 班级概览 / 1 学生趋势 / 2 考试对比</summary>
        private int _viewMode = 0;

        /// <summary>内容宿主</summary>
        private Panel _contentHost = null!;

        /// <summary>学生趋势模式下选中的学生索引</summary>
        private int _selectedStudentIndex = 0;

        /// <summary>对比模式下第二个考试索引</summary>
        private int _compareIndex = 0;

        /// <summary>
        /// 构造考试分析页。
        /// </summary>
        /// <param name="owner">主窗口</param>
        public PageExam(Form1 owner)
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

            // 考试下拉
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
                Width = 280, Height = 32,
            };
            _examBox.SelectedIndexChanged += (s, e) => OnExamChanged();
            Controls.Add(_examBox);

            _btnAddExam = new FlatButton
            {
                Text = I18n.T("score.addExam"),
                ButtonStyle = FlatButtonStyle.Primary,
                Left = left + 360, Top = top,
                Width = 100, Height = 32,
            };
            _btnAddExam.Click += (s, e) => AddExam();
            Controls.Add(_btnAddExam);

            _btnEditScores = new FlatButton
            {
                Text = I18n.T("score.editScores"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Left = left + 468, Top = top,
                Width = 100, Height = 32,
            };
            _btnEditScores.Click += (s, e) => EditScores();
            Controls.Add(_btnEditScores);

            _btnExport = new FlatButton
            {
                Text = I18n.T("points.export"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Left = left + 576, Top = top,
                Width = 100, Height = 32,
            };
            _btnExport.Click += (s, e) => ExportCsv();
            Controls.Add(_btnExport);

            top += 48;

            // 分段控件
            _segmented = new WinUI3Segmented
            {
                Left = left, Top = top,
                Width = 460, Height = 36,
            };
            _segmented.SetItems(new[]
            {
                I18n.T("exam.tabClass"),
                I18n.T("exam.tabStudent"),
                I18n.T("exam.tabCompare"),
            });
            _segmented.SelectedIndex = 0;
            _segmented.SelectedIndexChanged += (s, e) =>
            {
                _viewMode = _segmented.SelectedIndex;
                _contentHost.Invalidate();
            };
            Controls.Add(_segmented);

            top += 48;

            // 内容宿主
            _contentHost = new Panel
            {
                Left = left, Top = top,
                Width = Width - left * 2,
                Height = Height - top - 16,
                BackColor = AppTheme.Colors.WindowBg,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            };
            _contentHost.Paint += ContentHost_Paint;
            Controls.Add(_contentHost);

            Resize += (s, e) =>
            {
                _contentHost.Width = Width - left * 2;
                _contentHost.Height = Height - _contentHost.Top - 16;
            };
        }

        // =====================================================
        // 数据
        // =====================================================
        /// <summary>
        /// 设置数据。
        /// </summary>
        /// <param name="exams">考试列表</param>
        /// <param name="students">学生列表</param>
        public void SetData(List<ExamRecord> exams, List<Student> students)
        {
            _exams = exams ?? new List<ExamRecord>();
            _students = students ?? new List<Student>();

            var items = new List<IFlatComboItem>();
            foreach (var e in _exams)
                items.Add(new FlatTextItem($"{e.Name} · {e.Subject} ({e.Date})"));

            _examBox.SetItems(items);

            if (_exams.Count > 0 && _examBox.SelectedIndex < 0)
                _examBox.SelectedIndex = 0;

            OnExamChanged();
        }

        /// <summary>
        /// 考试切换。
        /// </summary>
        private void OnExamChanged()
        {
            int idx = _examBox.SelectedIndex;
            _currentExam = (idx >= 0 && idx < _exams.Count) ? _exams[idx] : null;

            if (_currentExam != null)
            {
                _selectedStudentIndex = 0;
                _compareIndex = Math.Min(1, Math.Max(0, _exams.Count - 1));
            }

            _contentHost.Invalidate();
        }

        // =====================================================
        // 内容绘制
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

            if (_currentExam == null)
            {
                using var font = new Font(AppTheme.BodyFont.FontFamily, 12f);
                TextRenderer.DrawText(g, I18n.T("score.noExam"), font,
                    _contentHost.ClientRectangle, colors.TextSecondary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                return;
            }

            switch (_viewMode)
            {
                case 0: DrawClassOverview(g); break;
                case 1: DrawStudentTrend(g); break;
                case 2: DrawCompare(g); break;
            }
        }

        // ---------- 班级概览 ----------
        /// <summary>
        /// 绘制班级概览。
        /// </summary>
        private void DrawClassOverview(Graphics g)
        {
            var colors = AppTheme.Colors;
            var exam = _currentExam!;

            var scores = exam.Scores != null ? exam.Scores.Values.ToList() : new List<double>();
            if (scores.Count == 0)
            {
                using var font = new Font(AppTheme.BodyFont.FontFamily, 12f);
                TextRenderer.DrawText(g, I18n.T("score.noData"), font,
                    _contentHost.ClientRectangle, colors.TextDisabled,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                return;
            }

            // 概览卡片
            int gap = 12;
            int cardW = (_contentHost.Width - gap * 5) / 6;
            int cardH = 84;

            double avg = scores.Average();
            double max = scores.Max();
            double min = scores.Min();
            double pass = (double)scores.Count(v => v >= exam.PassScore) / scores.Count;
            double excellent = (double)scores.Count(v => v >= exam.FullScore * 0.85) / scores.Count;
            double stdDev = Math.Sqrt(scores.Average(v => (v - avg) * (v - avg)));

            DrawStatCard(g, 0, 0, cardW, cardH, avg.ToString("F1"), I18n.T("exam.average"), colors.Accent);
            DrawStatCard(g, cardW + gap, 0, cardW, cardH, max.ToString("F1"), I18n.T("exam.max"), colors.TextPrimary);
            DrawStatCard(g, (cardW + gap) * 2, 0, cardW, cardH, min.ToString("F1"), I18n.T("exam.min"), colors.TextPrimary);
            DrawStatCard(g, (cardW + gap) * 3, 0, cardW, cardH, (pass * 100).ToString("F0") + "%", I18n.T("exam.passRate"), colors.TextPrimary);
            DrawStatCard(g, (cardW + gap) * 4, 0, cardW, cardH, (excellent * 100).ToString("F0") + "%", I18n.T("exam.excellentRate"), colors.TextPrimary);
            DrawStatCard(g, (cardW + gap) * 5, 0, cardW, cardH, stdDev.ToString("F1"), I18n.T("exam.stdDev"), colors.TextPrimary);

            int y = cardH + 20;

            // 分数段分布
            using (var titleFont = new Font(AppTheme.BodyFont.FontFamily, 14f, FontStyle.Bold))
                TextRenderer.DrawText(g, I18n.T("exam.distribution"), titleFont,
                    new Rectangle(0, y, 400, 24), colors.TextPrimary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            y += 34;

            var segments = new (string Label, double From, double To)[]
            {
                ("<60%", 0, exam.FullScore * 0.6),
                ("60-70%", exam.FullScore * 0.6, exam.FullScore * 0.7),
                ("70-80%", exam.FullScore * 0.7, exam.FullScore * 0.8),
                ("80-90%", exam.FullScore * 0.8, exam.FullScore * 0.9),
                (">90%", exam.FullScore * 0.9, exam.FullScore + 1),
            };

            var counts = new int[segments.Length];
            foreach (var v in scores)
            {
                for (int i = 0; i < segments.Length; i++)
                {
                    if (v >= segments[i].From && v < segments[i].To) { counts[i]++; break; }
                }
            }

            int barMaxW = Math.Min(400, _contentHost.Width - 200);
            int maxCount = Math.Max(1, counts.Max());

            for (int i = 0; i < segments.Length; i++)
            {
                var row = new Rectangle(0, y, _contentHost.Width, 30);

                TextRenderer.DrawText(g, segments[i].Label, AppTheme.BodyFont,
                    new Rectangle(row.X, row.Y, 80, row.Height), colors.TextSecondary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

                int barW = (int)((double)counts[i] / maxCount * barMaxW);
                if (barW < 2 && counts[i] > 0) barW = 2;

                var barRect = new Rectangle(row.X + 90, row.Y + 8, Math.Max(2, barW), 14);
                using (var brush = new SolidBrush(colors.Accent))
                using (var path = GraphicsExtensions.GetRoundPath(barRect, 7))
                    g.FillPath(brush, path);

                TextRenderer.DrawText(g, counts[i].ToString(), AppTheme.SmallFont,
                    new Rectangle(row.X + 100 + barMaxW, row.Y, 60, row.Height), colors.TextPrimary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

                y += 32;
            }

            y += 20;

            // 学生成绩列表
            using (var titleFont = new Font(AppTheme.BodyFont.FontFamily, 14f, FontStyle.Bold))
                TextRenderer.DrawText(g, I18n.T("exam.studentList"), titleFont,
                    new Rectangle(0, y, 400, 24), colors.TextPrimary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            y += 34;

            var sorted = exam.Scores!
                .OrderByDescending(kv => kv.Value)
                .ToList();

            for (int i = 0; i < sorted.Count && y < _contentHost.Height - 30; i++)
            {
                var student = _students.FirstOrDefault(s => s.Id == sorted[i].Key);
                string name = student?.Name ?? sorted[i].Key;

                TextRenderer.DrawText(g, (i + 1).ToString(), AppTheme.SmallFont,
                    new Rectangle(0, y, 30, 24), colors.TextSecondary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

                TextRenderer.DrawText(g, name, AppTheme.BodyFont,
                    new Rectangle(40, y, 200, 24), colors.TextPrimary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

                Color scoreColor = sorted[i].Value >= exam.PassScore
                    ? colors.TextPrimary
                    : Color.FromArgb(0xE8, 0x1B, 0x1B);

                TextRenderer.DrawText(g, sorted[i].Value.ToString("F1"), AppTheme.BodyFont,
                    new Rectangle(260, y, 100, 24), scoreColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

                y += 26;
            }
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

            using var valueFont = new Font(AppTheme.BodyFont.FontFamily, 20f, FontStyle.Bold);
            TextRenderer.DrawText(g, value, valueFont,
                new Rectangle(rect.X + 12, rect.Y + 12, rect.Width - 24, 32), accent,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            using var labelFont = new Font(AppTheme.BodyFont.FontFamily, 10f);
            TextRenderer.DrawText(g, label, labelFont,
                new Rectangle(rect.X + 12, rect.Y + 48, rect.Width - 24, 20), colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        // ---------- 学生趋势 ----------
        /// <summary>
        /// 绘制单个学生的历次考试趋势。
        /// </summary>
        private void DrawStudentTrend(Graphics g)
        {
            var colors = AppTheme.Colors;

            if (_students.Count == 0)
            {
                using var font = new Font(AppTheme.BodyFont.FontFamily, 12f);
                TextRenderer.DrawText(g, I18n.T("student.empty"), font,
                    _contentHost.ClientRectangle, colors.TextSecondary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                return;
            }

            if (_selectedStudentIndex < 0 || _selectedStudentIndex >= _students.Count)
                _selectedStudentIndex = 0;

            // 学生选择条
            var studentBar = new Rectangle(0, 0, _contentHost.Width, 36);
            using (var brush = new SolidBrush(colors.CardBg))
                g.FillRectangle(brush, studentBar);

            var curStudent = _students[_selectedStudentIndex];
            TextRenderer.DrawText(g, $"{curStudent.Name} ({curStudent.Id})", AppTheme.BodyFont,
                new Rectangle(12, 0, _contentHost.Width - 180, 36), colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            TextRenderer.DrawText(g, I18n.T("exam.prevStudent") + "  |  " + I18n.T("exam.nextStudent"),
                AppTheme.SmallFont,
                new Rectangle(_contentHost.Width - 160, 0, 150, 36), colors.TextSecondary,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            int y = 60;

            // 收集该学生的所有考试记录
            var studentScores = new List<(string Exam, double Score, double Full)>();
            foreach (var exam in _exams.OrderBy(e => e.Date))
            {
                if (exam.Scores != null && exam.Scores.TryGetValue(curStudent.Id, out var v))
                    studentScores.Add((exam.Name ?? "", v, exam.FullScore));
            }

            if (studentScores.Count == 0)
            {
                using var font = new Font(AppTheme.BodyFont.FontFamily, 12f);
                TextRenderer.DrawText(g, I18n.T("exam.noStudentScores"), font,
                    new Rectangle(0, y, _contentHost.Width, 40), colors.TextDisabled,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                return;
            }

            // 趋势折线图
            int chartH = 200;
            int chartW = Math.Min(_contentHost.Width - 80, 700);
            int chartX = 40;
            int chartY = y;

            using (var brush = new SolidBrush(colors.CardBg))
            using (var path = GraphicsExtensions.GetRoundPath(
                new Rectangle(chartX - 10, chartY - 10, chartW + 20, chartH + 20), WinUI3Tokens.CardRadius))
                g.FillPath(brush, path);

            using (var pen = new Pen(colors.Divider, 1f))
                g.DrawLine(pen, chartX, chartY + chartH, chartX + chartW, chartY + chartH);

            double maxScore = studentScores.Max(s => s.Full);
            var pts = new List<PointF>();
            for (int i = 0; i < studentScores.Count; i++)
            {
                float px = chartX + (studentScores.Count == 1 ? chartW / 2f : (float)i / (studentScores.Count - 1) * chartW);
                float py = chartY + chartH - (float)(studentScores[i].Score / maxScore * chartH);
                pts.Add(new PointF(px, py));
            }

            if (pts.Count > 1)
            {
                using var pen = new Pen(colors.Accent, 2f);
                g.DrawLines(pen, pts.ToArray());
            }

            using (var dotBrush = new SolidBrush(colors.Accent))
            {
                foreach (var p in pts)
                    g.FillEllipse(dotBrush, p.X - 4, p.Y - 4, 8, 8);
            }

            // 标签
            for (int i = 0; i < studentScores.Count; i++)
            {
                string label = studentScores[i].Exam;
                if (label.Length > 8) label = label.Substring(0, 8);

                TextRenderer.DrawText(g, label, AppTheme.SmallFont,
                    new Rectangle((int)pts[i].X - 40, chartY + chartH + 4, 80, 18), colors.TextSecondary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

                TextRenderer.DrawText(g, studentScores[i].Score.ToString("F0"), AppTheme.SmallFont,
                    new Rectangle((int)pts[i].X - 20, (int)pts[i].Y - 22, 40, 16), colors.TextPrimary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }

            y = chartY + chartH + 40;

            // 数据表
            using (var titleFont = new Font(AppTheme.BodyFont.FontFamily, 14f, FontStyle.Bold))
                TextRenderer.DrawText(g, I18n.T("exam.scoreTable"), titleFont,
                    new Rectangle(0, y, 400, 24), colors.TextPrimary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            y += 34;

            foreach (var (examName, score, full) in studentScores)
            {
                if (y > _contentHost.Height - 30) break;
                TextRenderer.DrawText(g, examName, AppTheme.BodyFont,
                    new Rectangle(0, y, 200, 24), colors.TextPrimary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

                TextRenderer.DrawText(g, $"{score:F1} / {full:F0}", AppTheme.BodyFont,
                    new Rectangle(220, y, 160, 24), colors.TextSecondary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

                y += 26;
            }
        }

        // ---------- 考试对比 ----------
        /// <summary>
        /// 绘制两次考试的对比。
        /// </summary>
        private void DrawCompare(Graphics g)
        {
            var colors = AppTheme.Colors;

            if (_exams.Count < 2)
            {
                using var font = new Font(AppTheme.BodyFont.FontFamily, 12f);
                TextRenderer.DrawText(g, I18n.T("exam.needTwoExams"), font,
                    _contentHost.ClientRectangle, colors.TextSecondary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                return;
            }

            if (_compareIndex < 0 || _compareIndex >= _exams.Count)
                _compareIndex = 0;

            var examA = _currentExam!;
            var examB = _exams[_compareIndex];

            int y = 0;
            using (var titleFont = new Font(AppTheme.BodyFont.FontFamily, 14f, FontStyle.Bold))
                TextRenderer.DrawText(g, $"{examA.Name}  vs  {examB.Name}", titleFont,
                    new Rectangle(0, y, _contentHost.Width, 28), colors.TextPrimary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            y += 40;

            double avgA = examA.Scores != null && examA.Scores.Count > 0 ? examA.Scores.Values.Average() : 0;
            double avgB = examB.Scores != null && examB.Scores.Count > 0 ? examB.Scores.Values.Average() : 0;

            double passA = examA.Scores != null && examA.Scores.Count > 0
                ? (double)examA.Scores.Values.Count(v => v >= examA.PassScore) / examA.Scores.Count
                : 0;
            double passB = examB.Scores != null && examB.Scores.Count > 0
                ? (double)examB.Scores.Values.Count(v => v >= examB.PassScore) / examB.Scores.Count
                : 0;

            double maxA = examA.Scores != null && examA.Scores.Count > 0 ? examA.Scores.Values.Max() : 0;
            double maxB = examB.Scores != null && examB.Scores.Count > 0 ? examB.Scores.Values.Max() : 0;

            y = DrawCompareRow(g, y, I18n.T("exam.average"), avgA, avgB, examA.FullScore);
            y = DrawCompareRow(g, y, I18n.T("exam.passRate"), passA * 100, passB * 100, 100);
            y = DrawCompareRow(g, y, I18n.T("exam.max"), maxA, maxB, examA.FullScore);

            y += 20;

            // 学生个体对比
            using (var titleFont = new Font(AppTheme.BodyFont.FontFamily, 14f, FontStyle.Bold))
                TextRenderer.DrawText(g, I18n.T("exam.studentCompare"), titleFont,
                    new Rectangle(0, y, 400, 24), colors.TextPrimary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            y += 34;

            foreach (var s in _students)
            {
                if (y > _contentHost.Height - 30) break;

                double sa = (examA.Scores != null && examA.Scores.TryGetValue(s.Id, out var va)) ? va : 0;
                double sb = (examB.Scores != null && examB.Scores.TryGetValue(s.Id, out var vb)) ? vb : 0;
                double diff = sa - sb;

                TextRenderer.DrawText(g, s.Name ?? "", AppTheme.BodyFont,
                    new Rectangle(0, y, 120, 24), colors.TextPrimary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

                TextRenderer.DrawText(g, sa.ToString("F1"), AppTheme.BodyFont,
                    new Rectangle(130, y, 80, 24), colors.TextSecondary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

                TextRenderer.DrawText(g, sb.ToString("F1"), AppTheme.BodyFont,
                    new Rectangle(220, y, 80, 24), colors.TextSecondary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

                Color diffColor = diff >= 0
                    ? Color.FromArgb(0x4C, 0xAF, 0x50)
                    : Color.FromArgb(0xE8, 0x1B, 0x1B);

                string diffStr = diff >= 0 ? "+" + diff.ToString("F1") : diff.ToString("F1");
                TextRenderer.DrawText(g, diffStr, AppTheme.BodyFont,
                    new Rectangle(310, y, 80, 24), diffColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

                y += 26;
            }
        }

        /// <summary>
        /// 绘制对比行。
        /// </summary>
        private int DrawCompareRow(Graphics g, int y, string label, double valA, double valB, double max)
        {
            var colors = AppTheme.Colors;

            TextRenderer.DrawText(g, label, AppTheme.BodyFont,
                new Rectangle(0, y, 120, 28), colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            int barMaxW = 200;

            // A 条
            int wA = (int)(valA / max * barMaxW);
            var barA = new Rectangle(130, y + 6, Math.Max(2, wA), 16);
            using (var brush = new SolidBrush(colors.Accent))
            using (var path = GraphicsExtensions.GetRoundPath(barA, 8))
                g.FillPath(brush, path);

            TextRenderer.DrawText(g, valA.ToString("F1"), AppTheme.SmallFont,
                new Rectangle(340, y, 80, 28), colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            y += 30;

            // B 条
            int wB = (int)(valB / max * barMaxW);
            var barB = new Rectangle(130, y + 6, Math.Max(2, wB), 16);
            using (var brush = new SolidBrush(colors.TextSecondary))
            using (var path = GraphicsExtensions.GetRoundPath(barB, 8))
                g.FillPath(brush, path);

            TextRenderer.DrawText(g, valB.ToString("F1"), AppTheme.SmallFont,
                new Rectangle(340, y, 80, 28), colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            return y + 40;
        }

        // =====================================================
        // 动作
        // =====================================================
        /// <summary>
        /// 新建考试。
        /// </summary>
        private void AddExam()
        {
            using var dlg = new ExamEditorDialog(null, _students);
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Result != null)
            {
                _exams.Add(dlg.Result);
                _owner.Exams = _exams;
                _owner.SaveAll();
                SetData(_exams, _students);
            }
        }

        /// <summary>
        /// 录入成绩。
        /// </summary>
        private void EditScores()
        {
            if (_currentExam == null)
            {
                MessageDialog.ShowInfo(I18n.T("common.info"), I18n.T("score.noExam"));
                return;
            }

            using var dlg = new ExamScoreEntryDialog(_currentExam, _students);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _owner.SaveAll();
                _contentHost.Invalidate();
            }
        }

        /// <summary>
        /// 导出成绩到 CSV。
        /// </summary>
        private void ExportCsv()
        {
            if (_currentExam == null) return;

            string path = FileBrowserDialog.PickFile(AppPaths.BaseDir, "CSV 文件|*.csv");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                var lines = new List<string> { "学号,姓名,分数" };
                if (_currentExam.Scores != null)
                {
                    foreach (var kv in _currentExam.Scores)
                    {
                        var s = _students.FirstOrDefault(x => x.Id == kv.Key);
                        lines.Add($"{kv.Key},{s?.Name ?? ""},{kv.Value}");
                    }
                }
                File.WriteAllLines(path, lines, System.Text.Encoding.UTF8);
                MessageDialog.ShowInfo(I18n.T("common.success"), I18n.T("dialog.exported"));
            }
            catch (Exception ex)
            {
                MessageDialog.ShowError(I18n.T("common.error"), ex.Message);
            }
        }
    }
}