using System;
using System.Drawing;
using System.Windows.Forms;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// 时间输入控件，由小时和分钟两个数字框组成，格式 HH:mm
    /// </summary>
    public class FlatTimeBox : Panel
    {
        private readonly FlatNumberBox hourBox;
        private readonly FlatNumberBox minuteBox;
        private readonly Label separatorLabel;

        private bool isUpdating = false;
        private bool allowEmpty = true;

        /// <summary>时间值变化时触发</summary>
        public event EventHandler? ValueChanged;

        /// <summary>
        /// 构造函数，初始化小时分钟输入框和分隔符
        /// </summary>
        public FlatTimeBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            BackColor = Color.Transparent;

            hourBox = new FlatNumberBox { Minimum = 0, Maximum = 23, Value = 0 };
            hourBox.ValueChanged += (s, e) => OnChanged();
            Controls.Add(hourBox);

            minuteBox = new FlatNumberBox { Minimum = 0, Maximum = 59, Value = 0 };
            minuteBox.ValueChanged += (s, e) => OnChanged();
            Controls.Add(minuteBox);

            separatorLabel = new Label
            {
                Text = ":",
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.Colors.TextPrimary,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
            };
            Controls.Add(separatorLabel);

            Height = 32;
            LayoutChildren();
        }

        /// <summary>时间值，格式 HH:mm，允许空时返回空字符串</summary>
        public string Value
        {
            get
            {
                if (allowEmpty && hourBox.Value == 0 && minuteBox.Value == 0)
                    return "";
                return $"{hourBox.Value:D2}:{minuteBox.Value:D2}";
            }
            set
            {
                isUpdating = true;
                try
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        hourBox.Value = 0;
                        minuteBox.Value = 0;
                        return;
                    }
                    if (TimeSpan.TryParse(value, out var timeSpan))
                    {
                        hourBox.Value = timeSpan.Hours;
                        minuteBox.Value = timeSpan.Minutes;
                    }
                }
                finally { isUpdating = false; }
            }
        }

        /// <summary>是否允许空值</summary>
        public bool AllowEmpty
        {
            get => allowEmpty;
            set => allowEmpty = value;
        }

        /// <summary>值变化时触发外部事件</summary>
        private void OnChanged()
        {
            if (isUpdating) return;
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>尺寸变化时重新布局子控件</summary>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutChildren();
        }

        /// <summary>布局小时分钟输入框和分隔符</summary>
        private void LayoutChildren()
        {
            const int SeparatorWidth = 16;
            int boxWidth = Math.Max(40, (Width - SeparatorWidth) / 2);
            const int boxHeight = 32;
            int y = Math.Max(0, (Height - boxHeight) / 2);

            hourBox.SetBounds(0, y, boxWidth, boxHeight);
            separatorLabel.SetBounds(boxWidth, y, SeparatorWidth, boxHeight);
            minuteBox.SetBounds(boxWidth + SeparatorWidth, y, boxWidth, boxHeight);
        }
    }
}