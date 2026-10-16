namespace CourseApp.Services
{
    /// <summary>
    /// 应用模式常量。
    /// school 学校 / student 学生 / office 办公 / minor 未成年。
    /// </summary>
    public static class AppMode
    {
        /// <summary>学校模式</summary>
        public const string School = "school";

        /// <summary>学生模式</summary>
        public const string Student = "student";

        /// <summary>办公模式</summary>
        public const string Office = "office";

        /// <summary>未成年模式</summary>
        public const string Minor = "minor";

        /// <summary>
        /// 是否办公模式。
        /// </summary>
        public static bool IsOffice(string? mode)
            => (mode ?? School).ToLowerInvariant() == Office;

        /// <summary>
        /// 是否未成年模式。
        /// </summary>
        public static bool IsMinor(string? mode)
            => (mode ?? School).ToLowerInvariant() == Minor;

        /// <summary>
        /// 是否学校模式。
        /// </summary>
        public static bool IsSchool(string? mode)
            => (mode ?? School).ToLowerInvariant() == School;

        /// <summary>
        /// 是否学生模式。
        /// </summary>
        public static bool IsStudent(string? mode)
            => (mode ?? School).ToLowerInvariant() == Student;

        /// <summary>
        /// 是否使用 Form1（学校 / 学生 / 未成年）。
        /// </summary>
        public static bool UseForm1(string? mode)
            => !IsOffice(mode);
    }
}