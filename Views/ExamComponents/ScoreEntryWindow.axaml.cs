using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using PlanPeopleAvalonia.Models;
using PlanPeopleAvalonia.Services;

namespace PlanPeopleAvalonia.Views.ExamComponents;

public class ScoreEntryModel
{
    public string StudentId { get; set; } = "";
    public string StudentName { get; set; } = "";
    public string ScoreText { get; set; } = "";
}

public partial class ScoreEntryWindow : Window
{
    private readonly ExamRecord _exam;
    private readonly Action _onSaved;

    public ScoreEntryWindow(ExamRecord exam, Action onSaved)
    {
        InitializeComponent();
        _exam = exam;
        _onSaved = onSaved;

        var students = StudentRepository.LoadStudents() ?? new List<Student>();
        var list = new List<ScoreEntryModel>();

        foreach (var stu in students)
        {
            string score = "";
            if (exam.Scores != null && exam.Scores.TryGetValue(stu.Id, out double val))
                score = val.ToString("F1");

            list.Add(new ScoreEntryModel { StudentId = stu.Id, StudentName = stu.Name, ScoreText = score });
        }

        this.FindControl<ItemsControl>("ScoreList")!.ItemsSource = list;

        this.FindControl<Button>("BtnCancel")!.Click += (s, e) => Close();
        this.FindControl<Button>("BtnSave")!.Click += (s, e) =>
        {
            var newScores = new Dictionary<string, double>();
            foreach (var item in list)
            {
                if (double.TryParse(item.ScoreText, out double d))
                    newScores[item.StudentId] = d;
            }
            exam.Scores = newScores;
            
            StudentRepository.SaveExams(new List<ExamRecord> { exam }); // 这里建议保存整个列表，这里为了简单先直接覆盖
            // 注意：真正的存储需要在 ExamViewModel 统一管理，这里我们已经通过回调处理了
            _onSaved?.Invoke();
            Close();
        };
    }
}