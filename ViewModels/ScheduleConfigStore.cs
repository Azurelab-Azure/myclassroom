using System;
using System.Collections.Generic;
using System.Linq;

namespace PlanPeopleAvalonia.ViewModels;

public class PeriodConfig
{
    public int Section { get; set; }
    public string StartTime { get; set; } = "";
    public string EndTime { get; set; } = "";
}

public class SubjectConfig
{
    public string Name { get; set; } = "";
    public string ColorHex { get; set; } = "#0078D4";
}

public static class ScheduleConfigStore
{
    // ==========================================
    // 1. 核心数据：节次与科目
    // ==========================================
    public static List<PeriodConfig> Periods { get; private set; } = new()
    {
        new() { Section = 1, StartTime = "08:00", EndTime = "08:45" },
        new() { Section = 2, StartTime = "08:55", EndTime = "09:40" },
        new() { Section = 3, StartTime = "10:00", EndTime = "10:45" },
        new() { Section = 4, StartTime = "10:55", EndTime = "11:40" },
        new() { Section = 5, StartTime = "14:00", EndTime = "14:45" },
        new() { Section = 6, StartTime = "14:55", EndTime = "15:40" },
        new() { Section = 7, StartTime = "16:00", EndTime = "16:45" },
        new() { Section = 8, StartTime = "16:55", EndTime = "17:40" },
    };

    public static List<SubjectConfig> Subjects { get; private set; } = new()
    {
        new() { Name = "语文", ColorHex = "#E81123" },
        new() { Name = "数学", ColorHex = "#0078D4" },
        new() { Name = "英语", ColorHex = "#00B294" },
        new() { Name = "物理", ColorHex = "#8863E0" },
        new() { Name = "化学", ColorHex = "#FF8C00" },
        new() { Name = "生物", ColorHex = "#107C10" },
        new() { Name = "历史", ColorHex = "#B4009E" },
        new() { Name = "地理", ColorHex = "#4F6BED" },
        new() { Name = "政治", ColorHex = "#C239B3" },
        new() { Name = "体育", ColorHex = "#FFB900" },
        new() { Name = "音乐", ColorHex = "#E3008C" },
        new() { Name = "美术", ColorHex = "#FF4343" },
        new() { Name = "信息", ColorHex = "#005A9E" },
        new() { Name = "班会", ColorHex = "#767676" },
    };

    // ==========================================
    // 2. ★ 补全：高级调课开关（解决 CS0117）
    // ==========================================
    public static bool DragSwapEnabled { get; set; } = true;
    public static bool ConflictWarnEnabled { get; set; } = true;
    public static bool LockPublished { get; set; } = false;

    // ==========================================
    // 3. 事件通知
    // ==========================================
    public static event Action? ConfigChanged;
    public static void NotifyChanged() => ConfigChanged?.Invoke();

    // ==========================================
    // 4. 辅助方法
    // ==========================================
    public static string GetColorForSubject(string subjectName)
    {
        var sub = Subjects.FirstOrDefault(s => s.Name == subjectName);
        return sub?.ColorHex ?? "#0078D4";
    }

    public static void Save()
    {
        // 后续可通过 System.Text.Json 保存到本地 JSON
    }
}