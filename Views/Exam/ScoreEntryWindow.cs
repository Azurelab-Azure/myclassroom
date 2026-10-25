using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using PlanPeopleAvalonia.Models;
using PlanPeopleAvalonia.Services;
using PlanPeopleAvalonia.ViewModels;

namespace PlanPeopleAvalonia.Views.ExamComponents;

public class ScoreEntryWindow : Window
{
    private readonly ExamRecord _exam;
    private readonly Action _onSaved;
    private readonly Dictionary<string, TextBox> _scoreBoxes = new();

    public ScoreEntryWindow(ExamRecord exam, Action onSaved)
    {
        _exam = exam;
        _onSaved = onSaved ?? (() => { });

        Title = $"录入成绩 - {exam.Name}";
        Width = 480;
        Height = 600;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = ThemeManager.WindowBrush;

        var root = new Grid { RowDefinitions = new RowDefinitions("Auto, *, Auto"), Margin = new Thickness(24) };

        var title = new TextBlock
        {
            Text = "成绩录入",
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            Foreground = ThemeManager.TextPrimaryBrush,
            Margin = new Thickness(0, 0, 0, 16)
        };
        Grid.SetRow(title, 0);
        root.Children.Add(title);

        var formStack = new StackPanel { Spacing = 12 };
        var students = StudentRepository.LoadStudents() ?? new List<Student>();

        foreach (var stu in students)
        {
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("120, *") };
            
            row.Children.Add(new TextBlock
            {
                Text = stu.Name,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = ThemeManager.TextPrimaryBrush
            });

            var tb = new TextBox
            {
                Watermark = "分数",
                Width = 100,
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = ThemeManager.CardBrush,
                Foreground = ThemeManager.TextPrimaryBrush,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 8)
            };

            // 回显已有分数
            if (exam.Scores != null && exam.Scores.TryGetValue(stu.Id, out double score))
                tb.Text = score.ToString("F1");

            _scoreBoxes[stu.Id] = tb;
            Grid.SetColumn(tb, 1);
            row.Children.Add(tb);
            formStack.Children.Add(row);
        }

        var scroll = new ScrollViewer { Content = formStack };
        Grid.SetRow(scroll, 1);
        root.Children.Add(scroll);

        var btnRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 20, 0, 0)
        };

        var cancel = new Button { Content = "取消", Padding = new Thickness(20, 8), CornerRadius = new CornerRadius(8), Background = ThemeManager.CardBrush, Foreground = ThemeManager.TextPrimaryBrush };
        cancel.Click += (s, e) => Close();
        btnRow.Children.Add(cancel);

        var save = new Button { Content = "保存成绩", Padding = new Thickness(20, 8), CornerRadius = new CornerRadius(8), Background = ThemeManager.AccentBrush, Foreground = Brushes.White, FontWeight = FontWeight.Bold };
        save.Click += (s, e) => SaveAndClose();
        btnRow.Children.Add(save);

        Grid.SetRow(btnRow, 2);
        root.Children.Add(btnRow);
        Content = root;
    }

    private void SaveAndClose()
    {
        var newScores = new Dictionary<string, double>();
        foreach (var kv in _scoreBoxes)
        {
            if (double.TryParse(kv.Value.Text, out double score))
                newScores[kv.Key] = score;
        }

        try
        {
            var exams = StudentRepository.LoadExams() ?? new List<ExamRecord>();
            var target = exams.FirstOrDefault(e => e.Name == _exam.Name);
            if (target != null)
            {
                target.Scores = newScores;
                StudentRepository.SaveExams(exams);
            }
            _onSaved();
            Close();
        }
        catch { }
    }
}