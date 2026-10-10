using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// WinUI 3 风格开关。
    /// </summary>
    public class WinUI3Toggle : Control
    {
        private bool _checked;
        private float _anim = 0f;
        private readonly System.Windows.Forms.Timer _timer;

        public event EventHandler? CheckedChanged;

        public bool Checked
        {
            get => _checked;
            set
            {
                if (_checked == value) return;
                _checked = value;
                _timer.Start();
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public WinUI3Toggle()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(44, 24);
            Cursor = Cursors.Hand;

            _timer = new System.Windows.Forms.Timer { Interval = WinUI3Tokens.AnimInterval };
            _timer.Tick += (s, e) =>
            {
                float target = _checked ? 1f : 0f;
                _anim += (target - _anim) * WinUI3Tokens.AnimEase;
                if (Math.Abs(_anim - target) < 0.01f) { _anim = target; _timer.Stop(); }
                Invalidate();
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _timer?.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) Checked = !Checked;
            base.OnMouseDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var colors = AppTheme.Colors;

            int h = Height;
            int w = Width;
            var rect = new Rectangle(0, 0, w - 1, h - 1);

            Color track = Blend(colors.Divider, colors.Accent, _anim);
            using (var path = GraphicsExtensions.GetRoundPath(rect, h / 2))
            using (var brush = new SolidBrush(track))
                g.FillPath(brush, path);

            int knobSize = h - 6;
            int knobX = (int)(3 + _anim * (w - knobSize - 6));
            var knobRect = new Rectangle(knobX, 3, knobSize, knobSize);

            using var knobPath = GraphicsExtensions.GetRoundPath(knobRect, knobSize / 2);
            using var knobBrush = new SolidBrush(Color.White);
            g.FillPath(knobBrush, knobPath);
        }

        private static Color Blend(Color a, Color b, float t)
        {
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * t),
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }
    }
}