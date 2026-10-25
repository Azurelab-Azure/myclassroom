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

namespace PlanPeopleAvalonia.Views.StudentComponents;

public class StudentListView : UserControl
{
    // 允许传入 null 以通知外部清空详情
    public event Action<Student?>? StudentSelected;
    private readonly ListBox _listBox;

    public StudentListView()
    {
        var root = new Grid { Width = 280, Background = ThemeManager.WindowBrush, RowDefinitions = new RowDefinitions("Auto, *") };
        
        var header = new TextBlock { Text = "学生列表", FontSize = 14, FontWeight = FontWeight.Bold, Foreground = ThemeManager.TextSecondaryBrush, Margin = new Thickness(16, 16, 16, 8) };
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        // 使用 ListBox 实现虚拟化
        _listBox = new ListBox
        {
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Margin = new Thickness(8, 0)
        };

        // ★ 核心修复：通过 Tag 提取真实的数据模型
        _listBox.SelectionChanged += (s, e) =>
        {
            if (_listBox.SelectedItem is ListBoxItem item && item.Tag is Student student)
            {
                StudentSelected?.Invoke(student);
            }
        };

        Grid.SetRow(_listBox, 1);
        root.Children.Add(_listBox);
        Content = root;
    }

    public void SetStudents(List<Student> students)
    {
        _listBox.Items.Clear();
        if (students == null) return;

        foreach (var s in students)
        {
            var stack = new StackPanel { Spacing = 4 };
            stack.Children.Add(new TextBlock { Text = s.Name, FontWeight = FontWeight.Bold, FontSize = 14, Foreground = ThemeManager.TextPrimaryBrush });
            stack.Children.Add(new TextBlock { Text = s.Id, FontSize = 12, Foreground = ThemeManager.AccentBrush });

            var item = new ListBoxItem
            {
                Content = stack,
                Padding = new Thickness(12, 10),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(4, 2),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Tag = s // ★ 把真实的 Student 对象挂在 Tag 上
            };

            // 右键删除菜单
            var menu = new ContextMenu();
            var delItem = new MenuItem { Header = "删除该学生" };
            delItem.Click += (sender, e) =>
            {
                var list = StudentRepository.LoadStudents();
                if (list != null)
                {
                    list.RemoveAll(x => x.Id == s.Id);
                    StudentRepository.SaveStudents(list);
                    SetStudents(list); 
                    StudentSelected?.Invoke(null); // 清空右侧
                }
            };
            menu.Items.Add(delItem);
            item.ContextMenu = menu;

            _listBox.Items.Add(item);
        }
    }
}