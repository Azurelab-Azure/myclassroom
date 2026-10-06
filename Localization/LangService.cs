using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using CourseApp.Services;

namespace CourseApp.Localization
{
    /// <summary>语言元数据。</summary>
    public class LangInfo
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string NativeName { get; set; } = "";
        public bool Rtl { get; set; } = false;
    }

    /// <summary>
    /// 语言列表服务：从 lang/languages.json 读。
    /// 自动处理 UTF-8 BOM。
    /// </summary>
    public static class LangService
    {
        private static List<LangInfo>? _cached;

        private static readonly JsonSerializerOptions ReadOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        public static List<LangInfo> GetAll()
        {
            if (_cached != null) return _cached;

            var path = Path.Combine(AppPaths.LangDir, "languages.json");

            try
            {
                if (File.Exists(path))
                {
                    // 读字节，跳过 BOM
                    var bytes = File.ReadAllBytes(path);
                    int offset = 0;
                    if (bytes.Length >= 3 &&
                        bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                        offset = 3;

                    var json = Encoding.UTF8.GetString(bytes, offset, bytes.Length - offset);

                    var list = JsonSerializer.Deserialize<List<LangInfo>>(json, ReadOpts);

                    if (list != null && list.Count > 0)
                    {
                        _cached = list;
                        return _cached;
                    }
                }
            }
            catch
            {
                // 解析失败，走回退
            }

            // 回退：只支持中英文
            _cached = new List<LangInfo>
            {
                new() { Code = "zh-CN", Name = "简体中文", NativeName = "简体中文" },
                new() { Code = "en",    Name = "英语",     NativeName = "English" },
            };
            return _cached;
        }

        public static LangInfo? Find(string code)
        {
            foreach (var l in GetAll())
                if (l.Code == code) return l;
            return null;
        }
    }
}