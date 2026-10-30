using System;
using System.Linq;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using PlanPeopleAvalonia.Models;
using PlanPeopleAvalonia.Services;

namespace PlanPeopleAvalonia.Views.Exam; // ★ 保持与文件夹同名

/// <summary>
/// 成绩详情视图（纯原生 Canvas 绘制柱状图，已去除 LiveCharts）。
/// </summary>
public class ExamDetailView : UserControl
{
    private readonly StackPanel _rootPanel;
    private ExamRecord? _currentExam = null; 
    private Action? _onScoreUpdated;

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
        _rootPanel.Children.Add(new TextBlock
        {
            Text = "请选择左侧考试查看成绩分布",
            Foreground = ThemeManager.TextSecondaryBrush,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 80, 0, 0)
        });
    }

    public void ShowExam(ExamRecord exam)
    {
        _currentExam = exam;
        _rootPanel.Children.Clear();
        if (exam == null) return;

        bool hasScores = exam.Scores != null && exam.Scores.Count > 0;
        var students = StudentRepository.LoadStudents() ?? new List<Student>();

        var topRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*, Auto") };
        topRow.Children.Add(new TextBlock { Text = "成绩分布", FontSize = 18, FontWeight = FontWeight.Bold, Foreground = ThemeManager.TextPrimaryBrush, VerticalAlignment = VerticalAlignment.Center });
        
        var btnEdit = new Button { Content = "✎ 录入分数", Background = ThemeManager.AccentBrush, Foreground = Brushes.White, Padding = new Thickness(12, 6), CornerRadius = new CornerRadius(6), FontWeight = FontWeight.Bold };
        btnEdit.Click += (s, e) =>
        {
            if (TopLevel.GetTopLevel(this) is Window owner)
            {
                // ★ 注意这里：ScoreEntryWindow 也归到了 Exam 命名空间下
                var dlg = new ScoreEntryWindow(exam, () =>
                {
                    _onScoreUpdated?.Invoke();
                    var updated = StudentRepository.LoadExams()?.Find(x => x.Name == exam.Name);
                    if (updated != null) ShowExam(updated);
                });
                dlg.ShowDialog(owner);
            }
        };
        Grid.SetColumn(btnEdit, 1);
        topRow.Children.Add(btnEdit);
        _rootPanel.Children.Add(topRow);

        if (!hasScores)
        {
            _rootPanel.Children.Add(new TextBlock { Text = "暂无成绩数据，请点击右上角录入成绩", Foreground = ThemeManager.TextSecondaryBrush, Margin = new Thickness(0, 20, 0, 0) });
            return;
        }

        // 加上 ? 和 ?? 防止空引用
var sortedScores = (exam.Scores ?? new Dictionary<string, double>()).OrderByDescending(kv => kv.Value).ToList();
        var names = sortedScores.Select(kv => students.FirstOrDefault(s => s.Id == kv.Key)?.Name ?? kv.Key).ToArray();
        var values = sortedScores.Select(kv => kv.Value).ToArray();

        var chartCard = new Border { Background = ThemeManager.CardBrush, CornerRadius = new CornerRadius(8), BorderBrush = ThemeManager.BorderBrush, BorderThickness = new Thickness(1), Padding = new Thickness(24) };
        var chartContainer = new Border { Height = 300, Child = BuildNativeBarChart(names, values) };
        chartCard.Child = chartContainer;
        _rootPanel.Children.Add(chartCard);

        var listCard = new Border { Background = ThemeManager.CardBrush, CornerRadius = new CornerRadius(8), BorderBrush = ThemeManager.BorderBrush, BorderThickness = new Thickness(1), Padding = new Thickness(24) };
        var listStack = new StackPanel { Spacing = 12 };
        listStack.Children.Add(new TextBlock { Text = "成绩明细", FontSize = 16, FontWeight = FontWeight.Bold, Foreground = ThemeManager.TextPrimaryBrush });
        
        foreach (var kv in exam.Scores)
        {
            var studentName = students.FirstOrDefault(s => s != null && s.Id == kv.Key)?.Name ?? kv.Key;
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*, Auto") };
            row.Children.Add(new TextBlock { Text = studentName, FontSize = 14, Foreground = ThemeManager.TextPrimaryBrush });
            var score = new TextBlock { Text = kv.Value.ToString("F1"), FontSize = 14, FontWeight = FontWeight.Bold, Foreground = ThemeManager.AccentBrush, [Grid.ColumnProperty] = 1 };
            row.Children.Add(score);
            listStack.Children.Add(row);
        }
        listCard.Child = listStack;
        _rootPanel.Children.Add(listCard);
    }

    private Grid BuildNativeBarChart(string[] names, double[] values)
    {
        var wrapper = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
        var canvas = new Canvas { Background = Brushes.Transparent };
        wrapper.Children.Add(canvas);

        double maxScore = values.Length > 0 ? values.Max() : 100;
        if (maxScore <= 0) maxScore = 100;
        maxScore += 10;

        void Draw()
        {
            canvas.Children.Clear();
            double width = canvas.Bounds.Width > 0 ? canvas.Bounds.Width : wrapper.Bounds.Width;
            double height = canvas.Bounds.Height > 0 ? canvas.Bounds.Height : wrapper.Bounds.Height;
            if (width <= 0) width = 500;
            if (height <= 0) height = 300;

            double paddingX = 40;
            double paddingY = 30;
            double plotWidth = Math.Max(10, width - paddingX * 2);
            double plotHeight = Math.Max(10, height - paddingY * 2);

            for (int i = 0; i <= 4; i++)
            {
                double y = paddingY + plotHeight * (i / 4.0);
                var line = new Border { Height = 1, Width = plotWidth, Background = ThemeManager.BorderBrush, Opacity = 0.5 };
                Canvas.SetLeft(line, paddingX);
                Canvas.SetTop(line, y);
                canvas.Children.Add(line);
            }

            if (values.Length == 0) return;

            double barWidth = plotWidth / values.Length * 0.6;
            double gap = plotWidth / values.Length;

            for (int i = 0; i < values.Length; i++)
            {
                double normalized = Math.Max(0, Math.Min(1, values[i] / maxScore));
                double barHeight = plotHeight * normalized;
                double x = paddingX + i * gap + (gap - barWidth) / 2;
                double y = paddingY + plotHeight - barHeight;

                var bar = new Border { Width = barWidth, Height = barHeight, Background = ThemeManager.AccentBrush, CornerRadius = new CornerRadius(4, 4, 0, 0), VerticalAlignment = VerticalAlignment.Bottom };
                Canvas.SetLeft(bar, x); Canvas.SetTop(bar, y); canvas.Children.Add(bar);

                var valueLabel = new TextBlock { Text = values[i].ToString("F0"), FontSize = 12, FontWeight = FontWeight.Bold, Foreground = ThemeManager.TextPrimaryBrush };
                Canvas.SetLeft(valueLabel, x + barWidth / 2 - 10); Canvas.SetTop(valueLabel, y - 20); canvas.Children.Add(valueLabel);

                var nameLabel = new TextBlock { Text = names[i], FontSize = 11, Foreground = ThemeManager.TextSecondaryBrush, TextAlignment = TextAlignment.Center, Width = gap };
                Canvas.SetLeft(nameLabel, paddingX + i * gap); Canvas.SetTop(nameLabel, paddingY + plotHeight + 8); canvas.Children.Add(nameLabel);
            }
        }

        wrapper.SizeChanged += (s, e) => { if (e.NewSize.Width > 0 && e.NewSize.Height > 0) Draw(); };
        canvas.Loaded += (s, e) => Draw();
        return wrapper;
    }
}