using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using LiveChartsCore.SkiaSharpView.Avalonia;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using PlanPeopleAvalonia.Models;
using PlanPeopleAvalonia.Services;
using PlanPeopleAvalonia.ViewModels;
using PlanPeopleAvalonia.Services;

namespace PlanPeopleAvalonia.Views.ExamComponents;

public class ExamDetailView : UserControl
{
    private readonly StackPanel _rootPanel;
    private ExamRecord _currentExam; // 记录当前查看的考试，用于刷新
    private Action _onScoreUpdated;  // 刷新外部列表的回调

    public ExamDetailView()
    {
        _rootPanel = new StackPanel { Spacing = 16, Margin = new Thickness(20) };
        Content = new ScrollViewer { Content = _rootPanel };
        Clear();
    }

    public void SetUpdateCallback(Action onUpdated) => _onScoreUpdated = onUpdated;

    public void Clear()
    {
        _currentExam = null;
        _rootPanel.Children.Clear();
        _rootPanel.Children.Add(new TextBlock { Text = "请选择左侧考试查看成绩分布", Foreground = ThemeManager.TextSecondaryBrush, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 80, 0, 0) });
    }

    public void ShowExam(ExamRecord exam)
    {
        _currentExam = exam;
        _rootPanel.Children.Clear();
        if (exam == null) return;

        // 如果没有分数，也不直接 return，而是显示一个空图表和提示，以便可以点击“录入分数”
        bool hasScores = exam.Scores != null && exam.Scores.Count > 0;
        var students = StudentRepository.LoadStudents() ?? new System.Collections.Generic.List<Student>();

        // 头部操作行
        var topRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*, Auto") };
        topRow.Children.Add(new TextBlock { Text = "成绩分布", FontSize = 18, FontWeight = FontWeight.Bold, Foreground = ThemeManager.TextPrimaryBrush, VerticalAlignment = VerticalAlignment.Center });
        
        var btnEdit = new Button { Content = "✎ 录入分数", Background = ThemeManager.AccentBrush, Foreground = Brushes.White, Padding = new Thickness(12, 6), CornerRadius = new CornerRadius(6), FontWeight = FontWeight.Bold };
        btnEdit.Click += (s, e) =>
        {
            if (TopLevel.GetTopLevel(this) is Window owner)
                new ScoreEntryWindow(exam, () => { 
                    _onScoreUpdated?.Invoke(); 
                    // 保存后自身重新加载
                    var updated = StudentRepository.LoadExams()?.Find(x => x.Name == exam.Name);
                    if (updated != null) ShowExam(updated);
                }).ShowDialog(owner);
        };
        Grid.SetColumn(btnEdit, 1);
        topRow.Children.Add(btnEdit);
        _rootPanel.Children.Add(topRow);

        if (!hasScores)
        {
            _rootPanel.Children.Add(new TextBlock { Text = "暂无成绩数据，请点击右上角录入成绩", Foreground = ThemeManager.TextSecondaryBrush, Margin = new Thickness(0, 20, 0, 0) });
            return;
        }

        var sortedScores = exam.Scores.OrderByDescending(kv => kv.Value).ToList();

        // 柱状图卡片 (保留原逻辑)
        var chartCard = new Border { Background = ThemeManager.CardBrush, CornerRadius = new CornerRadius(8), BorderBrush = ThemeManager.BorderBrush, BorderThickness = new Thickness(1), Padding = new Thickness(24) };
        var chartStack = new StackPanel { Spacing = 16 };
        var names = sortedScores.Select(kv => students.FirstOrDefault(s => s.Id == kv.Key)?.Name ?? kv.Key).ToArray();
        var values = sortedScores.Select(kv => kv.Value).ToArray();
        var xAxes = new[] { new LiveChartsCore.SkiaSharpView.Axis { Labels = names, LabelsPaint = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SkiaSharp.SKColors.Gray) } };
        var series = new LiveChartsCore.ISeries[] { new LiveChartsCore.SkiaSharpView.ColumnSeries<double> { Values = values, Fill = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SkiaSharp.SKColors.DodgerBlue), MaxBarWidth = 45 } };
        chartStack.Children.Add(new LiveChartsCore.SkiaSharpView.Avalonia.CartesianChart { Series = series, XAxes = xAxes, Height = 300, Background = Brushes.Transparent });
        chartCard.Child = chartStack;
        _rootPanel.Children.Add(chartCard);

        // 明细卡片 (保留原逻辑)
        var listCard = new Border { Background = ThemeManager.CardBrush, CornerRadius = new CornerRadius(8), BorderBrush = ThemeManager.BorderBrush, BorderThickness = new Thickness(1), Padding = new Thickness(24) };
        var listStack = new StackPanel { Spacing = 12 };
        listStack.Children.Add(new TextBlock { Text = "成绩明细", FontSize = 16, FontWeight = FontWeight.Bold, Foreground = ThemeManager.TextPrimaryBrush });
        foreach (var kv in exam.Scores)
        {
            var studentName = students.FirstOrDefault(s => s.Id == kv.Key)?.Name ?? kv.Key;
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*, Auto") };
            row.Children.Add(new TextBlock { Text = studentName, FontSize = 14, Foreground = ThemeManager.TextPrimaryBrush });
            var score = new TextBlock { Text = kv.Value.ToString("F1"), FontSize = 14, FontWeight = FontWeight.Bold, Foreground = ThemeManager.AccentBrush, [Grid.ColumnProperty] = 1 };
            row.Children.Add(score);
            listStack.Children.Add(row);
        }
        listCard.Child = listStack;
        _rootPanel.Children.Add(listCard);
    }
}