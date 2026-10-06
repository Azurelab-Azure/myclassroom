using System;
using System.Drawing;
using System.Windows.Forms;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// 时间输入控件：两个数字框（时 0~23）+（分 0~59）。
    /// 显示格式：24 小时制 "HH:mm"。
    /// 主题切换由顶层 Form1 统一触发 Invalidate。
    /// </summary>
    public class FlatTimeBox : Panel
    {
        private readonly FlatNumberBox _hourBox;
        private readonly FlatNumberBox _minBox;
        private readonly Label _sep;

        private bool _updating = false;
        private bool _allowEmpty = true;

        public event EventHandler? ValueChanged;

        public FlatTimeBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            BackColor = Color.Transparent;

            _hourBox = new FlatNumberBox { Minimum = 0, Maximum = 23, Value = 0 };
            _hourBox.ValueChanged += (s, e) => OnChanged();
            Controls.Add(_hourBox);

            _minBox = new FlatNumberBox { Minimum = 0, Maximum = 59, Value = 0 };
            _minBox.ValueChanged += (s, e) => OnChanged();
            Controls.Add(_minBox);

            _sep = new Label
            {
                Text = ":",
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.Colors.TextPrimary,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
            };
            Controls.Add(_sep);

            Height = 32;
            LayoutChildren();
        }

        // =====================================================
        // 属性
        // =====================================================
        /// <summary>"HH:mm" 格式，空时为 ""</summary>
        public string Value
        {
            get
            {
                if (_allowEmpty && _hourBox.Value == 0 && _minBox.Value == 0)
                    return "";
                return $"{_hourBox.Value:D2}:{_minBox.Value:D2}";
            }
            set
            {
                _updating = true;
                try
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        _hourBox.Value = 0;
                        _minBox.Value = 0;
                        return;
                    }
                    if (TimeSpan.TryParse(value, out var ts))
                    {
                        _hourBox.Value = ts.Hours;
                        _minBox.Value = ts.Minutes;
                    }
                }
                finally { _updating = false; }
            }
        }

        public bool AllowEmpty
        {
            get => _allowEmpty;
            set => _allowEmpty = value;
        }

        private void OnChanged()
        {
            if (_updating) return;
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }

        // =====================================================
        // 布局
        // =====================================================
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutChildren();
        }

        private void LayoutChildren()
        {
            const int sepW = 16;
            int boxW = Math.Max(40, (Width - sepW) / 2);
            const int h = 32;
            int y = Math.Max(0, (Height - h) / 2);

            _hourBox.SetBounds(0, y, boxW, h);
            _sep.SetBounds(boxW, y, sepW, h);
            _minBox.SetBounds(boxW + sepW, y, boxW, h);
        }
    }
}