using System;
using System.Collections.Generic;

namespace CourseApp.Models
{
    /// <summary>
    /// 办公模式的日程项。
    /// </summary>
    public class ScheduleItem
    {
        /// <summary>唯一 ID（GUID）</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>标题</summary>
        public string Title { get; set; } = "";

        /// <summary>分类：会议 / 任务 / 提醒 / 其他</summary>
        public string Category { get; set; } = "";

        /// <summary>日期 yyyy-MM-dd</summary>
        public string Date { get; set; } = "";

        /// <summary>时段：morning 上午 / noon 中午 / afternoon 下午 / evening 晚上 / allday 全天</summary>
        public string TimeSlot { get; set; } = "morning";

        /// <summary>开始时间 HH:mm</summary>
        public string StartTime { get; set; } = "";

        /// <summary>结束时间 HH:mm</summary>
        public string EndTime { get; set; } = "";

        /// <summary>地点</summary>
        public string Location { get; set; } = "";

        /// <summary>备注</summary>
        public string Notes { get; set; } = "";

        /// <summary>是否已完成</summary>
        public bool Done { get; set; } = false;

        /// <summary>重复模式：none / daily / weekly / monthly / weekdays</summary>
        public string Repeat { get; set; } = "none";

        /// <summary>重复结束日期 yyyy-MM-dd（空表示无限）</summary>
        public string RepeatUntil { get; set; } = "";

        /// <summary>提醒设置：none / at / 5min / 15min / 30min / 1hour / 1day</summary>
        public string Reminder { get; set; } = "none";

        /// <summary>颜色 hex</summary>
        public string Color { get; set; } = "#2E86E8";

        /// <summary>是否为待办（true 不显示时间段，只勾选完成）</summary>
        public bool IsTodo { get; set; } = false;

        /// <summary>
        /// 判断某天是否应显示该日程。
        /// </summary>
        public bool OccursOn(DateTime day)
        {
            if (!DateTime.TryParse(Date, out var start)) return false;
            if (day.Date < start.Date) return false;

            if (!string.IsNullOrEmpty(RepeatUntil)
                && DateTime.TryParse(RepeatUntil, out var until)
                && day.Date > until.Date)
                return false;

            return Repeat switch
            {
                "daily" => true,
                "weekdays" => day.DayOfWeek != DayOfWeek.Saturday && day.DayOfWeek != DayOfWeek.Sunday,
                "weekly" => day.DayOfWeek == start.DayOfWeek,
                "monthly" => day.Day == start.Day,
                _ => day.Date == start.Date,
            };
        }

        /// <summary>
        /// 计算提醒触发时间。
        /// </summary>
        public DateTime? GetReminderTime(DateTime day)
        {
            if (Reminder == "none") return null;
            if (string.IsNullOrEmpty(StartTime)) return null;
            if (!TimeSpan.TryParse(StartTime, out var ts)) return null;

            var start = day.Date + ts;

            return Reminder switch
            {
                "at" => start,
                "5min" => start.AddMinutes(-5),
                "15min" => start.AddMinutes(-15),
                "30min" => start.AddMinutes(-30),
                "1hour" => start.AddHours(-1),
                "1day" => start.AddDays(-1),
                _ => (DateTime?)null,
            };
        }
    }
}