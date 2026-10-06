namespace CourseApp.Models
{
    /// <summary>
    /// 教师。
    /// </summary>
    public class Teacher
    {
        /// <summary>姓名</summary>
        public string Name { get; set; } = "";

        /// <summary>一句话简介（兼容旧数据，新数据一般用 Remark）</summary>
        public string Info { get; set; } = "";

        /// <summary>照片文件路径（绝对路径或相对于 photos/ 的文件名）</summary>
        public string Photo { get; set; } = "";

        /// <summary>职称（特级教师 / 高级教师 / 一级教师 / ...）</summary>
        public string Title { get; set; } = "";

        /// <summary>学科（语文 / 数学 / 英语 / ...）</summary>
        public string Subject { get; set; } = "";

        /// <summary>电话</summary>
        public string Phone { get; set; } = "";

        /// <summary>邮箱</summary>
        public string Email { get; set; } = "";

        /// <summary>办公室（如"行政楼 302"）</summary>
        public string Office { get; set; } = "";

        /// <summary>备注</summary>
        public string Remark { get; set; } = "";
    }
}