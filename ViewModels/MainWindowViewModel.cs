using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanPeopleAvalonia.Services;
using PlanPeopleAvalonia.Views; // 用于 ThemeManager

namespace PlanPeopleAvalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentPage;

    [ObservableProperty]
    private int _selectedNavIndex = 0;

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

        // ★ 3. 核心！订阅全局配置变更事件
        ConfigService.ConfigChanged += OnConfigChanged;
    }

    // 当设置页保存时触发
    private void OnConfigChanged(AppConfig cfg)
    {
        // 1. 动态应用主题
        if (Enum.TryParse<AppTheme>(cfg.Theme, true, out var theme))
        {
            ThemeManager.ApplyTheme(theme);
        }

        // 2. 灵动岛开关逻辑 (你可以据此弹出/关闭浮动窗口)
        if (cfg.EnableIsland)
        {
            Console.WriteLine("【系统指令】灵动岛：开启");
            // TODO: 实例化 IslandWindow 并 Show()
        }
        else
        {
            Console.WriteLine("【系统指令】灵动岛：关闭");
            // TODO: 找到 IslandWindow 并 Close()
        }
    }

    // =====================================================
    // 导航路由
    // =====================================================

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
            _           => _dashboardPage ??= new DashboardViewModel()
        };
    }

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