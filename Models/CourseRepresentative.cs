namespace CourseApp.Models
{
    /// <summary>
    /// 课代表：某门课的课代表学生。
    /// </summary>
    public class CourseRepresentative
    {
        /// <summary>课程名（与 Course.Name 对应）</summary>
        public string CourseName { get; set; } = "";

        /// <summary>学生学号</summary>
        public string StudentId { get; set; } = "";

        /// <summary>学生姓名（冗余，便于显示）</summary>
        public string StudentName { get; set; } = "";
    }
}