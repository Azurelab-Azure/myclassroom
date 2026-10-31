using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PlanPeopleAvalonia.Models;
using PlanPeopleAvalonia.ViewModels;
using PlanPeopleAvalonia.Views.StudentComponents;

namespace PlanPeopleAvalonia.Views;

public partial class StudentView : UserControl
{
    private StudentViewModel _vm;

    public StudentView()
    {
        InitializeComponent();
        
        _vm = new StudentViewModel();
        this.DataContext = _vm;
    }

    private async void OnAddClick(object? sender, RoutedEventArgs e)
    {
        var newStudent = new Student { Name = "新同学", Id = Guid.NewGuid().ToString("N").Substring(0, 6) };
        
        var dialog = new StudentEditorWindow(newStudent, () =>
        {
            _vm.AddStudent(newStudent);
        });

        var parentWindow = TopLevel.GetTopLevel(this) as Window;
        if (parentWindow != null) await dialog.ShowDialog(parentWindow);
    }

    private void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (_vm.SelectedStudent == null) return;
        _vm.DeleteStudent(_vm.SelectedStudent);
    }

    private async void OnEditClick(object? sender, RoutedEventArgs e)
    {
        if (_vm.SelectedStudent == null) return;

        // 深拷贝防止修改中途取消污染数据
        var copy = new Student
        {
            Id = _vm.SelectedStudent.Id,
            Name = _vm.SelectedStudent.Name,
            Duty = _vm.SelectedStudent.Duty,
            Gender = _vm.SelectedStudent.Gender,
            Seat = _vm.SelectedStudent.Seat,
            Level = _vm.SelectedStudent.Level,
            HealthStatus = _vm.SelectedStudent.HealthStatus,
            ParentPhone = _vm.SelectedStudent.ParentPhone,
            Address = _vm.SelectedStudent.Address,
            Info = _vm.SelectedStudent.Info,
            PhotoPath = _vm.SelectedStudent.PhotoPath
        };

        var dialog = new StudentEditorWindow(copy, () =>
        {
            _vm.UpdateStudent(copy);
        });

        var parentWindow = TopLevel.GetTopLevel(this) as Window;
        if (parentWindow != null) await dialog.ShowDialog(parentWindow);
    }
}