using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// 数字输入框，内嵌文本框和上下加减按钮，支持长按加速
    /// </summary>
    public class FlatNumberBox : Control
    {
        private readonly FlatTextBox inputBox;
        private readonly FlatIconButton upButton;
        private readonly FlatIconButton downButton;

        private int minimumValue = 0;
        private int maximumValue = 100;
        private int currentValue = 0;

        private int repeatCount = 0;
        private int repeatDirection = 0;
        private readonly System.Windows.Forms.Timer repeatTimer;

        /// <summary>数值变化时触发</summary>
        public event EventHandler? ValueChanged;

        /// <summary>
        /// 构造函数，初始化数字框和按钮
        /// </summary>
        public FlatNumberBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;

            inputBox = new FlatTextBox { CornerRadius = 6 };
            inputBox.TextChanged += (s, e) => OnInputChanged();
            Controls.Add(inputBox);

            upButton = new FlatIconButton { Icon = Icons.ChevronUp, IconSize = 10, CornerRadius = 2 };
            upButton.MouseDown += (s, e) => StartRepeat(+1);
            upButton.MouseUp += (s, e) => StopRepeat();
            upButton.MouseLeave += (s, e) => StopRepeat();
            Controls.Add(upButton);

            downButton = new FlatIconButton { Icon = Icons.ChevronDown, IconSize = 10, CornerRadius = 2 };
            downButton.MouseDown += (s, e) => StartRepeat(-1);
            downButton.MouseUp += (s, e) => StopRepeat();
            downButton.MouseLeave += (s, e) => StopRepeat();
            Controls.Add(downButton);

            repeatTimer = new System.Windows.Forms.Timer { Interval = 100 };
            repeatTimer.Tick += (s, e) =>
            {
                repeatCount++;
                if (repeatCount >= 4) StepValue(repeatDirection);
            };

            Size = new Size(100, 32);
            UpdateInputText();
        }

        /// <summary>释放资源</summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing) repeatTimer?.Dispose();
            base.Dispose(disposing);
        }

        /// <summary>最小值</summary>
        public int Minimum
        {
            get => minimumValue;
            set { minimumValue = value; ClampValue(); }
        }

        /// <summary>最大值</summary>
        public int Maximum
        {
            get => maximumValue;
            set { maximumValue = value; ClampValue(); }
        }

        /// <summary>当前值</summary>
        public int Value
        {
            get => currentValue;
            set
            {
                int newValue = Math.Max(minimumValue, Math.Min(maximumValue, value));
                if (newValue != currentValue)
                {
                    currentValue = newValue;
                    UpdateInputText();
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>尺寸变化时重新布局子控件</summary>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutChildren();
        }

        /// <summary>布局输入框和加减按钮</summary>
        private void LayoutChildren()
        {
            const int ButtonAreaWidth = 20;
            const int Padding = 2;

            inputBox.SetBounds(Padding, Padding, Width - ButtonAreaWidth - Padding * 2, Height - Padding * 2);

            int buttonX = Width - ButtonAreaWidth - Padding;
            int halfHeight = (Height - Padding * 3) / 2;

            upButton.SetBounds(buttonX, Padding, ButtonAreaWidth, halfHeight);
            downButton.SetBounds(buttonX, Padding * 2 + halfHeight, ButtonAreaWidth, halfHeight);
        }

        /// <summary>输入内容变化时过滤非数字字符并校验范围</summary>
        private void OnInputChanged()
        {
            var rawText = inputBox.Text;
            string filtered = "";
            foreach (var c in rawText)
                if (char.IsDigit(c)) filtered += c;
            if (filtered != rawText) inputBox.Text = filtered;

            if (int.TryParse(filtered, out var parsedValue))
            {
                parsedValue = Math.Max(minimumValue, Math.Min(maximumValue, parsedValue));
                if (parsedValue != currentValue)
                {
                    currentValue = parsedValue;
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>更新输入框显示文本</summary>
        private void UpdateInputText() => inputBox.Text = currentValue.ToString();

        /// <summary>将当前值限制在范围内</summary>
        private void ClampValue()
        {
            if (currentValue < minimumValue) currentValue = minimumValue;
            if (currentValue > maximumValue) currentValue = maximumValue;
            UpdateInputText();
        }

        /// <summary>开始长按重复加减</summary>
        private void StartRepeat(int direction)
        {
            repeatDirection = direction;
            repeatCount = 0;
            StepValue(direction);
            repeatTimer.Start();
        }

        /// <summary>停止长按重复</summary>
        private void StopRepeat()
        {
            repeatTimer.Stop();
            repeatDirection = 0;
        }

        /// <summary>按方向步进数值</summary>
        private void StepValue(int direction) => Value += direction;

        /// <summary>绘制数字框外框</summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var colors = AppTheme.Colors;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            using var path = GraphicsExtensions.GetRoundPath(rect, 6);
            using var background = new SolidBrush(colors.CardBg);
            g.FillPath(background, path);

            using var pen = new Pen(colors.ButtonBorder, 1f);
            g.DrawPath(pen, path);
        }
    }
}