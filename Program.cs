using Avalonia;
using System;
using System.IO;

namespace PlanPeopleAvalonia;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        File.WriteAllText("startup_log.txt", "========== [1] 程序开始运行 ==========\n");
        
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            File.AppendAllText("startup_log.txt", "========== [崩溃] ==========\n" + e.ExceptionObject.ToString() + "\n");
        };

        try
        {
            // ★ 核心修复：关闭 GPU 硬件加速，强制使用软件渲染
            BuildAvaloniaApp()
                .With(new Win32PlatformOptions { RenderingMode = new[] { Win32RenderingMode.Software } })
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            File.AppendAllText("startup_log.txt", "========== [异常] ==========\n" + ex.ToString() + "\n");
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}