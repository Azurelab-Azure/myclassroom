using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using PlanPeopleAvalonia.ViewModels;
using TeacherModel = PlanPeopleAvalonia.Models.Teacher;

namespace PlanPeopleAvalonia.Views;

public class TeacherListView : UserControl
{
    // 通知外部选中
    public event Action<TeacherModel>? TeacherSelected;
    // 通知外部双击
    public event Action<TeacherModel>? TeacherDoubleClicked;

    private readonly StackPanel _listPanel;

    public TeacherListView()
    {
        var root = new Grid { Width = 280, Background = ThemeManager.WindowBrush, RowDefinitions = new RowDefinitions("Auto, *") };
        var header = new TextBlock { Text = "教师列表", FontSize = 14, FontWeight = FontWeight.Bold, Foreground = ThemeManager.TextSecondaryBrush, Margin = new Thickness(16, 16, 16, 8) };
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        // 用 ScrollViewer + StackPanel 替代 ListBox，彻底避开 ListBox 的选中机制
        var scroll = new ScrollViewer { Margin = new Thickness(8, 0) };
        _listPanel = new StackPanel { Spacing = 4 };
        scroll.Content = _listPanel;

        Grid.SetRow(scroll, 1);
        root.Children.Add(scroll);
        Content = root;
    }

    public void SetTeachers(List<TeacherModel> teachers)
    {
        _listPanel.Children.Clear();
        if (teachers == null) return;

        foreach (var t in teachers)
        {
            var stack = new StackPanel { Spacing = 4, Margin = new Thickness(12, 10) };
            stack.Children.Add(new TextBlock { Text = string.IsNullOrWhiteSpace(t.Name) ? "（未命名）" : t.Name, FontWeight = FontWeight.Bold, FontSize = 14, Foreground = ThemeManager.TextPrimaryBrush });
            stack.Children.Add(new TextBlock { Text = string.IsNullOrWhiteSpace(t.Subject) ? "未设置学科" : t.Subject, FontSize = 12, Foreground = ThemeManager.AccentBrush });

            var btn = new Border
            {
                Background = ThemeManager.CardBrush,
                CornerRadius = new CornerRadius(8),
                BorderBrush = ThemeManager.BorderBrush,
                BorderThickness = new Thickness(1),
                Child = stack,
                Cursor = new Cursor(StandardCursorType.Hand),
                Tag = t
            };

            // 核心修复：直接在 Border 上挂载点击和双击事件
            btn.PointerPressed += (s, e) =>
            {
                var point = e.GetCurrentPoint(this);
                if (!point.Properties.IsLeftButtonPressed) return;

                if (e.ClickCount == 2)
                {
                    // 双击
                    TeacherDoubleClicked?.Invoke(t);
                }
                else
                {
                    // 单击
                    TeacherSelected?.Invoke(t);
                }
            };

            _listPanel.Children.Add(btn);
        }
    }
}