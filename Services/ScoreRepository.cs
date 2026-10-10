using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using CourseApp.Models;

namespace CourseApp.Services
{
    /// <summary>
    /// 积分记录和分类的读写。
    /// 记录保存到 scores.json，分类保存到 score_categories.json。
    /// 学生总积分 = 基础分 + 所有加减分之和。
    /// </summary>
    public static class ScoreRepository
    {
        /// <summary>基础分。每个学生默认 80 分，加减分在此之上累加。</summary>
        public const int BasePoints = 80;

        /// <summary>JSON 写入选项（不转义中文）</summary>
        private static readonly JsonSerializerOptions WriteOpts = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        /// <summary>JSON 读取选项（大小写不敏感，允许注释）</summary>
        private static readonly JsonSerializerOptions ReadOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        /// <summary>记录文件路径</summary>
        public static string RecordsJson => Path.Combine(AppPaths.BaseDir, "scores.json");

        /// <summary>分类文件路径</summary>
        public static string CategoriesJson => Path.Combine(AppPaths.BaseDir, "score_categories.json");

        // =====================================================
        // 记录
        // =====================================================

        /// <summary>
        /// 加载所有积分记录。
        /// </summary>
        public static List<ScoreRecord> LoadRecords()
        {
            if (!File.Exists(RecordsJson)) return new List<ScoreRecord>();
            try
            {
                var json = File.ReadAllText(RecordsJson);
                return JsonSerializer.Deserialize<List<ScoreRecord>>(json, ReadOpts)
                       ?? new List<ScoreRecord>();
            }
            catch { return new List<ScoreRecord>(); }
        }

        /// <summary>
        /// 保存所有积分记录。
        /// </summary>
        public static void SaveRecords(List<ScoreRecord> list)
        {
            try
            {
                var json = JsonSerializer.Serialize(list, WriteOpts);
                File.WriteAllText(RecordsJson, json);
            }
            catch { }
        }

        // =====================================================
        // 分类
        // =====================================================

        /// <summary>
        /// 加载分类列表。文件不存在时返回默认分类。
        /// </summary>
        public static List<ScoreCategory> LoadCategories()
        {
            if (!File.Exists(CategoriesJson))
                return DefaultCategories();

            try
            {
                var json = File.ReadAllText(CategoriesJson);
                var list = JsonSerializer.Deserialize<List<ScoreCategory>>(json, ReadOpts);
                if (list != null && list.Count > 0) return list;
            }
            catch { }

            return DefaultCategories();
        }

        /// <summary>
        /// 保存分类列表。
        /// </summary>
        public static void SaveCategories(List<ScoreCategory> list)
        {
            try
            {
                var json = JsonSerializer.Serialize(list, WriteOpts);
                File.WriteAllText(CategoriesJson, json);
            }
            catch { }
        }

        /// <summary>
        /// 返回默认分类。
        /// </summary>
        private static List<ScoreCategory> DefaultCategories()
        {
            return new List<ScoreCategory>
            {
                new() { Name = "纪律", DefaultDelta = 1,  IsPositive = true,  Icon = "Ok",      Color = "#4CAF50" },
                new() { Name = "学习", DefaultDelta = 1,  IsPositive = true,  Icon = "Book",    Color = "#2E86E8" },
                new() { Name = "卫生", DefaultDelta = 1,  IsPositive = true,  Icon = "Folder",  Color = "#7C4DFF" },
                new() { Name = "体育", DefaultDelta = 1,  IsPositive = true,  Icon = "Success", Color = "#FF9800" },
                new() { Name = "助人", DefaultDelta = 1,  IsPositive = true,  Icon = "Person",  Color = "#E91E63" },
                new() { Name = "其他", DefaultDelta = 1,  IsPositive = true,  Icon = "Info",    Color = "#607D8B" },
            };
        }

        // =====================================================
        // 统计
        // =====================================================

        /// <summary>
        /// 重新计算每个学生的总积分。
        /// 基础分 80 起步，遍历所有记录，把 Delta 累加到对应学生上。
        /// </summary>
        public static void RecalculateStudentPoints(List<Student> students, List<ScoreRecord> records)
        {
            if (students == null) return;

            foreach (var s in students)
                s.TotalPoints = BasePoints;

            if (records == null) return;

            var byId = students.ToDictionary(s => s.Id, s => s);
            foreach (var r in records)
            {
                if (byId.TryGetValue(r.StudentId, out var s))
                    s.TotalPoints += r.Delta;
            }
        }

        /// <summary>
        /// 按学生统计积分总和（不含基础分）。
        /// </summary>
        public static int GetStudentTotal(string studentId, List<ScoreRecord> records)
        {
            if (string.IsNullOrEmpty(studentId) || records == null) return 0;
            return records.Where(r => r.StudentId == studentId).Sum(r => r.Delta);
        }

        /// <summary>
        /// 按学生统计指定日期范围内的积分（不含基础分）。
        /// </summary>
        public static int GetStudentTotalInRange(string studentId, List<ScoreRecord> records, DateTime from, DateTime to)
        {
            if (string.IsNullOrEmpty(studentId) || records == null) return 0;
            return records
                .Where(r => r.StudentId == studentId)
                .Where(r => DateTime.TryParse(r.Date, out var d) && d >= from && d <= to)
                .Sum(r => r.Delta);
        }

        /// <summary>
        /// 按学生统计最近 N 天的积分（不含基础分）。
        /// </summary>
        public static int GetStudentRecentTotal(string studentId, List<ScoreRecord> records, int days)
        {
            var to = DateTime.Now;
            var from = to.AddDays(-days);
            return GetStudentTotalInRange(studentId, records, from, to);
        }
    }
}