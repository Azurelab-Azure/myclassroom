using System.Collections.Generic;

namespace CourseApp.Models
{
    /// <summary>座位表布局。</summary>
    public class SeatLayout
    {
        public int Rows { get; set; } = 6;
        public int Cols { get; set; } = 8;
        public string PodiumSide { get; set; } = "top";    // top / bottom
        public List<int> AisleCols { get; set; } = new();   // 过道所在的列（左侧）
    }
}