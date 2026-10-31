using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanPeopleAvalonia.Models;
using PlanPeopleAvalonia.Services;

namespace PlanPeopleAvalonia.ViewModels;

public partial class TeacherViewModel : ViewModelBase
{
    [ObservableProperty] private Teacher? _selectedTeacher;
    public ObservableCollection<Teacher> Teachers { get; } = new();
    public ObservableCollection<Course> TeacherCourses { get; } = new();

    public TeacherViewModel()
    {
        LoadData();
    }

    public void LoadData()
    {
        try
        {
            var list = CourseRepository.LoadTeachers();
            Teachers.Clear();
            foreach (var t in list) Teachers.Add(t);
            
            // 如果有选中的，重新匹配；否则选第一个
            if (SelectedTeacher != null)
                SelectedTeacher = Teachers.FirstOrDefault(t => t.Name == SelectedTeacher.Name) ?? Teachers.FirstOrDefault();
            else
                SelectedTeacher = Teachers.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载教师出错: {ex.Message}");
        }
    }

    partial void OnSelectedTeacherChanged(Teacher? value)
    {
        TeacherCourses.Clear();
        if (value == null) return;

        var courses = CourseRepository.LoadCourses();
        foreach (var c in courses.Where(c => c.Teacher == value.Name)) 
            TeacherCourses.Add(c);
    }

    [RelayCommand]
    private void AddTeacher()
    {
        var newTeacher = new Teacher { Name = "新教师", Subject = "未设置", Title = "助教" };
        Teachers.Add(newTeacher);
        CourseRepository.SaveTeachers(Teachers);
        SelectedTeacher = newTeacher;
    }

    [RelayCommand]
    private void DeleteTeacher()
    {
        if (SelectedTeacher == null) return;
        Teachers.Remove(SelectedTeacher);
        CourseRepository.SaveTeachers(Teachers);
        SelectedTeacher = Teachers.FirstOrDefault();
    }
}