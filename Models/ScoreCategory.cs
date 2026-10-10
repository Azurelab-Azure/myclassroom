namespace CourseApp.Models
{
    /// <summary>
    /// 积分分类。
    /// 每个分类带默认分值和颜色，加分 / 减分时使用。
    /// </summary>
    public class ScoreCategory
    {
        /// <summary>分类名称：纪律 / 学习 / 卫生 / 体育 / 助人 / 其他</summary>
        public string Name { get; set; } = "";

        /// <summary>默认分值</summary>
        public int DefaultDelta { get; set; } = 1;

        /// <summary>是否为正向分类（加分）</summary>
        public bool IsPositive { get; set; } = true;

        /// <summary>图标短名（对应 Icons 中的键）</summary>
        public string Icon { get; set; } = "";

        /// <summary>颜色 hex，如 #4CAF50</summary>
        public string Color { get; set; } = "#4CAF50";
    }
}