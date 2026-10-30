using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using PlanPeopleAvalonia.Models;
using PlanPeopleAvalonia.Services;

namespace PlanPeopleAvalonia.ViewModels;

/// <summary>
/// 学生模块的视图模型，负责加载学生列表和成绩折线图数据。
/// </summary>
public partial class StudentViewModel : ViewModelBase
{
    public ObservableCollection<Student> Students { get; } = new();
    [ObservableProperty] private Student? _selectedStudent;

    // 供 UI 层（原生 Canvas）绘制的纯数据源
    public List<string> ChartLabels { get; private set; } = new();
    public List<double> ChartValues { get; private set; } = new();
    public bool HasChartData => ChartValues.Count > 0;

    public StudentViewModel()
    {
        LoadData();
    }

    /// <summary>
    /// 从本地加载学生数据。
    /// </summary>
    public void LoadData()
    {
        try
        {
            Students.Clear();
            var list = StudentRepository.LoadStudents() ?? new List<Student>();

            if (list.Count == 0)
            {
                list = new List<Student>
                {
                    new() { Id = "2023001", Name = "张三", Duty = "班长" },
                    new() { Id = "2023002", Name = "李四", Duty = "学习委员" }
                };
                StudentRepository.SaveStudents(list);
            }

            foreach (var s in list) Students.Add(s);
            SelectedStudent = Students.FirstOrDefault();
        }
        catch { }
    }

    /// <summary>
    /// 选中的学生改变时，重新拉取成绩点。
    /// </summary>
    partial void OnSelectedStudentChanged(Student? value)
    {
        UpdateChartData(value);
    }

    /// <summary>
    /// 从底层考试记录中提取该学生的分数，转成图表数据。
    /// </summary>
    private void UpdateChartData(Student? student)
    {
        ChartLabels = new List<string>();
        ChartValues = new List<double>();

        if (student == null) return;

        try
        {
            var exams = StudentRepository.LoadExams() ?? new List<ExamRecord>();
            foreach (var exam in exams)
            {
                if (exam.Scores != null && exam.Scores.TryGetValue(student.Id, out double score))
                {
                    ChartLabels.Add(exam.Name);
                    ChartValues.Add(score);
                }
            }
        }
        catch { }
    }
}