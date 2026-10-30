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

    /// <summary>当前选中的导航索引（供左侧菜单高亮绑定）</summary>
    [ObservableProperty]
    private int _selectedNavIndex = 0;

    // 六大核心页面实例（按需实例化以节省内存，首次点击时才创建）
    private DashboardViewModel? _dashboardPage;
    private ScheduleViewModel? _schedulePage;
    private TeacherViewModel? _teacherPage;
    private StudentViewModel? _studentPage;
    private ExamViewModel? _examPage;
    private SettingsViewModel? _settingsPage;

    public MainWindowViewModel()
    {
        // 1. 确保本地数据目录初始化
        AppPaths.EnsureAll();

        // 2. 默认显示首页
        _dashboardPage = new DashboardViewModel();
        _currentPage = _dashboardPage;
    }

    // =====================================================
    // 导航路由指令
    // =====================================================
    
    /// <summary>
    /// 接收字符串指令进行页面切换（供 XAML 的 CommandParameter 调用）
    /// </summary>
    [RelayCommand]
    private void NavigateTo(string pageName)
    {
        CurrentPage = pageName switch
        {
            "Dashboard" => _dashboardPage ??= new DashboardViewModel(),
            "Schedule"  => _schedulePage  ??= new ScheduleViewModel(),
            "Teacher"   => _teacherPage   ??= new TeacherViewModel(),
            "Student"   => _studentPage   ??= new StudentViewModel(),
            "Exam"      => _examPage      ??= new ExamViewModel(),
            "Settings"  => _settingsPage  ??= new SettingsViewModel(),
            _ => _dashboardPage ??= new DashboardViewModel()
        };
    }

    /// <summary>
    /// 接收索引指令切换页面（如果你用 ListBox 做左侧导航，绑定索引更方便）
    /// </summary>
    [RelayCommand]
    private void NavigateByIndex(int index)
    {
        SelectedNavIndex = index;
        string pageName = index switch
        {
            0 => "Dashboard",
            1 => "Schedule",
            2 => "Teacher",
            3 => "Student",
            4 => "Exam",
            5 => "Settings",
            _ => "Dashboard"
        };
        NavigateTo(pageName);
    }
}