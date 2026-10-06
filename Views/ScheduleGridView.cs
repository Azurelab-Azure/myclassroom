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
using CourseApp.Theme;

namespace CourseApp.Views
{
    public class ScheduleGridView : Panel
    {
        public event Action<int, int>? CellDoubleClicked;
        public event Action<Course>? CourseEditRequested;
        public event Action<Course>? CourseDeleteRequested;
        public event Action<Course>? CourseCopyRequested;
        public event Action<SectionTime>? SectionHeaderRightClicked;
        public event Action<int>? DayHeaderRightClicked;
        public event Action? EmptyAreaRightClicked;

        /// <summary>课程被拖动（松手 + 密码通过后触发）。参数：课程，新星期，新起始节。</summary>
       /// <summary>课程交换/移动完成。参数：拖动课程，目标课程（可空），新星期，新起始节，新结束节。</summary>
        public event Action<Course, Course?, int, int, int>? CourseSwapped;

        private const int SectionColW = 68;
        private const int TimeColW = 96;
        private const int HeaderH = 40;
        private const int MinRowH = 36;
        private const int FixedRowH = 48;
        private const int MinDayColW = 60;

        private List<Course> _courses = new();
        private List<SectionTime> _sections = new();
        private int _currentWeek = 1;

        private int _selectedRow = -1;
        private int _selectedCol = -1;
        private int _scrollOffset = 0;

        private FlatScrollBar _scrollBar = null!;
        private readonly List<RowInfo> _rowInfos = new();
        private readonly Dictionary<int, Rectangle> _dayHeaderRects = new();

        // =====================================================
        // 拖动状态
        // =====================================================
        private Course? _dragCourse;
        private Point _dragStartPt;
        private Point _dragCurrentPt;
        private bool _isDragging;
        private int _dragHoverDay = -1;
        private int _dragHoverSection = -1;
        private const int DragThreshold = 6;

        private class RowInfo
        {
            public int Index;
            public Rectangle Rect;
            public SectionTime Section = null!;
        }

        public ScheduleGridView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            BackColor = AppTheme.Colors.CardBg;

            _scrollBar = new FlatScrollBar
            {
                Width = 8,
                Visible = false,
                Left = Width - 8,
                Top = HeaderH,
                Height = Height - HeaderH,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right,
            };
            _scrollBar.ValueChanged += (s, e) =>
            {
                _scrollOffset = _scrollBar.Value;
                Invalidate();
            };
            Controls.Add(_scrollBar);

            MouseWheel += ScheduleGridView_MouseWheel;

            
        }

        // =====================================================
        // 数据
        // =====================================================
        public void SetData(List<SectionTime> sections, List<Course> courses, int currentWeek)
        {
            _sections = sections ?? new List<SectionTime>();
            _courses = courses ?? new List<Course>();
            _currentWeek = currentWeek;
            _selectedRow = -1;
            _selectedCol = -1;
            _scrollOffset = 0;
            Invalidate();
        }

        public void SetWeek(int week) { _currentWeek = week; Invalidate(); }

        public void ClearSelection()
        {
            _selectedRow = -1;
            _selectedCol = -1;
            Invalidate();
        }

        public List<Course> Courses => _courses;

        // =====================================================
        // 布局
        // =====================================================
        private int ContentWidth => Math.Max(0, Width - SectionColW - TimeColW - (_scrollBar.Visible ? 8 : 0));
        private int ContentHeight => Math.Max(0, Height - HeaderH);
        private int DayColW => Math.Max(MinDayColW, ContentWidth / 7);

        private int RowH
        {
            get
            {
                if (_sections.Count == 0) return FixedRowH;
                int fit = ContentHeight / _sections.Count;
                return fit >= MinRowH ? fit : FixedRowH;
            }
        }

        private int DayX(int day) => SectionColW + TimeColW + (day - 1) * DayColW;
        private Rectangle DayHeaderRect(int day) => new Rectangle(DayX(day), 0, DayColW, HeaderH);

        // =====================================================
        // 绘制
        // =====================================================
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var colors = AppTheme.Colors;

            using (var bg = new SolidBrush(colors.CardBg))
                g.FillRectangle(bg, ClientRectangle);

            _rowInfos.Clear();
            _dayHeaderRects.Clear();

            DrawHeader(g);

