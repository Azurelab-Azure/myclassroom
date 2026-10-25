using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanPeopleAvalonia.Models;
using PlanPeopleAvalonia.Services;

namespace PlanPeopleAvalonia.ViewModels;

/// <summary>
/// 主窗口 ViewModel，负责数据加载与全局路由。
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    /// <summary>当前显示的页面</summary>
    [ObservableProperty]
    private ViewModelBase _currentPage;

    // 六大核心页面实例
    private readonly DashboardViewModel _dashboardPage;
    private readonly ScheduleViewModel _schedulePage;
    private readonly TeacherViewModel _teacherPage;
    private readonly StudentViewModel _studentPage;
    private readonly ExamViewModel _examPage;
    private readonly SettingsViewModel _settingsPage;

    public MainWindowViewModel()
    {
        // 1. 确保本地数据目录初始化
        AppPaths.EnsureAll();

        // 2. 实例化所有页面
        _dashboardPage = new DashboardViewModel();
        _schedulePage = new ScheduleViewModel();
        _teacherPage = new TeacherViewModel();
        _studentPage = new StudentViewModel();
        _examPage = new ExamViewModel();
        _settingsPage = new SettingsViewModel();

        // 3. 首次启动无数据时，注入测试学生，防止出现空白页
        CheckAndInjectMockData();

        // 4. 默认显示首页
        _currentPage = _dashboardPage;
    }

    /// <summary>
    /// 检查如果本地是空的，就注入一些假数据，保证各个页面有东西显示
    /// </summary>
    private void CheckAndInjectMockData()
    {
        var students = StudentRepository.LoadStudents();
        if (students == null || students.Count == 0)
        {
            var mockStudents = new System.Collections.Generic.List<Student>
            {
                new() { Id = "2026001", Name = "张三", Gender = "male", Duty = "班长", Seat = "1,1" },
                new() { Id = "2026002", Name = "李四", Gender = "female", Duty = "学习委员", Seat = "1,2" },
                new() { Id = "2026003", Name = "王五", Gender = "male", Duty = "体育委员", Seat = "2,1" }
            };
            StudentRepository.SaveStudents(mockStudents);
        }

        var teachers = CourseRepository.LoadTeachers();
        if (teachers == null || teachers.Count == 0)
        {
            var mockTeachers = new System.Collections.Generic.List<Teacher>
            {
                new() { Name = "陈老师", Subject = "数学", Title = "高级教师" },
                new() { Name = "赵老师", Subject = "英语", Title = "特级教师" }
            };
            CourseRepository.SaveTeachers(mockTeachers);
            _teacherPage.LoadData();
        }
    }

    // =====================================================
    // 导航路由指令
    // =====================================================

    [RelayCommand]
    private void SwitchToDashboard() => CurrentPage = _dashboardPage;

    [RelayCommand]
    private void SwitchToSchedule() => CurrentPage = _schedulePage;

    [RelayCommand]
    private void SwitchToTeacher() => CurrentPage = _teacherPage;

    [RelayCommand]
    private void SwitchToStudent() => CurrentPage = _studentPage;

    [RelayCommand]
    private void SwitchToExam() => CurrentPage = _examPage;

    [RelayCommand]
    private void SwitchToSettings() => CurrentPage = _settingsPage;
}