using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using PlanPeopleAvalonia.Models;

namespace PlanPeopleAvalonia.Services;

public static class CourseRepository
{
    private static readonly string DataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
    private static readonly string CourseFilePath = Path.Combine(DataFolder, "courses.csv");
    private static readonly string TeacherFilePath = Path.Combine(DataFolder, "teachers.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>从 CSV 加载所有课程</summary>
    public static List<Course> LoadCourses()
    {
        if (!File.Exists(CourseFilePath)) return new List<Course>();

        var courses = new List<Course>();
        try
        {
            var lines = File.ReadAllLines(CourseFilePath, Encoding.UTF8);
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = ParseCsvLine(line);
                if (parts.Length < 7) continue;

                try
                {
                    var course = new Course
                    {
                        Name = parts[0],
                        Teacher = parts[1],
                        Classroom = parts[2],
                        WeekDay = int.Parse(parts[3]),
                        TimeStart = int.Parse(parts[4]),
                        TimeEnd = int.Parse(parts[5]),
                        Weeks = string.IsNullOrWhiteSpace(parts[6]) 
                            ? new List<int>() 
                            : parts[6].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList()
                    };
                    courses.Add(course);
                }
                catch (Exception ex) { Console.WriteLine($"[解析异常] {ex.Message}"); }
            }
        }
        catch (Exception ex) { Console.WriteLine($"[读取异常] {ex.Message}"); }
        return courses;
    }

    public static void SaveCourses(IEnumerable<Course> courses)
    {
        try
        {
            if (!Directory.Exists(DataFolder)) Directory.CreateDirectory(DataFolder);
            var sb = new StringBuilder();
            sb.AppendLine("Name,Teacher,Classroom,WeekDay,TimeStart,TimeEnd,Weeks");
            foreach (var c in courses)
            {
                string name = (c.Name ?? "").Replace("\"", "\"\"");
                string teacher = (c.Teacher ?? "").Replace("\"", "\"\"");
                string room = (c.Classroom ?? "").Replace("\"", "\"\"");
                string weeks = c.Weeks == null ? "" : string.Join(";", c.Weeks);
                sb.AppendLine($"\"{name}\",\"{teacher}\",\"{room}\",{c.WeekDay},{c.TimeStart},{c.TimeEnd},\"{weeks}\"");
            }
            File.WriteAllText(CourseFilePath, sb.ToString(), Encoding.UTF8);
        }
        catch (Exception ex) { Console.WriteLine($"[保存异常] {ex.Message}"); }
    }

    // ==========================================
    // 教师数据持久化 (新增)
    // ==========================================
public static List<Teacher> LoadTeachers()
{
    if (!File.Exists(TeacherFilePath)) return new List<Teacher>();
    try
    {
        var json = File.ReadAllText(TeacherFilePath);
        if (string.IsNullOrWhiteSpace(json)) return new List<Teacher>();
        return JsonSerializer.Deserialize<List<Teacher>>(json, JsonOpts) ?? new List<Teacher>();
    }
    catch 
    { 
        return new List<Teacher>(); // JSON 坏了也不会崩溃
    }
}
    public static void SaveTeachers(IEnumerable<Teacher> teachers)
    {
        try
        {
            if (!Directory.Exists(DataFolder)) Directory.CreateDirectory(DataFolder);
            var json = JsonSerializer.Serialize(teachers, JsonOpts);
            File.WriteAllText(TeacherFilePath, json);
        }
        catch { }
    }

    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
                else { inQuotes = !inQuotes; }
            }
            else if (c == ',' && !inQuotes) { result.Add(current.ToString()); current.Clear(); }
            else { current.Append(c); }
        }
        result.Add(current.ToString());
        return result.ToArray();
    }
}