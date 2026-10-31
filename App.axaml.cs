using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PlanPeopleAvalonia.Views;
using PlanPeopleAvalonia.Services;

namespace PlanPeopleAvalonia;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        
        // 启动时加载主题
        ThemeManager.LoadTheme();
    }

public override void OnFrameworkInitializationCompleted()
{
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        // 1. 检查隐私协议
        var cfg = ConfigService.Load();
        if (!cfg.HasAcceptedPrivacy)
        {
            var privacyWindow = new PrivacyWindow();
            privacyWindow.Show(); // 先展示协议
        }

        // 2. 正常启动主窗口
        ThemeManager.ApplyTheme(ThemeManager.CurrentTheme);

        desktop.MainWindow = new MainWindow
        {
            DataContext = new PlanPeopleAvalonia.ViewModels.MainWindowViewModel()
        };
    }
    base.OnFrameworkInitializationCompleted();
}
}