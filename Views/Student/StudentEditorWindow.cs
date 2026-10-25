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

public class StudentEditorWindow : Window
{
    private readonly Student? _existing;
    private readonly Action _onSaved;

    private readonly TextBox _tbName;
    private readonly TextBox _tbId;
    private readonly TextBox _tbDuty;

    public StudentEditorWindow(Student? existing, Action onSaved)
    {
        _existing = existing;
        _onSaved = onSaved ?? (() => { });

        Title = existing == null ? "添加学生" : "编辑学生";
        Width = 400;
        Height = 340;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = ThemeManager.WindowBrush;
        CanResize = false;

        var root = new Grid { RowDefinitions = new RowDefinitions("Auto, *, Auto"), Margin = new Thickness(24) };

        var title = new TextBlock { Text = existing == null ? "新增学生档案" : $"编辑：{existing.Name}", FontSize = 20, FontWeight = FontWeight.Bold, Foreground = ThemeManager.TextPrimaryBrush, Margin = new Thickness(0, 0, 0, 16) };
        Grid.SetRow(title, 0);
        root.Children.Add(title);

        var form = new StackPanel { Spacing = 12 };
        _tbName = AddField(form, "姓名 *", existing?.Name);
        _tbId = AddField(form, "学号 *", existing?.Id);
        _tbDuty = AddField(form, "职务", existing?.Duty);

        Grid.SetRow(form, 1);
        root.Children.Add(form);

        var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 20, 0, 0) };
        var cancel = new Button { Content = "取消", Padding = new Thickness(20, 8), CornerRadius = new CornerRadius(8), Background = ThemeManager.CardBrush, Foreground = ThemeManager.TextPrimaryBrush, BorderBrush = ThemeManager.BorderBrush, BorderThickness = new Thickness(1) };
        cancel.Click += (s, e) => Close();
        btnRow.Children.Add(cancel);

        var save = new Button { Content = "保存", Padding = new Thickness(20, 8), CornerRadius = new CornerRadius(8), Background = ThemeManager.AccentBrush, Foreground = Brushes.White, BorderThickness = new Thickness(0), FontWeight = FontWeight.Bold };
        save.Click += (s, e) => SaveAndClose();
        btnRow.Children.Add(save);

        Grid.SetRow(btnRow, 2);
        root.Children.Add(btnRow);
        Content = root;
    }

    private static TextBox AddField(StackPanel parent, string label, string? value)
    {
        parent.Children.Add(new TextBlock { Text = label, Foreground = ThemeManager.TextSecondaryBrush, FontSize = 13 });
        var tb = new TextBox { Text = value ?? "", Watermark = $"请输入{label.Replace("*", "").Trim()}", Background = ThemeManager.CardBrush, Foreground = ThemeManager.TextPrimaryBrush, CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 8) };
        parent.Children.Add(tb);
        return tb;
    }

    private void SaveAndClose()
    {
        var name = _tbName.Text?.Trim();
        var id = _tbId.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(id))
        {
            // 简易校验弹窗
            var err = new Window { Title = "提示", Width = 300, Height = 150, WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = ThemeManager.WindowBrush };
            err.Content = new TextBlock { Text = "姓名和学号不能为空", Margin = new Thickness(20), Foreground = ThemeManager.TextPrimaryBrush };
            err.ShowDialog(this);
            return;
        }

        var newStudent = new Student
        {
            Name = name,
            Id = id,
            Duty = _tbDuty.Text?.Trim() ?? ""
        };

        try
        {
            var list = StudentRepository.LoadStudents() ?? new List<Student>();
            if (_existing != null)
            {
                int idx = list.IndexOf(_existing);
                if (idx >= 0) list[idx] = newStudent;
                else list.Add(newStudent);
            }
            else list.Add(newStudent);

            StudentRepository.SaveStudents(list);
            _onSaved();
            Close();
        }
        catch { /* 忽略异常 */ }
    }
}