using System.Drawing;

namespace CourseApp.Theme
{
    /// <summary>
    /// 一套完整主题的所有颜色。
    /// 提供 4 种预设：Light（亮）/ Dark（暗）/ Clear（通透）/ Fresh（清新）。
    /// </summary>
    public class ThemeColors
    {
        // =====================================================
        // 窗口 / 表面
        // =====================================================
        public Color WindowBg;          // 主窗口背景
        public Color CardBg;            // 卡片背景（比 WindowBg 略亮）
        public Color CardBorder;        // 卡片边框

        // =====================================================
        // 文字
        // =====================================================
        public Color TextPrimary;       // 主要文字
        public Color TextSecondary;     // 次要文字（描述）
        public Color TextDisabled;      // 禁用文字

        // =====================================================
        // 强调色
        // =====================================================
        public Color Accent;
        public Color AccentHover;
        public Color AccentPressed;
        public Color AccentForeground;  // 强调色上的前景

        // =====================================================
        // 普通按钮（Secondary）
        // =====================================================
        public Color ButtonBg;
        public Color ButtonHover;
        public Color ButtonPressed;
        public Color ButtonBorder;
        public Color ButtonForeground;
        public Color ButtonHoverForeground;

        // =====================================================
        // 危险按钮（Danger）
        // =====================================================
        public Color Danger;
        public Color DangerHover;
        public Color DangerPressed;
        public Color DangerForeground;

        // =====================================================
        // 微妙按钮（Subtle，标题栏 / 工具栏）
        // =====================================================
        public Color SubtleHover;
        public Color SubtlePressed;
        public Color SubtleForeground;
        public Color SubtleHoverForeground;

        // =====================================================
        // 列表 / 表格
        // =====================================================
        public Color HoverBg;
        public Color SelectedBg;
        public Color SelectedHoverBg;
        public Color Divider;
        public Color GridHeaderBg;
        public Color GridStripeBg;

        // =====================================================
        // 滚动条
        // =====================================================
        public Color ScrollThumb;
        public Color ScrollThumbHover;

        // =====================================================
        // 遮罩
        // =====================================================
        public Color Overlay;

        // =====================================================
        // 亮色主题
        // =====================================================
        public static ThemeColors Light() => new ThemeColors
        {
            WindowBg = Color.FromArgb(0xF3, 0xF3, 0xF3),
            CardBg = Color.FromArgb(0xFF, 0xFF, 0xFF),
            CardBorder = Color.FromArgb(0xE5, 0xE5, 0xE5),

            TextPrimary = Color.FromArgb(0x1A, 0x1A, 0x1A),
            TextSecondary = Color.FromArgb(0x60, 0x60, 0x60),
            TextDisabled = Color.FromArgb(0xA0, 0xA0, 0xA0),

            Accent = Color.FromArgb(0x00, 0x78, 0xD4),
            AccentHover = Color.FromArgb(0x10, 0x6E, 0xBE),
            AccentPressed = Color.FromArgb(0x00, 0x5A, 0x9E),
            AccentForeground = Color.FromArgb(0xFF, 0xFF, 0xFF),

            ButtonBg = Color.FromArgb(0xFD, 0xFD, 0xFD),
            ButtonHover = Color.FromArgb(0xF0, 0xF0, 0xF0),
            ButtonPressed = Color.FromArgb(0xE0, 0xE0, 0xE0),
            ButtonBorder = Color.FromArgb(0xE0, 0xE0, 0xE0),
            ButtonForeground = Color.FromArgb(0x1A, 0x1A, 0x1A),
            ButtonHoverForeground = Color.FromArgb(0x1A, 0x1A, 0x1A),

            Danger = Color.FromArgb(0xD1, 0x34, 0x38),
            DangerHover = Color.FromArgb(0xE8, 0x11, 0x23),
            DangerPressed = Color.FromArgb(0xC4, 0x2B, 0x1C),
            DangerForeground = Color.FromArgb(0xFF, 0xFF, 0xFF),

            SubtleHover = Color.FromArgb(0xE8, 0xE8, 0xE8),
            SubtlePressed = Color.FromArgb(0xD8, 0xD8, 0xD8),
            SubtleForeground = Color.FromArgb(0x1A, 0x1A, 0x1A),
            SubtleHoverForeground = Color.FromArgb(0x1A, 0x1A, 0x1A),

            HoverBg = Color.FromArgb(0xF0, 0xF0, 0xF0),
            SelectedBg = Color.FromArgb(0xE5, 0xF1, 0xFB),
            SelectedHoverBg = Color.FromArgb(0xD6, 0xE9, 0xF8),
            Divider = Color.FromArgb(0xE6, 0xE6, 0xE6),
            GridHeaderBg = Color.FromArgb(0xFA, 0xFA, 0xFA),
            GridStripeBg = Color.FromArgb(0xF7, 0xF7, 0xF7),

            ScrollThumb = Color.FromArgb(0xC0, 0xC0, 0xC0),
            ScrollThumbHover = Color.FromArgb(0x80, 0x80, 0x80),

            Overlay = Color.FromArgb(80, 0, 0, 0),
        };

