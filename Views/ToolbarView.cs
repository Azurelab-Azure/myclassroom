using System;
using System.Drawing;
using System.Windows.Forms;
using CourseApp.Controls;
using CourseApp.Localization;
using CourseApp.Theme;

namespace CourseApp.Views
{
    /// <summary>
    /// 顶部工具栏：只留"上一周 / 下一周 / 周次"。
    /// 主题切换由 Form1 统一触发 Invalidate。
    /// </summary>
    public class ToolbarView : Panel
    {
        public event Action? PrevWeekClicked;
        public event Action? NextWeekClicked;

        private FlatButton _btnPrev = null!;
        private FlatButton _btnNext = null!;
        private Label _weekLabel = null!;

        private int _currentWeek = 1;

        public ToolbarView()
        {
            Dock = DockStyle.Top;
            Height = 56;
            BackColor = AppTheme.Colors.WindowBg;

            BuildUI();
        }

        private void BuildUI()
        {
            int left = 16;
            int top = 12;
            int h = 32;
            int gap = 8;

            _btnPrev = MakeToolButton(I18n.T("toolbar.prevWeek"), ref left, top, h, gap);
            _btnPrev.Click += (s, e) => PrevWeekClicked?.Invoke();

            _btnNext = MakeToolButton(I18n.T("toolbar.nextWeek"), ref left, top, h, gap);
            _btnNext.Click += (s, e) => NextWeekClicked?.Invoke();

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
        }

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

        public void SetWeek(int week)
        {
            _currentWeek = week;
            _weekLabel.Text = string.Format(I18n.T("toolbar.week"), week);
        }
    }
}