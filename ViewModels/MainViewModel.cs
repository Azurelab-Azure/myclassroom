using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PlanPeopleAvalonia.ViewModels;

/// <summary>
/// 主窗口 ViewModel，负责数据状态。
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    // ✅ 正确写法：打上标签的必须是带下划线的【私有字段】
    [ObservableProperty]
    private string _greeting = "早上好"; 

    [ObservableProperty]
    private string _currentDate = DateTime.Now.ToString("yyyy 年 M 月 d 日");

    public MainViewModel()
    {
        // 初始化逻辑
    }
}