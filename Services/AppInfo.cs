using System.Reflection;

namespace CourseApp.Services
{
    /// <summary>
    /// 应用元信息。
    /// </summary>
    public static class AppInfo
    {
        // =====================================================
        // 应用信息
        // =====================================================
        public const string AppName = "我的课表";
        public const string AppNameEn = "My Schedule";
        public const string Version = "1.1.0";
        public const string BuildDate = "2026-10-10";

        // =====================================================
        // 作者 / 许可
        // =====================================================
        public const string Author = "Kzure Lab";
        public const string Copyright = "© 2026 Kzure Lab. All rights reserved.";
        public const string License = "MIT License";
        public const string Homepage = "";
        public const string ContactEmail = "";

        /// <summary>更新检查 URL（留空表示不检查）</summary>
        public const string UpdateCheckUrl = "";

        // =====================================================
        // 简介
        // =====================================================
        public const string Description =
            "无服务器、纯本地、兼容老系统、接近原生的课程表应用。\n" +
            "支持课程管理、教师名片、节次自定义、CSV 导入导出、灵动岛下课提醒、PPT 放映监控等功能。";

        // =====================================================
        // 运行期版本
        // =====================================================
        public static string RuntimeVersion
        {
            get
            {
                try
                {
                    var asm = Assembly.GetEntryAssembly();
                    if (asm == null) return Version;

                    var attr = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                    if (attr != null && !string.IsNullOrEmpty(attr.InformationalVersion))
                    {
                        var s = attr.InformationalVersion;
                        int idx = s.IndexOf('+');
                        return idx > 0 ? s.Substring(0, idx) : s;
                    }

                    var v = asm.GetName().Version;
                    return v != null ? v.ToString(3) : Version;
                }
                catch
                {
                    return Version;
                }
            }
        }
    }
}