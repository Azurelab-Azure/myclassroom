using System;
using System.Collections.Generic;
using PlanPeopleAvalonia.Models;

namespace PlanPeopleAvalonia.Services
{
    /// <summary>
    /// 日程提醒服务（跨平台基础版）。
    /// </summary>
    public class ReminderService : IDisposable
    {
        private readonly System.Timers.Timer _timer;
        private List<ScheduleItem> _schedules = new();
        private readonly HashSet<string> _triggered = new();

        // ★ 核心补全：首页和灵动岛需要用到这个事件
        public event Action<ScheduleItem>? ReminderTriggered;

        public ReminderService()
        {
            _timer = new System.Timers.Timer { Interval = 10000 };
            _timer.Elapsed += (s, e) => CheckReminders();
            _timer.Start();
        }

        public void Dispose()
        {
            _timer.Stop();
            _timer.Dispose();
        }

        public void SetData(List<ScheduleItem> schedules)
        {
            _schedules = schedules ?? new List<ScheduleItem>();
        }

        private void CheckReminders()
        {
            var now = DateTime.Now;

            foreach (var item in _schedules)
            {
                if (item.Reminder == "none") continue;
                if (!item.OccursOn(now)) continue;

                var triggerTime = item.GetReminderTime(now);
                if (triggerTime == null) continue;

                var diff = now - triggerTime.Value;
                if (diff.TotalSeconds < 0 || diff.TotalSeconds > 60) continue;

                string key = $"{item.Id}_{now:yyyyMMdd}_{item.StartTime}";
                if (_triggered.Contains(key)) continue;
                _triggered.Add(key);

                ReminderTriggered?.Invoke(item);
            }
        }
    }
}