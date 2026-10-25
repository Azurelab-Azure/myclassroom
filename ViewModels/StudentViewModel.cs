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

public partial class StudentViewModel : ViewModelBase
{
    public ObservableCollection<Student> Students { get; } = new();
    [ObservableProperty] private Student? _selectedStudent;

    // 成绩折线图
    [ObservableProperty] private ISeries[] _series = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _xAxes = Array.Empty<Axis>();

    public StudentViewModel()
    {
        LoadData();
    }

    public void LoadData()
    {
        try
        {
            Students.Clear();
            var list = StudentRepository.LoadStudents() ?? new List<Student>();
            
            // 如果没有学生，塞入假数据测试
            if (list.Count == 0)
            {
                list = new List<Student> 
                { 
                    new Student { Id = "2023001", Name = "张三", Duty = "班长" }, 
                    new Student { Id = "2023002", Name = "李四", Duty = "学习委员" } 
                };
                StudentRepository.SaveStudents(list);
            }
            
            foreach (var s in list) Students.Add(s);
            SelectedStudent = Students.FirstOrDefault();
        }
        catch { }
    }

    partial void OnSelectedStudentChanged(Student? value)
    {
        UpdateChart(value);
    }

    private void UpdateChart(Student? student)
    {
        if (student == null) { Series = Array.Empty<ISeries>(); return; }

        try
        {
            // 联动核心：去 exams.json 里找该学生的所有成绩
            var exams = StudentRepository.LoadExams();
            var scores = new List<double>();
            var examNames = new List<string>();

            foreach (var exam in exams)
            {
                if (exam.Scores != null && exam.Scores.TryGetValue(student.Id, out double score))
                {
                    scores.Add(score);
                    examNames.Add(exam.Name);
                }
            }

            // 如果有成绩，画折线图；没成绩就清空
            if (scores.Count > 0)
            {
                XAxes = new[] { new Axis { Labels = examNames.ToArray(), LabelsPaint = new SolidColorPaint(SKColors.Gray) } };
                Series = new ISeries[] { new LineSeries<double> { Values = scores.ToArray(), Fill = new SolidColorPaint(SKColors.DodgerBlue.WithAlpha(50)), Stroke = new SolidColorPaint(SKColors.DodgerBlue, 2), GeometrySize = 8 } };
            }
            else
            {
                Series = Array.Empty<ISeries>();
            }
        }
        catch { Series = Array.Empty<ISeries>(); }
    }
}