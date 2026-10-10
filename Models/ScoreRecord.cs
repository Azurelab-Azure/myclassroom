namespace CourseApp.Models
{
    /// <summary>
    /// 一条积分记录。
    /// 加分 Delta 为正，减分 Delta 为负。
    /// </summary>
    public class ScoreRecord
    {
        /// <summary>记录 ID（GUID）</summary>
        public string Id { get; set; } = "";

        /// <summary>学号</summary>
        public string StudentId { get; set; } = "";

        /// <summary>学生姓名（冗余，便于显示）</summary>
        public string StudentName { get; set; } = "";

        /// <summary>分值变化，正为加分，负为减分</summary>
        public int Delta { get; set; } = 0;

        /// <summary>原因描述</summary>
        public string Reason { get; set; } = "";

        /// <summary>分类名称：纪律 / 学习 / 卫生 / 体育 / 助人 / 其他</summary>
        public string Category { get; set; } = "";

        /// <summary>发生时间 yyyy-MM-dd HH:mm:ss</summary>
        public string Date { get; set; } = "";

        /// <summary>操作人</summary>
        public string Operator { get; set; } = "";
    }
}