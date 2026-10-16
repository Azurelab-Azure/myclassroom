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

namespace CourseApp.Views
{
    /// <summary>
    /// 办公模式待办页。
    /// 显示所有 IsTodo 的日程，支持勾选完成、分类筛选。
    /// </summary>
    public class PageOfficeTodo : Panel
    {
        /// <summary>主窗口引用</summary>
        private readonly Form2 _owner;

        /// <summary>日程数据</summary>
        private List<ScheduleItem> _schedules = new();

        /// <summary>当前筛选分类：all / meeting / task / reminder / other</summary>
        private string _filter = "all";

        /// <summary>筛选按钮</summary>
        private readonly List<FlatButton> _filterButtons = new();

        /// <summary>列表宿主</summary>
        private Panel _listHost = null!;

        /// <summary>列表滚动条</summary>
        private FlatScrollBar _listScrollBar = null!;

        /// <summary>滚动偏移</summary>
        private int _scrollY = 0;

        /// <summary>行高</summary>
        private const int RowH = 48;

        /// <summary>
        /// 构造办公待办页。
        /// </summary>
        /// <param name="owner">办公模式主窗口</param>
        public PageOfficeTodo(Form2 owner)
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

            string[] filterKeys = { "all", "meeting", "task", "reminder", "other" };
            string[] filterTexts =
            {
                I18n.T("office.allCategories"),
                I18n.T("schedule.category.meeting"),
                I18n.T("schedule.category.task"),
                I18n.T("schedule.category.reminder"),
                I18n.T("schedule.category.other"),
            };

            int x = left;
            for (int i = 0; i < filterKeys.Length; i++)
            {
                var btn = new FlatButton
                {
                    Text = filterTexts[i],
                    ButtonStyle = i == 0 ? FlatButtonStyle.Primary : FlatButtonStyle.Secondary,
                    Left = x, Top = top,
                    Width = 90, Height = 32,
                };
                var key = filterKeys[i];
                btn.Click += (s, e) => SetFilter(key);
                Controls.Add(btn);
                _filterButtons.Add(btn);
                x += 100;
            }

            top += 48;

            _listHost = new Panel
            {
                Left = left, Top = top,
                Width = Width - left * 2 - 12,
                Height = Height - top - 16,
                BackColor = AppTheme.Colors.CardBg,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            };
            _listHost.Paint += ListHost_Paint;
            _listHost.MouseDown += ListHost_MouseDown;
            _listHost.MouseWheel += ListHost_MouseWheel;
            Controls.Add(_listHost);

            _listScrollBar = new FlatScrollBar
            {
                Left = Width - left - 8,
                Top = top,
                Width = 8,
                Height = _listHost.Height,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right,
            };
            _listScrollBar.ValueChanged += (s, e) =>
            {
                _scrollY = _listScrollBar.Value;
                _listHost.Invalidate();
            };
            Controls.Add(_listScrollBar);

            Resize += (s, e) =>
            {
                _listHost.Width = Width - left * 2 - 12;
                _listHost.Height = Height - _listHost.Top - 16;
                _listScrollBar.Left = Width - left - 8;
                _listScrollBar.Height = _listHost.Height;
            };
        }

        /// <summary>
        /// 设置筛选。
        /// </summary>
        private void SetFilter(string filter)
        {
            _filter = filter;

            string[] keys = { "all", "meeting", "task", "reminder", "other" };
            for (int i = 0; i < _filterButtons.Count; i++)
            {
                _filterButtons[i].ButtonStyle = (keys[i] == filter)
                    ? FlatButtonStyle.Primary
                    : FlatButtonStyle.Secondary;
            }

            UpdateScrollBar();
            _listHost.Invalidate();
        }

        /// <summary>
        /// 设置数据。
        /// </summary>
        public void SetData(List<ScheduleItem> schedules)
        {
            _schedules = schedules ?? new List<ScheduleItem>();
            UpdateScrollBar();
            _listHost.Invalidate();
        }

        /// <summary>
        /// 获取筛选后的待办。
        /// </summary>
        private List<ScheduleItem> GetFiltered()
        {
            var list = _schedules.Where(s => s.IsTodo).ToList();

            if (_filter != "all")
                list = list.Where(s => s.Category == _filter).ToList();

            return list
                .OrderBy(s => s.Done)
                .ThenBy(s => s.Date)
                .ThenBy(s => s.StartTime)
                .ToList();
        }

        /// <summary>
        /// 绘制列表。
        /// </summary>
        private void ListHost_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var colors = AppTheme.Colors;

            using (var bg = new SolidBrush(colors.CardBg))
                g.FillRectangle(bg, _listHost.ClientRectangle);

            // 表头
            var headerRect = new Rectangle(0, 0, _listHost.Width, 34);
            using (var brush = new SolidBrush(colors.GridHeaderBg))
                g.FillRectangle(brush, headerRect);

            using (var pen = new Pen(colors.Divider, 1f))
                g.DrawLine(pen, 0, 34, _listHost.Width, 34);

