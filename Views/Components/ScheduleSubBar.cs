using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using PlanPeopleAvalonia.ViewModels;

namespace PlanPeopleAvalonia.Views.Components;

public class ScheduleSubBar : UserControl
{
    private readonly ScheduleViewModel _vm;
    private readonly Action _onWeekChanged;
    private readonly TextBlock _scheduleNameText;
    private readonly TextBlock _weekText;
    private readonly TextBlock _dateText;

    public event Action<string>? ScheduleSelected;
    public event Action? RefreshClicked;

    private List<string> _availableSchedules = new();

    public ScheduleSubBar(ScheduleViewModel vm, Action onWeekChanged)
    {
        _vm = vm;
        _onWeekChanged = onWeekChanged;

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto, *, Auto"),
            Background = ThemeManager.WindowBrush
        };

        // ---- 左侧 ----
        var leftSub = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Margin = new Thickness(16, 8)
        };

        var refreshBtn = new Button
        {
            Content = "刷新",
            Background = Brushes.Transparent,
            Foreground = ThemeManager.AccentBrush,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(4, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        refreshBtn.Click += (s, e) =>
        {
            _vm.LoadData();
            RefreshClicked?.Invoke();
            _onWeekChanged();
        };
        leftSub.Children.Add(refreshBtn);

        _scheduleNameText = new TextBlock
        {
            Text = "未选择课表",
            Foreground = ThemeManager.TextSecondaryBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        _scheduleNameText.PointerPressed += (s, e) => ShowScheduleMenu();
        leftSub.Children.Add(_scheduleNameText);

        Grid.SetColumn(leftSub, 0);
        grid.Children.Add(leftSub);

        // ---- 右侧 ----
        var rightSub = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Margin = new Thickness(16, 8),
            HorizontalAlignment = HorizontalAlignment.Right
        };

        _dateText = new TextBlock
        {
            Text = DateTime.Now.ToString("yyyy/MM/dd"),
            Foreground = ThemeManager.TextPrimaryBrush,
            VerticalAlignment = VerticalAlignment.Center
        };
        rightSub.Children.Add(_dateText);

        var btnPrev = MakeArrowBtn("<");
        btnPrev.Click += (s, e) =>
        {
            if (_vm.CurrentWeek > 1)
            {
                _vm.CurrentWeek--;
                _vm.LoadData();
                UpdateWeekText();
                _onWeekChanged();
            }
        };

        var btnNext = MakeArrowBtn(">");
        btnNext.Click += (s, e) =>
        {
            _vm.CurrentWeek++;
            _vm.LoadData();
            UpdateWeekText();
            _onWeekChanged();
        };

        _weekText = new TextBlock
        {
            Text = $"第 {_vm.CurrentWeek} 周",
            Foreground = ThemeManager.TextPrimaryBrush,
            FontWeight = FontWeight.Bold,
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 64
        };

        var btnToday = new Button
        {
            Content = "本周",
            Background = Brushes.Transparent,
            Foreground = ThemeManager.TextSecondaryBrush,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(6, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        btnToday.Click += (s, e) =>
        {
            _vm.CurrentWeek = 1;
            _vm.LoadData();
            UpdateWeekText();
            _onWeekChanged();
        };

        rightSub.Children.Add(btnPrev);
        rightSub.Children.Add(_weekText);
        rightSub.Children.Add(btnNext);
        rightSub.Children.Add(btnToday);

        Grid.SetColumn(rightSub, 2);
        grid.Children.Add(rightSub);

        Content = grid;
    }

    private static Button MakeArrowBtn(string text) => new()
    {
        Content = text,
        Background = Brushes.Transparent,
        Foreground = ThemeManager.TextPrimaryBrush,
        BorderThickness = new Thickness(0),
        Padding = new Thickness(8, 0),
        FontWeight = FontWeight.Bold
    };

    private void UpdateWeekText()
    {
        _weekText.Text = $"第 {_vm.CurrentWeek} 周";
        _dateText.Text = DateTime.Now.ToString("yyyy/MM/dd");
    }

    // ---- 对外接口 ----
    public void SetScheduleName(string name)
        => _scheduleNameText.Text = string.IsNullOrWhiteSpace(name) ? "未选择课表" : name;

    public void SetAvailableSchedules(List<string> names)
        => _availableSchedules = names ?? new List<string>();

    public void UpdateWeek() => UpdateWeekText();

    private void ShowScheduleMenu()
    {
        if (_availableSchedules.Count == 0) return;
        if (TopLevel.GetTopLevel(this) is not Window owner) return;

        var dlg = new Window
        {
            Title = "切换课表",
            Width = 300,
            Height = 260,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = ThemeManager.WindowBrush
        };

        var stack = new StackPanel { Spacing = 6, Margin = new Thickness(16) };
        foreach (var name in _availableSchedules)
        {
            var btn = new Button
            {
                Content = name,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Background = ThemeManager.CardBrush,
                Foreground = ThemeManager.TextPrimaryBrush,
                Padding = new Thickness(12, 8),
                CornerRadius = new CornerRadius(6),
                BorderThickness = new Thickness(1),
                BorderBrush = ThemeManager.BorderBrush
            };
            var captured = name;
            btn.Click += (s, e) =>
            {
                SetScheduleName(captured);
                ScheduleSelected?.Invoke(captured);
                dlg.Close();
            };
            stack.Children.Add(btn);
        }

        dlg.Content = new ScrollViewer { Content = stack };
        dlg.ShowDialog(owner);
    }
}