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
/// 教师模块的视图模型。
/// </summary>
public partial class TeacherViewModel : ViewModelBase
{
    [ObservableProperty] private Teacher? _selectedTeacher;

    // 供 UI 层（原生 Canvas 柱状图）绘制的纯数据
    [ObservableProperty] private double _chartAverage;
    [ObservableProperty] private double _chartMax;
    [ObservableProperty] private double _chartMin;
    [ObservableProperty] private double _chartPassRate;

    public ObservableCollection<Teacher> Teachers { get; } = new();
    public ObservableCollection<Course> TeacherCourses { get; } = new();

    public TeacherViewModel()
    {
        LoadData();
    }

    /// <summary>
    /// 从底层加载教师数据。
    /// </summary>
    public void LoadData()
    {
        var list = CourseRepository.LoadTeachers() ?? new List<Teacher>();
        
        if (list.Count == 0)
        {
            list = new List<Teacher>
            {
                new() { Name = "张老师", Subject = "数学" },
                new() { Name = "李老师", Subject = "英语" }
            };
            CourseRepository.SaveTeachers(list);
        }
        
        Teachers.Clear();
        foreach (var t in list) Teachers.Add(t);
        SelectedTeacher = Teachers.FirstOrDefault();
    }

    partial void OnSelectedTeacherChanged(Teacher? value)
    {
        UpdateChartData(value);
        UpdateTeacherCourses(value);
    }

    /// <summary>
    /// 更新该教师所授课程列表。
    /// </summary>
    private void UpdateTeacherCourses(Teacher? teacher)
    {
        TeacherCourses.Clear();
        if (teacher == null) return;
        
        var courses = CourseRepository.LoadCourses();
        if (courses != null)
        {
            foreach (var c in courses.Where(c => c.Teacher == teacher.Name)) 
                TeacherCourses.Add(c);
        }
    }

    /// <summary>
    /// 计算教师所授科目的成绩统计（供柱状图使用）。
    /// </summary>
    private void UpdateChartData(Teacher? teacher)
    {
        if (teacher == null) { ChartAverage = 0; ChartMax = 0; ChartMin = 0; ChartPassRate = 0; return; }

        try
        {
            var exams = StudentRepository.LoadExams();
            var teacherCourses = CourseRepository.LoadCourses()
                .Where(c => c.Teacher == teacher.Name)
                .Select(c => c.Name)
                .ToHashSet();

            // 兼容 ExamRecord 没有 Subject 字段的情况，用考试名匹配
            var allScores = exams
                .Where(e => string.IsNullOrEmpty(e.Subject) || teacherCourses.Contains(e.Subject))
                .SelectMany(e => e.Scores.Values)
                .ToList();

            if (allScores.Count > 0)
            {
                ChartAverage = allScores.Average();
                ChartMax = allScores.Max();
                ChartMin = allScores.Min();
                ChartPassRate = (allScores.Count(s => s >= 60) / (double)allScores.Count) * 100;
            }
            else
            {
                ChartAverage = 0; ChartMax = 0; ChartMin = 0; ChartPassRate = 0;
            }
        }
        catch 
        { 
            ChartAverage = 0; ChartMax = 0; ChartMin = 0; ChartPassRate = 0; 
        }
    }

    [RelayCommand]
    private void OpenAddEditor() { /* 交由 View 层处理弹窗 */ }

    [RelayCommand]
    private void OpenEditEditor() { /* 交由 View 层处理弹窗 */ }
}