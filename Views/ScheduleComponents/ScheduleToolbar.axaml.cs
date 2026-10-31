using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree; // ★ 核心修复：添加这个命名空间

namespace PlanPeopleAvalonia.Views.ScheduleComponents;

public partial class ScheduleToolbar : UserControl
{
    public ScheduleToolbar()
    {
         InitializeComponent();
    }

    // 点击节次设置按钮
    private void OnSettingsClick(object? sender, RoutedEventArgs e)
    {
        // 向上查找父级 ScheduleView 并调用弹窗方法
        var parent = this.FindAncestorOfType<PlanPeopleAvalonia.Views.ScheduleView>();
        parent?.OpenSectionSettingsDialog();
    }

    // 点击样式设置按钮
    private void OnStyleClick(object? sender, RoutedEventArgs e)
    {
        // 向上查找父级 ScheduleView 并调用弹窗方法
        var parent = this.FindAncestorOfType<PlanPeopleAvalonia.Views.ScheduleView>();
        parent?.OpenStyleSettingsDialog();
    }
}