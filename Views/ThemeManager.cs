using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using PlanPeopleAvalonia.Services;

namespace PlanPeopleAvalonia.Views;

public enum AppTheme { Light, Dark, Clear, Fresh }

public static class ThemeManager
{
    public static AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

    /// <summary>从 config 加载主题</summary>
    public static void LoadTheme()
    {
        var cfg = ConfigService.Load();
        if (Enum.TryParse<AppTheme>(cfg.Theme, true, out var theme))
            CurrentTheme = theme;
        else
            CurrentTheme = AppTheme.Light;
    }

    /// <summary>真实应用主题到全局 UI</summary>
    public static void ApplyTheme(AppTheme theme)
    {
        CurrentTheme = theme;
        
        if (Application.Current != null)
        {
            Application.Current.RequestedThemeVariant = theme switch
            {
                AppTheme.Dark => ThemeVariant.Dark,
                _ => ThemeVariant.Light
            };

            // ★ 真实变更全局背景色
            if (Application.Current.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow != null)
                {
                    desktop.MainWindow.Background = new SolidColorBrush(WindowBg);
                }
            }
        }
    }

    /// <summary>保存并应用主题</summary>
    public static void SaveAndApply(AppTheme theme)
    {
        var cfg = ConfigService.Load();
        cfg.Theme = theme.ToString();
        ConfigService.Save(cfg);
        ApplyTheme(theme);
    }

    // ================= 颜色定义 =================
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
    
    // 供 XAML 绑定的 Brush
    public static IBrush WindowBrush => new SolidColorBrush(WindowBg);
    public static IBrush CardBrush => new SolidColorBrush(CardBg);
    public static IBrush BorderBrush => new SolidColorBrush(BorderColor);
    public static IBrush TextPrimaryBrush => new SolidColorBrush(TextPrimary);
    public static IBrush TextSecondaryBrush => new SolidColorBrush(TextSecondary);
    public static IBrush AccentBrush => new SolidColorBrush(AccentColor);
}