using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CourseApp.Dialogs;
using CourseApp.Localization;
using CourseApp.Models;
using CourseApp.Services;

namespace CourseApp.Services
{
    /// <summary>
    /// 日程提醒服务。
    /// </summary>
    public class ReminderService : IDisposable
    {
        private readonly System.Windows.Forms.Timer _timer;
        private List<ScheduleItem> _schedules = new();
        private readonly HashSet<string> _triggered = new();
        
        private NotifyIcon? _trayIcon;

        /// <summary>
        /// 构造提醒服务。
        /// </summary>
        public ReminderService()
        {
            _trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information,
                Visible = true,
                Text = AppInfo.AppName
            };

            _timer = new System.Windows.Forms.Timer { Interval = 10000 };
            _timer.Tick += (s, e) => CheckReminders();
            _timer.Start();
        }

        public void Dispose()
        {
            _timer?.Stop();
            _timer?.Dispose();
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }
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

                ShowNotification(item);
            }
        }

        private void ShowNotification(ScheduleItem item)
        {
            string title = I18n.T("schedule.reminderTitle") + " - " + (item.Title ?? "");
            string msg = $"{I18n.T("schedule.startTime")}: {item.StartTime}\n";

            if (!string.IsNullOrEmpty(item.Location))
                msg += $"{I18n.T("schedule.location")}: {item.Location}";

            // 1. 弹托盘气泡
            if (_trayIcon != null)
            {
                _trayIcon.BalloonTipTitle = title;
                _trayIcon.BalloonTipText = msg;
                _trayIcon.BalloonTipIcon = ToolTipIcon.Info;
                _trayIcon.ShowBalloonTip(5000);
            }

            // 2. 同时在主界面弹窗（如果用户在主界面）
            var mainForm = Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null;
            if (mainForm != null && !mainForm.IsDisposed && mainForm.ContainsFocus)
            {
                try
                {
                    mainForm.Invoke(new Action(() =>
                    {
                        using var dlg = new MessageDialog(title, msg, false);
                        dlg.ShowDialog(mainForm);
                    }));
                }
                catch { }
            }
        }
    }
}