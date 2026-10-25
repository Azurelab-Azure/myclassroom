using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using PlanPeopleAvalonia.ViewModels;

namespace PlanPeopleAvalonia.Views.Settings;

public class SettingsNavView : UserControl
{
    public event Action<string>? CategorySelected;

    private readonly StackPanel _navStack;
    private readonly Dictionary<string, Button> _buttons = new();
    private string _selectedCategory = "通用";

    public SettingsNavView()
    {
        var root = new Border
        {
            Width = 200,
            Background = ThemeManager.WindowBrush,
            BorderBrush = ThemeManager.BorderBrush,
            BorderThickness = new Thickness(0, 0, 1, 0)
        };

        _navStack = new StackPanel { Spacing = 4, Margin = new Thickness(12, 16) };

        string[] categories = { "通用", "外观", "课表", "声音", "数据", "高级" };
        foreach (var cat in categories)
        {
            var btn = new Button
            {
                Content = cat,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Background = Brushes.Transparent,
                Foreground = ThemeManager.TextPrimaryBrush,
                Padding = new Thickness(16, 10),
                CornerRadius = new CornerRadius(6),
                FontSize = 14
            };
            
            var captured = cat;
            btn.Click += (s, e) => 
            {
                SetSelected(captured);
                CategorySelected?.Invoke(captured);
            };
            
            _buttons[cat] = btn;
            _navStack.Children.Add(btn);
        }

        root.Child = _navStack;
        Content = root;

        SetSelected("通用");
    }

    public void SetSelected(string category)
    {
        _selectedCategory = category;
        foreach (var kv in _buttons)
        {
            bool isSelected = kv.Key == category;
            kv.Value.Background = isSelected ? ThemeManager.AccentBrush : Brushes.Transparent;
            kv.Value.Foreground = isSelected ? Brushes.White : ThemeManager.TextPrimaryBrush;
            kv.Value.FontWeight = isSelected ? FontWeight.Bold : FontWeight.Normal;
        }
    }
}