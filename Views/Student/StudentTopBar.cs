using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using PlanPeopleAvalonia.ViewModels;

namespace PlanPeopleAvalonia.Views.StudentComponents;

public class StudentTopBar : UserControl
{
    public event Action? AddClicked;
    public event Action<string>? SearchTextChanged;

    public string SearchText => _searchBox.Text ?? string.Empty;
    private readonly TextBox _searchBox;

    public StudentTopBar()
    {
        var borderWrap = new Border { Background = ThemeManager.CardBrush, BorderBrush = ThemeManager.BorderBrush, BorderThickness = new Thickness(0, 0, 0, 1) };
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto, *, Auto"), Margin = new Thickness(20, 12) };

        var title = new TextBlock { Text = "学生", FontSize = 18, FontWeight = FontWeight.Bold, Foreground = ThemeManager.TextPrimaryBrush, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(title, 0);
        grid.Children.Add(title);

        _searchBox = new TextBox { Watermark = "搜索姓名或学号...", Width = 260, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(24, 0, 0, 0), Background = ThemeManager.WindowBrush, Foreground = ThemeManager.TextPrimaryBrush, CornerRadius = new CornerRadius(8) };
        _searchBox.TextChanged += (s, e) => SearchTextChanged?.Invoke(SearchText);
        Grid.SetColumn(_searchBox, 1);
        grid.Children.Add(_searchBox);

        var addBtn = new Button { Content = "＋ 添加学生", Background = ThemeManager.AccentBrush, Foreground = Brushes.White, Padding = new Thickness(16, 8), CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(0), FontWeight = FontWeight.Bold };
        addBtn.Click += (s, e) => AddClicked?.Invoke();
        Grid.SetColumn(addBtn, 2);
        grid.Children.Add(addBtn);

        borderWrap.Child = grid;
        Content = borderWrap;
    }
}