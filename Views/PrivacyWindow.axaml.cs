using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PlanPeopleAvalonia.Services;

namespace PlanPeopleAvalonia.Views;

public partial class PrivacyWindow : Window
{
    public PrivacyWindow()
    {
        InitializeComponent();

        this.FindControl<Button>("BtnAccept")!.Click += OnAcceptClick;
        this.FindControl<Button>("BtnDecline")!.Click += OnDeclineClick;
    }

    private void OnAcceptClick(object? sender, RoutedEventArgs e)
    {
        // 1. 记录用户已同意
        var cfg = ConfigService.Load();
        cfg.HasAcceptedPrivacy = true;
        ConfigService.Save(cfg);

        // 2. 关闭窗口，返回 OK
        Close(true);
    }

    private void OnDeclineClick(object? sender, RoutedEventArgs e)
    {
        // 不同意就直接退出程序
        Environment.Exit(0);
    }
}