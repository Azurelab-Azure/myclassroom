using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using CourseApp.Models;

namespace CourseApp.Services
{
    /// <summary>
    /// 悬浮组件配置读写。
    /// 独立保存到 overlay.json，支持导出 / 导入。
    /// </summary>
    public static class OverlayConfigService
    {
        /// <summary>JSON 写入选项（不转义中文）</summary>
        private static readonly JsonSerializerOptions WriteOpts = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        /// <summary>JSON 读取选项</summary>
        private static readonly JsonSerializerOptions ReadOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        /// <summary>配置文件路径</summary>
        public static string JsonPath => Path.Combine(AppPaths.BaseDir, "overlay.json");

        /// <summary>
        /// 加载配置。文件不存在时返回默认配置。
        /// </summary>
        public static OverlayConfig Load()
        {
            if (!File.Exists(JsonPath)) return new OverlayConfig();

            try
            {
                var json = File.ReadAllText(JsonPath);
                var cfg = JsonSerializer.Deserialize<OverlayConfig>(json, ReadOpts);
                if (cfg == null) return new OverlayConfig();

                // 兼容旧配置
                cfg.IslandModules ??= new System.Collections.Generic.List<string>();
                cfg.SidebarModules ??= new System.Collections.Generic.List<string>();
                return cfg;
            }
            catch
            {
                return new OverlayConfig();
            }
        }

        /// <summary>
        /// 保存配置。
        /// </summary>
        public static void Save(OverlayConfig cfg)
        {
            if (cfg == null) return;
            try
            {
                var json = JsonSerializer.Serialize(cfg, WriteOpts);
                File.WriteAllText(JsonPath, json);
            }
            catch { }
        }

        /// <summary>
        /// 导出配置到指定路径。
        /// </summary>
        public static bool Export(string path, OverlayConfig cfg)
        {
            if (string.IsNullOrEmpty(path) || cfg == null) return false;
            try
            {
                var json = JsonSerializer.Serialize(cfg, WriteOpts);
                File.WriteAllText(path, json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 从指定路径导入配置。
        /// </summary>
        public static OverlayConfig? Import(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            try
            {
                var json = File.ReadAllText(path);
                var cfg = JsonSerializer.Deserialize<OverlayConfig>(json, ReadOpts);
                if (cfg == null) return null;

                cfg.IslandModules ??= new System.Collections.Generic.List<string>();
                cfg.SidebarModules ??= new System.Collections.Generic.List<string>();
                return cfg;
            }
            catch
            {
                return null;
            }
        }
    }
}