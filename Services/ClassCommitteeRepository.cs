using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using CourseApp.Models;

namespace CourseApp.Services
{
    /// <summary>
    /// 班委公告 / 班委名单读写。
    /// </summary>
    public static class ClassCommitteeRepository
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

        public static string JsonPath => Path.Combine(AppPaths.BaseDir, "committees.json");

        public static List<ClassCommittee> Load()
        {
            if (!File.Exists(JsonPath)) return new List<ClassCommittee>();
            try
            {
                var json = File.ReadAllText(JsonPath);
                return JsonSerializer.Deserialize<List<ClassCommittee>>(json, ReadOpts)
                       ?? new List<ClassCommittee>();
            }
            catch { return new List<ClassCommittee>(); }
        }

        public static void Save(List<ClassCommittee> list)
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