using System.Drawing;
using Microsoft.Win32;

namespace CourseApp.Theme
{
    /// <summary>
    /// 全局主题门面。
    /// 用法：AppTheme.Colors.Accent 取当前主题的强调色。
    /// 切换主题：AppTheme.ApplyLight() / ApplyDark() / ApplyClear() / ApplyFresh() / ApplySystem()。
    /// 每次切换会触发 ThemeChanged 事件，由 Form1 统一处理重绘。
    /// </summary>
    public static class AppTheme
    {
        // =====================================================
        // 当前主题
        // =====================================================
        public static ThemeColors Colors { get; private set; } = ThemeColors.Light();

        /// <summary>主题切换时触发（由 Form1 订阅，统一重绘）</summary>
        public static event Action? ThemeChanged;

        /// <summary>当前是否暗色（用于图标 / 特殊逻辑）</summary>
        public static bool IsDark { get; private set; } = false;

        /// <summary>当前主题名：light / dark / clear / fresh</summary>
        public static string CurrentTheme { get; private set; } = "light";

        // =====================================================
        // 圆角半径
        // =====================================================
        public static class Radius
        {
            public const int Control = 6;
            public const int Card = 8;
            public const int Dialog = 12;
            public const int Window = 8;
        }

        // =====================================================
        // 间距
        // =====================================================
        public static class Space
        {
            public const int XS = 4;
            public const int S = 8;
            public const int M = 12;
            public const int L = 16;
            public const int XL = 24;
        }

        // =====================================================
        // 字体
        // =====================================================
        public static Font BodyFont => SystemFonts.MessageBoxFont;
        public static Font TitleFont => new Font(BodyFont.FontFamily, BodyFont.Size + 2f, FontStyle.Bold);
        public static Font SmallFont => new Font(BodyFont.FontFamily, BodyFont.Size - 1f);

        // =====================================================
        // 应用主题
        // =====================================================
        public static void ApplyLight()
        {
            Colors = ThemeColors.Light();
            IsDark = false;
            CurrentTheme = "light";
            ThemeChanged?.Invoke();
        }

        public static void ApplyDark()
        {
            Colors = ThemeColors.Dark();
            IsDark = true;
            CurrentTheme = "dark";
            ThemeChanged?.Invoke();
        }

        public static void ApplyClear()
        {
            Colors = ThemeColors.Clear();
            IsDark = false;
            CurrentTheme = "clear";
            ThemeChanged?.Invoke();
        }

        public static void ApplyFresh()
        {
            Colors = ThemeColors.Fresh();
            IsDark = false;
            CurrentTheme = "fresh";
            ThemeChanged?.Invoke();
        }

        /// <summary>跟随系统主题</summary>
        public static void ApplySystem()
        {
            if (IsSystemDark()) ApplyDark();
            else ApplyLight();
        }

        /// <summary>按名字应用主题</summary>
        public static void Apply(string name)
        {
            switch ((name ?? "light").ToLowerInvariant())
            {
                case "dark": ApplyDark(); break;
                case "clear": ApplyClear(); break;
                case "fresh": ApplyFresh(); break;
                case "system": ApplySystem(); break;
                default: ApplyLight(); break;
            }
        }

        public static void Toggle()
        {
            if (IsDark) ApplyLight();
            else ApplyDark();
        }

        // =====================================================
        // 系统暗色判断
        // =====================================================
        private static bool IsSystemDark()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key == null) return false;
                var v = key.GetValue("AppsUseLightTheme");
                if (v is int i) return i == 0;
            }
            catch { }
            return false;
        }
    }
}