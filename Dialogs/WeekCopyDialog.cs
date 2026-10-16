using System;
using System.Drawing;
using System.Windows.Forms;
using CourseApp.Controls;
using CourseApp.Localization;
using CourseApp.Theme;

namespace CourseApp.Dialogs
{
    /// <summary>
    /// 周次复制 / 调课弹窗。
    /// 选择源周、目标周，决定复制或移动，以及冲突处理方式。
    /// </summary>
    public class WeekCopyDialog : FlatDialogBase
    {
        /// <summary>源周输入框</summary>
        private FlatNumberBox _srcWeekBox = null!;

        /// <summary>目标周输入框</summary>
        private FlatNumberBox _dstWeekBox = null!;

        /// <summary>模式下拉框</summary>
        private FlatComboBox _modeBox = null!;

        /// <summary>冲突处理下拉框</summary>
        private FlatComboBox _conflictBox = null!;

        /// <summary>源周</summary>
        public int SrcWeek { get; private set; }

        /// <summary>目标周</summary>
        public int DstWeek { get; private set; }

        /// <summary>是否移动（false 复制）</summary>
        public bool IsMove { get; private set; }

        /// <summary>是否覆盖目标周已有课程</summary>
        public bool Overwrite { get; private set; }

        /// <summary>是否确认</summary>
        public bool Confirmed { get; private set; }

        /// <summary>
        /// 构造周次复制 / 调课弹窗。
        /// </summary>
        /// <param name="currentWeek">当前周次</param>
        public WeekCopyDialog(int currentWeek)
            : base(I18n.T("weekCopy.title"), 440, 380)
        {
            SrcWeek = currentWeek;
            DstWeek = currentWeek + 1;

            BuildUI();
        }

        /// <summary>
        /// 构建界面。
        /// </summary>
        private void BuildUI()
        {
            int x = 20;
            int y = 10;
            int fieldX = 140;
            int rowH = 40;
            int gap = 10;

            // 说明
            ContentPanel.Controls.Add(new Label
            {
                Text = I18n.T("weekCopy.desc"),
                Font = AppTheme.SmallFont,
                ForeColor = AppTheme.Colors.TextSecondary,
                Left = x, Top = y, Width = 380, Height = 40,
                BackColor = Color.Transparent,
                AutoSize = false,
            });
            y += 50;

            // 源周
            AddLabel(I18n.T("weekCopy.srcWeek"), x, y);
            _srcWeekBox = new FlatNumberBox
            {
                Left = fieldX, Top = y, Width = 100, Height = 32,
                Minimum = 1, Maximum = 30, Value = SrcWeek,
            };
            ContentPanel.Controls.Add(_srcWeekBox);
            y += rowH + gap;

            // 目标周
            AddLabel(I18n.T("weekCopy.dstWeek"), x, y);
            _dstWeekBox = new FlatNumberBox
            {
                Left = fieldX, Top = y, Width = 100, Height = 32,
                Minimum = 1, Maximum = 30, Value = DstWeek,
            };
            ContentPanel.Controls.Add(_dstWeekBox);
            y += rowH + gap;

            // 模式
            AddLabel(I18n.T("weekCopy.mode"), x, y);
            _modeBox = new FlatComboBox
            {
                Left = fieldX, Top = y, Width = 180, Height = 32,
            };
            _modeBox.SetItems(new IFlatComboItem[]
            {
                new FlatTextItem(I18n.T("weekCopy.mode.copy")),
                new FlatTextItem(I18n.T("weekCopy.mode.move")),
            });
            _modeBox.SelectedIndex = 0;
            ContentPanel.Controls.Add(_modeBox);
            y += rowH + gap;

            // 冲突
            AddLabel(I18n.T("weekCopy.conflict"), x, y);
            _conflictBox = new FlatComboBox
            {
                Left = fieldX, Top = y, Width = 180, Height = 32,
            };
            _conflictBox.SetItems(new IFlatComboItem[]
            {
                new FlatTextItem(I18n.T("weekCopy.conflict.overwrite")),
                new FlatTextItem(I18n.T("weekCopy.conflict.skip")),
            });
            _conflictBox.SelectedIndex = 0;
            ContentPanel.Controls.Add(_conflictBox);
            y += rowH + gap + 10;

            // 按钮
            var btnOk = new FlatButton
            {
                Text = I18n.T("dialog.ok"),
                ButtonStyle = FlatButtonStyle.Primary,
                Width = 100, Height = 36,
                Left = 150, Top = y,
            };
            btnOk.Click += (s, e) => Confirm();
            ContentPanel.Controls.Add(btnOk);

            var btnCancel = new FlatButton
            {
                Text = I18n.T("dialog.cancel"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Width = 100, Height = 36,
                Left = 260, Top = y,
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
                Left = x, Top = y + 6, Width = 110,
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.Colors.TextPrimary,
                BackColor = Color.Transparent,
                AutoSize = false,
            });
        }

        /// <summary>
        /// 确认。
        /// </summary>
        private void Confirm()
        {
            int src = _srcWeekBox.Value;
            int dst = _dstWeekBox.Value;

            if (src == dst)
            {
                MessageDialog.ShowInfo(I18n.T("common.info"), I18n.T("weekCopy.sameWeek"));
                return;
            }

            SrcWeek = src;
            DstWeek = dst;
            IsMove = (_modeBox.SelectedIndex == 1);
            Overwrite = (_conflictBox.SelectedIndex == 0);
            Confirmed = true;

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}