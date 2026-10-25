using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using PlanPeopleAvalonia.Models;
using PlanPeopleAvalonia.Services;

namespace PlanPeopleAvalonia.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    [ObservableProperty] private string _greeting = "";
    [ObservableProperty] private string _currentDate = "";
    [ObservableProperty] private string _currentTime = "";
    [ObservableProperty] private int _todayCourseCount;
    [ObservableProperty] private int _todayPointsDelta;
    [ObservableProperty] private int _totalPoints;

    public ObservableCollection<Course> TodayCourses { get; } = new();

    public DashboardViewModel()
    {
        LoadData();
    }

    public void LoadData()
    {
        var now = DateTime.Now;
        Greeting = now.Hour switch
        {
            < 6 => "夜深了", < 12 => "早上好", < 14 => "中午好", < 18 => "下午好", _ => "晚上好"
        };
        CurrentDate = now.ToString("yyyy 年 M 月 d 日 dddd");
        CurrentTime = now.ToString("HH:mm");

        int currentWeek = 1; 
        int todayWeekday = (int)now.DayOfWeek;
        if (todayWeekday == 0) todayWeekday = 7;

        // 1. 读取真实课程
        var allCourses = CourseRepository.LoadCourses();
        var coursesToday = allCourses.Where(c => c.WeekDay == todayWeekday && c.Weeks != null && c.Weeks.Contains(currentWeek))
            .OrderBy(c => c.TimeStart).ToList();

        TodayCourses.Clear();
        foreach (var c in coursesToday) TodayCourses.Add(c);
        TodayCourseCount = TodayCourses.Count;

        // 2. 读取真实积分
        var students = StudentRepository.LoadStudents() ?? new();
        var records = ScoreRepository.LoadRecords() ?? new();
        
        TodayPointsDelta = records.Where(r => DateTime.TryParse(r.Date, out var d) && d.Date == DateTime.Today).Sum(r => r.Delta);
        ScoreRepository.RecalculateStudentPoints(students, records);
        TotalPoints = students.Sum(s => s.TotalPoints);
    }
}