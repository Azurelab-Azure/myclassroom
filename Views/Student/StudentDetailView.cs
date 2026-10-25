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

namespace PlanPeopleAvalonia.Views.StudentComponents;

public class StudentDetailView : UserControl
{
    private readonly StackPanel _root;

    public StudentDetailView()
    {
        _root = new StackPanel { Spacing = 16, Margin = new Thickness(20) };
        Content = new ScrollViewer { Content = _root, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        Clear();
    }

    public void Clear()
    {
        _root.Children.Clear();
        _root.Children.Add(new TextBlock
        {
            Text = "请选择左侧学生查看详情",
            Foreground = ThemeManager.TextSecondaryBrush,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 80, 0, 0)
        });
    }

    public void ShowStudent(Student student)
    {
        _root.Children.Clear();
        if (student == null) return;
        
        _root.Children.Add(BuildProfileCard(student));
        _root.Children.Add(BuildScoresCard(student));
    }

    // ================= 档案卡片 =================
    private Border BuildProfileCard(Student s)
    {
        var card = new Border
        {
            Background = ThemeManager.CardBrush,
            BorderBrush = ThemeManager.BorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(20)
        };

        var outer = new Grid { ColumnDefinitions = new ColumnDefinitions("120, *") };
        outer.Children.Add(BuildAvatar(s));

        var info = new StackPanel { Spacing = 10, Margin = new Thickness(16, 0, 0, 0) };
        
        info.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(s.Name) ? "（未命名）" : s.Name,
            FontSize = 22, FontWeight = FontWeight.Bold, Foreground = ThemeManager.TextPrimaryBrush
        });

        var infoGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*, *"), RowDefinitions = new RowDefinitions("Auto, Auto") };
        void Put(int row, int col, string label, string val)
        {
            var p = new StackPanel { Spacing = 2, Margin = new Thickness(0, 4, 8, 4) };
            p.Children.Add(new TextBlock { Text = label, FontSize = 11, Foreground = ThemeManager.TextSecondaryBrush });
            p.Children.Add(new TextBlock { Text = string.IsNullOrWhiteSpace(val) ? "—" : val, FontSize = 14, Foreground = ThemeManager.TextPrimaryBrush, TextWrapping = TextWrapping.Wrap });
            Grid.SetRow(p, row); Grid.SetColumn(p, col);
            infoGrid.Children.Add(p);
        }
        
        Put(0, 0, "学号", s.Id);
        Put(0, 1, "职务", s.Duty);
        info.Children.Add(infoGrid);

        Grid.SetColumn(info, 1);
        outer.Children.Add(info);
        card.Child = outer;
        return card;
    }

    private static Border BuildAvatar(Student s)
    {
        var layer = new Grid();
        layer.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(s.Name) ? "?" : s.Name.Substring(0, 1),
            FontSize = 44, FontWeight = FontWeight.Bold, Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
        });
        return new Border { Width = 110, Height = 110, CornerRadius = new CornerRadius(55), Background = ThemeManager.AccentBrush, ClipToBounds = true, Child = layer };
    }

    // ================= 成绩卡片 =================
    private Border BuildScoresCard(Student s)
    {
        var card = new Border
        {
            Background = ThemeManager.CardBrush,
            BorderBrush = ThemeManager.BorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(20)
        };

        var stack = new StackPanel { Spacing = 16 };
        stack.Children.Add(new TextBlock
        {
            Text = "历次考试成绩趋势",
            FontSize = 16, FontWeight = FontWeight.Bold,
            Foreground = ThemeManager.TextPrimaryBrush
        });

        var (names, scores) = SafeLoadScores(s);

        if (scores.Count > 0)
        {
            // 将 Canvas 放入一个指定最小高度和宽度的容器，确保 Canvas 能获取到尺寸
            var chartContainer = new Border
            {
                Height = 220,
                Background = Brushes.Transparent,
                Child = BuildNativeChart(names, scores)
            };
            stack.Children.Add(chartContainer);

            var detailRow = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 16, LineSpacing = 8, Margin = new Thickness(0, 8, 0, 0) };
            for (int i = 0; i < scores.Count; i++)
            {
                detailRow.Children.Add(new TextBlock
                {
                    Text = $"{names[i]}: {scores[i]:F1}",
                    Foreground = ThemeManager.TextSecondaryBrush,
                    FontSize = 12
                });
            }
            stack.Children.Add(detailRow);
        }
        else
        {
            stack.Children.Add(new TextBlock
            {
                Text = "暂无该学生的考试数据",
                Foreground = ThemeManager.TextSecondaryBrush,
                Margin = new Thickness(0, 10, 0, 10)
            });
        }

        card.Child = stack;
        return card;
    }

    private static (List<string> names, List<double> scores) SafeLoadScores(Student s)
    {
        var names = new List<string>();
        var scores = new List<double>();
        if (s == null || string.IsNullOrEmpty(s.Id)) return (names, scores);

        try
        {
            var exams = StudentRepository.LoadExams();
            if (exams == null) return (names, scores);

            foreach (var e in exams)
            {
                if (e.Scores != null && e.Scores.TryGetValue(s.Id, out double score))
                {
                    names.Add(e.Name ?? "考试");
                    scores.Add(score);
                }
            }
        }
        catch { }
        return (names, scores);
    }

    private Grid BuildNativeChart(List<string> names, List<double> scores)
    {
        // 用 Grid 包裹 Canvas，确保它能被父容器撑开
        var wrapper = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
        var canvas = new Canvas { Background = Brushes.Transparent };
        wrapper.Children.Add(canvas);

        double maxScore = scores.Count > 0 ? scores.Max() : 100;
        double minScore = scores.Count > 0 ? Math.Max(0, scores.Min() - 10) : 0;
        if (maxScore == minScore) maxScore += 10;

        // 强制在 Loaded 和 SizeChanged 时重新绘制，确保尺寸变化时图表自适应拉伸
        void Draw()
        {
            canvas.Children.Clear();
            var points = new List<Point>();

            // 获取真实的可用宽高
            double width = canvas.Bounds.Width > 0 ? canvas.Bounds.Width : wrapper.Bounds.Width;
            double height = canvas.Bounds.Height > 0 ? canvas.Bounds.Height : wrapper.Bounds.Height;
            
            if (width <= 0) width = 500; // 兜底宽度
            if (height <= 0) height = 200; // 兜底高度

            double paddingX = 30;
            double paddingY = 20;
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

            for (int i = 0; i < scores.Count; i++)
            {
                double x = paddingX + (scores.Count == 1 ? plotWidth / 2 : plotWidth * (i / (double)(scores.Count - 1)));
                double normalizedValue = (scores[i] - minScore) / (maxScore - minScore);
                double y = paddingY + plotHeight * (1 - normalizedValue);
                points.Add(new Point(x, y));
            }

            for (int i = 0; i < points.Count - 1; i++)
            {
                var p1 = points[i];
                var p2 = points[i + 1];
                double length = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
                double angle = Math.Atan2(p2.Y - p1.Y, p2.X - p1.X) * 180 / Math.PI;

                var line = new Border
                {
                    Width = length,
                    Height = 2,
                    Background = ThemeManager.AccentBrush,
                    RenderTransform = new RotateTransform(angle),
                    RenderTransformOrigin = new RelativePoint(0, 0.5, RelativeUnit.Relative)
                };
                Canvas.SetLeft(line, p1.X);
                Canvas.SetTop(line, p1.Y);
                canvas.Children.Add(line);
            }

            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                var dot = new Border
                {
                    Width = 10, Height = 10,
                    CornerRadius = new CornerRadius(5),
                    Background = ThemeManager.CardBrush,
                    BorderBrush = ThemeManager.AccentBrush,
                    BorderThickness = new Thickness(2)
                };
                Canvas.SetLeft(dot, p.X - 5);
                Canvas.SetTop(dot, p.Y - 5);
                canvas.Children.Add(dot);

                var label = new TextBlock
                {
                    Text = scores[i].ToString("F0"),
                    FontSize = 12, FontWeight = FontWeight.Bold,
                    Foreground = ThemeManager.TextPrimaryBrush
                };
                Canvas.SetLeft(label, p.X - 10);
                Canvas.SetTop(label, p.Y - 25);
                canvas.Children.Add(label);
            }
        }

        wrapper.SizeChanged += (s, e) => { if (e.NewSize.Width > 0 && e.NewSize.Height > 0) Draw(); };
        canvas.Loaded += (s, e) => Draw();

        return wrapper;
    }
}