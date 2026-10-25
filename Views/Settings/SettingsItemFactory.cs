using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using PlanPeopleAvalonia.ViewModels;

namespace PlanPeopleAvalonia.Views.Settings;

public static class SettingsItemFactory
{
    public static Control CreateSwitchItem(string title, string desc, bool isChecked, Action<bool> onChange)
    {
        var checkBox = new CheckBox
        {
            IsChecked = isChecked,
            VerticalAlignment = VerticalAlignment.Center
        };
        checkBox.IsCheckedChanged += (s, e) => onChange(checkBox.IsChecked == true);
        return CreateBaseRow(title, desc, checkBox);
    }

    public static Control CreateTextItem(string title, string desc, string value, Action<string> onChanged)
    {
        var textBox = new TextBox
        {
            Text = value,
            Width = 200,
            Background = ThemeManager.WindowBrush,
            Foreground = ThemeManager.TextPrimaryBrush,
            CornerRadius = new CornerRadius(6),
            VerticalAlignment = VerticalAlignment.Center
        };
        textBox.TextChanged += (s, e) => onChanged(textBox.Text ?? "");
        return CreateBaseRow(title, desc, textBox);
    }

    public static Control CreateButtonItem(string title, string desc, string btnText, Action onClick)
    {
        var btn = new Button
        {
            Content = btnText,
            Background = ThemeManager.CardBrush,
            Foreground = ThemeManager.TextPrimaryBrush,
            BorderBrush = ThemeManager.BorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(16, 6),
            VerticalAlignment = VerticalAlignment.Center
        };
        btn.Click += (s, e) => onClick();
        return CreateBaseRow(title, desc, btn);
    }

    private static Control CreateBaseRow(string title, string desc, Control rightControl)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*, Auto"),
            Margin = new Thickness(0, 12) // WinUI 标准边距
        };

        var textStack = new StackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        textStack.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 14,
            FontWeight = FontWeight.Bold,
            Foreground = ThemeManager.TextPrimaryBrush
        });
        
        if (!string.IsNullOrWhiteSpace(desc))
        {
            textStack.Children.Add(new TextBlock
            {
                Text = desc,
                FontSize = 12,
                Foreground = ThemeManager.TextSecondaryBrush,
                TextWrapping = TextWrapping.Wrap
            });
        }

        Grid.SetColumn(textStack, 0);
        grid.Children.Add(textStack);

        Grid.SetColumn(rightControl, 1);
        grid.Children.Add(rightControl);

        // 包装底部细线
        var container = new Border
        {
            BorderBrush = ThemeManager.BorderBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = grid,
            Padding = new Thickness(0, 0, 0, 12)
        };

        return container;
    }
}