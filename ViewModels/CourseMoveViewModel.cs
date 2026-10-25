using System;
using CommunityToolkit.Mvvm.ComponentModel;
using PlanPeopleAvalonia.Models;
using PlanPeopleAvalonia.Services;

namespace PlanPeopleAvalonia.ViewModels;

public partial class CourseMoveViewModel : ViewModelBase
{
    [ObservableProperty] private int _targetWeekDayIndex = 0;
    [ObservableProperty] private decimal _targetStartSection = 1;
    [ObservableProperty] private decimal _targetEndSection = 1;

    private Course? _courseToMove;
    public event Action? OnMoved;

    public void LoadData(Course course)
    {
        _courseToMove = course;
        // 默认填入当前的值
        TargetWeekDayIndex = course.WeekDay - 1;
        TargetStartSection = course.TimeStart;
        TargetEndSection = course.TimeEnd;
    }

    public void Confirm()
    {
        if (_courseToMove == null) return;

        var courses = CourseRepository.LoadCourses();
        // 找到原课程并修改坐标
        var target = courses.Find(c => c.Name == _courseToMove.Name && c.WeekDay == _courseToMove.WeekDay && c.TimeStart == _courseToMove.TimeStart);
        
        if (target != null)
        {
            target.WeekDay = TargetWeekDayIndex + 1;
            target.TimeStart = (int)TargetStartSection;
            target.TimeEnd = (int)TargetEndSection;
            CourseRepository.SaveCourses(courses);
            OnMoved?.Invoke();
        }
    }
}