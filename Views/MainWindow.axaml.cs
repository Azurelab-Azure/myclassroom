using Avalonia.Controls;
using Avalonia.Interactivity; // 引入事件参数
using PlanPeopleAvalonia.ViewModels;

namespace PlanPeopleAvalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // 绑定总控 ViewModel
        DataContext = new MainWindowViewModel();
    }

    /// <summary>
    /// 当左侧导航栏选中项改变时触发（辅助 UI 交互）
    /// </summary>
    private void OnNavSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            // 触发 ViewModel 中的命令，切换页面
            vm.NavigateByIndexCommand.Execute(vm.SelectedNavIndex);
        }
    }
}