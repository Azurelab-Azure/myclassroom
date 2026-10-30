using System;
using System.IO;

namespace PlanPeopleAvalonia.Services
{
    /// <summary>
    /// 统一管理所有本地路径（全平台兼容修复版）。
    /// </summary>
    public static class AppPaths
    {
        private static string? _cachedBaseDir;

        /// <summary>数据根目录（桌面端在 exe/Data，移动端在沙盒）</summary>
        public static string BaseDir
        {
            get
            {
                if (_cachedBaseDir != null) return _cachedBaseDir;

                // 移动端：绝对不能访问 exe 目录，必须用沙盒
                bool isMobile = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();
                if (isMobile)
                {
                    _cachedBaseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PlanPeopleData");
                }
                else
                {
                    // 桌面端：exe/Data，支持绿色版
                    _cachedBaseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                }

                if (!Directory.Exists(_cachedBaseDir)) Directory.CreateDirectory(_cachedBaseDir);
                return _cachedBaseDir;
            }
        }

        public static string CoursesJson => Path.Combine(BaseDir, "courses.json");
        public static string SectionsJson => Path.Combine(BaseDir, "sections.json");
        public static string TeachersJson => Path.Combine(BaseDir, "teachers.json");
        
        // ★ 修复了原来多出来的那个句号，导致编译失败
        public static string ConfigJson => Path.Combine(BaseDir, "config.json"); 

        public static string LangDir => Path.Combine(BaseDir, "lang");
        public static string PhotosDir => Path.Combine(BaseDir, "photos");

        // 资源目录跟 exe 走
        public static string AssetsDir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
        public static string IconsDir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons");

        public static string LangFile(string lang) => Path.Combine(LangDir, lang + ".json");

        public static void EnsureAll()
        {
            if (!Directory.Exists(BaseDir)) Directory.CreateDirectory(BaseDir);
            if (!Directory.Exists(LangDir)) Directory.CreateDirectory(LangDir);
            if (!Directory.Exists(PhotosDir)) Directory.CreateDirectory(PhotosDir);
        }

        public static string GetLegacyV2Dir()
        {
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            var oldExeDir = Path.Combine(exeDir, "CourseApp");
            if (Directory.Exists(oldExeDir) && File.Exists(Path.Combine(oldExeDir, "courses.json")))
                return oldExeDir;
            return "";
        }
    }
}