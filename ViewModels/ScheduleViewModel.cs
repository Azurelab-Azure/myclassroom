using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanPeopleAvalonia.Models;
using PlanPeopleAvalonia.Services;

namespace PlanPeopleAvalonia.ViewModels;

public partial class ScheduleViewModel : ViewModelBase
{
    // ★ 尺寸固定值（由设置弹窗修改）
    [ObservableProperty] private double _cellWidth = 100;
    [ObservableProperty] private double _cellHeight = 80;
    
    // 当前周次
    [ObservableProperty] private int _currentWeek = 1;
    
    // 动态节数
    [ObservableProperty] private int _totalSections = 8; 

    // UI 绑定的数据源
    public ObservableCollection<SectionTime> SectionTimes { get; } = new();
    public ObservableCollection<string> WeekDays { get; } = new() { "周一", "周二", "周三", "周四", "周五", "周六", "周日" };
    public ObservableCollection<string> Sections { get; } = new();
    public ObservableCollection<CellViewModel> Cells { get; } = new();
    public ObservableCollection<CourseViewModel> CurrentCourses { get; } = new();

    public List<string> QuickSubjects { get; } = new() { "语文", "数学", "英语", "物理", "化学", "生物", "历史", "地理", "政治" };

    private List<Course> _allCourses = new();

    // 总像素宽高计算
    public double CanvasTotalWidth => CellWidth * 7;
    public double CanvasTotalHeight => CellHeight * TotalSections;

    public ScheduleViewModel()
    {
        // 初始化默认时间（8节课）
        for (int i = 1; i <= 8; i++)
        {
            SectionTimes.Add(new SectionTime { Section = i, StartTime = "08:00", EndTime = "08:45" });
        }

        InitializeGrid();
        LoadData();
    }

    // 监听宽高变化，实时通知 UI 更新总尺寸
    partial void OnCellWidthChanged(double value) => OnPropertyChanged(nameof(CanvasTotalWidth));
    partial void OnCellHeightChanged(double value) => OnPropertyChanged(nameof(CanvasTotalHeight));

    // 监听节数变化（如果用户在设置里直接改了数值，同步刷新）
    partial void OnTotalSectionsChanged(int value)
    {
        InitializeGrid();
    }

    /// <summary>
    /// 根据当前总节数重新生成 7 x N 的网格，并同步时间轴
    /// </summary>
    public void InitializeGrid()
    {
        Sections.Clear();
        Cells.Clear();

        for (int i = 1; i <= TotalSections; i++)
        {
            Sections.Add(i.ToString());
        }

        for (int r = 0; r < TotalSections; r++)
        {
            for (int c = 0; c < 7; c++)
            {
                Cells.Add(new CellViewModel(r, c, this)); 
            }
        }

        // 同步更新左侧时间轴的数量（防止减少节数时残留多余的格子）
        while (SectionTimes.Count < TotalSections)
        {
            SectionTimes.Add(new SectionTime { Section = SectionTimes.Count + 1, StartTime = "00:00", EndTime = "00:00" });
        }
        while (SectionTimes.Count > TotalSections)
        {
            SectionTimes.RemoveAt(SectionTimes.Count - 1);
        }
        
        OnPropertyChanged(nameof(CanvasTotalHeight));
    }

    public void LoadData()
    {
        _allCourses = CourseRepository.LoadCourses();
        RefreshCurrentWeek();
    }

    public void SaveData() => CourseRepository.SaveCourses(_allCourses);

    /// <summary>
    /// 过滤当前周的课程并刷新 UI
    /// </summary>
    public void RefreshCurrentWeek()
    {
        CurrentCourses.Clear();
        var filtered = _allCourses
            .Where(c => c.Weeks == null || c.Weeks.Count == 0 || c.Weeks.Contains(CurrentWeek))
            .OrderBy(c => c.WeekDay).ThenBy(c => c.TimeStart)
            .ToList();

        foreach (var course in filtered)
            CurrentCourses.Add(new CourseViewModel(course, this));
    }

    /// <summary>
    /// 根据坐标反推落点（已严格限制不会越界）
    /// </summary>
    public (int WeekDay, int Section) GetCellByPosition(double x, double y)
    {
        int col = (int)(x / CellWidth);
        int row = (int)(y / CellHeight);
        
        // 限制合法范围，防止拖拽出界导致异常
        int clampedCol = Math.Clamp(col + 1, 1, 7);
        int clampedRow = Math.Clamp(row + 1, 1, TotalSections);
        
        return (clampedCol, clampedRow);
    }

    public void AddCourse(Course course)
    {
        _allCourses.Add(course);
        SaveData();
        RefreshCurrentWeek();
    }

    public void DeleteCourse(CourseViewModel courseVm)
    {
        _allCourses.Remove(courseVm.Model);
        SaveData();
        RefreshCurrentWeek();
    }

    [RelayCommand]
    private void NextWeek()
    {
        if (CurrentWeek < 20) CurrentWeek++;
    }

    [RelayCommand]
    private void PreviousWeek()
    {
        if (CurrentWeek > 1) CurrentWeek--;
    }
}

// 辅助模型：用来存每一节的起止时间
public class SectionTime
{
    public int Section { get; set; }
    public string StartTime { get; set; } = "";
    public string EndTime { get; set; } = "";
}