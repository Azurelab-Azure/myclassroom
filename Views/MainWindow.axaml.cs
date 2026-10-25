using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using PlanPeopleAvalonia.ViewModels;
namespace PlanPeopleAvalonia.Views;

public partial class MainWindow : Window
{
    private ContentControl _contentControl = null!;

    public MainWindow()
    {
        this.Width = 1280;
        this.Height = 800;
        this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

        var root = new Grid { Name = "MainRoot" };
        this.Content = root;

        var vm = new MainWindowViewModel();
        this.DataContext = vm;

        BuildUI(root, vm);

        // ★ 核心修复：监听 CurrentPage 变化，实现动态页面切换
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.CurrentPage))
            {
                if (_contentControl != null)
                {
                    _contentControl.Content = vm.CurrentPage;
                }
            }
        };
    }

    private void BuildUI(Grid root, MainWindowViewModel vm)
    {
        root.Children.Clear();
        root.RowDefinitions = new RowDefinitions("44, *");
        root.Background = ThemeManager.WindowBrush;

        // 1. 顶部标题栏
        var titleBar = new Grid { ColumnDefinitions = new ColumnDefinitions("*, Auto"), Background = ThemeManager.WindowBrush };
        titleBar.PointerPressed += (s, e) =>
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginMoveDrag(e);
        };

        var titleText = new TextBlock { Text = "PlanPeople", FontSize = 14, FontWeight = FontWeight.Bold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(16, 0, 0, 0), Foreground = ThemeManager.TextPrimaryBrush };
        titleBar.Children.Add(titleText);

        var winBtns = new StackPanel { Orientation = Orientation.Horizontal, [Grid.ColumnProperty] = 1 };
        var btnMin = new Button { Content = "—", Width = 46, Height = 44, Background = Brushes.Transparent, BorderThickness = new Thickness(0), Foreground = ThemeManager.TextPrimaryBrush };
        btnMin.Click += (s, e) => WindowState = WindowState.Minimized;
        
        var btnMax = new Button { Content = "□", Width = 46, Height = 44, Background = Brushes.Transparent, BorderThickness = new Thickness(0), Foreground = ThemeManager.TextPrimaryBrush };
        btnMax.Click += (s, e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        
        var btnClose = new Button { Content = "✕", Width = 46, Height = 44, Background = Brushes.Transparent, BorderThickness = new Thickness(0), Foreground = ThemeManager.TextPrimaryBrush };
        btnClose.Click += (s, e) => Close();
        
        winBtns.Children.Add(btnMin);
        winBtns.Children.Add(btnMax);
        winBtns.Children.Add(btnClose);
        titleBar.Children.Add(winBtns);
        Grid.SetRow(titleBar, 0);
        root.Children.Add(titleBar);

        // 2. 主体
        var mainGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("240, *") };
        Grid.SetRow(mainGrid, 1);
        root.Children.Add(mainGrid);

        var sidebar = new StackPanel { Margin = new Thickness(0, 16, 0, 0) };
        
        void AddNavBtn(string pathData, string text, Action action)
        {
            var btn = new Button 
            { 
                HorizontalAlignment = HorizontalAlignment.Stretch, 
                HorizontalContentAlignment = HorizontalAlignment.Left, 
                Padding = new Thickness(16, 12),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0)
            };
            
            var stack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };
            var icon = new Path
            {
                Data = Geometry.Parse(pathData),
                Width = 18,
                Height = 18,
                Stretch = Stretch.Uniform,
                Fill = ThemeManager.TextSecondaryBrush,
                VerticalAlignment = VerticalAlignment.Center
            };
            stack.Children.Add(icon);
            stack.Children.Add(new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center, Foreground = ThemeManager.TextPrimaryBrush, FontSize = 14 });
            
            btn.Content = stack;
            btn.Click += (s, e) => action();
            sidebar.Children.Add(btn);
        }

        AddNavBtn("M10 20v-6h4v6h5v-8h3L12 3 2 12h3v8z", "首页", () => vm.SwitchToDashboardCommand.Execute(null));
        AddNavBtn("M19 4h-1V2h-2v2H8V2H6v2H5c-1.11 0-1.99.9-1.99 2L3 20c0 1.1.89 2 2 2h14c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm0 16H5V10h14v10z", "课表", () => vm.SwitchToScheduleCommand.Execute(null));
        AddNavBtn("M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z", "教师", () => vm.SwitchToTeacherCommand.Execute(null));
        AddNavBtn("M16 11c1.66 0 2.99-1.34 2.99-3S17.66 5 16 5c-1.66 0-3 1.34-3 3s1.34 3 3 3zm-8 0c1.66 0 2.99-1.34 2.99-3S9.66 5 8 5C6.34 5 5 6.34 5 8s1.34 3 3 3zm0 2c-2.33 0-7 1.17-7 3.5V19h14v-2.5c0-2.33-4.67-3.5-7-3.5zm8 0c-.29 0-.62.02-.97.05 1.16.84 1.97 1.97 1.97 3.45V19h6v-2.5c0-2.33-4.67-3.5-7-3.5z", "学生", () => vm.SwitchToStudentCommand.Execute(null));
        AddNavBtn("M12 3L1 9l11 6 9-4.91V17h2V9L12 3zM5 13.18v4L12 21l7-3.82v-4L12 17l-7-3.82z", "成绩", () => vm.SwitchToExamCommand.Execute(null));
        
        sidebar.Children.Add(new Rectangle { Height = 1, Fill = ThemeManager.BorderBrush, Margin = new Thickness(16, 8, 16, 8) });
        AddNavBtn("M19.14,12.94c0.04-0.3,0.06-0.61,0.06-0.94c0-0.32-0.02-0.64-0.06-0.94l2.03-1.58c0.18-0.14,0.23-0.41,0.12-0.61 l-1.92-3.32c-0.12-0.22-0.37-0.29-0.59-0.22l-2.39,0.96c-0.5-0.38-1.03-0.7-1.62-0.94L14.4,2.81c-0.04-0.24-0.24-0.41-0.48-0.41 h-3.84c-0.24,0-0.43,0.17-0.47,0.41L9.25,5.35C8.66,5.59,8.12,5.92,7.63,6.29L5.24,5.33c-0.22-0.08-0.47,0-0.59,0.22L2.74,8.87 C2.62,9.08,2.66,9.34,2.86,9.48l2.03,1.58C4.84,11.36,4.8,11.69,4.8,12s0.02,0.64,0.06,0.94l-2.03,1.58 c-0.18,0.14-0.23,0.41-0.12,0.61l1.92,3.32c0.12,0.22,0.37,0.29,0.59,0.22l2.39-0.96c0.5,0.38,1.03,0.7,1.62,0.94l0.36,2.54 c0.05,0.24,0.24,0.41,0.48,0.41h3.84c0.24,0,0.44-0.17,0.47-0.41l0.36-2.54c0.59-0.24,1.13-0.56,1.62-0.94l2.39,0.96 c0.22,0.08,0.47,0,0.59-0.22l1.92-3.32c0.12-0.22,0.07-0.49-0.12-0.61L19.14,12.94z M12,15.6c-1.98,0-3.6-1.62-3.6-3.6 s1.62-3.6,3.6-3.6s3.6,1.62,3.6,3.6S13.98,15.6,12,15.6z", "设置", () => vm.SwitchToSettingsCommand.Execute(null));

        var sideBorder = new Border { Background = ThemeManager.WindowBrush, BorderBrush = ThemeManager.BorderBrush, BorderThickness = new Thickness(0, 0, 1, 0), Child = sidebar };
        Grid.SetColumn(sideBorder, 0);
        mainGrid.Children.Add(sideBorder);

        // ★ 核心修复：把右侧的 ContentControl 存下来，方便上面监听事件时更新
        _contentControl = new ContentControl { Content = vm.CurrentPage, [Grid.ColumnProperty] = 1 };
        mainGrid.Children.Add(_contentControl);
    }
}