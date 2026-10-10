using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// 自绘垂直滚动条，悬停时变宽变深，静止后淡出
    /// </summary>
    public class FlatScrollBar : Control
    {
        private int minimumValue = 0;
        private int maximumValue = 100;
        private int currentValue = 0;
        private int largeChange = 10;
        private int smallChange = 1;

        private bool isHover;
        private bool isDragging;
        private int dragStartY;
        private int dragStartValue;

        private readonly System.Windows.Forms.Timer fadeTimer;
        private int idleMilliseconds = 0;
        private bool isVisible = true;

        /// <summary>值变化时触发</summary>
        public event EventHandler? ValueChanged;

        /// <summary>
        /// 构造函数，初始化滚动条和淡出计时器
        /// </summary>
        public FlatScrollBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Width = 8;

            fadeTimer = new System.Windows.Forms.Timer { Interval = 100 };
            fadeTimer.Tick += (s, e) =>
            {
                if (!isHover && !isDragging)
                {
                    idleMilliseconds += 100;
                    if (idleMilliseconds >= 1000 && isVisible)
                    {
                        isVisible = false;
                        Invalidate();
                    }
                }
            };
            fadeTimer.Start();
        }

        /// <summary>释放资源</summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing) fadeTimer?.Dispose();
            base.Dispose(disposing);
        }

        /// <summary>最小值</summary>
        public int Minimum
        {
            get => minimumValue;
            set { minimumValue = value; Invalidate(); }
        }

        /// <summary>最大值</summary>
        public int Maximum
        {
            get => maximumValue;
            set { maximumValue = value; Invalidate(); }
        }

        /// <summary>当前值</summary>
        public int Value
        {
            get => currentValue;
            set
            {
                int clampedValue = Math.Max(minimumValue, Math.Min(maximumValue - largeChange + 1, value));
                if (clampedValue != currentValue)
                {
                    currentValue = clampedValue;
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        /// <summary>翻页步长</summary>
        public int LargeChange
        {
            get => largeChange;
            set { largeChange = Math.Max(1, value); Invalidate(); }
        }

        /// <summary>单步滚动量</summary>
        public int SmallChange
        {
            get => smallChange;
            set { smallChange = Math.Max(1, value); }
        }

        /// <summary>唤醒滚动条，重置淡出计时</summary>
        public void Wake()
        {
            idleMilliseconds = 0;
            if (!isVisible)
            {
                isVisible = true;
                Invalidate();
            }
        }

        /// <summary>鼠标进入时显示滚动条</summary>
        protected override void OnMouseEnter(EventArgs e)
        {
            isHover = true;
            isVisible = true;
            idleMilliseconds = 0;
            Invalidate();
            base.OnMouseEnter(e);
        }

        /// <summary>鼠标离开时重置计时</summary>
        protected override void OnMouseLeave(EventArgs e)
        {
            isHover = false;
            idleMilliseconds = 0;
            Invalidate();
            base.OnMouseLeave(e);
        }

        /// <summary>鼠标按下时开始拖动或翻页</summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            isVisible = true;
            idleMilliseconds = 0;

            var thumb = GetThumbRect();
            if (thumb.Contains(e.Location))
            {
                isDragging = true;
                dragStartY = e.Y;
                dragStartValue = currentValue;
            }
            else
            {
                if (e.Y < thumb.Y) Value -= largeChange;
                else Value += largeChange;
            }
            Invalidate();
            base.OnMouseDown(e);
        }

        /// <summary>鼠标移动时拖动滑块</summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (isDragging)
            {
                var thumb = GetThumbRect();
                int trackLength = Height - thumb.Height;
                if (trackLength <= 0) trackLength = 1;

                int deltaY = e.Y - dragStartY;
                int range = maximumValue - minimumValue - largeChange + 1;
                if (range <= 0) range = 1;

                int deltaValue = (int)Math.Round((double)deltaY / trackLength * range);
                Value = dragStartValue + deltaValue;
            }
            base.OnMouseMove(e);
        }

        /// <summary>鼠标抬起时结束拖动</summary>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                idleMilliseconds = 0;
                Invalidate();
            }
            base.OnMouseUp(e);
        }

        /// <summary>鼠标滚轮滚动</summary>
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            Value -= Math.Sign(e.Delta) * smallChange * 3;
            Wake();
            base.OnMouseWheel(e);
        }

        /// <summary>绘制滚动条滑块</summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            if (!isVisible && !isHover && !isDragging) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var colors = AppTheme.Colors;
            var thumb = GetThumbRect();
            int thumbWidth = (isHover || isDragging) ? 12 : 8;

            int x = (Width - thumbWidth) / 2;
            var drawRect = new Rectangle(x, thumb.Y, thumbWidth, thumb.Height);

            Color thumbColor = (isHover || isDragging) ? colors.ScrollThumbHover : colors.ScrollThumb;

            using var brush = new SolidBrush(thumbColor);
            using var path = GraphicsExtensions.GetRoundPath(drawRect, thumbWidth / 2);
            g.FillPath(brush, path);
        }

        /// <summary>计算滑块矩形区域</summary>
        private Rectangle GetThumbRect()
        {
            int range = maximumValue - minimumValue;
            if (range <= 0) return new Rectangle(0, 0, Width, Height);

            double visibleRatio = (double)largeChange / (range + largeChange);
            int thumbHeight = Math.Max(20, (int)(Height * visibleRatio));
            if (thumbHeight > Height) thumbHeight = Height;

            double valueRatio = (double)(currentValue - minimumValue) / range;
            int thumbY = (int)((Height - thumbHeight) * valueRatio);

            return new Rectangle(0, thumbY, Width, thumbHeight);
        }
    }
}