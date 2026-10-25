using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using PlanPeopleAvalonia.ViewModels;

namespace PlanPeopleAvalonia.Views.Settings;

public class SettingsTopBar : UserControl
{
    public event Action? SaveClicked;
    public event Action? ResetClicked;

    public SettingsTopBar()
    {
        var borderWrap = new Border
        {
            Background = ThemeManager.CardBrush,
            BorderBrush = ThemeManager.BorderBrush,
            BorderThickness = new Thickness(0, 0, 0, 1)
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*, Auto"),
            Margin = new Thickness(24, 16)
        };

        var title = new TextBlock
        {
            Text = "设置",
            FontSize = 22,
            FontWeight = FontWeight.Bold,
            Foreground = ThemeManager.TextPrimaryBrush,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(title, 0);
        grid.Children.Add(title);

        var btnStack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            VerticalAlignment = VerticalAlignment.Center
        };

        var btnReset = new Button
        {
            Content = "恢复默认",
            Background = Brushes.Transparent,
            Foreground = ThemeManager.TextSecondaryBrush,
            BorderThickness = new Thickness(1),
            BorderBrush = ThemeManager.BorderBrush,
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(16, 8)
        };
        btnReset.Click += (s, e) => ResetClicked?.Invoke();
        btnStack.Children.Add(btnReset);

        var btnSave = new Button
        {
            Content = "保存",
            Background = ThemeManager.AccentBrush,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(24, 8),
            FontWeight = FontWeight.Bold
        };
        btnSave.Click += (s, e) => SaveClicked?.Invoke();
        btnStack.Children.Add(btnSave);

        Grid.SetColumn(btnStack, 1);
        grid.Children.Add(btnStack);

        borderWrap.Child = grid;
        Content = borderWrap;
    }
}