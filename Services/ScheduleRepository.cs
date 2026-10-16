using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using CourseApp.Models;

namespace CourseApp.Services
{
    /// <summary>
    /// 办公模式日程读写。
    /// 保存到 schedules.json，支持导入导出。
    /// </summary>
    public static class ScheduleRepository
    {
        /// <summary>JSON 写入选项（不转义中文）</summary>
        private static readonly JsonSerializerOptions WriteOpts = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        /// <summary>JSON 读取选项</summary>
        private static readonly JsonSerializerOptions ReadOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        /// <summary>日程文件路径</summary>
        public static string JsonPath => Path.Combine(AppPaths.BaseDir, "schedules.json");

        /// <summary>
        /// 加载所有日程。
        /// </summary>
        public static List<ScheduleItem> Load()
        {
            if (!File.Exists(JsonPath)) return new List<ScheduleItem>();
            try
            {
                var json = File.ReadAllText(JsonPath);
                return JsonSerializer.Deserialize<List<ScheduleItem>>(json, ReadOpts)
                       ?? new List<ScheduleItem>();
            }
            catch { return new List<ScheduleItem>(); }
        }

        /// <summary>
        /// 保存所有日程。
        /// </summary>
        public static void Save(List<ScheduleItem> list)
        {
            try
            {
                var json = JsonSerializer.Serialize(list, WriteOpts);
                File.WriteAllText(JsonPath, json);
            }
            catch { }
        }

        /// <summary>
        /// 导出到指定路径。
        /// </summary>
        public static bool Export(string path, List<ScheduleItem> list)
        {
            if (string.IsNullOrEmpty(path) || list == null) return false;
            try
            {
                var json = JsonSerializer.Serialize(list, WriteOpts);
                File.WriteAllText(path, json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 从指定路径导入。
        /// </summary>
        public static List<ScheduleItem>? Import(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            try
            {
                var json = File.ReadAllText(path);
                var list = JsonSerializer.Deserialize<List<ScheduleItem>>(json, ReadOpts);
                return list;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 按日期区间获取日程。
        /// </summary>
        /// <param name="items">所有日程</param>
        /// <param name="from">起始日期（含）</param>
        /// <param name="to">结束日期（含）</param>
        /// <returns>按时间排序的日程列表</returns>
        public static List<ScheduleItem> GetInRange(List<ScheduleItem> items, DateTime from, DateTime to)
        {
            var result = new List<ScheduleItem>();
            if (items == null) return result;

            for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
            {
                foreach (var item in items)
                {
                    if (item.OccursOn(d))
                    {
                        // 避免重复（重复日程每天只加一次）
                        if (!result.Any(x => x.Id == item.Id && x.Date == d.ToString("yyyy-MM-dd")))
                        {
                            var clone = CloneForDate(item, d);
                            result.Add(clone);
                        }
                    }
                }
            }

            return result
                .OrderBy(x => x.Date)
                .ThenBy(x => x.StartTime)
                .ToList();
        }

        /// <summary>
        /// 获取某天的日程。
        /// </summary>
        public static List<ScheduleItem> GetForDay(List<ScheduleItem> items, DateTime day)
        {
            return GetInRange(items, day, day);
        }

        /// <summary>
        /// 克隆一份日程，用于指定日期显示。
        /// </summary>
        private static ScheduleItem CloneForDate(ScheduleItem src, DateTime day)
        {
            return new ScheduleItem
            {
                Id = src.Id,
                Title = src.Title,
                Category = src.Category,
                Date = day.ToString("yyyy-MM-dd"),
                StartTime = src.StartTime,
                EndTime = src.EndTime,
                Location = src.Location,
                Notes = src.Notes,
                Done = src.Done,
                Repeat = src.Repeat,
                RepeatUntil = src.RepeatUntil,
                Reminder = src.Reminder,
                Color = src.Color,
                IsTodo = src.IsTodo,
            };
        }
    }
}