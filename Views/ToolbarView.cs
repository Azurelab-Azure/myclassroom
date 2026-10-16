using System;
using System.Drawing;
using System.Windows.Forms;
using CourseApp.Controls;
using CourseApp.Localization;
using CourseApp.Theme;

namespace CourseApp.Views
{
    /// <summary>
    /// 顶部工具栏：上一周 / 下一周 / 周次 / 视图过滤 / 复制到 / 调课 / 导入 / 导出。
    /// 主题切换由 Form1 统一触发重绘。
    /// </summary>
    public class ToolbarView : Panel
    {
        /// <summary>上一周点击</summary>
        public event Action? PrevWeekClicked;

        /// <summary>下一周点击</summary>
        public event Action? NextWeekClicked;

        /// <summary>视图过滤变化</summary>
        public event Action<string>? WeekFilterChanged;

        /// <summary>复制到点击</summary>
        public event Action? CopyWeekClicked;

        /// <summary>调课点击</summary>
        public event Action? MoveWeekClicked;

        /// <summary>导入 CSV 点击</summary>
        public event Action? ImportClicked;

        /// <summary>导出 CSV 点击</summary>
        public event Action? ExportClicked;

        /// <summary>上一周按钮</summary>
        private FlatButton _btnPrev = null!;

        /// <summary>下一周按钮</summary>
        private FlatButton _btnNext = null!;

        /// <summary>周次标签</summary>
        private Label _weekLabel = null!;

        /// <summary>视图过滤下拉框</summary>
        private FlatComboBox _filterBox = null!;

        /// <summary>复制到按钮</summary>
        private FlatButton _btnCopy = null!;

        /// <summary>调课按钮</summary>
        private FlatButton _btnMove = null!;

        /// <summary>导入按钮</summary>
        private FlatButton _btnImport = null!;

        /// <summary>导出按钮</summary>
        private FlatButton _btnExport = null!;

        /// <summary>当前周次</summary>
        private int _currentWeek = 1;

        /// <summary>
        /// 构造工具栏。
        /// </summary>
        public ToolbarView()
        {
            Dock = DockStyle.Top;
            Height = 56;
            BackColor = AppTheme.Colors.WindowBg;

            BuildUI();
        }

        /// <summary>
        /// 构建界面。
        /// </summary>
        private void BuildUI()
        {
            int left = 16;
            int top = 12;
            int h = 32;
            int gap = 8;

            // 上一周 / 下一周
            _btnPrev = MakeToolButton(I18n.T("toolbar.prevWeek"), ref left, top, h, gap);
            _btnPrev.Click += (s, e) => PrevWeekClicked?.Invoke();

            _btnNext = MakeToolButton(I18n.T("toolbar.nextWeek"), ref left, top, h, gap);
            _btnNext.Click += (s, e) => NextWeekClicked?.Invoke();

            // 周次标签
            left += 8;
            _weekLabel = new Label
            {
                Text = string.Format(I18n.T("toolbar.week"), _currentWeek),
                Font = AppTheme.TitleFont,
                ForeColor = AppTheme.Colors.Accent,
                AutoSize = true,
                Left = left,
                Top = top + 6,
                BackColor = Color.Transparent,
            };
            Controls.Add(_weekLabel);
            left = _weekLabel.Right + 20;

            // 视图过滤下拉
            _filterBox = new FlatComboBox
            {
                Left = left, Top = top,
                Width = 120, Height = h,
            };
            _filterBox.SetItems(new IFlatComboItem[]
            {
                new FlatTextItem(I18n.T("weekFilter.all")),
                new FlatTextItem(I18n.T("weekFilter.odd")),
                new FlatTextItem(I18n.T("weekFilter.even")),
                new FlatTextItem(I18n.T("weekFilter.cycle4")),
                new FlatTextItem(I18n.T("weekFilter.cycle3")),
            });
            _filterBox.SelectedIndex = 0;
            _filterBox.SelectedIndexChanged += (s, e) =>
            {
                string filter = _filterBox.SelectedIndex switch
                {
                    1 => "odd",
                    2 => "even",
                    3 => "cycle4",
                    4 => "cycle3",
                    _ => "all",
                };
                WeekFilterChanged?.Invoke(filter);
            };
            Controls.Add(_filterBox);
            left = _filterBox.Right + 8;

            // 复制到 / 调课
            _btnCopy = new FlatButton
            {
                Text = I18n.T("weekCopy.copyTo"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Left = left, Top = top,
                Width = 80, Height = h,
            };
            _btnCopy.Click += (s, e) => CopyWeekClicked?.Invoke();
            Controls.Add(_btnCopy);
            left = _btnCopy.Right + 8;

            _btnMove = new FlatButton
            {
                Text = I18n.T("weekCopy.moveTo"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Left = left, Top = top,
                Width = 80, Height = h,
            };
            _btnMove.Click += (s, e) => MoveWeekClicked?.Invoke();
            Controls.Add(_btnMove);

            // 导入 / 导出放右侧
            int right = 16;

            _btnExport = new FlatButton
            {
                Text = I18n.T("toolbar.export"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Width = 100, Height = h,
                Top = top,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };
            _btnExport.Click += (s, e) => ExportClicked?.Invoke();
            Controls.Add(_btnExport);

            _btnImport = new FlatButton
            {
                Text = I18n.T("toolbar.import"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Width = 100, Height = h,
                Top = top,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };
            _btnImport.Click += (s, e) => ImportClicked?.Invoke();
            Controls.Add(_btnImport);

            Resize += (s, e) =>
            {
                _btnExport.Left = Width - _btnExport.Width - right;
                _btnImport.Left = _btnExport.Left - _btnImport.Width - 8;
            };

            _btnExport.Left = Width - _btnExport.Width - right;
            _btnImport.Left = _btnExport.Left - _btnImport.Width - 8;
        }

        /// <summary>
        /// 创建工具栏按钮。
        /// </summary>
        private FlatButton MakeToolButton(string text, ref int left, int top, int h, int gap)
        {
            var btn = new FlatButton
            {
                Text = text ?? "",
                ButtonStyle = FlatButtonStyle.Secondary,
                Left = left,
                Top = top,
                Height = h,
            };

            int textWidth;
            try
            {
                using var bmp = new Bitmap(1, 1);
                using var g = Graphics.FromImage(bmp);
                var size = TextRenderer.MeasureText(g, btn.Text, AppTheme.BodyFont);
                textWidth = size.Width;
            }
            catch
            {
                textWidth = (btn.Text?.Length ?? 0) * 14;
            }

            btn.Width = Math.Max(48, textWidth + 28);

            Controls.Add(btn);
            left = btn.Right + gap;
            return btn;
        }

        /// <summary>
        /// 设置周次。
        /// </summary>
        public void SetWeek(int week)
        {
            _currentWeek = week;
            _weekLabel.Text = string.Format(I18n.T("toolbar.week"), week);
        }

        /// <summary>
        /// 读取当前视图过滤。
        /// </summary>
        public string CurrentWeekFilter
        {
            get
            {
                return _filterBox.SelectedIndex switch
                {
                    1 => "odd",
                    2 => "even",
                    3 => "cycle4",
                    4 => "cycle3",
                    _ => "all",
                };
            }
        }
    }
}