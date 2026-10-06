using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CourseApp.Models;

namespace CourseApp.Services
{
    /// <summary>
    /// 负责 courses.json / sections.json / teachers.json 的读写。
    /// 无 UI 依赖，纯粹数据层。
    /// </summary>
    public static class CourseRepository
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
        // Courses
        // =====================================================
        public static List<Course> LoadCourses() => LoadList<Course>(AppPaths.CoursesJson);
        public static void SaveCourses(List<Course> list) => SaveList(AppPaths.CoursesJson, list);

        // =====================================================
        // Sections
        // =====================================================
        public static List<SectionTime> LoadSections() => LoadList<SectionTime>(AppPaths.SectionsJson);
        public static void SaveSections(List<SectionTime> list) => SaveList(AppPaths.SectionsJson, list);

        // =====================================================
        // Teachers
        // =====================================================
        public static List<Teacher> LoadTeachers() => LoadList<Teacher>(AppPaths.TeachersJson);
        public static void SaveTeachers(List<Teacher> list) => SaveList(AppPaths.TeachersJson, list);

        // =====================================================
        // 内部
        // =====================================================
        private static List<T> LoadList<T>(string path)
        {
            if (!File.Exists(path)) return new List<T>();
            try
            {
                var json = File.ReadAllText(path);
                var list = JsonSerializer.Deserialize<List<T>>(json, ReadOpts);
                return list ?? new List<T>();
            }
            catch
            {
                return new List<T>();
            }
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
    }
}