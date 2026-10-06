using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using CourseApp.Models;

namespace CourseApp.Services
{
    /// <summary>
    /// 学生 + 值日生 + 考试数据读写。
    /// </summary>
    public static class StudentRepository
    {
        private static readonly JsonSerializerOptions WriteOpts = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        private static readonly JsonSerializerOptions ReadOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        // =====================================================
        // 学生
        // =====================================================
        public static string StudentsJson => Path.Combine(AppPaths.BaseDir, "students.json");

        public static List<Student> LoadStudents() => LoadList<Student>(StudentsJson);
        public static void SaveStudents(List<Student> list) => SaveList(StudentsJson, list);

        // =====================================================
        // 值日生
        // =====================================================
        public static string DutyJson => Path.Combine(AppPaths.BaseDir, "duty.json");

        public static DutyRoster LoadDuty()
        {
            if (!File.Exists(DutyJson)) return new DutyRoster();
            try
            {
                var json = File.ReadAllText(DutyJson);
                return JsonSerializer.Deserialize<DutyRoster>(json, ReadOpts) ?? new DutyRoster();
            }
            catch { return new DutyRoster(); }
        }

        public static void SaveDuty(DutyRoster roster) => SaveObj(DutyJson, roster);

        // =====================================================
        // 考试
        // =====================================================
        public static string ExamsJson => Path.Combine(AppPaths.BaseDir, "exams.json");

        public static List<ExamRecord> LoadExams() => LoadList<ExamRecord>(ExamsJson);
        public static void SaveExams(List<ExamRecord> list) => SaveList(ExamsJson, list);

        // =====================================================
        // 座位表
        // =====================================================
        public static string SeatLayoutJson => Path.Combine(AppPaths.BaseDir, "seats.json");

        public static SeatLayout LoadSeatLayout()
        {
            if (!File.Exists(SeatLayoutJson)) return new SeatLayout();
            try
            {
                var json = File.ReadAllText(SeatLayoutJson);
                return JsonSerializer.Deserialize<SeatLayout>(json, ReadOpts) ?? new SeatLayout();
            }
            catch { return new SeatLayout(); }
        }

        public static void SaveSeatLayout(SeatLayout layout) => SaveObj(SeatLayoutJson, layout);

        // =====================================================
        // 通用读写
        // =====================================================
        private static List<T> LoadList<T>(string path)
        {
            if (!File.Exists(path)) return new List<T>();
            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<List<T>>(json, ReadOpts) ?? new List<T>();
            }
            catch { return new List<T>(); }
        }

        private static void SaveList<T>(string path, List<T> list)
        {
            try
            {
                var json = JsonSerializer.Serialize(list, WriteOpts);
                File.WriteAllText(path, json);
            }
            catch { }
        }

        private static void SaveObj<T>(string path, T obj)
        {
            try
            {
                var json = JsonSerializer.Serialize(obj, WriteOpts);
                File.WriteAllText(path, json);
            }
            catch { }
        }

        // =====================================================
        // CSV 导入学生
        // =====================================================
        /// <summary>
        /// CSV 格式：学号,姓名,性别,座位,备注
        /// </summary>
        public static List<Student> ImportCsv(string filePath)
        {
            var list = new List<Student>();
            if (!File.Exists(filePath)) return list;

            try
            {
                var lines = File.ReadAllLines(filePath, Encoding.UTF8);
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (trimmed.Length == 0) continue;

                    var parts = trimmed.Split(',');
                    if (parts.Length < 2) continue;

                    var s = new Student
                    {
                        Id = parts.Length > 0 ? parts[0].Trim() : "",
                        Name = parts.Length > 1 ? parts[1].Trim() : "",
                        Gender = parts.Length > 2 ? parts[2].Trim() : "",
                        Seat = parts.Length > 3 ? parts[3].Trim() : "",
                        Remark = parts.Length > 4 ? parts[4].Trim() : "",
                    };
                    if (!string.IsNullOrEmpty(s.Name))
                        list.Add(s);
                }
            }
            catch { }

            return list;
        }

        // =====================================================
        // CSV 导出学生
        // =====================================================
        public static void ExportCsv(string filePath, List<Student> list)
        {
            try
            {
                var lines = new List<string>();
                lines.Add("学号,姓名,性别,座位,备注");   // 表头
                foreach (var s in list)
                {
                    lines.Add($"{s.Id},{s.Name},{s.Gender},{s.Seat},{s.Remark}");
                }
                File.WriteAllLines(filePath, lines, Encoding.UTF8);
            }
            catch { }
        }
    }
}