        // =====================================================
        // 暗色主题
        // =====================================================
        public static ThemeColors Dark() => new ThemeColors
        {
            WindowBg = Color.FromArgb(0x20, 0x20, 0x20),
            CardBg = Color.FromArgb(0x2C, 0x2C, 0x2C),
            CardBorder = Color.FromArgb(0x3D, 0x3D, 0x3D),

            TextPrimary = Color.FromArgb(0xFF, 0xFF, 0xFF),
            TextSecondary = Color.FromArgb(0xC5, 0xC5, 0xC5),
            TextDisabled = Color.FromArgb(0x7A, 0x7A, 0x7A),

            Accent = Color.FromArgb(0x60, 0xCD, 0xFF),
            AccentHover = Color.FromArgb(0x7B, 0xD8, 0xFF),
            AccentPressed = Color.FromArgb(0x4A, 0xB8, 0xE8),
            AccentForeground = Color.FromArgb(0x10, 0x10, 0x10),

            ButtonBg = Color.FromArgb(0x33, 0x33, 0x33),
            ButtonHover = Color.FromArgb(0x3D, 0x3D, 0x3D),
            ButtonPressed = Color.FromArgb(0x2A, 0x2A, 0x2A),
            ButtonBorder = Color.FromArgb(0x50, 0x50, 0x50),
            ButtonForeground = Color.FromArgb(0xFF, 0xFF, 0xFF),
            ButtonHoverForeground = Color.FromArgb(0xFF, 0xFF, 0xFF),

            Danger = Color.FromArgb(0xE8, 0x1B, 0x1B),
            DangerHover = Color.FromArgb(0xFF, 0x33, 0x33),
            DangerPressed = Color.FromArgb(0xC4, 0x14, 0x14),
            DangerForeground = Color.FromArgb(0xFF, 0xFF, 0xFF),

            SubtleHover = Color.FromArgb(0x3A, 0x3A, 0x3A),
            SubtlePressed = Color.FromArgb(0x2A, 0x2A, 0x2A),
            SubtleForeground = Color.FromArgb(0xFF, 0xFF, 0xFF),
            SubtleHoverForeground = Color.FromArgb(0xFF, 0xFF, 0xFF),

            HoverBg = Color.FromArgb(0x3A, 0x3A, 0x3A),
            SelectedBg = Color.FromArgb(0x26, 0x48, 0x6B),
            SelectedHoverBg = Color.FromArgb(0x2E, 0x54, 0x7C),
            Divider = Color.FromArgb(0x3D, 0x3D, 0x3D),
            GridHeaderBg = Color.FromArgb(0x28, 0x28, 0x28),
            GridStripeBg = Color.FromArgb(0x25, 0x25, 0x25),

            ScrollThumb = Color.FromArgb(0x60, 0x60, 0x60),
            ScrollThumbHover = Color.FromArgb(0x80, 0x80, 0x80),

            Overlay = Color.FromArgb(120, 0, 0, 0),
        };

