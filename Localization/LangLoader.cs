using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace CourseApp.Localization
{
    /// <summary>
    /// 加载 lang/zh-CN.json 之类的翻译字典。
    /// 自动处理 UTF-8 BOM。
    /// </summary>
    public static class LangLoader
    {
        private static readonly JsonSerializerOptions ReadOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        public static Dictionary<string, string> Load(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return new Dictionary<string, string>();

                // 读字节，跳过 BOM
                var bytes = File.ReadAllBytes(filePath);
                int offset = 0;
                if (bytes.Length >= 3 &&
                    bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                    offset = 3;

                var json = Encoding.UTF8.GetString(bytes, offset, bytes.Length - offset);

                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json, ReadOpts);
                return dict ?? new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }
    }
}