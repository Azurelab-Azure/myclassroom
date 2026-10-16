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
    /// <summary>
    /// 课表网格。
    /// 支持：拖动交换、课代表显示、右键菜单、周次切换、复制粘贴、视图周次过滤。
    /// </summary>
    public class ScheduleGridView : Panel
    {
        /// <summary>单元格双击</summary>
        public event Action<int, int>? CellDoubleClicked;

        /// <summary>课程编辑请求</summary>
        public event Action<Course>? CourseEditRequested;

        /// <summary>课程删除请求</summary>
        public event Action<Course>? CourseDeleteRequested;

        /// <summary>课程复制请求</summary>
        public event Action<Course>? CourseCopyRequested;

        /// <summary>课程粘贴请求</summary>
        public event Action<Course>? CoursePasteRequested;

        /// <summary>节次表头右键</summary>
        public event Action<SectionTime>? SectionHeaderRightClicked;

        /// <summary>星期表头右键</summary>
        public event Action<int>? DayHeaderRightClicked;

        /// <summary>空白区右键</summary>
        public event Action? EmptyAreaRightClicked;

        /// <summary>课程交换 / 移动完成</summary>
        public event Action<Course, Course?, int, int, int>? CourseSwapped;

        /// <summary>请求上一周</summary>
        public event Action? PrevWeekRequested;

        /// <summary>请求下一周</summary>
        public event Action? NextWeekRequested;

        // =====================================================
        // 布局常量
        // =====================================================
        /// <summary>节次列宽</summary>
        private const int SectionColW = 68;

        /// <summary>时间列宽</summary>
        private const int TimeColW = 96;

        /// <summary>表头高度</summary>
        private const int HeaderH = 40;

        /// <summary>最小行高</summary>
        private const int MinRowH = 36;

        /// <summary>固定行高</summary>
        private const int FixedRowH = 48;

        /// <summary>最小天列宽</summary>
        private const int MinDayColW = 60;

        /// <summary>拖动阈值</summary>
        private const int DragThreshold = 6;

        /// <summary>滚轮步长</summary>
        private const int ScrollStep = 60;

        // =====================================================
        // 数据
        // =====================================================
        /// <summary>课程数据</summary>
        private List<Course> _courses = new();

        /// <summary>节次数据</summary>
        private List<SectionTime> _sections = new();

        /// <summary>课代表</summary>
        private List<CourseRepresentative> _representatives = new();

        /// <summary>当前周次</summary>
        private int _currentWeek = 1;

        /// <summary>视图周次过滤：all / odd / even / cycle4 / cycle3</summary>
        private string _weekFilter = "all";

        // =====================================================
        // 状态
        // =====================================================
        /// <summary>选中行</summary>
        private int _selectedRow = -1;

        /// <summary>选中列</summary>
        private int _selectedCol = -1;

        /// <summary>滚动偏移</summary>
        private int _scrollOffset = 0;

        /// <summary>滚动条</summary>
        private FlatScrollBar _scrollBar = null!;

        /// <summary>行信息缓存</summary>
        private readonly List<RowInfo> _rowInfos = new();

        /// <summary>星期表头矩形缓存</summary>
        private readonly Dictionary<int, Rectangle> _dayHeaderRects = new();

        /// <summary>周次按钮矩形：上一周</summary>
        private Rectangle _prevWeekBtnRect = Rectangle.Empty;

        /// <summary>周次按钮矩形：下一周</summary>
        private Rectangle _nextWeekBtnRect = Rectangle.Empty;

        // 拖动
        /// <summary>正在拖动的课程</summary>
        private Course? _dragCourse;

        /// <summary>拖动起点</summary>
        private Point _dragStartPt;

        /// <summary>拖动当前点</summary>
        private Point _dragCurrentPt;

        /// <summary>是否正在拖动</summary>
        private bool _isDragging;

        /// <summary>拖动悬停的星期</summary>
        private int _dragHoverDay = -1;

        /// <summary>拖动悬停的节次</summary>
        private int _dragHoverSection = -1;

        // 剪贴板
        /// <summary>剪贴板中的课程</summary>
        private Course? _clipboardCourse;

        /// <summary>行信息</summary>
        private class RowInfo
        {
            /// <summary>行索引</summary>
            public int Index;

            /// <summary>行矩形</summary>
            public Rectangle Rect;

            /// <summary>节次</summary>
            public SectionTime Section = null!;
        }

        /// <summary>
        /// 构造课表网格。
        /// </summary>
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

        /// <summary>
        /// 设置数据。
        /// </summary>
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

        /// <summary>
        /// 设置课代表。
        /// </summary>
        public void SetRepresentatives(List<CourseRepresentative> reps)
        {
            _representatives = reps ?? new List<CourseRepresentative>();
            Invalidate();
        }

        /// <summary>
        /// 设置当前周次。
        /// </summary>
        public void SetWeek(int week)
        {
            _currentWeek = week;
            Invalidate();
        }

        /// <summary>
        /// 设置视图周次过滤。
        /// </summary>
        /// <param name="filter">all / odd / even / cycle4 / cycle3</param>
        public void SetWeekFilter(string filter)
        {
            _weekFilter = filter ?? "all";
            Invalidate();
        }

        /// <summary>
        /// 清除选中。
        /// </summary>
        public void ClearSelection()
        {
            _selectedRow = -1;
            _selectedCol = -1;
            Invalidate();
        }

        /// <summary>
        /// 当前课程列表。
        /// </summary>
        public List<Course> Courses => _courses;

        // =====================================================
        // 视图过滤
        // =====================================================
        /// <summary>
        /// 判断某周在当前视图下是否可见。
        /// </summary>
        private bool IsWeekVisible(int week)
        {
            int current = _currentWeek;
            return _weekFilter switch
            {
                "odd" => week % 2 == 1,
                "even" => week % 2 == 0,
                "cycle4" => ((week - 1) % 4) == ((current - 1) % 4),
                "cycle3" => ((week - 1) % 3) == ((current - 1) % 3),
                _ => true,
            };
        }

        /// <summary>
        /// 判断课程在当前视图下是否可见。
        /// </summary>
        private bool IsCourseVisible(Course c)
        {
            if (c.Weeks == null) return false;
            foreach (var w in c.Weeks)
            {
                if (IsWeekVisible(w)) return true;
            }
            return false;
        }

        // =====================================================
        // 布局
        // =====================================================
        /// <summary>内容宽度</summary>
        private int ContentWidth => Math.Max(0, Width - SectionColW - TimeColW - (_scrollBar.Visible ? 8 : 0));

        /// <summary>内容高度</summary>
        private int ContentHeight => Math.Max(0, Height - HeaderH);

        /// <summary>天列宽</summary>
        private int DayColW => Math.Max(MinDayColW, ContentWidth / 7);

        /// <summary>行高</summary>
        private int RowH
        {
            get
            {
                if (_sections.Count == 0) return FixedRowH;
                int fit = ContentHeight / _sections.Count;
                return fit >= MinRowH ? fit : FixedRowH;
            }
        }

        /// <summary>计算某天 X 坐标</summary>
        private int DayX(int day) => SectionColW + TimeColW + (day - 1) * DayColW;

        /// <summary>计算某天表头矩形</summary>
        private Rectangle DayHeaderRect(int day) => new Rectangle(DayX(day), 0, DayColW, HeaderH);

        // =====================================================
        // 绘制
        // =====================================================

        /// <summary>
        /// 绘制课表。
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var colors = AppTheme.Colors;

            using (var bg = new SolidBrush(colors.CardBg))
                g.FillRectangle(bg, ClientRectangle);

            _rowInfos.Clear();
            _dayHeaderRects.Clear();

            if (_sections.Count == 0)
            {
                DrawEmptyState(g);
                return;
            }

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

        /// <summary>
        /// 空状态。
        /// </summary>
        private void DrawEmptyState(Graphics g)
        {
            var colors = AppTheme.Colors;
            using var font = new Font(AppTheme.BodyFont.FontFamily, 14f);
            TextRenderer.DrawText(g, I18n.T("schedule.empty"), font,
                ClientRectangle, colors.TextSecondary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPrefix);
        }

        /// <summary>
        /// 绘制表头。
        /// </summary>
        private void DrawHeader(Graphics g)
        {
            var colors = AppTheme.Colors;

            var headerRect = new Rectangle(SectionColW + TimeColW, 0, DayColW * 7, HeaderH);
            using (var hbg = new SolidBrush(colors.GridHeaderBg))
                g.FillRectangle(hbg, headerRect);

            using (var pen = new Pen(colors.Divider, 1f))
                g.DrawLine(pen, 0, HeaderH - 1, Width, HeaderH - 1);

            int todayWeekday = (int)DateTime.Now.DayOfWeek;
            if (todayWeekday == 0) todayWeekday = 7;

            for (int day = 1; day <= 7; day++)
            {
                var rect = DayHeaderRect(day);
                _dayHeaderRects[day] = rect;

                bool isToday = (day == todayWeekday);

                if (isToday)
                {
                    using var accentBrush = new SolidBrush(Color.FromArgb(30, colors.Accent));
                    g.FillRectangle(accentBrush, rect);
                }

                Color fg = isToday ? colors.Accent : colors.TextPrimary;
                var font = isToday
                    ? new Font(AppTheme.BodyFont, FontStyle.Bold)
                    : AppTheme.BodyFont;

                TextRenderer.DrawText(g, I18n.T("weekday." + day), font,
                    rect, fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix);

                if (day < 7)
                {
                    using var pen = new Pen(colors.Divider, 1f);
                    g.DrawLine(pen, rect.Right - 1, 10, rect.Right - 1, HeaderH - 10);
                }
            }

            // 周次按钮
            DrawWeekButtons(g);
        }

        /// <summary>
        /// 绘制"上一周 / 下一周"按钮。
        /// </summary>
        private void DrawWeekButtons(Graphics g)
        {
            var colors = AppTheme.Colors;

            int btnSize = 24;
            int gap = 4;
            int right = Width - 12;
            int top = (HeaderH - btnSize) / 2;

            _nextWeekBtnRect = new Rectangle(right - btnSize, top, btnSize, btnSize);
            _prevWeekBtnRect = new Rectangle(_nextWeekBtnRect.Left - btnSize - gap, top, btnSize, btnSize);

            DrawWeekButton(g, _prevWeekBtnRect, false);
            DrawWeekButton(g, _nextWeekBtnRect, true);
        }

        /// <summary>
        /// 绘制单个周次按钮。
        /// </summary>
        private void DrawWeekButton(Graphics g, Rectangle rect, bool next)
        {
            var colors = AppTheme.Colors;

            using (var path = GraphicsExtensions.GetRoundPath(rect, 4))
            using (var brush = new SolidBrush(colors.CardBg))
                g.FillPath(brush, path);

            using (var path = GraphicsExtensions.GetRoundPath(rect, 4))
            using (var pen = new Pen(colors.ButtonBorder, 1f))
                g.DrawPath(pen, path);

            int cx = rect.X + rect.Width / 2;
            int cy = rect.Y + rect.Height / 2;

            using var arrowPen = new Pen(colors.TextPrimary, 1.6f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
            };

            if (next)
            {
                g.DrawLines(arrowPen, new[]
                {
                    new Point(cx - 3, cy - 5),
                    new Point(cx + 3, cy),
                    new Point(cx - 3, cy + 5),
                });
            }
            else
            {
                g.DrawLines(arrowPen, new[]
                {
                    new Point(cx + 3, cy - 5),
                    new Point(cx - 3, cy),
                    new Point(cx + 3, cy + 5),
                });
            }
        }

        /// <summary>
        /// 绘制节次行。
        /// </summary>
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

        /// <summary>
        /// 绘制节次单元格。
        /// </summary>
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

        /// <summary>
        /// 绘制网格线。
        /// </summary>
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

        /// <summary>
        /// 绘制课程。
        /// </summary>
        private void DrawCourses(Graphics g)
        {
            int rowH = RowH;

            foreach (var c in _courses)
            {
                if (c.WeekDay < 1 || c.WeekDay > 7) continue;
                if (!IsCourseVisible(c)) continue;

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

            if (_isDragging && _dragCourse != null)
                DrawDragGhost(g);
        }

        /// <summary>
        /// 绘制单个课程格。
        /// </summary>
        private void DrawCourseCell(Graphics g, Course c, Rectangle rect, int alpha = 255)
        {
            int hue = GetHue(c.Name);
            bool isDark = IsDarkColor(AppTheme.Colors.CardBg);

            Color bg = isDark
                ? ColorFromHsl(hue, 0.45, 0.22)
                : ColorFromHsl(hue, 0.55, 0.92);

            if (alpha < 255)
                bg = Color.FromArgb(alpha, bg);

            using (var path = GraphicsExtensions.GetRoundPath(rect, WinUI3Tokens.ControlRadius))
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

            TextRenderer.DrawText(g, c.Name ?? "", AppTheme.BodyFont, nameRect,
                textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.Bottom |
                TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPrefix);

            string sub = (c.Teacher ?? "").Trim();
            string room = (c.Classroom ?? "").Trim();
            if (!string.IsNullOrEmpty(room))
                sub = string.IsNullOrEmpty(sub) ? room : (sub + " · " + room);

            TextRenderer.DrawText(g, sub, AppTheme.SmallFont, subRect,
                subColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.Top |
                TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPrefix);

            var rep = _representatives.FirstOrDefault(r => r.CourseName == c.Name);
            if (rep != null && !string.IsNullOrEmpty(rep.StudentName) && innerH >= 36)
            {
                using var repFont = new Font(AppTheme.BodyFont.FontFamily, 9f);
                var repRect = new Rectangle(rect.X + pad, rect.Bottom - 16, innerW, 14);
                TextRenderer.DrawText(g, I18n.T("courseRep.short") + " " + rep.StudentName,
                    repFont, repRect,
                    Color.FromArgb(alpha < 255 ? alpha : 200, subColor),
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
        }

        /// <summary>
        /// 绘制拖动幽灵块。
        /// </summary>
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

            bg = Color.FromArgb(200, bg);

            using (var path = GraphicsExtensions.GetRoundPath(ghostRect, WinUI3Tokens.ControlRadius))
            using (var brush = new SolidBrush(bg))
                g.FillPath(brush, path);

            using (var path = GraphicsExtensions.GetRoundPath(ghostRect, WinUI3Tokens.ControlRadius))
            using (var pen = new Pen(AppTheme.Colors.Accent, 2f))
                g.DrawPath(pen, path);

            var textRect = new Rectangle(ghostRect.X + 4, ghostRect.Y + 4,
                                          ghostRect.Width - 8, ghostRect.Height - 8);
            TextRenderer.DrawText(g, _dragCourse.Name ?? "", AppTheme.BodyFont, textRect,
                Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        /// <summary>
        /// 绘制拖动目标高亮。
        /// </summary>
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

        /// <summary>
        /// 绘制选中格。
        /// </summary>
        private void DrawSelection(Graphics g)
        {
            if (_selectedRow < 0 || _selectedRow >= _sections.Count) return;
            if (_selectedCol < 1 || _selectedCol > 7) return;

            int rowH = RowH;
            int y = HeaderH + _selectedRow * rowH - _scrollOffset;
            if (y + rowH <= HeaderH || y >= Height) return;

            var selRect = new Rectangle(
                DayX(_selectedCol) + 1,
                y + 1,
                DayColW - 2,
                rowH - 2);

            using (var bg = new SolidBrush(Color.FromArgb(50, AppTheme.Colors.Accent)))
                g.FillRectangle(bg, selRect);

            using (var pen = new Pen(AppTheme.Colors.Accent, 2f))
                g.DrawRectangle(pen, selRect);
        }

        /// <summary>
        /// 更新滚动条状态。
        /// </summary>
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
            else _scrollOffset = 0;
        }

        /// <summary>
        /// 滚轮滚动。
        /// </summary>
        private void ScheduleGridView_MouseWheel(object? sender, MouseEventArgs e)
        {
            if (!_scrollBar.Visible) return;

            int maxScroll = Math.Max(0, _scrollBar.Maximum - _scrollBar.LargeChange);
            _scrollOffset -= Math.Sign(e.Delta) * ScrollStep;
            if (_scrollOffset < 0) _scrollOffset = 0;
            if (_scrollOffset > maxScroll) _scrollOffset = maxScroll;

            _scrollBar.Value = _scrollOffset;
            _scrollBar.Wake();
            Invalidate();
        }

        /// <summary>
        /// 尺寸变化时重置滚动。
        /// </summary>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            _scrollOffset = 0;
            Invalidate();
        }

        // =====================================================
        // 工具
        // =====================================================
        /// <summary>
        /// 根据节次找行索引。
        /// </summary>
        private int FindRowIndexBySection(int section)
        {
            for (int i = 0; i < _sections.Count; i++)
            {
                var st = _sections[i];
                if (st.Type == "normal" && st.Section == section) return i;
            }
            return -1;
        }

        /// <summary>
        /// 课程名 → 色相。
        /// </summary>
        private static int GetHue(string name)
        {
            if (string.IsNullOrEmpty(name)) return 210;
            return Math.Abs(name.GetHashCode()) % 360;
        }

        /// <summary>
        /// 判断颜色是否为暗色。
        /// </summary>
        private static bool IsDarkColor(Color c)
        {
            double lum = (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255.0;
            return lum < 0.5;
        }

        /// <summary>
        /// HSL 转 RGB。
        /// </summary>
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

        /// <summary>
        /// 限制 0~255。
        /// </summary>
        private static int Clamp(int v) => v < 0 ? 0 : (v > 255 ? 255 : v);

        // =====================================================
        // 命中
        // =====================================================
        /// <summary>
        /// 命中星期。
        /// </summary>
        private int HitTestDay(int x)
        {
            for (int day = 1; day <= 7; day++)
            {
                var rect = DayHeaderRect(day);
                if (x >= rect.X && x < rect.Right) return day;
            }
            return -1;
        }

        /// <summary>
        /// 命中行。
        /// </summary>
        private RowInfo? HitTestRow(int y)
        {
            foreach (var r in _rowInfos)
                if (y >= r.Rect.Y && y < r.Rect.Bottom) return r;
            return null;
        }

        /// <summary>
        /// 命中课程。
        /// </summary>
        private Course? HitTestCourse(Point p)
        {
            int day = HitTestDay(p.X);
            if (day < 1) return null;

            var row = HitTestRow(p.Y);
            if (row == null) return null;

            foreach (var c in _courses)
            {
                if (c.WeekDay != day) continue;
                if (!IsCourseVisible(c)) continue;
                if (row.Section.Section >= c.TimeStart && row.Section.Section <= c.TimeEnd)
                    return c;
            }
            return null;
        }

        /// <summary>
        /// 找某天某节的课程。
        /// </summary>
        private Course? HitTestCourseAt(int day, int section)
        {
            foreach (var c in _courses)
            {
                if (c.WeekDay != day) continue;
                if (!IsCourseVisible(c)) continue;
                if (section >= c.TimeStart && section <= c.TimeEnd) return c;
            }
            return null;
        }

        // =====================================================
        // 鼠标
        // =====================================================

        /// <summary>
        /// 鼠标按下。
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            bool inHeader = e.Y < HeaderH;
            bool inLeftCols = e.X < SectionColW + TimeColW;

            if (e.Button == MouseButtons.Left)
            {
                // 周次按钮
                if (inHeader)
                {
                    if (_prevWeekBtnRect.Contains(e.Location))
                    {
                        PrevWeekRequested?.Invoke();
                        return;
                    }
                    if (_nextWeekBtnRect.Contains(e.Location))
                    {
                        NextWeekRequested?.Invoke();
                        return;
                    }
                }

                if (!inHeader && !inLeftCols)
                {
                    var course = HitTestCourse(e.Location);
                    if (course != null)
                    {
                        _dragCourse = course;
                        _dragStartPt = e.Location;
                        _dragCurrentPt = e.Location;
                        _isDragging = false;
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

        /// <summary>
        /// 鼠标移动。
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_dragCourse != null && e.Button == MouseButtons.Left)
            {
                _dragCurrentPt = e.Location;

                if (!_isDragging)
                {
                    int dx = e.X - _dragStartPt.X;
                    int dy = e.Y - _dragStartPt.Y;
                    if (dx * dx + dy * dy > DragThreshold * DragThreshold)
                        _isDragging = true;
                }

                if (_isDragging)
                {
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

        /// <summary>
        /// 鼠标抬起。
        /// </summary>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_dragCourse != null && _isDragging)
                TryMoveCourse();

            _dragCourse = null;
            _isDragging = false;
            _dragHoverDay = -1;
            _dragHoverSection = -1;
            Cursor = Cursors.Default;
            Invalidate();

            base.OnMouseUp(e);
        }

        /// <summary>
        /// 尝试移动 / 交换。
        /// </summary>
        private void TryMoveCourse()
        {
            if (_dragCourse == null) return;
            if (_dragHoverDay < 1 || _dragHoverSection < 1) return;

            if (_dragHoverDay == _dragCourse.WeekDay &&
                _dragHoverSection == _dragCourse.TimeStart)
                return;

            var targetCourse = HitTestCourseAt(_dragHoverDay, _dragHoverSection);

            using var dlg = new SwapCourseDialog(_dragCourse, targetCourse, _dragHoverDay, _dragHoverSection);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            if (!dlg.Confirmed) return;

            CourseSwapped?.Invoke(_dragCourse, targetCourse, dlg.NewDay, dlg.NewStart, dlg.NewEnd);
        }

        /// <summary>
        /// 双击。
        /// </summary>
        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left
                && e.Y >= HeaderH
                && e.X >= SectionColW + TimeColW)
            {
                int day = HitTestDay(e.X);
                var row = HitTestRow(e.Y);
                if (day >= 1 && row != null && row.Section.Type == "normal")
                    CellDoubleClicked?.Invoke(day, row.Section.Section);
            }
            base.OnMouseDoubleClick(e);
        }

        /// <summary>
        /// 单元格右键菜单。
        /// </summary>
        private void ShowCellContextMenu(int day, RowInfo row, Point localPt)
        {
            var course = _courses.FirstOrDefault(c =>
                c.WeekDay == day
                && IsCourseVisible(c)
                && row.Section.Section >= c.TimeStart
                && row.Section.Section <= c.TimeEnd);

            var menu = new FlatContextMenu();

            if (course != null)
            {
                menu.AddItem(FlatMenuItem.Create(I18n.T("menu.edit"),
                    () => CourseEditRequested?.Invoke(course), Icons.Edit));
                menu.AddItem(FlatMenuItem.Create(I18n.T("menu.copy"),
                    () => CopyCourse(course), Icons.Copy, "Ctrl+C"));
                menu.AddSeparator();
                menu.AddItem(FlatMenuItem.CreateDanger(I18n.T("menu.delete"),
                    () => CourseDeleteRequested?.Invoke(course), Icons.Delete, "Del"));
            }
            else
            {
                menu.AddItem(FlatMenuItem.Create(I18n.T("menu.addCourse"),
                    () => CellDoubleClicked?.Invoke(day, row.Section.Section), Icons.Add));

                var pasteItem = FlatMenuItem.Create(I18n.T("menu.paste"), null, Icons.Paste, "Ctrl+V");
                pasteItem.Enabled = _clipboardCourse != null;
                pasteItem.OnClick = () => PasteCourse(day, row.Section.Section);
                menu.AddItem(pasteItem);
            }

            menu.ShowAt(this, PointToScreen(localPt));
        }

        /// <summary>
        /// 复制课程到剪贴板。
        /// </summary>
        private void CopyCourse(Course c)
        {
            _clipboardCourse = new Course
            {
                Name = c.Name,
                Teacher = c.Teacher,
                Classroom = c.Classroom,
                Weeks = new List<int>(c.Weeks ?? new List<int>()),
                WeekDay = c.WeekDay,
                TimeStart = c.TimeStart,
                TimeEnd = c.TimeEnd,
            };

            CourseCopyRequested?.Invoke(c);
        }

        /// <summary>
        /// 粘贴课程到目标位置。
        /// </summary>
        private void PasteCourse(int day, int section)
        {
            if (_clipboardCourse == null) return;

            int span = _clipboardCourse.TimeEnd - _clipboardCourse.TimeStart;
            var newCourse = new Course
            {
                Name = _clipboardCourse.Name,
                Teacher = _clipboardCourse.Teacher,
                Classroom = _clipboardCourse.Classroom,
                Weeks = new List<int>(_clipboardCourse.Weeks ?? new List<int>()),
                WeekDay = day,
                TimeStart = section,
                TimeEnd = section + span,
            };

            CoursePasteRequested?.Invoke(newCourse);
        }
    }
}