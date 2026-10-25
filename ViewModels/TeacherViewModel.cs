using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanPeopleAvalonia.Models;
using PlanPeopleAvalonia.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace PlanPeopleAvalonia.ViewModels;

public partial class TeacherViewModel : ViewModelBase
{
    [ObservableProperty] private ISeries[] _series = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _xAxes = Array.Empty<Axis>();
    [ObservableProperty] private Teacher? _selectedTeacher;

    public ObservableCollection<Teacher> Teachers { get; } = new();
    public ObservableCollection<Course> TeacherCourses { get; } = new();

    public TeacherViewModel()
    {
        LoadData();
    }

    /// <summary>
    /// 从底层加载教师数据并刷新图表
    /// </summary>
    public void LoadData()
    {
        var list = CourseRepository.LoadTeachers() ?? new List<Teacher>();
        
        // 如果是空的，塞入测试数据
        if (list.Count == 0)
        {
            list = new List<Teacher> 
            { 
                new Teacher { Name = "张老师", Subject = "数学" }, 
                new Teacher { Name = "李老师", Subject = "英语" } 
            };
            CourseRepository.SaveTeachers(list);
        }
        
        Teachers.Clear();
        foreach (var t in list) Teachers.Add(t);
        SelectedTeacher = Teachers.FirstOrDefault();
    }

    partial void OnSelectedTeacherChanged(Teacher? value)
    {
        UpdateChart(value);
        UpdateTeacherCourses(value);
    }

    private void UpdateTeacherCourses(Teacher? teacher)
    {
        TeacherCourses.Clear();
        if (teacher == null) return;
        var courses = CourseRepository.LoadCourses();
        if (courses != null)
        {
            foreach (var c in courses.Where(c => c.Teacher == teacher.Name)) TeacherCourses.Add(c);
        }
    }

    private void UpdateChart(Teacher? teacher)
    {
        if (teacher == null) { Series = Array.Empty<ISeries>(); return; }
        try
        {
            var exams = StudentRepository.LoadExams();
            var teacherCourses = CourseRepository.LoadCourses().Where(c => c.Teacher == teacher.Name).Select(c => c.Name).ToHashSet();
            var allScores = exams.Where(e => teacherCourses.Contains(e.Subject)).SelectMany(e => e.Scores.Values).ToList();

            double average = allScores.Count > 0 ? allScores.Average() : 0;
            double max = allScores.Count > 0 ? allScores.Max() : 0;
            double min = allScores.Count > 0 ? allScores.Min() : 0;
            double passRate = allScores.Count > 0 ? (allScores.Count(s => s >= 60) / (double)allScores.Count) * 100 : 0;

            XAxes = new[] { new Axis { Labels = new[] { "平均分", "最高分", "最低分", "及格率(%)" }, LabelsPaint = new SolidColorPaint(SKColors.Gray) } };
            Series = new ISeries[] { new ColumnSeries<double> { Values = new double[] { average, max, min, passRate }, Fill = new SolidColorPaint(SKColors.DodgerBlue), MaxBarWidth = 45 } };
        }
        catch { Series = Array.Empty<ISeries>(); }
    }

    [RelayCommand]
    private void OpenAddEditor()
    {
        // 弹窗逻辑已经移交给 TeacherView.axaml.cs 处理
    }

    [RelayCommand]
    private void OpenEditEditor()
    {
        // 弹窗逻辑已经移交给 TeacherView.axaml.cs 处理
    }
}