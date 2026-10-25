using System;
using System.IO;
using Avalonia;
using Avalonia.Controls; // ★ 核心修复：引入 WindowStartupLocation
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PlanPeopleAvalonia.Views;

namespace PlanPeopleAvalonia;

public partial class App : Application
{
    public override void Initialize()
    {
        File.AppendAllText("startup_log.txt", "========== [2] App Initialize 完成 ==========\n");
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        File.AppendAllText("startup_log.txt", "========== [3] 生命周期启动 ==========\n");
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                File.AppendAllText("startup_log.txt", "========== [4] 准备创建主窗口 ==========\n");
                
                var mainWindow = new MainWindow();
                
                File.AppendAllText("startup_log.txt", "========== [5] 主窗口构造函数执行完毕 ==========\n");

                desktop.MainWindow = mainWindow;
                
                // 强行把窗口拉到屏幕正中央并设置尺寸
                mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                mainWindow.Width = 1280;
                mainWindow.Height = 800;
                
                mainWindow.Show();
                mainWindow.Activate();

                File.AppendAllText("startup_log.txt", "========== [6] 主窗口 Show() 成功！ ==========\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText("startup_log.txt", "========== [致命错误] ==========\n" + ex.ToString() + "\n");
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}