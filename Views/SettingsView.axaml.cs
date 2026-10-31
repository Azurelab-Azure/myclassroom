using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PlanPeopleAvalonia.ViewModels;
using PlanPeopleAvalonia.Services;

namespace PlanPeopleAvalonia.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        DataContext = new SettingsViewModel();

        // 找到右侧面板组件
        var detailPanel = this.FindControl<SettingsComponents.SettingsDetailPanel>("DetailPanel");
        
        if (detailPanel != null)
        {
            // 绑定“保存设置”按钮
            var btnSave = detailPanel.FindControl<Button>("BtnSave");
            if (btnSave != null) btnSave.Click += OnSaveClick;

            // 绑定“恢复默认”按钮
            var btnReset = detailPanel.FindControl<Button>("BtnReset");
            if (btnReset != null) btnReset.Click += OnResetClick;

            // ★ 如果你在 SettingsDetailPanel.axaml 里加了“查看隐私协议”按钮，可以在这里绑定
            // var btnPrivacy = detailPanel.FindControl<Button>("BtnPrivacy");
            // if (btnPrivacy != null) btnPrivacy.Click += OnPrivacyClick;
        }
    }

    // 保存设置并重启
    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            vm.SaveSettings();
            // 保存后必须重启，才能应用新主题和字体
            RestartApp();
        }
    }

    // 恢复默认并重启
    private void OnResetClick(object? sender, RoutedEventArgs e)
    {
        // 保留用户的隐私协议状态，重置其他所有设置
        var currentConfig = ConfigService.Load();
        var newConfig = new AppConfig { HasAcceptedPrivacy = currentConfig.HasAcceptedPrivacy };
        
        ConfigService.Save(newConfig);
        RestartApp();
    }

    // 查看隐私协议弹窗
    private async void OnPrivacyClick(object? sender, RoutedEventArgs e)
    {
        var privacyWindow = new PrivacyWindow();
        var parentWindow = TopLevel.GetTopLevel(this) as Window;
        if (parentWindow != null) await privacyWindow.ShowDialog(parentWindow);
    }

    // 重启应用
    private void RestartApp()
    {
        try
        {
            var exe = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exe)) 
            {
                System.Diagnostics.Process.Start(exe);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"重启失败: {ex.Message}");
        }
        finally
        {
            Environment.Exit(0);
        }
    }
}