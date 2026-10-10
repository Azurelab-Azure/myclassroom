namespace CourseApp.Models
{
    /// <summary>
    /// 班委公告 / 班委名单。
    /// </summary>
    public class ClassCommittee
    {
        /// <summary>公告标题</summary>
        public string Title { get; set; } = "";

        /// <summary>公告内容</summary>
        public string Content { get; set; } = "";

        /// <summary>发布日期 yyyy-MM-dd</summary>
        public string Date { get; set; } = "";

        /// <summary>发布人</summary>
        public string Publisher { get; set; } = "";

        /// <summary>是否置顶</summary>
        public bool Pinned { get; set; } = false;

        /// <summary>公告类型：notice / committee / duty</summary>
        public string Type { get; set; } = "notice";
    }
}