            var contentRegion = new Rectangle(0, HeaderH, Width, Height - HeaderH);
            var oldClip = g.Clip;
            g.SetClip(contentRegion);

            DrawSectionRows(g);
            DrawGridLines(g);
            DrawCourses(g);
            DrawDragHighlight(g);

            g.Clip = oldClip;

            DrawSelection(g);
            UpdateScrollBarState();
        }

        private void DrawHeader(Graphics g)
        {
            var colors = AppTheme.Colors;

            var headerRect = new Rectangle(SectionColW + TimeColW, 0, DayColW * 7, HeaderH);
            using (var hbg = new SolidBrush(colors.GridHeaderBg))
                g.FillRectangle(hbg, headerRect);

            using (var pen = new Pen(colors.Divider, 1f))
                g.DrawLine(pen, 0, HeaderH - 1, Width, HeaderH - 1);

            for (int day = 1; day <= 7; day++)
            {
                var rect = DayHeaderRect(day);
                _dayHeaderRects[day] = rect;

                TextRenderer.DrawText(g, I18n.T("weekday." + day), AppTheme.BodyFont,
                    rect, colors.TextPrimary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix);

                if (day < 7)
                {
                    using var pen = new Pen(colors.Divider, 1f);
                    g.DrawLine(pen, rect.Right - 1, 10, rect.Right - 1, HeaderH - 10);
                }
            }
        }

        private void DrawSectionRows(Graphics g)
        {
            int rowH = RowH;
            int y = HeaderH - _scrollOffset;

            for (int i = 0; i < _sections.Count; i++)
            {
                var st = _sections[i];
                var rowRect = new Rectangle(0, y, SectionColW + TimeColW, rowH);
                _rowInfos.Add(new RowInfo { Index = i, Rect = rowRect, Section = st });

                if (rowRect.Bottom > HeaderH && rowRect.Top < Height)
                    DrawSectionCell(g, st, rowRect);

                y += rowH;
            }
        }

        private void DrawSectionCell(Graphics g, SectionTime st, Rectangle rect)
        {
            var colors = AppTheme.Colors;
            bool isSpecial = st.Type == "special";

            var numRect = new Rectangle(0, rect.Y, SectionColW, rect.Height);
            string leftText = isSpecial ? "◆" : string.Format(I18n.T("schedule.section"), st.Section);
            TextRenderer.DrawText(g, leftText, AppTheme.SmallFont, numRect,
                isSpecial ? colors.Accent : colors.TextPrimary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPrefix);

            var timeRect = new Rectangle(SectionColW, rect.Y, TimeColW, rect.Height);
            if (isSpecial)
            {
                TextRenderer.DrawText(g, st.Name ?? "", AppTheme.SmallFont, timeRect,
                    colors.TextPrimary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
            else
            {
                string start = st.StartTime ?? "";
                string end = st.EndTime ?? "";
                if (!string.IsNullOrEmpty(start) || !string.IsNullOrEmpty(end))
                {
                    int half = rect.Height / 2;
                    var topRect = new Rectangle(SectionColW, rect.Y + 2, TimeColW, half - 2);
                    var botRect = new Rectangle(SectionColW, rect.Y + half, TimeColW, half - 2);

                    TextRenderer.DrawText(g, start, AppTheme.SmallFont, topRect,
                        colors.TextSecondary,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.Bottom |
                        TextFormatFlags.NoPrefix);

                    TextRenderer.DrawText(g, end, AppTheme.SmallFont, botRect,
                        colors.TextSecondary,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.Top |
                        TextFormatFlags.NoPrefix);
                }
            }
        }

        private void DrawGridLines(Graphics g)
        {
            var colors = AppTheme.Colors;
            int rowH = RowH;

            using var pen = new Pen(colors.Divider, 1f);

            int y = HeaderH - _scrollOffset;
            for (int i = 0; i <= _sections.Count; i++)
            {
                if (y >= HeaderH && y <= Height)
                    g.DrawLine(pen, 0, y, Width, y);
                y += rowH;
            }

            g.DrawLine(pen, SectionColW, HeaderH, SectionColW, Height);
            g.DrawLine(pen, SectionColW + TimeColW, HeaderH, SectionColW + TimeColW, Height);

            for (int day = 1; day <= 7; day++)
            {
                int x = DayX(day);
                g.DrawLine(pen, x, HeaderH, x, Height);
            }
            int rightX = DayX(7) + DayColW;
            g.DrawLine(pen, rightX, HeaderH, rightX, Height);
        }

        private void DrawCourses(Graphics g)
        {
            int rowH = RowH;

            foreach (var c in _courses)
            {
                if (c.WeekDay < 1 || c.WeekDay > 7) continue;
                if (c.Weeks == null || !c.Weeks.Contains(_currentWeek)) continue;

                // 拖动的课程——画半透明
                bool isDragged = _isDragging && _dragCourse == c;

                int startIdx = FindRowIndexBySection(c.TimeStart);
                int endIdx = FindRowIndexBySection(c.TimeEnd);
                if (startIdx < 0) startIdx = 0;
                if (endIdx < 0) endIdx = startIdx;
                if (endIdx < startIdx) endIdx = startIdx;

                int x = DayX(c.WeekDay);
                int y = HeaderH + startIdx * rowH - _scrollOffset;
                int w = DayColW;
                int h = (endIdx - startIdx + 1) * rowH;

                var rect = new Rectangle(x + 1, y + 1, w - 2, h - 2);

                if (rect.Bottom > HeaderH && rect.Top < Height)
                    DrawCourseCell(g, c, rect, isDragged ? 80 : 255);
            }

            // 拖动幽灵块
            if (_isDragging && _dragCourse != null)
                DrawDragGhost(g);
        }

private void DrawCourseCell(Graphics g, Course c, Rectangle rect, int alpha = 255)
{
    int hue = GetHue(c.Name);
    bool isDark = IsDarkColor(AppTheme.Colors.CardBg);

    Color bg = isDark
        ? ColorFromHsl(hue, 0.45, 0.22)
        : ColorFromHsl(hue, 0.55, 0.92);

    if (alpha < 255)
        bg = Color.FromArgb(alpha, bg);

    using (var path = GraphicsExtensions.GetRoundPath(rect, 6))
    using (var bgBrush = new SolidBrush(bg))
        g.FillPath(bgBrush, path);

    Color textColor = isDark ? Color.FromArgb(0xF0, 0xF0, 0xF0) : Color.FromArgb(0x20, 0x20, 0x20);
    Color subColor = isDark ? Color.FromArgb(0xC0, 0xC0, 0xC0) : Color.FromArgb(0x60, 0x60, 0x60);

    if (alpha < 255)
    {
        textColor = Color.FromArgb(alpha, textColor);
        subColor = Color.FromArgb(alpha, subColor);
    }

    int pad = 6;
    int innerW = rect.Width - pad * 2;
    int innerH = rect.Height - pad * 2;
    if (innerW < 10 || innerH < 10) return;

    var nameRect = new Rectangle(rect.X + pad, rect.Y + pad, innerW, innerH / 2);
    var subRect = new Rectangle(rect.X + pad, rect.Y + pad + innerH / 2, innerW, innerH / 2);

    // 课程名（完整）
    TextRenderer.DrawText(g, c.Name ?? "", AppTheme.BodyFont, nameRect,
        textColor,
        TextFormatFlags.HorizontalCenter | TextFormatFlags.Bottom |
        TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis |
        TextFormatFlags.NoPrefix);

    // 教师 + 教室
    string sub = (c.Teacher ?? "").Trim();
    string room = (c.Classroom ?? "").Trim();
    if (!string.IsNullOrEmpty(room))
        sub = string.IsNullOrEmpty(sub) ? room : (sub + " · " + room);

    TextRenderer.DrawText(g, sub, AppTheme.SmallFont, subRect,
        subColor,
        TextFormatFlags.HorizontalCenter | TextFormatFlags.Top |
        TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis |
        TextFormatFlags.NoPrefix);
}
/// <summary>取课程名第一个字符（支持中文 / 英文）。</summary>
private static string GetFirstChar(string name)
{
    if (string.IsNullOrEmpty(name)) return "";
    return name.Substring(0, 1).ToUpperInvariant();
}

        /// <summary>拖动幽灵块——跟随鼠标</summary>
        private void DrawDragGhost(Graphics g)
        {
            if (_dragCourse == null) return;

            int rowH = RowH;
            int spanSections = _dragCourse.TimeEnd - _dragCourse.TimeStart + 1;
            if (spanSections < 1) spanSections = 1;

            int w = DayColW - 2;
            int h = spanSections * rowH - 2;

            int x = _dragCurrentPt.X - w / 2;
            int y = _dragCurrentPt.Y - h / 2;

            var ghostRect = new Rectangle(x, y, w, h);

            int hue = GetHue(_dragCourse.Name);
            bool isDark = IsDarkColor(AppTheme.Colors.CardBg);

            Color bg = isDark
                ? ColorFromHsl(hue, 0.45, 0.32)
                : ColorFromHsl(hue, 0.55, 0.85);

            bg = Color.FromArgb(180, bg);

            using (var path = GraphicsExtensions.GetRoundPath(ghostRect, 6))
            using (var brush = new SolidBrush(bg))
                g.FillPath(brush, path);

            using (var path = GraphicsExtensions.GetRoundPath(ghostRect, 6))
            using (var pen = new Pen(AppTheme.Colors.Accent, 2f))
                g.DrawPath(pen, path);

            // 文字
            var textRect = new Rectangle(ghostRect.X + 4, ghostRect.Y + 4,
                                          ghostRect.Width - 8, ghostRect.Height - 8);
            TextRenderer.DrawText(g, _dragCourse.Name ?? "", AppTheme.BodyFont, textRect,
                Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        /// <summary>目标格子高亮</summary>
        private void DrawDragHighlight(Graphics g)
        {
            if (!_isDragging) return;
            if (_dragHoverDay < 1 || _dragHoverDay > 7) return;
            if (_dragHoverSection < 1) return;

            int rowH = RowH;

            int startIdx = FindRowIndexBySection(_dragHoverSection);
            if (startIdx < 0) return;

            int spanSections = _dragCourse?.TimeEnd - _dragCourse?.TimeStart + 1 ?? 1;
            if (spanSections < 1) spanSections = 1;

            int x = DayX(_dragHoverDay) + 1;
            int y = HeaderH + startIdx * rowH - _scrollOffset + 1;
            int w = DayColW - 2;
            int h = spanSections * rowH - 2;

            var rect = new Rectangle(x, y, w, h);
            if (rect.Bottom < HeaderH || rect.Top > Height) return;

            using (var brush = new SolidBrush(Color.FromArgb(50, AppTheme.Colors.Accent)))
                g.FillRectangle(brush, rect);

            using (var pen = new Pen(AppTheme.Colors.Accent, 2f))
                g.DrawRectangle(pen, rect);
        }

        private void DrawSelection(Graphics g)
        {
            if (_selectedRow < 0 || _selectedRow >= _sections.Count) return;
            if (_selectedCol < 1 || _selectedCol > 7) return;

            int y = HeaderH + _selectedRow * RowH - _scrollOffset;
            if (y + RowH <= HeaderH || y >= Height) return;

            var selRect = new Rectangle(
                DayX(_selectedCol) + 1,
                y + 1,
                DayColW - 2,
                RowH - 2);

            using (var bg = new SolidBrush(Color.FromArgb(50, AppTheme.Colors.Accent)))
                g.FillRectangle(bg, selRect);

            using (var pen = new Pen(AppTheme.Colors.Accent, 2f))
                g.DrawRectangle(pen, selRect);
        }

        private void UpdateScrollBarState()
        {
            int contentH = _sections.Count * RowH;
            bool need = contentH > ContentHeight && RowH == FixedRowH;

            _scrollBar.Visible = need;
            if (need)
            {
                _scrollBar.Maximum = contentH;
                _scrollBar.LargeChange = ContentHeight;
                _scrollBar.Left = Width - 8;
                _scrollBar.Top = HeaderH;
                _scrollBar.Height = Height - HeaderH;
                _scrollBar.Value = Math.Min(_scrollOffset, Math.Max(0, contentH - ContentHeight));
            }
            else
            {
                _scrollOffset = 0;
            }
        }

        private void ScheduleGridView_MouseWheel(object? sender, MouseEventArgs e)
        {
            if (!_scrollBar.Visible) return;

            int maxScroll = Math.Max(0, _scrollBar.Maximum - _scrollBar.LargeChange);
            _scrollOffset -= Math.Sign(e.Delta) * 60;
            if (_scrollOffset < 0) _scrollOffset = 0;
            if (_scrollOffset > maxScroll) _scrollOffset = maxScroll;

            _scrollBar.Value = _scrollOffset;
            _scrollBar.Wake();
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            _scrollOffset = 0;
            Invalidate();
        }

        // =====================================================
        // 工具
        // =====================================================
        private int FindRowIndexBySection(int section)
        {
            for (int i = 0; i < _sections.Count; i++)
            {
                var st = _sections[i];
                if (st.Type == "normal" && st.Section == section) return i;
            }
            return -1;
        }

        private static int GetHue(string name)
        {
            if (string.IsNullOrEmpty(name)) return 210;
            return Math.Abs(name.GetHashCode()) % 360;
        }

        private static bool IsDarkColor(Color c)
        {
            double lum = (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255.0;
            return lum < 0.5;
        }

        private static Color ColorFromHsl(double h, double s, double l)
        {
            double c = (1 - Math.Abs(2 * l - 1)) * s;
            double x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
            double m = l - c / 2;
            double r, g, b;

            if (h < 60) { r = c; g = x; b = 0; }
            else if (h < 120) { r = x; g = c; b = 0; }
            else if (h < 180) { r = 0; g = c; b = x; }
            else if (h < 240) { r = 0; g = x; b = c; }
            else if (h < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }

            return Color.FromArgb(
                Clamp((int)Math.Round((r + m) * 255)),
                Clamp((int)Math.Round((g + m) * 255)),
                Clamp((int)Math.Round((b + m) * 255)));
        }

        private static int Clamp(int v) => v < 0 ? 0 : (v > 255 ? 255 : v);

        // =====================================================
        // 命中
        // =====================================================
        private int HitTestDay(int x)
        {
            for (int day = 1; day <= 7; day++)
            {
                var rect = DayHeaderRect(day);
                if (x >= rect.X && x < rect.Right) return day;
            }
            return -1;
        }

        private RowInfo? HitTestRow(int y)
        {
            foreach (var r in _rowInfos)
                if (y >= r.Rect.Y && y < r.Rect.Bottom) return r;
            return null;
        }

        /// <summary>找到鼠标位置下的课程</summary>
        private Course? HitTestCourse(Point p)
        {
            int day = HitTestDay(p.X);
            if (day < 1) return null;

            var row = HitTestRow(p.Y);
            if (row == null) return null;

            foreach (var c in _courses)
            {
                if (c.WeekDay != day) continue;
                if (c.Weeks == null || !c.Weeks.Contains(_currentWeek)) continue;
                if (row.Section.Section >= c.TimeStart && row.Section.Section <= c.TimeEnd)
                    return c;
            }
            return null;
        }

        // =====================================================
        // 鼠标
        // =====================================================
        protected override void OnMouseDown(MouseEventArgs e)
        {
            bool inHeader = e.Y < HeaderH;
            bool inLeftCols = e.X < SectionColW + TimeColW;

            if (e.Button == MouseButtons.Left)
            {
                if (!inHeader && !inLeftCols)
                {
                    // 准备拖动
                    var course = HitTestCourse(e.Location);
                    if (course != null)
                    {
                        _dragCourse = course;
                        _dragStartPt = e.Location;
                        _dragCurrentPt = e.Location;
                        _isDragging = false;   // 还没达到阈值
                    }

                    int day = HitTestDay(e.X);
                    var row = HitTestRow(e.Y);
                    if (day >= 1 && row != null)
                    {
                        _selectedRow = row.Index;
                        _selectedCol = day;
                        Invalidate();
                    }
                    else ClearSelection();
                }
                else ClearSelection();
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (inHeader)
                {
                    int day = HitTestDay(e.X);
                    if (day >= 1) { DayHeaderRightClicked?.Invoke(day); return; }
                }
                else if (inLeftCols)
                {
                    var row = HitTestRow(e.Y);
                    if (row != null && row.Section.Type == "normal")
                    {
                        SectionHeaderRightClicked?.Invoke(row.Section);
                        return;
                    }
                }
                else
                {
                    int day = HitTestDay(e.X);
                    var row = HitTestRow(e.Y);
                    if (day >= 1 && row != null && row.Section.Type == "normal")
                    {
                        ShowCellContextMenu(day, row, e.Location);
                        return;
                    }
                }
                EmptyAreaRightClicked?.Invoke();
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            // 拖动中
            if (_dragCourse != null && e.Button == MouseButtons.Left)
            {
                _dragCurrentPt = e.Location;

                if (!_isDragging)
                {
                    // 判断是否超过阈值
                    int dx = e.X - _dragStartPt.X;
                    int dy = e.Y - _dragStartPt.Y;
                    if (dx * dx + dy * dy > DragThreshold * DragThreshold)
                        _isDragging = true;
                }

                if (_isDragging)
                {
                    // 计算目标位置
                    int day = HitTestDay(e.X);
                    var row = HitTestRow(e.Y);
                    if (day >= 1 && row != null && row.Section.Type == "normal")
                    {
                        _dragHoverDay = day;
                        _dragHoverSection = row.Section.Section;
                    }
                    else
                    {
                        _dragHoverDay = -1;
                        _dragHoverSection = -1;
                    }

                    Cursor = Cursors.Hand;
                    Invalidate();
                    return;
                }
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_dragCourse != null)
            {
                if (_isDragging)
                {
                    // 尝试移动
                    TryMoveCourse();
                }
            }

            // 清拖动状态
            _dragCourse = null;
            _isDragging = false;
            _dragHoverDay = -1;
            _dragHoverSection = -1;
            Cursor = Cursors.Default;
            Invalidate();

            base.OnMouseUp(e);
        }

        /// <summary>拖动松手 → 密码 → 移动</summary>
/// <summary>拖动松手 → 弹交换对话框 → 移动/交换</summary>
private void TryMoveCourse()
{
    if (_dragCourse == null) return;
    if (_dragHoverDay < 1 || _dragHoverSection < 1) return;

    // 位置没变 → 不动
    if (_dragHoverDay == _dragCourse.WeekDay &&
        _dragHoverSection == _dragCourse.TimeStart)
        return;

    // 找到目标位置已有的课程
    var targetCourse = HitTestCourseAt(_dragHoverDay, _dragHoverSection);

    // 弹交换对话框
    using var dlg = new SwapCourseDialog(_dragCourse, targetCourse, _dragHoverDay, _dragHoverSection);
    if (dlg.ShowDialog(this) != DialogResult.OK) return;
    if (!dlg.Confirmed) return;

    // 触发交换事件
    CourseSwapped?.Invoke(_dragCourse, targetCourse, dlg.NewDay, dlg.NewStart, dlg.NewEnd);
}

/// <summary>找某天某节的课程</summary>
private Course? HitTestCourseAt(int day, int section)
{
    foreach (var c in _courses)
    {
        if (c.WeekDay != day) continue;
        if (c.Weeks == null || !c.Weeks.Contains(_currentWeek)) continue;
        if (section >= c.TimeStart && section <= c.TimeEnd) return c;
    }
    return null;
}
        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left
                && e.Y >= HeaderH
                && e.X >= SectionColW + TimeColW)
            {
                int day = HitTestDay(e.X);
                var row = HitTestRow(e.Y);
                if (day >= 1 && row != null && row.Section.Type == "normal")
                {
                    CellDoubleClicked?.Invoke(day, row.Section.Section);
                }
            }
            base.OnMouseDoubleClick(e);
        }

        private void ShowCellContextMenu(int day, RowInfo row, Point localPt)
        {
            var course = _courses.FirstOrDefault(c =>
                c.WeekDay == day
                && c.Weeks != null && c.Weeks.Contains(_currentWeek)
                && row.Section.Section >= c.TimeStart
                && row.Section.Section <= c.TimeEnd);

            var menu = new FlatContextMenu();

            if (course != null)
            {
                menu.AddItem(FlatMenuItem.Create(I18n.T("menu.edit"),
                    () => CourseEditRequested?.Invoke(course), Icons.Edit));
                menu.AddItem(FlatMenuItem.Create(I18n.T("menu.copy"),
                    () => CourseCopyRequested?.Invoke(course), Icons.Copy, "Ctrl+C"));
                menu.AddSeparator();
                menu.AddItem(FlatMenuItem.CreateDanger(I18n.T("menu.delete"),
                    () => CourseDeleteRequested?.Invoke(course), Icons.Delete, "Del"));
            }
            else
            {
                menu.AddItem(FlatMenuItem.Create(I18n.T("menu.addCourse"),
                    () => CellDoubleClicked?.Invoke(day, row.Section.Section), Icons.Add));

                var pasteItem = FlatMenuItem.Create(I18n.T("menu.paste"), null, Icons.Paste, "Ctrl+V");
                pasteItem.Enabled = false;
                menu.AddItem(pasteItem);
            }

            menu.ShowAt(this, PointToScreen(localPt));
        }
    }
}