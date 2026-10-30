using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using PlanPeopleAvalonia.ViewModels;

namespace PlanPeopleAvalonia.Views.Exam; // ★ 修改这里

public class ExamTopBar : UserControl
{
    public event Action? AddClicked;

    public ExamTopBar()
    {
        var borderWrap = new Border { Background = ThemeManager.CardBrush, BorderBrush = ThemeManager.BorderBrush, BorderThickness = new Thickness(0, 0, 0, 1) };
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto, *, Auto"), Margin = new Thickness(20, 12) };

        var title = new TextBlock { Text = "成绩管理", FontSize = 18, FontWeight = FontWeight.Bold, Foreground = ThemeManager.TextPrimaryBrush, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(title, 0);
        grid.Children.Add(title);

        var addBtn = new Button { Content = "＋ 录入新考试", Background = ThemeManager.AccentBrush, Foreground = Brushes.White, Padding = new Thickness(16, 8), CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(0), FontWeight = FontWeight.Bold };
        addBtn.Click += (s, e) => AddClicked?.Invoke();
        Grid.SetColumn(addBtn, 2);
        grid.Children.Add(addBtn);

        borderWrap.Child = grid;
        Content = borderWrap;
    }
}