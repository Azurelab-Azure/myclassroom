using Avalonia.Media;
using PlanPeopleAvalonia.Services;

namespace PlanPeopleAvalonia.Views;

public enum AppTheme { Light, Dark, Clear, Fresh }

public static class ThemeManager
{
    public static AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

    /// <summary>从 config.json 加载主题</summary>
    public static void LoadTheme()
    {
        var cfg = ConfigService.Load();
        if (System.Enum.TryParse<AppTheme>(cfg.Theme, true, out var theme))
        {
            CurrentTheme = theme;
        }
        else
        {
            CurrentTheme = AppTheme.Light;
        }
    }

    /// <summary>保存主题并重启应用</summary>
    public static void SaveAndRestart(AppTheme theme)
    {
        var cfg = ConfigService.Load();
        cfg.Theme = theme.ToString();
        ConfigService.Save(cfg);

        // 重启应用
        try
        {
            var exe = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(exe))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exe,
                    UseShellExecute = true
                });
            }
        }
        catch { }
        
        // 退出当前进程
        System.Environment.Exit(0);
    }

    // --- 颜色定义保持不变 ---
    public static Color WindowBg => CurrentTheme switch
    {
        AppTheme.Light => Color.Parse("#F3F3F3"),
        AppTheme.Dark => Color.Parse("#1E1E1E"),
        AppTheme.Clear => Color.Parse("#ECF1F8"),
        AppTheme.Fresh => Color.Parse("#F0F7F2"),
        _ => Color.Parse("#F3F3F3")
    };

    public static Color CardBg => CurrentTheme switch
    {
        AppTheme.Light => Colors.White,
        AppTheme.Dark => Color.Parse("#2D2D2D"),
        AppTheme.Clear => Color.Parse("#F8FBFF"),
        AppTheme.Fresh => Color.Parse("#FBFFFC"),
        _ => Colors.White
    };

    public static Color BorderColor => CurrentTheme switch
    {
        AppTheme.Light => Color.Parse("#E5E5E5"),
        AppTheme.Dark => Color.Parse("#404040"),
        AppTheme.Clear => Color.Parse("#CCD9E8"),
        AppTheme.Fresh => Color.Parse("#D2E8D8"),
        _ => Color.Parse("#E5E5E5")
    };

    public static Color TextPrimary => CurrentTheme switch
    {
        AppTheme.Light => Color.Parse("#1A1A1A"),
        AppTheme.Dark => Colors.White,
        AppTheme.Clear => Color.Parse("#1F2A3D"),
        AppTheme.Fresh => Color.Parse("#1A3326"),
        _ => Color.Parse("#1A1A1A")
    };

    public static Color TextSecondary => CurrentTheme switch
    {
        AppTheme.Light => Color.Parse("#606060"),
        AppTheme.Dark => Color.Parse("#C5C5C5"),
        AppTheme.Clear => Color.Parse("#5A6B82"),
        AppTheme.Fresh => Color.Parse("#557A62"),
        _ => Color.Parse("#606060")
    };

    public static Color AccentColor => CurrentTheme switch
    {
        AppTheme.Light => Color.Parse("#0078D4"),
        AppTheme.Dark => Color.Parse("#60CDFF"),
        AppTheme.Clear => Color.Parse("#2E86E8"),
        AppTheme.Fresh => Color.Parse("#2EA05E"),
        _ => Color.Parse("#0078D4")
    };
    
    public static IBrush WindowBrush => new SolidColorBrush(WindowBg);
    public static IBrush CardBrush => new SolidColorBrush(CardBg);
    public static IBrush BorderBrush => new SolidColorBrush(BorderColor);
    public static IBrush TextPrimaryBrush => new SolidColorBrush(TextPrimary);
    public static IBrush TextSecondaryBrush => new SolidColorBrush(TextSecondary);
    public static IBrush AccentBrush => new SolidColorBrush(AccentColor);
}