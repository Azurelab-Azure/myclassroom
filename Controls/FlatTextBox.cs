using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CourseApp.Theme;

namespace CourseApp.Controls
{
    /// <summary>
    /// 自绘外框并内嵌原生 TextBox 的输入框，内嵌 TextBox 颜色随主题变化
    /// </summary>
    public class FlatTextBox : Control
    {
        private readonly TextBox innerTextBox;
        private bool isFocused;
        private bool isHover;
        private string placeholder = "";

        private int cornerRadius = 6;
        private const int HorizontalPadding = 10;

        /// <summary>
        /// 构造函数，初始化输入框和内嵌文本框
        /// </summary>
        public FlatTextBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;

            innerTextBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = AppTheme.Colors.CardBg,
                ForeColor = AppTheme.Colors.TextPrimary,
                Font = AppTheme.BodyFont,
            };
            innerTextBox.GotFocus += (s, e) => { isFocused = true; Invalidate(); };
            innerTextBox.LostFocus += (s, e) => { isFocused = false; Invalidate(); };
            innerTextBox.TextChanged += (s, e) => { OnTextChanged(EventArgs.Empty); Invalidate(); };
            innerTextBox.KeyDown += (s, e) => OnKeyDown(e);
            Controls.Add(innerTextBox);

            AppTheme.ThemeChanged += ApplyInnerTheme;

            Size = new Size(200, 32);
        }

        /// <summary>释放资源并取消主题事件订阅</summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing) AppTheme.ThemeChanged -= ApplyInnerTheme;
            base.Dispose(disposing);
        }

        /// <summary>内嵌 TextBox 的颜色是缓存的，主题切换时需重新设置</summary>
        private void ApplyInnerTheme()
        {
            innerTextBox.BackColor = AppTheme.Colors.CardBg;
            innerTextBox.ForeColor = AppTheme.Colors.TextPrimary;
            innerTextBox.Font = AppTheme.BodyFont;
            Invalidate();
        }

        /// <summary>文本内容</summary>
        public override string Text
        {
            get => innerTextBox.Text;
            set => innerTextBox.Text = value ?? "";
        }

        /// <summary>占位提示文本</summary>
        public string Placeholder
        {
            get => placeholder;
            set { placeholder = value ?? ""; Invalidate(); }
        }

        /// <summary>密码字符</summary>
        public char PasswordChar
        {
            get => innerTextBox.PasswordChar;
            set => innerTextBox.PasswordChar = value;
        }

        /// <summary>是否只读</summary>
        public bool ReadOnly
        {
            get => innerTextBox.ReadOnly;
            set => innerTextBox.ReadOnly = value;
        }

        /// <summary>圆角半径</summary>
        public int CornerRadius
        {
            get => cornerRadius;
            set { cornerRadius = value; Invalidate(); }
        }

        /// <summary>内嵌原生 TextBox 引用</summary>
        public TextBox Inner => innerTextBox;

        /// <summary>尺寸变化时重新布局内嵌文本框</summary>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutInner();
        }

        /// <summary>布局内嵌文本框位置</summary>
        private void LayoutInner()
        {
            int textBoxHeight = innerTextBox.PreferredHeight;
            int y = (Height - textBoxHeight) / 2;
            innerTextBox.SetBounds(HorizontalPadding, y,
                Math.Max(1, Width - HorizontalPadding * 2), textBoxHeight);
        }

        /// <summary>鼠标进入时标记悬停状态</summary>
        protected override void OnMouseEnter(EventArgs e)
        {
            isHover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        /// <summary>鼠标离开时清除悬停状态</summary>
        protected override void OnMouseLeave(EventArgs e)
        {
            isHover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        /// <summary>鼠标按下时让内嵌文本框获得焦点</summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            innerTextBox.Focus();
            base.OnMouseDown(e);
        }

        /// <summary>绘制输入框外框和占位文本</summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var colors = AppTheme.Colors;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            using (var path = GraphicsExtensions.GetRoundPath(rect, cornerRadius))
            {
                using (var background = new SolidBrush(colors.CardBg))
                    g.FillPath(background, path);

                Color borderColor = isFocused ? colors.Accent
                                  : isHover ? colors.TextSecondary
                                            : colors.ButtonBorder;
                float borderWidth = isFocused ? 2f : 1f;

                using var pen = new Pen(borderColor, borderWidth);
                g.DrawPath(pen, path);
            }

            if (string.IsNullOrEmpty(innerTextBox.Text) &&
                !string.IsNullOrEmpty(placeholder) && !isFocused)
            {
                var textRect = new Rectangle(HorizontalPadding, 0,
                    Width - HorizontalPadding * 2, Height);
                TextRenderer.DrawText(g, placeholder, AppTheme.BodyFont, textRect,
                    colors.TextDisabled,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix);
            }
        }
    }
}