using System;
using System.Windows.Forms;
using CourseApp.Localization;
using CourseApp.Services;
using CourseApp.Theme;

namespace CourseApp;

/// <summary>
/// 程序入口。
/// 根据 AppMode 选择 Form1 或 Form2。
/// </summary>
static class Program
{
    /// <summary>
    /// 主入口。
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // 读配置
        var cfg = ConfigService.Load();

        // 初始化语言
        I18n.Init(cfg.Language);

        // 初始化主题
        AppTheme.Apply(cfg.Theme ?? "light");

        // 按模式选择主窗口
        if (AppMode.IsOffice(cfg.AppMode))
            Application.Run(new Form2(cfg));
        else
            Application.Run(new Form1(cfg));
    }
}