using System;
using Avalonia.Controls;
using PlanPeopleAvalonia.Models;

namespace PlanPeopleAvalonia.Views.ExamComponents;

public partial class ExamEditorWindow : Window
{
    private readonly ExamRecord _exam;
    private readonly Action _onSaved;

    public ExamEditorWindow(ExamRecord exam, Action onSaved)
    {
        InitializeComponent();
        _exam = exam;
        _onSaved = onSaved;

        this.FindControl<TextBox>("TbName")!.Text = exam.Name;
        this.FindControl<TextBox>("TbSubject")!.Text = exam.Subject;
        this.FindControl<TextBox>("TbDate")!.Text = exam.Date;

        this.FindControl<Button>("BtnCancel")!.Click += (s, e) => Close();
        this.FindControl<Button>("BtnSave")!.Click += (s, e) =>
        {
            _exam.Name = this.FindControl<TextBox>("TbName")!.Text ?? "未命名";
            _exam.Subject = this.FindControl<TextBox>("TbSubject")!.Text ?? "";
            _exam.Date = this.FindControl<TextBox>("TbDate")!.Text ?? "";
            _onSaved?.Invoke();
            Close();
        };
    }
}