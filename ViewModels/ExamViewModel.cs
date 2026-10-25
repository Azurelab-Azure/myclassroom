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
    /// <summary>
    /// 考试记录列表，供左侧 ExamListView 绑定
    /// </summary>
    public ObservableCollection<ExamRecord> Exams { get; } = new();

    /// <summary>
    /// 当前选中的考试（当用户点击左侧列表时变化）
    /// </summary>
    [ObservableProperty]
    private ExamRecord? _selectedExam;

    /// <summary>
    /// 当 SelectedExam 发生变化时，触发此事件。
    /// 由总装文件 ExamView.axaml.cs 订阅，用来刷新右侧的 ExamDetailView。
    /// </summary>
    public event Action<ExamRecord?>? ExamChanged;

    public ExamViewModel()
    {
        LoadData();
    }

    /// <summary>
    /// 从底层 JSON 读取所有的考试记录
    /// </summary>
    public void LoadData()
    {
        try
        {
            Exams.Clear();
            var list = StudentRepository.LoadExams() ?? new List<ExamRecord>();
            
            foreach (var exam in list)
            {
                Exams.Add(exam);
            }

            // 默认选中第一场考试
            SelectedExam = Exams.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ExamViewModel] 加载数据失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 当选中的考试改变时，通知外部 UI 更新
    /// </summary>
    partial void OnSelectedExamChanged(ExamRecord? value)
    {
        ExamChanged?.Invoke(value);
    }

    /// <summary>
    /// 新增或更新一场考试（供 ExamEditorWindow 保存时调用）
    /// </summary>
    public void SaveExam(ExamRecord newExam, ExamRecord? existingExam = null)
    {
        try
        {
            var list = StudentRepository.LoadExams() ?? new List<ExamRecord>();

            if (existingExam != null)
            {
                // 编辑模式：找到旧的并替换
                int idx = list.FindIndex(e => e.Name == existingExam.Name && e.Date == existingExam.Date);
                if (idx >= 0) list[idx] = newExam;
                else list.Add(newExam);
            }
            else
            {
                // 新增模式
                list.Add(newExam);
            }

            StudentRepository.SaveExams(list);
            
            // 重新加载以刷新左侧列表
            LoadData();
            
            // 保持选中刚刚保存的考试
            var saved = Exams.FirstOrDefault(e => e.Name == newExam.Name && e.Date == newExam.Date);
            if (saved != null) SelectedExam = saved;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ExamViewModel] 保存失败: {ex.Message}");
        }
    }
}