using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using PlanPeopleAvalonia.ViewModels; // ★ 引入 ViewModels 下的真实配置中心

namespace PlanPeopleAvalonia.Views.Components;

public class ScheduleSettingsView : Window
{
    private readonly string _tab;
    private readonly Action? _onSaved;
    private readonly StackPanel _content = new() { Spacing = 12 };

    public ScheduleSettingsView(string tab, Action? onSaved = null)
    {
        _tab = tab;
        _onSaved = onSaved;

        Title = $"管理: {tab}";
        Width = 500;
        Height = 600;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = ThemeManager.WindowBrush;

        var root = new Grid { RowDefinitions = new RowDefinitions("Auto, *, Auto"), Margin = new Thickness(24) };

        var title = new TextBlock
        {
            Text = $"高级设置: {tab}",
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            Foreground = ThemeManager.TextPrimaryBrush,
            Margin = new Thickness(0, 0, 0, 16)
        };
        Grid.SetRow(title, 0);
        root.Children.Add(title);

        var scroll = new ScrollViewer { Content = _content };
        Grid.SetRow(scroll, 1);
        root.Children.Add(scroll);

        // 底部按钮
        var btnPanel = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto, *, Auto, Auto"),
            Margin = new Thickness(0, 24, 0, 0)
        };
        Grid.SetRow(btnPanel, 2);

        if (tab is "时间表" or "科目")
        {
            var btnAdd = new Button { Content = "＋ 新增", Padding = new Thickness(14, 8) };
            btnAdd.Click += (s, e) => { AddItem(); BuildList(); };
            Grid.SetColumn(btnAdd, 0);
            btnPanel.Children.Add(btnAdd);
        }

        var btnCancel = new Button { Content = "取消", Padding = new Thickness(16, 8) };
        btnCancel.Click += (s, e) => Close();
        Grid.SetColumn(btnCancel, 2);
        btnPanel.Children.Add(btnCancel);

        var btnSave = new Button
        {
            Content = "保存",
            Background = ThemeManager.AccentBrush,
            Foreground = Brushes.White,
            Padding = new Thickness(16, 8),
            Margin = new Thickness(8, 0, 0, 0)
        };
        btnSave.Click += (s, e) =>
        {
            ScheduleConfigStore.NotifyChanged();
            _onSaved?.Invoke();
            Close();
        };
        Grid.SetColumn(btnSave, 3);
        btnPanel.Children.Add(btnSave);

        root.Children.Add(btnPanel);
        Content = root;

