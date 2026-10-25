using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using PlanPeopleAvalonia.Services;
using PlanPeopleAvalonia.ViewModels;
using PlanPeopleAvalonia.Views.Settings;

namespace PlanPeopleAvalonia.Views;

public partial class SettingsView : UserControl
{
    private readonly SettingsTopBar _topBar;
    private readonly SettingsNavView _navView;
    private readonly SettingsDetailView _detailView;

    public SettingsView()
    {
        _topBar = new SettingsTopBar();
        _navView = new SettingsNavView();
        _detailView = new SettingsDetailView();

        // 1. 事件联动
        _navView.CategorySelected += category => _detailView.ShowCategory(category);
        _topBar.SaveClicked += OnSaveClicked;
        _topBar.ResetClicked += OnResetClicked;

        // 2. 整体布局：上 TopBar，下分左右
        var body = new Grid { ColumnDefinitions = new ColumnDefinitions("200, *") };
        
        Grid.SetColumn(_navView, 0);
        body.Children.Add(_navView);

        Grid.SetColumn(_detailView, 1);
        body.Children.Add(_detailView);

        var root = new Grid { RowDefinitions = new RowDefinitions("Auto, *") };
        
        Grid.SetRow(_topBar, 0);
        root.Children.Add(_topBar);
        
        Grid.SetRow(body, 1);
        root.Children.Add(body);

        Content = root;

        // 3. 初始化默认视图
        _detailView.ShowCategory("通用");
    }

    private async void OnSaveClicked()
    {
        try
        {
            // 假设存在 ConfigService 且提供了 Save 方法
            // ConfigService.Save();
            
            if (TopLevel.GetTopLevel(this) is Window owner)
            {
                var dlg = new Window
                {
                    Title = "提示",
                    Width = 300,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Background = ThemeManager.WindowBrush
                };
                var panel = new StackPanel { Margin = new Thickness(20), Spacing = 16 };
                panel.Children.Add(new TextBlock { Text = "设置已成功保存！", Foreground = ThemeManager.TextPrimaryBrush });
                var ok = new Button { Content = "确定", HorizontalAlignment = HorizontalAlignment.Right, Background = ThemeManager.AccentBrush, Foreground = Brushes.White, Padding = new Thickness(16, 6), CornerRadius = new CornerRadius(6) };
                ok.Click += (s, e) => dlg.Close();
                panel.Children.Add(ok);
                dlg.Content = panel;
                await dlg.ShowDialog(owner);
            }
        }
        catch (Exception ex)
        {
            // 处理保存异常
        }
    }

    private void OnResetClicked()
    {
        // 处理恢复默认逻辑
        _detailView.ShowCategory("通用");
    }
}