using System.Collections.Generic;

namespace CourseApp.Models
{
    /// <summary>
    /// 课程。
    /// 一门课 = 一个"课格"（周几 + 第几节到第几节 + 哪些周）。
    /// </summary>
    public class Course
    {
        /// <summary>课程名（语文 / 数学 / ...）</summary>
        public string Name { get; set; } = "";

        /// <summary>教师名（与 Teacher.Name 对应，可为空）</summary>
        public string Teacher { get; set; } = "";

        /// <summary>教室（A101 / 实验室 / ...，可为空）</summary>
        public string Classroom { get; set; } = "";

        /// <summary>周次列表（如 [1,2,3,...,16]）</summary>
        public List<int> Weeks { get; set; } = new();

        /// <summary>星期几：1~7（周一 ~ 周日）</summary>
        public int WeekDay { get; set; }

        /// <summary>起始节（1 ~ N）</summary>
        public int TimeStart { get; set; }

        /// <summary>结束节（1 ~ N）</summary>
        public int TimeEnd { get; set; }
    }
}