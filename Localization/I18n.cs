using System;
using System.Collections.Generic;
using CourseApp.Services;

namespace CourseApp.Localization
{
    /// <summary>
    /// 全局多语言门面。
    /// 用法：I18n.T("toolbar.save")。
    /// 缺项时返回 key 本身（便于定位未翻译的 key）。
    /// </summary>
    public static class I18n
    {
        // =====================================================
        // 当前语言
        // =====================================================
        /// <summary>当前语言："zh-CN" / "en-US"</summary>
        public static string CurrentLang { get; private set; } = "zh-CN";

        private static Dictionary<string, string> _dict = new();

        /// <summary>语言切换时触发（订阅者刷新界面）</summary>
        public static event Action? LanguageChanged;

        // =====================================================
        // 初始化 / 切换
        // =====================================================
        /// <summary>启动时初始化语言（会触发一次 LanguageChanged）</summary>
        public static void Init(string? lang)
        {
            CurrentLang = string.IsNullOrEmpty(lang) ? "zh-CN" : lang;
            _dict = LangLoader.Load(AppPaths.LangFile(CurrentLang));
            LanguageChanged?.Invoke();
        }

        /// <summary>运行时切换语言（不重启），并持久化到 config.json</summary>
        public static void ChangeLanguage(string? lang)
        {
            if (string.IsNullOrEmpty(lang)) return;
            if (lang == CurrentLang) return;

            CurrentLang = lang;
            _dict = LangLoader.Load(AppPaths.LangFile(CurrentLang));

            var cfg = ConfigService.Load();
            cfg.Language = lang;
            ConfigService.Save(cfg);

            LanguageChanged?.Invoke();
        }

        // =====================================================
        // 翻译
        // =====================================================
        /// <summary>
        /// 翻译。找不到 key 时返回 key 本身。
        /// </summary>
        public static string T(string? key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            if (_dict.TryGetValue(key, out var text)) return text;
            return key;
        }

        /// <summary>
        /// 翻译 + 格式化（string.Format）。
        /// 例：I18n.Tf("toolbar.week", 5) → "第 5 周"
        /// </summary>
        public static string Tf(string key, params object[] args)
        {
            var template = T(key);
            if (args == null || args.Length == 0) return template;

            try
            {
                return string.Format(template, args);
            }
            catch
            {
                return template;
            }
        }
    }
}