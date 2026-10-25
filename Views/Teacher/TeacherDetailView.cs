using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PlanPeopleAvalonia.Services;
using PlanPeopleAvalonia.ViewModels;
using TeacherModel = PlanPeopleAvalonia.Models.Teacher;
using CourseModel = PlanPeopleAvalonia.Models.Course;

namespace PlanPeopleAvalonia.Views; // ★ 确保命名空间与你实际文件夹一致

public class TeacherDetailView : UserControl
{
    private readonly StackPanel _root;

    public TeacherDetailView()
    {
        _root = new StackPanel { Spacing = 16, Margin = new Thickness(20) };
        Content = new ScrollViewer { Content = _root };
        Clear();
    }

    public void ShowTeacher(TeacherModel teacher)
    {
        _root.Children.Clear();
        if (teacher == null) return;
        _root.Children.Add(BuildProfileCard(teacher));
        _root.Children.Add(BuildCoursesCard(teacher));
    }

    public void Clear()
    {
        _root.Children.Clear();
        _root.Children.Add(new TextBlock
        {
            Text = "请选择左侧教师查看详情",
            Foreground = ThemeManager.TextSecondaryBrush,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 80, 0, 0)
        });
    }

    private Border BuildProfileCard(TeacherModel t)
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
        outer.Children.Add(BuildAvatar(t));

        var info = new StackPanel { Spacing = 10, Margin = new Thickness(16, 0, 0, 0) };
        var headerRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        headerRow.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(t.Name) ? "（未命名）" : t.Name,
            FontSize = 22, FontWeight = FontWeight.Bold, Foreground = ThemeManager.TextPrimaryBrush
        });
        if (!string.IsNullOrWhiteSpace(t.Title))
        {
            headerRow.Children.Add(new Border
            {
                Background = ThemeManager.AccentBrush,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 2),
                Child = new TextBlock { Text = t.Title, FontSize = 12, Foreground = Brushes.White }
            });
        }
        info.Children.Add(headerRow);

        var infoGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*, *"), RowDefinitions = new RowDefinitions("Auto, Auto, Auto, Auto") };
        void Put(int row, int col, string label, string val)
        {
            var p = new StackPanel { Spacing = 2, Margin = new Thickness(0, 4, 8, 4) };
            p.Children.Add(new TextBlock { Text = label, FontSize = 11, Foreground = ThemeManager.TextSecondaryBrush });
            p.Children.Add(new TextBlock { Text = string.IsNullOrWhiteSpace(val) ? "—" : val, FontSize = 14, Foreground = ThemeManager.TextPrimaryBrush, TextWrapping = TextWrapping.Wrap });
            Grid.SetRow(p, row); Grid.SetColumn(p, col);
            infoGrid.Children.Add(p);
        }
        Put(0, 0, "学科", t.Subject); Put(0, 1, "性别", t.Gender);
        Put(1, 0, "工号", t.EmployeeId); Put(1, 1, "学历", t.Education);
        Put(2, 0, "电话", t.Phone); Put(2, 1, "邮箱", t.Email);
        Put(3, 0, "办公室", t.Office); Put(3, 1, "职称", t.Title);
        info.Children.Add(infoGrid);

        if (!string.IsNullOrWhiteSpace(t.Remark))
        {
            info.Children.Add(new TextBlock { Text = "备注", Foreground = ThemeManager.TextSecondaryBrush, FontSize = 12, Margin = new Thickness(0, 6, 0, 2) });
            info.Children.Add(new TextBlock { Text = t.Remark, Foreground = ThemeManager.TextPrimaryBrush, FontSize = 13, TextWrapping = TextWrapping.Wrap });
        }

        Grid.SetColumn(info, 1);
        outer.Children.Add(info);
        card.Child = outer;
        return card;
    }

    private static Border BuildAvatar(TeacherModel t)
    {
        var layer = new Grid();
        layer.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(t.Name) ? "?" : t.Name.Substring(0, 1),
            FontSize = 44, FontWeight = FontWeight.Bold, Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
        });
        if (!string.IsNullOrWhiteSpace(t.PhotoPath) && File.Exists(t.PhotoPath))
        {
            try { layer.Children.Add(new Image { Source = new Bitmap(t.PhotoPath), Stretch = Stretch.UniformToFill }); } catch { }
        }
        return new Border { Width = 110, Height = 110, CornerRadius = new CornerRadius(55), Background = ThemeManager.AccentBrush, ClipToBounds = true, Child = layer };
    }

    private Border BuildCoursesCard(TeacherModel t)
    {
        var card = new Border
        {
            Background = ThemeManager.CardBrush, BorderBrush = ThemeManager.BorderBrush,
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(20)
        };
        var stack = new StackPanel { Spacing = 10 };
        stack.Children.Add(new TextBlock { Text = "所授课程", FontSize = 16, FontWeight = FontWeight.Bold, Foreground = ThemeManager.TextPrimaryBrush, Margin = new Thickness(0, 0, 0, 4) });

        var courses = SafeLoad(t);
        if (courses.Count == 0) stack.Children.Add(new TextBlock { Text = "暂无排课记录", Foreground = ThemeManager.TextSecondaryBrush });
        else
        {
            foreach (var c in courses)
            {
                var row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto, *, Auto"), Margin = new Thickness(0, 4) };
                
                // ★ 修复：Course 没有 Color，用主题强调色或固定色代替
                var color = Colors.SteelBlue; 
                
                row.Children.Add(new Border { Width = 8, Height = 8, CornerRadius = new CornerRadius(4), Background = new SolidColorBrush(color), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) });
                
                row.Children.Add(new TextBlock { Text = c.Name, FontSize = 14, FontWeight = FontWeight.Bold, Foreground = ThemeManager.TextPrimaryBrush, VerticalAlignment = VerticalAlignment.Center, [Grid.ColumnProperty] = 1 });
                
                // ★ 修复：使用 WeekDay, TimeStart, TimeEnd
                row.Children.Add(new TextBlock { Text = $"周{DayName(c.WeekDay)} 第{c.TimeStart}-{c.TimeEnd}节", FontSize = 12, Foreground = ThemeManager.TextSecondaryBrush, VerticalAlignment = VerticalAlignment.Center, [Grid.ColumnProperty] = 2 });
                
                stack.Children.Add(row);
            }
        }
        card.Child = stack;
        return card;
    }

    private static System.Collections.Generic.List<CourseModel> SafeLoad(TeacherModel t)
    {
        try
        {
            var all = CourseRepository.LoadCourses();
            // ★ 修复：使用 WeekDay, TimeStart 排序
            return all?.Where(c => string.Equals(c.Teacher, t.Name, StringComparison.Ordinal))
                       .OrderBy(c => c.WeekDay)
                       .ThenBy(c => c.TimeStart)
                       .ToList() ?? new();
        }
        catch { return new(); }
    }

    private static string DayName(int d) => d switch { 1 => "一", 2 => "二", 3 => "三", 4 => "四", 5 => "五", 6 => "六", 7 => "日", _ => d.ToString() };
}