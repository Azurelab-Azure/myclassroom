using System.Collections.Generic;

namespace CourseApp.Models
{
    /// <summary>
    /// 值日生表。
    /// Key 是星期几（1~7），Value 是学生姓名列表。
    /// </summary>
    public class DutyRoster
    {
        /// <summary>每天的值日生：{1:["张三","李四"], 2:["王五"]}</summary>
        public Dictionary<int, List<string>> ByWeekday { get; set; } = new();

        /// <summary>每周轮换的值日生（可选）</summary>
        public List<string> WeeklyRotation { get; set; } = new();

        /// <summary>获取某天的值日生</summary>
        public List<string> GetForDay(int weekday)
        {
            if (ByWeekday.TryGetValue(weekday, out var list))
                return list;
            return new List<string>();
        }
    }
}