using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using PlanPeopleAvalonia.Models;
using PlanPeopleAvalonia.Services;
using PlanPeopleAvalonia.ViewModels;

namespace PlanPeopleAvalonia.Views.Exam; // ★ 修改这里

public class ExamListView : UserControl
{
    public event Action<ExamRecord?>? ExamSelected;
    private readonly ListBox _listBox;

    public ExamListView()
    {
        var root = new Grid { Width = 280, Background = ThemeManager.WindowBrush, RowDefinitions = new RowDefinitions("Auto, *") };
        
        var header = new TextBlock { Text = "考试列表", FontSize = 14, FontWeight = FontWeight.Bold, Foreground = ThemeManager.TextSecondaryBrush, Margin = new Thickness(16, 16, 16, 8) };
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        _listBox = new ListBox { Background = Brushes.Transparent, BorderThickness = new Thickness(0), Margin = new Thickness(8, 0) };

        _listBox.SelectionChanged += (s, e) =>
        {
            if (_listBox.SelectedItem is ListBoxItem item && item.Tag is ExamRecord record)
                ExamSelected?.Invoke(record);
        };

        Grid.SetRow(_listBox, 1);
        root.Children.Add(_listBox);
        Content = root;
    }

    public void SetExams(List<ExamRecord> exams)
    {
        _listBox.Items.Clear();
        if (exams == null) return;

        foreach (var e in exams)
        {
            var stack = new StackPanel { Spacing = 4 };
            stack.Children.Add(new TextBlock { Text = e.Name, FontWeight = FontWeight.Bold, FontSize = 14, Foreground = ThemeManager.TextPrimaryBrush });
            stack.Children.Add(new TextBlock { Text = e.Date, FontSize = 12, Foreground = ThemeManager.AccentBrush });

            var item = new ListBoxItem
            {
                Content = stack,
                Padding = new Thickness(12, 10),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(4, 2),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Tag = e
            };

            var menu = new ContextMenu();
            var delItem = new MenuItem { Header = "删除该考试记录" };
            delItem.Click += (sender, args) =>
            {
                var list = StudentRepository.LoadExams();
                if (list != null)
                {
                    list.RemoveAll(x => x.Name == e.Name);
                    StudentRepository.SaveExams(list);
                    SetExams(list); 
                    ExamSelected?.Invoke(null); 
                }
            };
            menu.Items.Add(delItem);
            item.ContextMenu = menu;

            _listBox.Items.Add(item);
        }
    }
}