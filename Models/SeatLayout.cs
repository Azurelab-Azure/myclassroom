using System.Collections.Generic;

namespace CourseApp.Models
{
    /// <summary>
    /// 座位表布局。
    /// </summary>
    public class SeatLayout
    {
        /// <summary>行数</summary>
        public int Rows { get; set; } = 6;

        /// <summary>列数</summary>
        public int Cols { get; set; } = 8;

        /// <summary>讲台位置：top / bottom</summary>
        public string PodiumSide { get; set; } = "top";

        /// <summary>过道所在的列（左侧）</summary>
        public List<int> AisleCols { get; set; } = new();

        /// <summary>布局模式：grid / exam / group</summary>
        public string Mode { get; set; } = "grid";

        /// <summary>分组名称（group 模式使用）</summary>
        public List<string> GroupNames { get; set; } = new();
    }
}