using System.Collections.Generic;
using CourseApp.Models;

namespace CourseApp.Services
{
    /// <summary>
    /// 主窗口接口。
    /// Form1 和 Form2 都实现，供 SettingsContentView 等共享页面调用。
    /// </summary>
    public interface IMainForm
    {
        /// <summary>课程列表</summary>
        List<Course> Courses { get; set; }

        /// <summary>节次列表</summary>
        List<SectionTime> SectionTimes { get; set; }

        /// <summary>加载全部数据</summary>
        void LoadAll();

        /// <summary>保存全部数据</summary>
        void SaveAll();

        /// <summary>刷新全部页面</summary>
        void RefreshAll();

        /// <summary>重启软件</summary>
        void Restart();

        /// <summary>应用灵动岛配置</summary>
        void ApplyIslandConfig();

        /// <summary>应用今日侧边栏配置</summary>
        void ApplyTodaySidebarConfig();

        /// <summary>应用侧边栏配置</summary>
        void ApplySidebarConfig();
    }
}