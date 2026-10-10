using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using CourseApp.Models;

namespace CourseApp.Services
{
    /// <summary>
    /// 课代表读写。
    /// </summary>
    public static class CourseRepresentativeRepository
    {
        private static readonly JsonSerializerOptions WriteOpts = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        private static readonly JsonSerializerOptions ReadOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        public static string JsonPath => Path.Combine(AppPaths.BaseDir, "representatives.json");

        public static List<CourseRepresentative> Load()
        {
            if (!File.Exists(JsonPath)) return new List<CourseRepresentative>();
            try
            {
                var json = File.ReadAllText(JsonPath);
                return JsonSerializer.Deserialize<List<CourseRepresentative>>(json, ReadOpts)
                       ?? new List<CourseRepresentative>();
            }
            catch { return new List<CourseRepresentative>(); }
        }

        public static void Save(List<CourseRepresentative> list)
        {
            try
            {
                var json = JsonSerializer.Serialize(list, WriteOpts);
                File.WriteAllText(JsonPath, json);
            }
            catch { }
        }
    }
}