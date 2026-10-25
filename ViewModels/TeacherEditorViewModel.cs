using System;
using CommunityToolkit.Mvvm.ComponentModel;
using PlanPeopleAvalonia.Models;
using PlanPeopleAvalonia.Services;

namespace PlanPeopleAvalonia.ViewModels;

public partial class TeacherEditorViewModel : ViewModelBase
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _subject = "";
    [ObservableProperty] private string _title = "";
    [ObservableProperty] private string _phone = "";
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _office = "";
    [ObservableProperty] private string _info = "";
    [ObservableProperty] private string _remark = "";
    [ObservableProperty] private string _photoPath = "";
    
    private Teacher? _editingTeacher;
    public event Action<Teacher>? OnSaved;

    public void LoadData(Teacher? existing)
    {
        _editingTeacher = existing;
        if (existing != null)
        {
            Name = existing.Name ?? "";
            Subject = existing.Subject ?? "";
            Title = existing.Title ?? "";
            Phone = existing.Phone ?? "";
            Email = existing.Email ?? "";
            Office = existing.Office ?? "";
            Info = existing.Info ?? "";
            Remark = existing.Remark ?? "";
            PhotoPath = existing.PhotoPath ?? existing.Photo ?? "";
        }
        else
        {
            Name = ""; Subject = ""; Title = ""; Phone = ""; Email = "";
            Office = ""; Info = ""; Remark = ""; PhotoPath = "";
        }
    }

    public void Save()
    {
        if (string.IsNullOrWhiteSpace(Name)) return;

        var newTeacher = new Teacher
        {
            Name = Name,
            Subject = Subject,
            Title = Title,
            Phone = Phone,
            Email = Email,
            Office = Office,
            Info = Info,
            Remark = Remark,
            PhotoPath = PhotoPath,
            Photo = PhotoPath // 同步旧字段
        };

        OnSaved?.Invoke(newTeacher);
    }
}