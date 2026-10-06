using System;
using System.IO;

namespace CourseApp.Services
{
    /// <summary>
    /// 统一管理所有本地路径。
    /// 优先级：
    ///   1. 环境变量 COURSEAPP_DATA
    ///   2. exe 所在目录（不在 Program Files 时）
    ///   3. %APPDATA%\CourseApp（在 Program Files 时）
    /// </summary>
    public static class AppPaths
    {
        private static string? _cachedBaseDir;

        // =====================================================
        // 根目录
        // =====================================================
        /// <summary>数据根目录</summary>
        public static string BaseDir
        {
            get
            {
                if (_cachedBaseDir != null) return _cachedBaseDir;

                // 1. 环境变量优先
                var env = Environment.GetEnvironmentVariable("COURSEAPP_DATA");
                if (!string.IsNullOrEmpty(env) && Directory.Exists(env))
                {
                    _cachedBaseDir = env;
                    return _cachedBaseDir;
                }

                // 2. exe 目录
                var exeDir = AppDomain.CurrentDomain.BaseDirectory;

                // 3. Program Files 下 → 用 %APPDATA%
                bool inProgramFiles =
                    exeDir.IndexOf("Program Files", StringComparison.OrdinalIgnoreCase) >= 0
                    || exeDir.IndexOf("Program Files (x86)", StringComparison.OrdinalIgnoreCase) >= 0;

                if (inProgramFiles)
                {
                    var appData = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "CourseApp");
                    if (!Directory.Exists(appData)) Directory.CreateDirectory(appData);
                    _cachedBaseDir = appData;
                }
                else
                {
                    _cachedBaseDir = exeDir;
                }

                return _cachedBaseDir;
            }
        }

        // =====================================================
        // 数据文件
        // =====================================================
        public static string CoursesJson => Path.Combine(BaseDir, "courses.json");
        public static string SectionsJson => Path.Combine(BaseDir, "sections.json");
        public static string TeachersJson => Path.Combine(BaseDir, "teachers.json");
        public static string ConfigJson => Path.Combine(BaseDir, "config.json");

        // =====================================================
        // 子目录（数据侧）
        // =====================================================
        public static string LangDir => Path.Combine(BaseDir, "lang");
        public static string PhotosDir => Path.Combine(BaseDir, "photos");

        // =====================================================
        // 资源目录（跟 exe 走，不跟数据走）
        // =====================================================
        /// <summary>assets 目录</summary>
        public static string AssetsDir
        {
            get
            {
                var exeDir = AppDomain.CurrentDomain.BaseDirectory;
                return Path.Combine(exeDir, "assets");
            }
        }

        /// <summary>icons 目录</summary>
        public static string IconsDir
        {
            get
            {
                var exeDir = AppDomain.CurrentDomain.BaseDirectory;
                return Path.Combine(exeDir, "icons");
            }
        }

        /// <summary>应用图标完整路径</summary>
        public static string AppIconPath => Path.Combine(AssetsDir, "app.ico");

        // =====================================================
        // 翻译文件
        // =====================================================
        public static string LangFile(string lang) => Path.Combine(LangDir, lang + ".json");

        // =====================================================
        // 确保目录存在
        // =====================================================
        public static void EnsureAll()
        {
            if (!Directory.Exists(BaseDir)) Directory.CreateDirectory(BaseDir);
            if (!Directory.Exists(LangDir)) Directory.CreateDirectory(LangDir);
            if (!Directory.Exists(PhotosDir)) Directory.CreateDirectory(PhotosDir);
        }
    }
}