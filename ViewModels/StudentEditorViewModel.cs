using System;
using CommunityToolkit.Mvvm.ComponentModel;
using PlanPeopleAvalonia.Models;

namespace PlanPeopleAvalonia.ViewModels;

public partial class StudentEditorViewModel : ViewModelBase
{
    [ObservableProperty] private string _id = "";
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _gender = "male";
    [ObservableProperty] private string _seat = "";
    [ObservableProperty] private string _duty = "";
    [ObservableProperty] private string _phone = ""; // 用作 ParentPhone
    [ObservableProperty] private string _birthDate = "";
    [ObservableProperty] private string _level = "";
    [ObservableProperty] private string _healthStatus = "";
    [ObservableProperty] private string _address = "";
    [ObservableProperty] private string _remark = "";
    [ObservableProperty] private string _badge = "";

    private Student? _editingStudent;
    public event Action<Student>? OnSaved;
    public event Action? OnCancelled;

    public void LoadData(Student? existing)
    {
        _editingStudent = existing;
        if (existing != null)
        {
            Id = existing.Id ?? "";
            Name = existing.Name ?? "";
            Gender = existing.Gender ?? "male";
            Seat = existing.Seat ?? "";
            Duty = existing.Duty ?? "";
            Phone = existing.ParentPhone ?? "";
            BirthDate = existing.BirthDate ?? "";
            Level = existing.Level ?? "";
            HealthStatus = existing.HealthStatus ?? "";
            Address = existing.Address ?? "";
            Remark = existing.Remark ?? "";
            Badge = existing.Badge ?? "";
        }
        else
        {
            Id = ""; Name = ""; Gender = "male"; Seat = ""; Duty = ""; Phone = "";
            BirthDate = ""; Level = ""; HealthStatus = ""; Address = ""; Remark = ""; Badge = "";
        }
    }

    public void Save()
    {
        if (string.IsNullOrWhiteSpace(Name)) return;

        var newStudent = new Student
        {
            Id = Id,
            Name = Name,
            Gender = Gender,
            Seat = Seat,
            Duty = Duty,
            ParentPhone = Phone,
            BirthDate = BirthDate,
            Level = Level,
            HealthStatus = HealthStatus,
            Address = Address,
            Remark = Remark,
            Badge = Badge
        };

        // 保留原来的其他属性（如果有）
        if (_editingStudent != null)
        {
            newStudent.Photo = _editingStudent.Photo;
            newStudent.TotalPoints = _editingStudent.TotalPoints;
            newStudent.Scores = _editingStudent.Scores;
        }

        OnSaved?.Invoke(newStudent);
    }

    public void Cancel() => OnCancelled?.Invoke();
}