        BuildList();
    }

    private void BuildList()
    {
        _content.Children.Clear();
        switch (_tab)
        {
            case "时间表": BuildTimeTable(); break;
            case "科目":   BuildSubjects();  break;
            default:       BuildAdvanced();  break;
        }
    }

    private void BuildTimeTable()
    {
        _content.Children.Add(new TextBlock
        {
            Text = "编辑各节次的开始/结束时间（HH:mm）：",
            Foreground = ThemeManager.TextSecondaryBrush,
            TextWrapping = TextWrapping.Wrap
        });

        foreach (var p in ScheduleConfigStore.Periods.ToList())
        {
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto, *, *, Auto"), Margin = new Thickness(0, 4) };

            row.Children.Add(new TextBlock
            {
                Text = $"第 {p.Section} 节",
                VerticalAlignment = VerticalAlignment.Center,
                Width = 60,
                Foreground = ThemeManager.TextPrimaryBrush
            });

            var tbStart = new TextBox { Text = p.StartTime, Width = 100, Margin = new Thickness(8, 0) };
            Grid.SetColumn(tbStart, 1);
            row.Children.Add(tbStart);

            var tbEnd = new TextBox { Text = p.EndTime, Width = 100, Margin = new Thickness(8, 0) };
            Grid.SetColumn(tbEnd, 2);
            row.Children.Add(tbEnd);

            var btnDel = MakeIconBtn();
            var captured = p;
            btnDel.Click += (s, e) =>
            {
                ScheduleConfigStore.Periods.Remove(captured);
                ReindexPeriods();
                BuildList();
            };
            Grid.SetColumn(btnDel, 3);
            row.Children.Add(btnDel);

            tbStart.TextChanged += (s, e) => captured.StartTime = tbStart.Text ?? "";
            tbEnd.TextChanged   += (s, e) => captured.EndTime   = tbEnd.Text ?? "";

            _content.Children.Add(row);
        }
    }

    private static void ReindexPeriods()
    {
        for (int i = 0; i < ScheduleConfigStore.Periods.Count; i++)
            ScheduleConfigStore.Periods[i].Section = i + 1;
    }

    private void BuildSubjects()
    {
        _content.Children.Add(new TextBlock
        {
            Text = "编辑科目名称与颜色（HEX，如 #E81123）：",
            Foreground = ThemeManager.TextSecondaryBrush,
            TextWrapping = TextWrapping.Wrap
        });

        foreach (var sub in ScheduleConfigStore.Subjects.ToList())
        {
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto, *, Auto, Auto"), Margin = new Thickness(0, 4) };

            var colorDot = new Border
            {
                Width = 18, Height = 18,
                CornerRadius = new CornerRadius(9),
                Background = ParseBrush(sub.ColorHex),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            Grid.SetColumn(colorDot, 0);
            row.Children.Add(colorDot);

            var tbName = new TextBox { Text = sub.Name, Width = 80 };
            Grid.SetColumn(tbName, 1);
            row.Children.Add(tbName);

            var tbColor = new TextBox { Text = sub.ColorHex, Width = 120, Margin = new Thickness(8, 0) };
            Grid.SetColumn(tbColor, 2);
            row.Children.Add(tbColor);

            var btnDel = MakeIconBtn();
            var captured = sub;
            btnDel.Click += (s, e) =>
            {
                ScheduleConfigStore.Subjects.Remove(captured);
                BuildList();
            };
            Grid.SetColumn(btnDel, 3);
            row.Children.Add(btnDel);

            tbName.TextChanged  += (s, e) => captured.Name = tbName.Text ?? "";
            tbColor.TextChanged += (s, e) =>
            {
                captured.ColorHex = string.IsNullOrWhiteSpace(tbColor.Text) ? "#0078D4" : tbColor.Text!;
                colorDot.Background = ParseBrush(captured.ColorHex);
            };

            _content.Children.Add(row);
        }
    }

    private void BuildAdvanced()
    {
        _content.Children.Add(new TextBlock
        {
            Text = "高级调课模式",
            FontWeight = FontWeight.Bold,
            FontSize = 16,
            Foreground = ThemeManager.TextPrimaryBrush,
            Margin = new Thickness(0, 0, 0, 8)
        });

        AddToggle("开启拖拽自动交换课时", ScheduleConfigStore.DragSwapEnabled, v => ScheduleConfigStore.DragSwapEnabled = v);
        AddToggle("调课时检测冲突并提示", ScheduleConfigStore.ConflictWarnEnabled, v => ScheduleConfigStore.ConflictWarnEnabled = v);
        AddToggle("锁定已发布课表（禁止拖动）", ScheduleConfigStore.LockPublished, v => ScheduleConfigStore.LockPublished = v);
    }

    private void AddToggle(string text, bool initial, Action<bool> onChange)
    {
        var check = new CheckBox
        {
            Content = text,
            IsChecked = initial,
            Foreground = ThemeManager.TextPrimaryBrush,
            Margin = new Thickness(0, 4)
        };
        check.IsCheckedChanged += (s, e) => onChange(check.IsChecked == true);
        _content.Children.Add(check);
    }

    private void AddItem()
    {
        if (_tab == "时间表")
        {
            var next = ScheduleConfigStore.Periods.Count + 1;
            ScheduleConfigStore.Periods.Add(new PeriodConfig { Section = next, StartTime = "08:00", EndTime = "08:45" });
        }
        else if (_tab == "科目")
        {
            ScheduleConfigStore.Subjects.Add(new SubjectConfig { Name = "新科目", ColorHex = "#0078D4" });
        }
    }

    private static Button MakeIconBtn() => new()
    {
        Content = "✕",
        Background = Brushes.Transparent,
        Foreground = ThemeManager.TextSecondaryBrush,
        BorderThickness = new Thickness(0),
        Padding = new Thickness(6),
        VerticalAlignment = VerticalAlignment.Center
    };

    private static IBrush ParseBrush(string? hex)
        => Color.TryParse(hex, out var c) ? new SolidColorBrush(c) : new SolidColorBrush(Colors.Gray);
}