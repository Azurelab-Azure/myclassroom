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
    /// 日程编辑弹窗。
    /// 支持重复、提醒、颜色、作为待办。
    /// </summary>
    public class ScheduleItemEditorDialog : FlatDialogBase
    {
        /// <summary>正在编辑的日程。为 null 表示新建。</summary>
        private readonly ScheduleItem? _editing;

        /// <summary>标题</summary>
        private FlatTextBox _titleBox = null!;

        /// <summary>分类</summary>
        private FlatComboBox _categoryBox = null!;

        /// <summary>日期</summary>
        private FlatTextBox _dateBox = null!;

        /// <summary>开始时间</summary>
        private FlatTextBox _startTimeBox = null!;

        /// <summary>结束时间</summary>
        private FlatTextBox _endTimeBox = null!;

        /// <summary>地点</summary>
        private FlatTextBox _locationBox = null!;

        /// <summary>备注</summary>
        private FlatTextBox _notesBox = null!;

        /// <summary>重复</summary>
        private FlatComboBox _repeatBox = null!;

        /// <summary>重复至</summary>
        private FlatTextBox _repeatUntilBox = null!;

        /// <summary>提醒</summary>
        private FlatComboBox _reminderBox = null!;

        /// <summary>颜色</summary>
        private FlatTextBox _colorBox = null!;

        /// <summary>作为待办开关</summary>
        private WinUI3Toggle _todoToggle = null!;

        /// <summary>颜色预览</summary>
        private Panel _colorPreview = null!;

        /// <summary>结果</summary>
        public ScheduleItem? Result { get; private set; }

        /// <summary>
        /// 构造日程编辑弹窗。
        /// </summary>
        /// <param name="existing">正在编辑的日程，为 null 表示新建</param>
        public ScheduleItemEditorDialog(ScheduleItem? existing)
            : base(existing == null ? I18n.T("schedule.new") : I18n.T("schedule.edit"), 520, 620)
        {
            _editing = existing;
            BuildUI();
            if (existing != null) LoadFrom(existing);
        }

        /// <summary>
        /// 构建界面。
        /// </summary>
        private void BuildUI()
        {
            int x = 20;
            int y = 10;
            int fieldX = 130;
            int fieldW = 340;
            int rowH = 36;
            int gap = 8;

            // 标题
            AddLabel(I18n.T("schedule.title"), x, y);
            _titleBox = new FlatTextBox { Left = fieldX, Top = y, Width = fieldW, Height = 32 };
            ContentPanel.Controls.Add(_titleBox);
            y += rowH + gap;

            // 分类
            AddLabel(I18n.T("schedule.category"), x, y);
            _categoryBox = new FlatComboBox { Left = fieldX, Top = y, Width = 180, Height = 32 };
            _categoryBox.SetItems(new IFlatComboItem[]
            {
                new FlatTextItem(I18n.T("schedule.category.meeting")),
                new FlatTextItem(I18n.T("schedule.category.task")),
                new FlatTextItem(I18n.T("schedule.category.reminder")),
                new FlatTextItem(I18n.T("schedule.category.other")),
            });
            _categoryBox.SelectedIndex = 0;
            ContentPanel.Controls.Add(_categoryBox);
            y += rowH + gap;

            // 日期
            AddLabel(I18n.T("schedule.date"), x, y);
            _dateBox = new FlatTextBox
            {
                Left = fieldX, Top = y, Width = fieldW, Height = 32,
                Placeholder = "yyyy-MM-dd",
                Text = DateTime.Now.ToString("yyyy-MM-dd"),
            };
            ContentPanel.Controls.Add(_dateBox);
            y += rowH + gap;

            // 开始时间
            AddLabel(I18n.T("schedule.startTime"), x, y);
            _startTimeBox = new FlatTextBox
            {
                Left = fieldX, Top = y, Width = 120, Height = 32,
                Placeholder = "HH:mm",
            };
            ContentPanel.Controls.Add(_startTimeBox);

            // 结束时间
            ContentPanel.Controls.Add(new Label
            {
                Text = "~",
                Left = fieldX + 130, Top = y + 6, Width = 20,
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.Colors.TextPrimary,
                BackColor = Color.Transparent,
                AutoSize = false,
            });

            _endTimeBox = new FlatTextBox
            {
                Left = fieldX + 160, Top = y, Width = 120, Height = 32,
                Placeholder = "HH:mm",
            };
            ContentPanel.Controls.Add(_endTimeBox);
            y += rowH + gap;

            // 地点
            AddLabel(I18n.T("schedule.location"), x, y);
            _locationBox = new FlatTextBox { Left = fieldX, Top = y, Width = fieldW, Height = 32 };
            ContentPanel.Controls.Add(_locationBox);
            y += rowH + gap;

            // 重复
            AddLabel(I18n.T("schedule.repeat"), x, y);
            _repeatBox = new FlatComboBox { Left = fieldX, Top = y, Width = 180, Height = 32 };
            _repeatBox.SetItems(new IFlatComboItem[]
            {
                new FlatTextItem(I18n.T("schedule.repeat.none")),
                new FlatTextItem(I18n.T("schedule.repeat.daily")),
                new FlatTextItem(I18n.T("schedule.repeat.weekdays")),
                new FlatTextItem(I18n.T("schedule.repeat.weekly")),
                new FlatTextItem(I18n.T("schedule.repeat.monthly")),
            });
            _repeatBox.SelectedIndex = 0;
            ContentPanel.Controls.Add(_repeatBox);
            y += rowH + gap;

            // 重复至
            AddLabel(I18n.T("schedule.repeatUntil"), x, y);
            _repeatUntilBox = new FlatTextBox
            {
                Left = fieldX, Top = y, Width = fieldW, Height = 32,
                Placeholder = "yyyy-MM-dd（可空）",
            };
            ContentPanel.Controls.Add(_repeatUntilBox);
            y += rowH + gap;

            // 提醒
            AddLabel(I18n.T("schedule.reminder"), x, y);
            _reminderBox = new FlatComboBox { Left = fieldX, Top = y, Width = 200, Height = 32 };
            _reminderBox.SetItems(new IFlatComboItem[]
            {
                new FlatTextItem(I18n.T("schedule.reminder.none")),
                new FlatTextItem(I18n.T("schedule.reminder.at")),
                new FlatTextItem(I18n.T("schedule.reminder.5min")),
                new FlatTextItem(I18n.T("schedule.reminder.15min")),
                new FlatTextItem(I18n.T("schedule.reminder.30min")),
                new FlatTextItem(I18n.T("schedule.reminder.1hour")),
                new FlatTextItem(I18n.T("schedule.reminder.1day")),
            });
            _reminderBox.SelectedIndex = 0;
            ContentPanel.Controls.Add(_reminderBox);
            y += rowH + gap;

            // 颜色
            AddLabel(I18n.T("schedule.color"), x, y);
            _colorPreview = new Panel
            {
                Left = fieldX, Top = y,
                Width = 40, Height = 32,
                BackColor = Color.FromArgb(0x2E, 0x86, 0xE8),
            };
            _colorPreview.Paint += (s, e) =>
            {
                using var pen = new Pen(AppTheme.Colors.CardBorder, 1f);
                e.Graphics.DrawRectangle(pen, 0, 0, _colorPreview.Width - 1, _colorPreview.Height - 1);
            };
            ContentPanel.Controls.Add(_colorPreview);

            _colorBox = new FlatTextBox
            {
                Left = fieldX + 48, Top = y, Width = 120, Height = 32,
                Text = "#2E86E8",
            };
            _colorBox.TextChanged += (s, e) =>
            {
                _colorPreview.BackColor = ParseColor(_colorBox.Text, Color.FromArgb(0x2E, 0x86, 0xE8));
            };
            ContentPanel.Controls.Add(_colorBox);
            y += rowH + gap;

            // 作为待办
            AddLabel(I18n.T("schedule.isTodo"), x, y);
            _todoToggle = new WinUI3Toggle { Left = fieldX, Top = y + 4 };
            ContentPanel.Controls.Add(_todoToggle);
            y += rowH + gap;

            // 备注
            AddLabel(I18n.T("schedule.notes"), x, y);
            _notesBox = new FlatTextBox { Left = fieldX, Top = y, Width = fieldW, Height = 32 };
            ContentPanel.Controls.Add(_notesBox);
            y += rowH + gap + 10;

            // 按钮
            var btnOk = new FlatButton
            {
                Text = I18n.T("dialog.save"),
                ButtonStyle = FlatButtonStyle.Primary,
                Width = 100, Height = 36,
                Left = 220, Top = y,
            };
            btnOk.Click += BtnOk_Click;
            ContentPanel.Controls.Add(btnOk);

            var btnCancel = new FlatButton
            {
                Text = I18n.T("dialog.cancel"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Width = 100, Height = 36,
                Left = 330, Top = y,
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            ContentPanel.Controls.Add(btnCancel);
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
        /// 从日程对象加载数据。
        /// </summary>
        private void LoadFrom(ScheduleItem item)
        {
            _titleBox.Text = item.Title ?? "";
            _categoryBox.SelectedIndex = item.Category switch
            {
                "task" => 1,
                "reminder" => 2,
                "other" => 3,
                _ => 0,
            };
            _dateBox.Text = item.Date ?? "";
            _startTimeBox.Text = item.StartTime ?? "";
            _endTimeBox.Text = item.EndTime ?? "";
            _locationBox.Text = item.Location ?? "";
            _notesBox.Text = item.Notes ?? "";

            _repeatBox.SelectedIndex = item.Repeat switch
            {
                "daily" => 1,
                "weekdays" => 2,
                "weekly" => 3,
                "monthly" => 4,
                _ => 0,
            };
            _repeatUntilBox.Text = item.RepeatUntil ?? "";

            _reminderBox.SelectedIndex = item.Reminder switch
            {
                "at" => 1,
                "5min" => 2,
                "15min" => 3,
                "30min" => 4,
                "1hour" => 5,
                "1day" => 6,
                _ => 0,
            };

            _colorBox.Text = item.Color ?? "#2E86E8";
            _colorPreview.BackColor = ParseColor(_colorBox.Text, Color.FromArgb(0x2E, 0x86, 0xE8));

            _todoToggle.Checked = item.IsTodo;
        }

        /// <summary>
        /// 点击保存。
        /// </summary>
        private void BtnOk_Click(object? sender, EventArgs e)
        {
            string title = _titleBox.Text.Trim();
            if (string.IsNullOrEmpty(title))
            {
                MessageDialog.ShowInfo(I18n.T("common.info"), I18n.T("schedule.needTitle"));
                return;
            }

            var target = _editing ?? new ScheduleItem();

            target.Title = title;
            target.Category = _categoryBox.SelectedIndex switch
            {
                1 => "task",
                2 => "reminder",
                3 => "other",
                _ => "meeting",
            };
            target.Date = _dateBox.Text.Trim();
            target.StartTime = _startTimeBox.Text.Trim();
            target.EndTime = _endTimeBox.Text.Trim();
            target.Location = _locationBox.Text.Trim();
            target.Notes = _notesBox.Text.Trim();

            target.Repeat = _repeatBox.SelectedIndex switch
            {
                1 => "daily",
                2 => "weekdays",
                3 => "weekly",
                4 => "monthly",
                _ => "none",
            };
            target.RepeatUntil = _repeatUntilBox.Text.Trim();

            target.Reminder = _reminderBox.SelectedIndex switch
            {
                1 => "at",
                2 => "5min",
                3 => "15min",
                4 => "30min",
                5 => "1hour",
                6 => "1day",
                _ => "none",
            };

            target.Color = _colorBox.Text.Trim();
            target.IsTodo = _todoToggle.Checked;

            Result = target;
            DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>
        /// hex 转 Color。
        /// </summary>
        private static Color ParseColor(string hex, Color fallback)
        {
            if (string.IsNullOrEmpty(hex)) return fallback;
            try
            {
                if (hex.StartsWith("#")) hex = hex.Substring(1);
                if (hex.Length != 6) return fallback;
                int r = Convert.ToInt32(hex.Substring(0, 2), 16);
                int g = Convert.ToInt32(hex.Substring(2, 2), 16);
                int b = Convert.ToInt32(hex.Substring(4, 2), 16);
                return Color.FromArgb(r, g, b);
            }
            catch { return fallback; }
        }
    }
}