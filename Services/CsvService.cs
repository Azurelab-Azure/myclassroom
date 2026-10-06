using System.Collections.Generic;
using System.IO;
using System.Linq;
using CourseApp.Models;

namespace CourseApp.Services
{
    /// <summary>
    /// CSV 格式：
    /// 课程名,教师,教室,周次列表,星期,开始节,结束节
    /// 例：数学,李老师,A101,1|2|3|4|5,1,1,2
    /// </summary>
    public static class CsvService
    {
        // =====================================================
        // 导入
        // =====================================================
        public static List<Course> Load(string filePath)
        {
            var list = new List<Course>();
            if (!File.Exists(filePath)) return list;

            var lines = File.ReadAllLines(filePath);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0) continue;

                var parts = trimmed.Split(',');
                if (parts.Length < 7) continue;

                var c = new Course
                {
                    Name = parts[0].Trim(),
                    Teacher = parts[1].Trim(),
                    Classroom = parts[2].Trim(),
                    Weeks = parts[3].Split('|')
                        .Select(s => s.Trim())
                        .Where(s => int.TryParse(s, out _))
                        .Select(int.Parse)
                        .ToList(),
                    WeekDay = int.TryParse(parts[4].Trim(), out var wd) ? wd : 0,
                    TimeStart = int.TryParse(parts[5].Trim(), out var ts) ? ts : 0,
                    TimeEnd = int.TryParse(parts[6].Trim(), out var te) ? te : 0,
                };
                list.Add(c);
            }
            return list;
        }

        // =====================================================
        // 导出
        // =====================================================
        public static void Save(string filePath, List<Course> courses)
        {
            var lines = new List<string>();
            foreach (var c in courses)
            {
                string weeks = c.Weeks == null ? "" : string.Join("|", c.Weeks);
                lines.Add($"{c.Name},{c.Teacher},{c.Classroom},{weeks},{c.WeekDay},{c.TimeStart},{c.TimeEnd}");
            }
            File.WriteAllLines(filePath, lines);
        }
    }
}