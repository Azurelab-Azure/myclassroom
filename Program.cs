using System;
using System.Windows.Forms;
using CourseApp.Localization;
using CourseApp.Services;
using CourseApp.Theme;

namespace CourseApp;

static class Program
{
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

        // 主窗口接收 config
        Application.Run(new Form1(cfg));
    }
}