            TextRenderer.DrawText(g, I18n.T("office.todoList"), AppTheme.BodyFont,
                new Rectangle(16, 0, _listHost.Width - 32, 34), colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            var items = GetFiltered();

            int y = 34 - _scrollY;
            for (int i = 0; i < items.Count; i++)
            {
                var rect = new Rectangle(0, y, _listHost.Width, RowH);
                if (rect.Bottom > 34 && rect.Top < _listHost.Height)
                    DrawRow(g, items[i], rect);
                y += RowH;
            }

            if (items.Count == 0)
            {
                using var font = new Font(AppTheme.BodyFont.FontFamily, 12f);
                TextRenderer.DrawText(g, I18n.T("office.noTodo"), font,
                    new Rectangle(16, 60, _listHost.Width - 32, 30), colors.TextDisabled,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
        }

        /// <summary>
        /// 绘制单行。
        /// </summary>
        private void DrawRow(Graphics g, ScheduleItem item, Rectangle rect)
        {
            var colors = AppTheme.Colors;

            // 勾选框
            var boxRect = new Rectangle(16, rect.Y + (rect.Height - 18) / 2, 18, 18);
            using (var pen = new Pen(item.Done ? colors.Accent : colors.ButtonBorder, 1.5f))
                g.DrawRectangle(pen, boxRect);

            if (item.Done)
            {
                using var pen = new Pen(colors.Accent, 2f)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                };
                g.DrawLines(pen, new[]
                {
                    new Point(boxRect.X + 4, boxRect.Y + 9),
                    new Point(boxRect.X + 8, boxRect.Y + 13),
                    new Point(boxRect.X + 14, boxRect.Y + 5),
                });
            }

            // 标题
            Color titleColor = item.Done ? colors.TextDisabled : colors.TextPrimary;
            var titleFont = item.Done
                ? new Font(AppTheme.BodyFont, FontStyle.Strikeout)
                : AppTheme.BodyFont;

            TextRenderer.DrawText(g, item.Title ?? "", titleFont,
                new Rectangle(44, rect.Y, rect.Width - 300, rect.Height), titleColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            // 分类
            TextRenderer.DrawText(g, CategoryText(item.Category), AppTheme.SmallFont,
                new Rectangle(rect.Right - 240, rect.Y, 80, rect.Height), colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            // 日期
            TextRenderer.DrawText(g, item.Date ?? "", AppTheme.SmallFont,
                new Rectangle(rect.Right - 150, rect.Y, 130, rect.Height), colors.TextSecondary,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            using var divider = new Pen(colors.Divider, 1f);
            g.DrawLine(divider, 16, rect.Bottom - 1, rect.Right - 16, rect.Bottom - 1);
        }

        /// <summary>
        /// 列表点击。
        /// </summary>
        private void ListHost_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Y < 34) return;

            int idx = (e.Y - 34 + _scrollY) / RowH;
            var items = GetFiltered();
            if (idx < 0 || idx >= items.Count) return;

            var item = items[idx];
            var target = _schedules.Find(x => x.Id == item.Id);
            if (target == null) return;

            if (e.Button == MouseButtons.Left)
            {
                var boxRect = new Rectangle(16, 34 + idx * RowH - _scrollY + (RowH - 18) / 2, 18, 18);
                if (boxRect.Contains(e.Location))
                    _owner.ToggleScheduleDone(target);
            }
            else if (e.Button == MouseButtons.Right)
            {
                var menu = new FlatContextMenu();
                menu.AddItem(FlatMenuItem.Create(I18n.T("menu.edit"),
                    () => _owner.OpenScheduleEditor(target), Icons.Edit));
                menu.AddSeparator();
                menu.AddItem(FlatMenuItem.CreateDanger(I18n.T("menu.delete"),
                    () => _owner.DeleteSchedule(target), Icons.Delete));
                menu.ShowAt(_listHost, _listHost.PointToScreen(e.Location));
            }
        }

        /// <summary>
        /// 列表滚轮。
        /// </summary>
        private void ListHost_MouseWheel(object? sender, MouseEventArgs e)
        {
            if (!_listScrollBar.Visible) return;

            int maxScroll = Math.Max(0, _listScrollBar.Maximum - _listScrollBar.LargeChange);
            _scrollY -= Math.Sign(e.Delta) * 60;
            if (_scrollY < 0) _scrollY = 0;
            if (_scrollY > maxScroll) _scrollY = maxScroll;
            _listScrollBar.Value = _scrollY;
            _listScrollBar.Wake();
            _listHost.Invalidate();
        }

        /// <summary>
        /// 更新滚动条。
        /// </summary>
        private void UpdateScrollBar()
        {
            int count = GetFiltered().Count;
            int contentH = count * RowH + 34;
            bool need = contentH > _listHost.Height;

            _listScrollBar.Visible = need;
            if (need)
            {
                _listScrollBar.Maximum = contentH;
                _listScrollBar.LargeChange = _listHost.Height;
                _listScrollBar.Value = 0;
                _scrollY = 0;
            }
            else _scrollY = 0;
        }

        /// <summary>
        /// 分类翻译。
        /// </summary>
        private static string CategoryText(string category)
        {
            return category switch
            {
                "meeting" => I18n.T("schedule.category.meeting"),
                "task" => I18n.T("schedule.category.task"),
                "reminder" => I18n.T("schedule.category.reminder"),
                "other" => I18n.T("schedule.category.other"),
                _ => "",
            };
        }
    }
}