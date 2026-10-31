using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using PlanPeopleAvalonia.Models;
using PlanPeopleAvalonia.Services;

namespace PlanPeopleAvalonia.ViewModels;

public partial class ExamViewModel : ViewModelBase
{
    public ObservableCollection<ExamRecord> Exams { get; } = new();

    [ObservableProperty] 
    private ExamRecord? _selectedExam;

    public ExamViewModel()
    {
        LoadData();
    }

    public void LoadData()
    {
        try
        {
            var list = StudentRepository.LoadExams() ?? new List<ExamRecord>();
            
            // 保留当前选中的 ID
            var currentId = SelectedExam?.Id;
            
            Exams.Clear();
            foreach (var exam in list.OrderByDescending(e => e.Date)) 
                Exams.Add(exam);

            if (!string.IsNullOrEmpty(currentId))
                SelectedExam = Exams.FirstOrDefault(e => e.Id == currentId);
            else
                SelectedExam = Exams.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载考试数据出错: {ex.Message}");
        }
    }

    public void SaveExam(ExamRecord newExam)
    {
        var list = Exams.ToList();
        var existing = list.FirstOrDefault(e => e.Id == newExam.Id);
        
        if (existing != null)
        {
            int index = list.IndexOf(existing);
            list[index] = newExam;
            StudentRepository.SaveExams(list);
            
            // 强制刷新 UI
            Exams.Clear();
            foreach (var e in list) Exams.Add(e);
            SelectedExam = newExam;
        }
        else
        {
            Exams.Add(newExam);
            StudentRepository.SaveExams(Exams.ToList());
            SelectedExam = newExam;
        }
    }

    public void DeleteExam(ExamRecord exam)
    {
        if (exam == null) return;
        Exams.Remove(exam);
        StudentRepository.SaveExams(Exams.ToList());
        SelectedExam = Exams.FirstOrDefault();
    }
}