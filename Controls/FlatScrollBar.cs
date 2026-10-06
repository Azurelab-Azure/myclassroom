using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// 自绘垂直滚动条。宽度 8px，hover 时滑块变宽变深，静止 1 秒后淡出。
    /// 主题切换由顶层 Form1 统一触发 Invalidate。
    /// </summary>
    public class FlatScrollBar : Control
    {
        private int _min = 0;
        private int _max = 100;
        private int _value = 0;
        private int _largeChange = 10;
        private int _smallChange = 1;

        private bool _hover;
        private bool _dragging;
        private int _dragStartY;
        private int _dragStartValue;

        private readonly System.Windows.Forms.Timer _fadeTimer;
        private int _idleMs = 0;
        private bool _visible = true;

        public event EventHandler? ValueChanged;

        public FlatScrollBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Width = 8;

            _fadeTimer = new System.Windows.Forms.Timer { Interval = 100 };
            _fadeTimer.Tick += (s, e) =>
            {
                if (!_hover && !_dragging)
                {
                    _idleMs += 100;
                    if (_idleMs >= 1000 && _visible)
                    {
                        _visible = false;
                        Invalidate();
                    }
                }
            };
            _fadeTimer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _fadeTimer?.Dispose();
            base.Dispose(disposing);
        }

        // =====================================================
        // 属性
        // =====================================================
        public int Minimum
        {
            get => _min;
            set { _min = value; Invalidate(); }
        }

        public int Maximum
        {
            get => _max;
            set { _max = value; Invalidate(); }
        }

        public int Value
        {
            get => _value;
            set
            {
                int v = Math.Max(_min, Math.Min(_max - _largeChange + 1, value));
                if (v != _value)
                {
                    _value = v;
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        public int LargeChange
        {
            get => _largeChange;
            set { _largeChange = Math.Max(1, value); Invalidate(); }
        }

        public int SmallChange
        {
            get => _smallChange;
            set { _smallChange = Math.Max(1, value); }
        }

        /// <summary>滚动时调用，重置淡出计时</summary>
        public void Wake()
        {
            _idleMs = 0;
            if (!_visible) { _visible = true; Invalidate(); }
        }

        // =====================================================
        // 鼠标
        // =====================================================
        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            _visible = true;
            _idleMs = 0;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            _idleMs = 0;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            _visible = true;
            _idleMs = 0;

            var thumb = GetThumbRect();
            if (thumb.Contains(e.Location))
            {
                _dragging = true;
                _dragStartY = e.Y;
                _dragStartValue = _value;
            }
            else
            {
                // 点轨道翻页
                if (e.Y < thumb.Y) Value -= _largeChange;
                else Value += _largeChange;
            }
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_dragging)
            {
                var thumb = GetThumbRect();
                int trackLen = Height - thumb.Height;
                if (trackLen <= 0) trackLen = 1;

                int deltaY = e.Y - _dragStartY;
                int range = _max - _min - _largeChange + 1;
                if (range <= 0) range = 1;

                int deltaValue = (int)Math.Round((double)deltaY / trackLen * range);
                Value = _dragStartValue + deltaValue;
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_dragging)
            {
                _dragging = false;
                _idleMs = 0;
                Invalidate();
            }
            base.OnMouseUp(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            Value -= Math.Sign(e.Delta) * _smallChange * 3;
            Wake();
            base.OnMouseWheel(e);
        }

        // =====================================================
        // 绘制
        // =====================================================
        protected override void OnPaint(PaintEventArgs e)
        {
            if (!_visible && !_hover && !_dragging) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var colors = AppTheme.Colors;
            var thumb = GetThumbRect();
            int thumbWidth = (_hover || _dragging) ? 12 : 8;

            int x = (Width - thumbWidth) / 2;
            var drawRect = new Rectangle(x, thumb.Y, thumbWidth, thumb.Height);

            Color color = (_hover || _dragging) ? colors.ScrollThumbHover : colors.ScrollThumb;

            using var brush = new SolidBrush(color);
            using var path = GraphicsExtensions.GetRoundPath(drawRect, thumbWidth / 2);
            g.FillPath(brush, path);
        }

        // =====================================================
        // 计算
        // =====================================================
        private Rectangle GetThumbRect()
        {
            int range = _max - _min;
            if (range <= 0) return new Rectangle(0, 0, Width, Height);

            double visibleRatio = (double)_largeChange / (range + _largeChange);
            int thumbHeight = Math.Max(20, (int)(Height * visibleRatio));
            if (thumbHeight > Height) thumbHeight = Height;

            double valueRatio = (double)(_value - _min) / range;
            int thumbY = (int)((Height - thumbHeight) * valueRatio);

            return new Rectangle(0, thumbY, Width, thumbHeight);
        }
    }
}