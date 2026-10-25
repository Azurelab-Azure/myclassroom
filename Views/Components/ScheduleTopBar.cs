using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using PlanPeopleAvalonia.Services;

namespace PlanPeopleAvalonia.Views.Components;

public class ScheduleTopBar : UserControl
{
    public event Action? SaveClicked;
    public event Action? ImportClicked;
    public event Action<string>? TabChanged;

    private readonly TextBlock _tabCourse;
    private readonly TextBlock _tabTimeTable;
    private readonly TextBlock _tabSubject;
    private readonly TextBlock _tabAdjust;
    private string _activeTab = "课表";

    public ScheduleTopBar()
    {
        var borderWrap = new Border
        {
            Background = ThemeManager.CardBrush,
            BorderBrush = ThemeManager.BorderBrush,
            BorderThickness = new Thickness(0, 0, 0, 1)
        };

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*, Auto") };
        borderWrap.Child = grid;

        var titleArea = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 24,
            Margin = new Thickness(16, 10)
        };

        _tabCourse    = MakeTab("课表", isActive: true);
        _tabTimeTable = MakeTab("时间表");
        _tabSubject   = MakeTab("科目");
        _tabAdjust    = MakeTab("调课");

        _tabCourse.PointerPressed    += (s, e) => ActivateTab("课表");
        _tabTimeTable.PointerPressed += (s, e) => ActivateTab("时间表");
        _tabSubject.PointerPressed   += (s, e) => ActivateTab("科目");
        _tabAdjust.PointerPressed    += (s, e) => ActivateTab("调课");

        titleArea.Children.Add(_tabCourse);
        titleArea.Children.Add(_tabTimeTable);
        titleArea.Children.Add(_tabSubject);
        titleArea.Children.Add(_tabAdjust);
        Grid.SetColumn(titleArea, 0);
        grid.Children.Add(titleArea);

        var rightActions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Margin = new Thickness(16, 10),
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var btnSaveAs = CreateActionBtn("另存为...");
        btnSaveAs.Click += async (s, e) => await ExportJsonAsync();
        rightActions.Children.Add(btnSaveAs);

        var btnImport = CreateActionBtn("导入...");
        btnImport.Click += async (s, e) => await ImportJsonAsync();
        rightActions.Children.Add(btnImport);

        var btnSave = CreateActionBtn("保存");
        btnSave.Click += (s, e) =>
        {
            SaveClicked?.Invoke();
            ShowMessage("提示", "已覆盖保存到当前数据路径。");
        };
        rightActions.Children.Add(btnSave);

        Grid.SetColumn(rightActions, 1);
        grid.Children.Add(rightActions);
        Content = borderWrap;
    }

    private TextBlock MakeTab(string text, bool isActive = false) => new()
    {
        Text = text,
        FontSize = 16,
        FontWeight = isActive ? FontWeight.Bold : FontWeight.Normal,
        Foreground = isActive ? ThemeManager.AccentBrush : ThemeManager.TextSecondaryBrush,
        VerticalAlignment = VerticalAlignment.Center,
        Cursor = new Cursor(StandardCursorType.Hand)
    };

    private void ActivateTab(string tab)
    {
        _activeTab = tab;
        SetActiveTab(tab);
        TabChanged?.Invoke(tab);
    }

    public void SetActiveTab(string tab)
    {
        var map = new (string name, TextBlock tb)[]
        {
            ("课表",   _tabCourse),
            ("时间表", _tabTimeTable),
            ("科目",   _tabSubject),
            ("调课",   _tabAdjust),
        };
        foreach (var (name, tb) in map)
        {
            bool active = name == tab;
            tb.FontWeight = active ? FontWeight.Bold : FontWeight.Normal;
            tb.Foreground = active ? ThemeManager.AccentBrush : ThemeManager.TextSecondaryBrush;
        }
        _activeTab = tab;
    }

    public string ActiveTab => _activeTab;

    private Button CreateActionBtn(string text) => new()
    {
        Content = text,
        Background = Brushes.Transparent,
        BorderThickness = new Thickness(0),
        Foreground = ThemeManager.TextSecondaryBrush,
        Padding = new Thickness(8, 4),
        FontSize = 13,
        Cursor = new Cursor(StandardCursorType.Hand)
    };

    private async System.Threading.Tasks.Task ExportJsonAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "导出课表",
            SuggestedFileName = "MySchedule.json",
            FileTypeChoices = new[] { new FilePickerFileType("JSON 文件") { Patterns = new[] { "*.json" } } }
        });

        if (file != null)
        {
            string? path = file.TryGetLocalPath();
            if (!string.IsNullOrEmpty(path))
            {
                var courses = CourseRepository.LoadCourses();
                var json = System.Text.Json.JsonSerializer.Serialize(courses);
                System.IO.File.WriteAllText(path, json);
                ShowMessage("导出成功", $"数据已保存至：\n{path}");
            }
        }
    }

    private async System.Threading.Tasks.Task ImportJsonAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "导入课表",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("JSON 文件") { Patterns = new[] { "*.json" } } }
        });

        if (files.Count >= 1)
        {
            string? path = files[0].TryGetLocalPath();
            if (!string.IsNullOrEmpty(path))
            {
                var json = System.IO.File.ReadAllText(path);
                var courses = System.Text.Json.JsonSerializer.Deserialize<List<PlanPeopleAvalonia.Models.Course>>(json);
                if (courses != null)
                {
                    CourseRepository.SaveCourses(courses);
                    ImportClicked?.Invoke();
                    ShowMessage("导入成功", "课表已覆盖更新！");
                }
            }
        }
    }

    private async void ShowMessage(string title, string content)
    {
        if (TopLevel.GetTopLevel(this) is Window owner)
        {
            var dlg = new Window
            {
                Title = title,
                Width = 400,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = ThemeManager.WindowBrush
            };
            var panel = new StackPanel { Margin = new Thickness(24), Spacing = 16 };
            panel.Children.Add(new TextBlock
            {
                Text = content,
                Foreground = ThemeManager.TextPrimaryBrush,
                TextWrapping = TextWrapping.Wrap
            });
            var okBtn = new Button
            {
                Content = "确定",
                Background = ThemeManager.AccentBrush,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Right,
                Padding = new Thickness(16, 6)
            };
            okBtn.Click += (s, e) => dlg.Close();
            panel.Children.Add(okBtn);
            dlg.Content = panel;
            await dlg.ShowDialog(owner);
        }
    }
}