        // =====================================================
        // 通透主题（Clear）
        // 半透明、灰白基调，适合大屏 / 投影
        // =====================================================
        public static ThemeColors Clear() => new ThemeColors
        {
            WindowBg = Color.FromArgb(0xEC, 0xF1, 0xF8),
            CardBg = Color.FromArgb(0xF8, 0xFB, 0xFF),
            CardBorder = Color.FromArgb(0xCC, 0xD9, 0xE8),

            TextPrimary = Color.FromArgb(0x1F, 0x2A, 0x3D),
            TextSecondary = Color.FromArgb(0x5A, 0x6B, 0x82),
            TextDisabled = Color.FromArgb(0xA0, 0xAD, 0xBD),

            Accent = Color.FromArgb(0x2E, 0x86, 0xE8),
            AccentHover = Color.FromArgb(0x46, 0x95, 0xEC),
            AccentPressed = Color.FromArgb(0x1F, 0x6F, 0xC4),
            AccentForeground = Color.FromArgb(0xFF, 0xFF, 0xFF),

            ButtonBg = Color.FromArgb(0xF2, 0xF7, 0xFD),
            ButtonHover = Color.FromArgb(0xE4, 0xED, 0xF9),
            ButtonPressed = Color.FromArgb(0xD4, 0xE2, 0xF4),
            ButtonBorder = Color.FromArgb(0xC8, 0xD8, 0xEC),
            ButtonForeground = Color.FromArgb(0x1F, 0x2A, 0x3D),
            ButtonHoverForeground = Color.FromArgb(0x1F, 0x2A, 0x3D),

            Danger = Color.FromArgb(0xE0, 0x4A, 0x4A),
            DangerHover = Color.FromArgb(0xEE, 0x60, 0x60),
            DangerPressed = Color.FromArgb(0xC8, 0x3A, 0x3A),
            DangerForeground = Color.FromArgb(0xFF, 0xFF, 0xFF),

            SubtleHover = Color.FromArgb(0xDE, 0xE8, 0xF4),
            SubtlePressed = Color.FromArgb(0xCC, 0xDA, 0xEA),
            SubtleForeground = Color.FromArgb(0x1F, 0x2A, 0x3D),
            SubtleHoverForeground = Color.FromArgb(0x1F, 0x2A, 0x3D),

            HoverBg = Color.FromArgb(0xE4, 0xED, 0xF9),
            SelectedBg = Color.FromArgb(0xCF, 0xE3, 0xFA),
            SelectedHoverBg = Color.FromArgb(0xBC, 0xD8, 0xF6),
            Divider = Color.FromArgb(0xD8, 0xE2, 0xEE),
            GridHeaderBg = Color.FromArgb(0xF0, 0xF5, 0xFC),
            GridStripeBg = Color.FromArgb(0xEA, 0xF1, 0xFA),

            ScrollThumb = Color.FromArgb(0xB0, 0xC0, 0xD4),
            ScrollThumbHover = Color.FromArgb(0x8A, 0xA0, 0xBA),

            Overlay = Color.FromArgb(60, 30, 50, 80),
        };

        // =====================================================
        // 清新主题（Fresh）
        // 浅绿 / 薄荷基调，护眼
        // =====================================================
        public static ThemeColors Fresh() => new ThemeColors
        {
            WindowBg = Color.FromArgb(0xF0, 0xF7, 0xF2),
            CardBg = Color.FromArgb(0xFB, 0xFF, 0xFC),
            CardBorder = Color.FromArgb(0xD2, 0xE8, 0xD8),

            TextPrimary = Color.FromArgb(0x1A, 0x33, 0x26),
            TextSecondary = Color.FromArgb(0x55, 0x7A, 0x62),
            TextDisabled = Color.FromArgb(0xA0, 0xBA, 0xA8),

            Accent = Color.FromArgb(0x2E, 0xA0, 0x5E),
            AccentHover = Color.FromArgb(0x38, 0xB8, 0x6C),
            AccentPressed = Color.FromArgb(0x22, 0x88, 0x4C),
            AccentForeground = Color.FromArgb(0xFF, 0xFF, 0xFF),

            ButtonBg = Color.FromArgb(0xF5, 0xFB, 0xF7),
            ButtonHover = Color.FromArgb(0xE6, 0xF4, 0xEC),
            ButtonPressed = Color.FromArgb(0xD6, 0xEB, 0xDE),
            ButtonBorder = Color.FromArgb(0xCA, 0xE2, 0xD2),
            ButtonForeground = Color.FromArgb(0x1A, 0x33, 0x26),
            ButtonHoverForeground = Color.FromArgb(0x1A, 0x33, 0x26),

            Danger = Color.FromArgb(0xE0, 0x54, 0x4A),
            DangerHover = Color.FromArgb(0xEE, 0x66, 0x5A),
            DangerPressed = Color.FromArgb(0xC8, 0x42, 0x38),
            DangerForeground = Color.FromArgb(0xFF, 0xFF, 0xFF),

            SubtleHover = Color.FromArgb(0xE0, 0xF0, 0xE6),
            SubtlePressed = Color.FromArgb(0xCE, 0xE4, 0xD6),
            SubtleForeground = Color.FromArgb(0x1A, 0x33, 0x26),
            SubtleHoverForeground = Color.FromArgb(0x1A, 0x33, 0x26),

            HoverBg = Color.FromArgb(0xE6, 0xF4, 0xEC),
            SelectedBg = Color.FromArgb(0xD0, 0xEE, 0xDC),
            SelectedHoverBg = Color.FromArgb(0xBE, 0xE5, 0xCE),
            Divider = Color.FromArgb(0xDA, 0xEB, 0xE0),
            GridHeaderBg = Color.FromArgb(0xF2, 0xFA, 0xF5),
            GridStripeBg = Color.FromArgb(0xEC, 0xF7, 0xF0),

            ScrollThumb = Color.FromArgb(0xB0, 0xCC, 0xBC),
            ScrollThumbHover = Color.FromArgb(0x8A, 0xAA, 0x98),

            Overlay = Color.FromArgb(60, 20, 50, 35),
        };
    }
}