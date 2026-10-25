using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using PlanPeopleAvalonia.Models;
using PlanPeopleAvalonia.Services;
using PlanPeopleAvalonia.ViewModels;

namespace PlanPeopleAvalonia.Views.ExamComponents;

public class ExamEditorWindow : Window
{
    private readonly Action _onSaved;
    private readonly TextBox _tbName;
    private readonly TextBox _tbDate;

    public ExamEditorWindow(Action onSaved)
    {
        _onSaved = onSaved ?? (() => { });
        Title = "录入新考试";
        Width = 400;
        Height = 300;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = ThemeManager.WindowBrush;
        CanResize = false;

        var root = new Grid { RowDefinitions = new RowDefinitions("Auto, *, Auto"), Margin = new Thickness(24) };

        var title = new TextBlock { Text = "创建考试", FontSize = 20, FontWeight = FontWeight.Bold, Foreground = ThemeManager.TextPrimaryBrush, Margin = new Thickness(0, 0, 0, 16) };
        Grid.SetRow(title, 0);
        root.Children.Add(title);

        var form = new StackPanel { Spacing = 12 };
        form.Children.Add(new TextBlock { Text = "考试名称 *", Foreground = ThemeManager.TextSecondaryBrush, FontSize = 13 });
        _tbName = new TextBox { Watermark = "如：期中考试", Background = ThemeManager.CardBrush, Foreground = ThemeManager.TextPrimaryBrush, CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 8) };
        form.Children.Add(_tbName);

        form.Children.Add(new TextBlock { Text = "考试日期 (YYYY-MM-DD)", Foreground = ThemeManager.TextSecondaryBrush, FontSize = 13 });
        _tbDate = new TextBox { Text = DateTime.Now.ToString("yyyy-MM-dd"), Background = ThemeManager.CardBrush, Foreground = ThemeManager.TextPrimaryBrush, CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 8) };
        form.Children.Add(_tbDate);

        Grid.SetRow(form, 1);
        root.Children.Add(form);

        var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 20, 0, 0) };
        var cancel = new Button { Content = "取消", Padding = new Thickness(20, 8), CornerRadius = new CornerRadius(8), Background = ThemeManager.CardBrush, Foreground = ThemeManager.TextPrimaryBrush };
        cancel.Click += (s, e) => Close();
        btnRow.Children.Add(cancel);

        var save = new Button { Content = "保存", Padding = new Thickness(20, 8), CornerRadius = new CornerRadius(8), Background = ThemeManager.AccentBrush, Foreground = Brushes.White, FontWeight = FontWeight.Bold };
        save.Click += (s, e) => SaveAndClose();
        btnRow.Children.Add(save);

        Grid.SetRow(btnRow, 2);
        root.Children.Add(btnRow);
        Content = root;
    }

    private void SaveAndClose()
    {
        var name = _tbName.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return;

        var newExam = new ExamRecord
        {
            Name = name,
            Date = _tbDate.Text?.Trim() ?? DateTime.Now.ToString("yyyy-MM-dd"),
            Scores = new Dictionary<string, double>()
        };

        try
        {
            var list = StudentRepository.LoadExams() ?? new List<ExamRecord>();
            list.Add(newExam);
            StudentRepository.SaveExams(list);
            _onSaved();
            Close();
        }
        catch { }
    }
}