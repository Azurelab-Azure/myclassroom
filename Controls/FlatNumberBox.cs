using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// 数字输入框。左侧内嵌 FlatTextBox，右侧上下两个按钮，长按加速。
    /// 主题切换由顶层 Form1 统一触发 Invalidate。
    /// </summary>
    public class FlatNumberBox : Control
    {
        private readonly FlatTextBox _input;
        private readonly FlatIconButton _btnUp;
        private readonly FlatIconButton _btnDown;

        private int _min = 0;
        private int _max = 100;
        private int _value = 0;

        private int _repeatCount = 0;
        private int _repeatDir = 0;
        private readonly System.Windows.Forms.Timer _repeatTimer;

        public event EventHandler? ValueChanged;

        public FlatNumberBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;

            _input = new FlatTextBox { CornerRadius = 6 };
            _input.TextChanged += (s, e) => OnInputChanged();
            Controls.Add(_input);

            _btnUp = new FlatIconButton { Icon = Icons.ChevronUp, IconSize = 10, CornerRadius = 2 };
            _btnUp.MouseDown += (s, e) => StartRepeat(+1);
            _btnUp.MouseUp += (s, e) => StopRepeat();
            _btnUp.MouseLeave += (s, e) => StopRepeat();
            Controls.Add(_btnUp);

            _btnDown = new FlatIconButton { Icon = Icons.ChevronDown, IconSize = 10, CornerRadius = 2 };
            _btnDown.MouseDown += (s, e) => StartRepeat(-1);
            _btnDown.MouseUp += (s, e) => StopRepeat();
            _btnDown.MouseLeave += (s, e) => StopRepeat();
            Controls.Add(_btnDown);

            _repeatTimer = new System.Windows.Forms.Timer { Interval = 100 };
            _repeatTimer.Tick += (s, e) =>
            {
                _repeatCount++;
                if (_repeatCount >= 4) StepValue(_repeatDir);
            };

            Size = new Size(100, 32);
            UpdateInputText();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _repeatTimer?.Dispose();
            base.Dispose(disposing);
        }

        // =====================================================
        // 属性
        // =====================================================
        public int Minimum
        {
            get => _min;
            set { _min = value; ClampValue(); }
        }

        public int Maximum
        {
            get => _max;
            set { _max = value; ClampValue(); }
        }

        public int Value
        {
            get => _value;
            set
            {
                int v = Math.Max(_min, Math.Min(_max, value));
                if (v != _value)
                {
                    _value = v;
                    UpdateInputText();
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                }
            }
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
            const int btnArea = 20;
            const int pad = 2;

            _input.SetBounds(pad, pad, Width - btnArea - pad * 2, Height - pad * 2);

            int bx = Width - btnArea - pad;
            int halfH = (Height - pad * 3) / 2;

            _btnUp.SetBounds(bx, pad, btnArea, halfH);
            _btnDown.SetBounds(bx, pad * 2 + halfH, btnArea, halfH);
        }

        // =====================================================
        // 输入
        // =====================================================
        private void OnInputChanged()
        {
            var s = _input.Text;
            string filtered = "";
            foreach (var c in s) if (char.IsDigit(c)) filtered += c;
            if (filtered != s) _input.Text = filtered;

            if (int.TryParse(filtered, out var v))
            {
                v = Math.Max(_min, Math.Min(_max, v));
                if (v != _value)
                {
                    _value = v;
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void UpdateInputText() => _input.Text = _value.ToString();

        private void ClampValue()
        {
            if (_value < _min) _value = _min;
            if (_value > _max) _value = _max;
            UpdateInputText();
        }

        // =====================================================
        // 加减
        // =====================================================
        private void StartRepeat(int dir)
        {
            _repeatDir = dir;
            _repeatCount = 0;
            StepValue(dir);
            _repeatTimer.Start();
        }

        private void StopRepeat()
        {
            _repeatTimer.Stop();
            _repeatDir = 0;
        }

        private void StepValue(int dir) => Value += dir;

        // =====================================================
        // 绘制
        // =====================================================
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var colors = AppTheme.Colors;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            using var path = GraphicsExtensions.GetRoundPath(rect, 6);
            using var bg = new SolidBrush(colors.CardBg);
            g.FillPath(bg, path);

            using var pen = new Pen(colors.ButtonBorder, 1f);
            g.DrawPath(pen, path);
        }
    }
}