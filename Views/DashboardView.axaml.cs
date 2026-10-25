using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using PlanPeopleAvalonia.ViewModels;

namespace PlanPeopleAvalonia.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        var root = new Grid { Background = ThemeManager.WindowBrush, Margin = new Thickness(40, 32, 40, 32) };
        this.Content = root;
        root.RowDefinitions = new RowDefinitions("Auto, *");

        var headerPanel = new StackPanel { Spacing = 8, Margin = new Thickness(0, 0, 0, 24) };
        headerPanel.Children.Add(new TextBlock { Text = "早上好", FontSize = 28, FontWeight = FontWeight.Bold, Foreground = ThemeManager.TextPrimaryBrush });
        headerPanel.Children.Add(new TextBlock { Text = "正在加载...", FontSize = 14, Foreground = ThemeManager.TextSecondaryBrush });
        root.Children.Add(headerPanel);

        var scroll = new ScrollViewer { [Grid.RowProperty] = 1 };
        var panel = new StackPanel { Spacing = 24 };
        scroll.Content = panel;
        root.Children.Add(scroll);

        this.DataContextChanged += (s, e) =>
        {
            if (DataContext is DashboardViewModel vm) 
            {
                // 更新问候语
                if (headerPanel.Children[0] is TextBlock tbGreeting) tbGreeting.Text = vm.Greeting;
                if (headerPanel.Children[1] is TextBlock tbDate) tbDate.Text = vm.CurrentDate;

                // 刷新面板
                panel.Children.Clear();
                var statsGrid = new UniformGrid { Columns = 3, Margin = new Thickness(-8) };
                void AddStat(string title, string value, Color accent)
                {
                    var card = new Border { Background = ThemeManager.CardBrush, CornerRadius = new CornerRadius(8), BorderBrush = ThemeManager.BorderBrush, BorderThickness = new Thickness(1), Padding = new Thickness(20), Margin = new Thickness(8) };
                    var stack = new StackPanel { Spacing = 8 };
                    stack.Children.Add(new TextBlock { Text = title, FontSize = 12, Foreground = ThemeManager.TextSecondaryBrush });
                    stack.Children.Add(new TextBlock { Text = value, FontSize = 28, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(accent) });
                    card.Child = stack;
                    statsGrid.Children.Add(card);
                }
                AddStat("当前时间", vm.CurrentTime, ThemeManager.AccentColor);
                AddStat("今日课程", vm.TodayCourseCount.ToString(), ThemeManager.TextPrimary);
                AddStat("今日积分", vm.TodayPointsDelta > 0 ? $"+{vm.TodayPointsDelta}" : vm.TodayPointsDelta.ToString(), vm.TodayPointsDelta >= 0 ? Color.Parse("#4CAF50") : Color.Parse("#E81123"));
                panel.Children.Add(statsGrid);

                panel.Children.Add(new TextBlock { Text = "今日课程", FontSize = 18, FontWeight = FontWeight.Bold, Foreground = ThemeManager.TextPrimaryBrush });
                if (vm.TodayCourses.Count == 0) panel.Children.Add(new TextBlock { Text = "今天没有课程安排，好好休息！", Foreground = ThemeManager.TextSecondaryBrush });
                else
                {
                    foreach (var c in vm.TodayCourses)
                    {
                        var row = new Border { Background = ThemeManager.CardBrush, CornerRadius = new CornerRadius(8), BorderBrush = ThemeManager.BorderBrush, BorderThickness = new Thickness(1), Padding = new Thickness(20, 16), Margin = new Thickness(0, 0, 0, 8) };
                        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto, *, Auto") };
                        grid.Children.Add(new Border { Width = 4, Background = ThemeManager.AccentBrush, CornerRadius = new CornerRadius(2), Margin = new Thickness(0, 0, 16, 0) });
                        
                        var info = new StackPanel { Spacing = 4, [Grid.ColumnProperty] = 1 };
                        info.Children.Add(new TextBlock { Text = c.Name, FontSize = 16, FontWeight = FontWeight.Bold, Foreground = ThemeManager.TextPrimaryBrush });
                        info.Children.Add(new TextBlock { Text = c.Classroom, FontSize = 12, Foreground = ThemeManager.TextSecondaryBrush });
                        grid.Children.Add(info);
                        
                        grid.Children.Add(new TextBlock { Text = c.Teacher, FontSize = 14, Foreground = ThemeManager.TextSecondaryBrush, [Grid.ColumnProperty] = 2, VerticalAlignment = VerticalAlignment.Center });
                        row.Child = grid;
                        panel.Children.Add(row);
                    }
                }
            }
        };
    }
}