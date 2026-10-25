namespace PlanPeopleAvalonia.Models
{
    /// <summary>
    /// 教师。
    /// </summary>
    public class Teacher
    {
        /// <summary>姓名</summary>
        public string Name { get; set; } = "";

        /// <summary>一句话简介</summary>
        public string Info { get; set; } = "";

        /// <summary>照片文件路径（旧字段）</summary>
        public string Photo { get; set; } = "";

        /// <summary>照片文件路径（新字段，供详情页使用）</summary>
        public string PhotoPath { get; set; } = "";

        /// <summary>性别</summary>
        public string Gender { get; set; } = "";

        /// <summary>工号</summary>
        public string EmployeeId { get; set; } = "";

        /// <summary>学历</summary>
        public string Education { get; set; } = "";

        /// <summary>职称（特级教师 / 高级教师 / ...）</summary>
        public string Title { get; set; } = "";

        /// <summary>学科（语文 / 数学 / 英语 / ...）</summary>
        public string Subject { get; set; } = "";

        /// <summary>电话</summary>
        public string Phone { get; set; } = "";

        /// <summary>邮箱</summary>
        public string Email { get; set; } = "";

        /// <summary>办公室</summary>
        public string Office { get; set; } = "";

        /// <summary>备注</summary>
        public string Remark { get; set; } = "";
    }
}