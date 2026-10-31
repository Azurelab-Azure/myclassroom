using Avalonia.Controls;
using Avalonia.Interactivity;
using PlanPeopleAvalonia.ViewModels;

namespace PlanPeopleAvalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent(); // ★ 致命修复：解注释，保证 UI 渲染
        DataContext = new MainWindowViewModel();
    }

    private void OnNavSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.NavigateByIndexCommand.Execute(vm.SelectedNavIndex);
        }
    }
}