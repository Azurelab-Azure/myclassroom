using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using PlanPeopleAvalonia.ViewModels;

namespace PlanPeopleAvalonia.Views.Settings;

public class SettingsDetailView : UserControl
{
    private readonly StackPanel _contentStack;
    private readonly ScrollViewer _scrollViewer;

    public SettingsDetailView()
    {
        _contentStack = new StackPanel { Spacing = 24, Margin = new Thickness(32, 24) };
        _scrollViewer = new ScrollViewer
        {
            Content = _contentStack,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled
        };
        Content = _scrollViewer;
    }

    public void ShowCategory(string category)
    {
        _contentStack.Children.Clear();
        
        var title = new TextBlock
        {
            Text = $"{category}设置",
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            Foreground = ThemeManager.TextPrimaryBrush,
            Margin = new Thickness(0, 0, 0, 8)
        };
        _contentStack.Children.Add(title);

        switch (category)
        {
            case "通用":
                _contentStack.Children.Add(BuildCard(
                    SettingsItemFactory.CreateSwitchItem("开机自动启动", "Windows 启动时自动运行程序", false, (v) => { }),
                    SettingsItemFactory.CreateTextItem("默认语言", "设置应用的界面语言", "简体中文", (v) => { })
                ));
                break;
                
            case "外观":
                _contentStack.Children.Add(BuildCard(
                    SettingsItemFactory.CreateSwitchItem("跟随系统主题", "自动适应系统的明亮/暗黑模式", true, (v) => { }),
                    SettingsItemFactory.CreateButtonItem("自定义主题色", "点击选择强调色", "选择颜色", () => { })
                ));
                break;

            case "课表":
                _contentStack.Children.Add(BuildCard(
                    SettingsItemFactory.CreateSwitchItem("显示周末", "在课表中显示周六和周日", true, (v) => { }),
                    SettingsItemFactory.CreateTextItem("每节课时长", "单位：分钟", "45", (v) => { })
                ));
                break;

            case "数据":
                _contentStack.Children.Add(BuildCard(
                    SettingsItemFactory.CreateButtonItem("备份数据", "将当前数据导出到本地", "立即备份", () => { }),
                    SettingsItemFactory.CreateButtonItem("恢复数据", "从本地备份文件恢复", "选择备份文件", () => { })
                ));
                break;

            default:
                _contentStack.Children.Add(BuildCard(
                    SettingsItemFactory.CreateTextItem("分类占位", "该分类的具体内容待开发", "", (v) => { })
                ));
                break;
        }
    }

    private Border BuildCard(params Control[] items)
    {
        var stack = new StackPanel();
        foreach (var item in items)
        {
            stack.Children.Add(item);
        }

        return new Border
        {
            Background = ThemeManager.CardBrush,
            BorderBrush = ThemeManager.BorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(20, 4, 20, 4), // 上下由内部的 Row Padding 撑起
            Child = stack
        };
    }
}