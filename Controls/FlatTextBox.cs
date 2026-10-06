using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// 自绘外框 + 内嵌原生 TextBox 的输入框。
    /// 说明：内嵌 TextBox 的 BackColor/ForeColor 是缓存的，主题切换需要订阅 ThemeChanged 重新设置。
    /// </summary>
    public class FlatTextBox : Control
    {
        private readonly TextBox _inner;
        private bool _focused;
        private bool _hover;
        private string _placeholder = "";

        private int _cornerRadius = 6;
        private const int PadH = 10;

        public FlatTextBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;

            _inner = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = AppTheme.Colors.CardBg,
                ForeColor = AppTheme.Colors.TextPrimary,
                Font = AppTheme.BodyFont,
            };
            _inner.GotFocus += (s, e) => { _focused = true; Invalidate(); };
            _inner.LostFocus += (s, e) => { _focused = false; Invalidate(); };
            _inner.TextChanged += (s, e) => { OnTextChanged(EventArgs.Empty); Invalidate(); };
            _inner.KeyDown += (s, e) => OnKeyDown(e);
            Controls.Add(_inner);

            AppTheme.ThemeChanged += ApplyInnerTheme;

            Size = new Size(200, 32);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) AppTheme.ThemeChanged -= ApplyInnerTheme;
            base.Dispose(disposing);
        }

        /// <summary>内嵌 TextBox 的颜色是缓存的，主题切换需要重新设置</summary>
        private void ApplyInnerTheme()
        {
            _inner.BackColor = AppTheme.Colors.CardBg;
            _inner.ForeColor = AppTheme.Colors.TextPrimary;
            _inner.Font = AppTheme.BodyFont;
            Invalidate();
        }

        // =====================================================
        // 属性
        // =====================================================
        public override string Text
        {
            get => _inner.Text;
            set => _inner.Text = value ?? "";
        }

        public string Placeholder
        {
            get => _placeholder;
            set { _placeholder = value ?? ""; Invalidate(); }
        }

        public char PasswordChar
        {
            get => _inner.PasswordChar;
            set => _inner.PasswordChar = value;
        }

        public bool ReadOnly
        {
            get => _inner.ReadOnly;
            set => _inner.ReadOnly = value;
        }

        public int CornerRadius
        {
            get => _cornerRadius;
            set { _cornerRadius = value; Invalidate(); }
        }

        public TextBox Inner => _inner;

        // =====================================================
        // 尺寸
        // =====================================================
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutInner();
        }

        private void LayoutInner()
        {
            int h = _inner.PreferredHeight;
            int y = (Height - h) / 2;
            _inner.SetBounds(PadH, y, Math.Max(1, Width - PadH * 2), h);
        }

        // =====================================================
        // 鼠标
        // =====================================================
        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true; Invalidate(); base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false; Invalidate(); base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            _inner.Focus();
            base.OnMouseDown(e);
        }

        // =====================================================
        // 绘制
        // =====================================================
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var colors = AppTheme.Colors;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            using (var path = GraphicsExtensions.GetRoundPath(rect, _cornerRadius))
            {
                using (var bg = new SolidBrush(colors.CardBg))
                    g.FillPath(bg, path);

                Color border = _focused ? colors.Accent
                             : _hover ? colors.TextSecondary
                             : colors.ButtonBorder;
                float bw = _focused ? 2f : 1f;

                using var pen = new Pen(border, bw);
                g.DrawPath(pen, path);
            }

            // Placeholder
            if (string.IsNullOrEmpty(_inner.Text) && !string.IsNullOrEmpty(_placeholder) && !_focused)
            {
                var textRect = new Rectangle(PadH, 0, Width - PadH * 2, Height);
                TextRenderer.DrawText(g, _placeholder, AppTheme.BodyFont, textRect,
                    colors.TextDisabled,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
        }
    }
}