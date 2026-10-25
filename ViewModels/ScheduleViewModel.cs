using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanPeopleAvalonia.Models;
using PlanPeopleAvalonia.Services;

namespace PlanPeopleAvalonia.ViewModels;

/// <summary>
/// 课表的一行（代表一个节次）
/// </summary>
public class ScheduleRow
{
    public int SectionNumber { get; set; }
    public string TimeText { get; set; } = "";
    
    // 周一到周日的课程实例（如果该节次没有课，则为 null）
    public Course? MondayCourse { get; set; }
    public Course? TuesdayCourse { get; set; }
    public Course? WednesdayCourse { get; set; }
    public Course? ThursdayCourse { get; set; }
    public Course? FridayCourse { get; set; }
    public Course? SaturdayCourse { get; set; }
    public Course? SundayCourse { get; set; }
}

public partial class ScheduleViewModel : ViewModelBase
{
    [ObservableProperty]
    private int _currentWeek = 1;

    [ObservableProperty]
    private bool _isEditorOpen;

    /// <summary>当前正在查看的课表名称（预留多课表功能）</summary>
    [ObservableProperty]
    private string _currentScheduleName = "默认课表";

    /// <summary>供 UI 绑定的网格数据</summary>
    public ObservableCollection<ScheduleRow> GridRows { get; } = new();

    /// <summary>侧边栏的编辑 ViewModel</summary>
    public CourseEditorViewModel EditorVM { get; } = new();

    public ScheduleViewModel()
    {
        // 挂载侧边栏保存事件
        EditorVM.OnSaved += () =>
        {
            LoadData();
        };
    }

    /// <summary>
    /// 从底层 JSON 重新加载课表数据，并生成用于 UI 渲染的网格
    /// </summary>
    public void LoadData()
    {
        try
        {
            // 1. 从本地读取所有课程
            var allCourses = CourseRepository.LoadCourses() ?? new List<Course>();

            // 2. 过滤出当前周的课程
            var currentWeekCourses = allCourses
                .Where(c => c.Weeks == null || c.Weeks.Contains(CurrentWeek))
                .ToList();

            GridRows.Clear();

            // 3. 根据 ScheduleConfigStore 的节次配置动态生成行
            var periods = ScheduleConfigStore.Periods.OrderBy(p => p.Section).ToList();
            if (periods.Count == 0)
            {
                // 如果没有配置，默认生成 8 行
                for (int i = 1; i <= 8; i++)
                {
                    periods.Add(new PeriodConfig { Section = i, StartTime = "", EndTime = "" });
                }
            }

            foreach (var period in periods)
            {
                var row = new ScheduleRow
                {
                    SectionNumber = period.Section,
                    TimeText = $"{period.StartTime}\n{period.EndTime}"
                };

                // 4. 遍历周一到周日，把对应的课程实例填入
                for (int day = 1; day <= 7; day++)
                {
                    // 找寻：星期几等于day，且起始节次等于当前行节次的课程
                    // （注意：这里我们只在起始节次渲染卡片，跨节由 UI 层的 RowSpan 处理）
                    var course = currentWeekCourses.FirstOrDefault(c => c.WeekDay == day && c.TimeStart == period.Section);

                    switch (day)
                    {
                        case 1: row.MondayCourse = course; break;
                        case 2: row.TuesdayCourse = course; break;
                        case 3: row.WednesdayCourse = course; break;
                        case 4: row.ThursdayCourse = course; break;
                        case 5: row.FridayCourse = course; break;
                        case 6: row.SaturdayCourse = course; break;
                        case 7: row.SundayCourse = course; break;
                    }
                }

                GridRows.Add(row);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[课表加载失败] {ex.Message}");
        }
    }

    /// <summary>
    /// 删除指定课程（供右键菜单调用）
    /// </summary>
    public void DeleteCourse(Course course)
    {
        if (course == null) return;
        try
        {
            var all = CourseRepository.LoadCourses();
            all.RemoveAll(c => c.Name == course.Name && c.WeekDay == course.WeekDay && c.TimeStart == course.TimeStart);
            CourseRepository.SaveCourses(all);
            LoadData();
        }
        catch { }
    }

    /// <summary>
    /// 切换课表名称（供子工具栏调用）
    /// </summary>
    [RelayCommand]
    private void SwitchSchedule(string name)
    {
        CurrentScheduleName = name;
        LoadData();
    }

    /// <summary>
    /// 增加一周 / 减少一周
    /// </summary>
    [RelayCommand]
    private void PrevWeek()
    {
        if (CurrentWeek > 1)
        {
            CurrentWeek--;
            LoadData();
        }
    }

    [RelayCommand]
    private void NextWeek()
    {
        CurrentWeek++;
        LoadData();
    }

    /// <summary>
    /// 打开“添加/编辑”弹窗
    /// </summary>
    [RelayCommand]
    private void OpenAddEditor()
    {
        IsEditorOpen = false;
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            EditorVM.LoadData(null, CourseRepository.LoadTeachers());
            IsEditorOpen = true;
        });
    }

    /// <summary>
    /// 指定位置打开编辑弹窗（用于双击空格）
    /// </summary>
    public void OpenAddEditorAt(int weekDayIndex, int startSection, Course? existingCourse = null)
    {
        IsEditorOpen = false;
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            EditorVM.LoadData(existingCourse, CourseRepository.LoadTeachers());
            if (existingCourse == null)
            {
                EditorVM.WeekDayIndex = weekDayIndex;
                EditorVM.StartSection = startSection;
                EditorVM.EndSection = startSection;
            }
            IsEditorOpen = true;
        });
    }
}