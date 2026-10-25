using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq; // ★ 核心修复：补上 LINQ 引用
using CommunityToolkit.Mvvm.ComponentModel;
using PlanPeopleAvalonia.Models;
using PlanPeopleAvalonia.Services;

namespace PlanPeopleAvalonia.ViewModels;

public partial class CourseEditorViewModel : ViewModelBase
{
    [ObservableProperty] private string _courseName = "";
    [ObservableProperty] private string _classroom = "";
    [ObservableProperty] private int _weekDayIndex = 0;
    [ObservableProperty] private decimal _startSection = 1;
    [ObservableProperty] private decimal _endSection = 2;
    [ObservableProperty] private Teacher? _selectedTeacher;

    // 供下拉框选择的教师列表
    public ObservableCollection<Teacher> AvailableTeachers { get; } = new();

    private Course? _editingCourse;
    public event Action? OnSaved;

    public void LoadData(Course? existing = null, List<Teacher>? teachers = null)
    {
        _editingCourse = existing;

        // 加载教师列表
        AvailableTeachers.Clear();
        if (teachers != null)
        {
            foreach (var t in teachers) AvailableTeachers.Add(t);
        }

        if (existing != null)
        {
            CourseName = existing.Name ?? "";
            Classroom = existing.Classroom ?? "";
            WeekDayIndex = existing.WeekDay - 1;
            StartSection = existing.TimeStart;
            EndSection = existing.TimeEnd;
            
            // 匹配原有教师
            SelectedTeacher = AvailableTeachers.FirstOrDefault(t => t.Name == existing.Teacher);
            if (SelectedTeacher == null && AvailableTeachers.Count > 0)
                SelectedTeacher = AvailableTeachers[0];
        }
        else
        {
            CourseName = "";
            Classroom = "";
            WeekDayIndex = 0;
            StartSection = 1;
            EndSection = 2;
            if (AvailableTeachers.Count > 0) SelectedTeacher = AvailableTeachers[0];
        }
    }

    public void Save()
    {
        if (string.IsNullOrWhiteSpace(CourseName)) return;

        var courses = CourseRepository.LoadCourses() ?? new List<Course>();

        // 如果是编辑，先删掉旧的
        if (_editingCourse != null)
        {
            courses.RemoveAll(c => c.Name == _editingCourse.Name 
                                && c.WeekDay == _editingCourse.WeekDay 
                                && c.TimeStart == _editingCourse.TimeStart);
        }

        var newCourse = new Course
        {
            Name = CourseName,
            Teacher = SelectedTeacher?.Name ?? "", // 取下拉框选中的教师
            Classroom = Classroom,
            WeekDay = WeekDayIndex + 1,
            TimeStart = (int)StartSection,
            TimeEnd = (int)EndSection,
            Weeks = new List<int> { 1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20 }
        };

        courses.Add(newCourse);
        CourseRepository.SaveCourses(courses);

        OnSaved?.Invoke();
    }
}