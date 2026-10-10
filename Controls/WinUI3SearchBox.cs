using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// WinUI 3 风格搜索框：图标 + 输入 + 清除。
    /// </summary>
    public class WinUI3SearchBox : Control
    {
        private readonly TextBox _input;
        private bool _hover;
        private bool _focused;

        public new event EventHandler? TextChanged;

        public override string Text
        {
            get => _input.Text ?? "";
            set => _input.Text = value ?? "";
        }

        public string Placeholder { get; set; } = "";

        public WinUI3SearchBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;

            // ★ 先创建 _input，避免 Height 触发的 OnResize → LayoutInput 空引用
            _input = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = AppTheme.Colors.CardBg,
                ForeColor = AppTheme.Colors.TextPrimary,
                Font = AppTheme.BodyFont,
            };
            _input.GotFocus += (s, e) => { _focused = true; Invalidate(); };
            _input.LostFocus += (s, e) => { _focused = false; Invalidate(); };
            _input.TextChanged += (s, e) => { TextChanged?.Invoke(this, EventArgs.Empty); Invalidate(); };
            Controls.Add(_input);

            // ★ 设置高度放在 _input 创建之后
            Height = WinUI3Tokens.ControlHeight;

            AppTheme.ThemeChanged += ApplyTheme;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) AppTheme.ThemeChanged -= ApplyTheme;
            base.Dispose(disposing);
        }

        private void ApplyTheme()
        {
            _input.BackColor = AppTheme.Colors.CardBg;
            _input.ForeColor = AppTheme.Colors.TextPrimary;
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutInput();
        }

        private void LayoutInput()
        {
            if (_input == null) return;   // ★ 双保险
            int h = _input.PreferredHeight;
            _input.SetBounds(34, (Height - h) / 2, Math.Max(1, Width - 34 - 30), h);
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _input.Focus(); base.OnMouseDown(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var colors = AppTheme.Colors;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = GraphicsExtensions.GetRoundPath(rect, WinUI3Tokens.ControlRadius))
            {
                using (var bg = new SolidBrush(colors.CardBg))
                    g.FillPath(bg, path);

                Color border = _focused ? colors.Accent : _hover ? colors.TextSecondary : colors.ButtonBorder;
                using var pen = new Pen(border, _focused ? 2f : 1f);
                g.DrawPath(pen, path);
            }

            var iconRect = new Rectangle(10, (Height - 16) / 2, 16, 16);
            IconRenderer.Draw(g, Icons.Search, iconRect, colors.TextSecondary, 16);

            if (string.IsNullOrEmpty(_input.Text) && !string.IsNullOrEmpty(Placeholder))
            {
                var phRect = new Rectangle(34, 0, Width - 64, Height);
                TextRenderer.DrawText(g, Placeholder, AppTheme.BodyFont, phRect, colors.TextDisabled,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }

            if (!string.IsNullOrEmpty(_input.Text))
            {
                var clearRect = new Rectangle(Width - 26, (Height - 16) / 2, 16, 16);
                IconRenderer.Draw(g, Icons.Close, clearRect, colors.TextSecondary, 12);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && !string.IsNullOrEmpty(_input.Text))
            {
                var clearRect = new Rectangle(Width - 26, (Height - 16) / 2, 16, 16);
                if (clearRect.Contains(e.Location))
                {
                    _input.Text = "";
                    return;
                }
            }
            base.OnMouseUp(e);
        }
    }
}