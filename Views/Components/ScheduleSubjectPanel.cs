using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using PlanPeopleAvalonia.ViewModels;

namespace PlanPeopleAvalonia.Views.Components;

public class ScheduleSubjectPanel : UserControl
{
    public event Action<string>? SubjectClicked;
    public event Action<string>? SubjectDeleted;

    private readonly WrapPanel _wrapPanel;
    private readonly Grid _root;

    public ScheduleSubjectPanel()
    {
        _root = new Grid
        {
            Width = 240,
            Background = ThemeManager.CardBrush,
            RowDefinitions = new RowDefinitions("Auto, *, Auto")
        };

        var title = new TextBlock
        {
            Text = "快捷添加科目",
            FontWeight = FontWeight.Bold,
            Foreground = ThemeManager.TextPrimaryBrush,
            FontSize = 16,
            Margin = new Thickness(16, 16, 16, 8)
        };
        Grid.SetRow(title, 0);
        _root.Children.Add(title);

        _wrapPanel = new WrapPanel { Margin = new Thickness(16, 8) };
        var scroll = new ScrollViewer { Content = _wrapPanel };
        Grid.SetRow(scroll, 1);
        _root.Children.Add(scroll);

        var addBtn = new Button
        {
            Content = "＋ 自定义科目",
            Background = ThemeManager.WindowBrush,
            Foreground = ThemeManager.AccentBrush,
            BorderBrush = ThemeManager.BorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 8),
            Margin = new Thickness(16, 8, 16, 16),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        addBtn.Click += (s, e) => ShowCustomInput();
        Grid.SetRow(addBtn, 2);
        _root.Children.Add(addBtn);

        Content = _root;
        ReloadSubjects();

        // 配置变更自动刷新
        ScheduleConfigStore.ConfigChanged += ReloadSubjects;
    }

    public void ReloadSubjects()
    {
        _wrapPanel.Children.Clear();

        foreach (var sub in ScheduleConfigStore.Subjects.ToList())
        {
            var content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                VerticalAlignment = VerticalAlignment.Center
            };
            content.Children.Add(new Border
            {
                Width = 10, Height = 10,
                CornerRadius = new CornerRadius(5),
                Background = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });
            content.Children.Add(new TextBlock
            {
                Text = sub.Name,
                VerticalAlignment = VerticalAlignment.Center
            });

            var btn = new Button
            {
                Content = content,
                Background = ParseBrush(sub.ColorHex),
                Foreground = Brushes.White,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 8),
                Margin = new Thickness(3),
                FontWeight = FontWeight.Bold,
                FontSize = 13
            };

            var name = sub.Name;
            btn.Click += (s, e) => SubjectClicked?.Invoke(name);
            btn.ContextMenu = BuildContextMenu(name);

            _wrapPanel.Children.Add(btn);
        }
    }

    private ContextMenu BuildContextMenu(string name)
    {
        var menu = new ContextMenu();
        var delItem = new MenuItem { Header = $"删除科目「{name}」" };
        delItem.Click += (s, e) =>
        {
            var sub = ScheduleConfigStore.Subjects.FirstOrDefault(x => x.Name == name);
            if (sub != null)
            {
                ScheduleConfigStore.Subjects.Remove(sub);
                ScheduleConfigStore.NotifyChanged();
                SubjectDeleted?.Invoke(name);
            }
        };
        menu.Items.Add(delItem);
        return menu;
    }

    private void ShowCustomInput()
    {
        if (TopLevel.GetTopLevel(this) is not Window owner) return;

        var dlg = new Window
        {
            Title = "自定义科目",
            Width = 320,
            Height = 220,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = ThemeManager.WindowBrush
        };

        var panel = new StackPanel { Margin = new Thickness(20), Spacing = 12 };

        panel.Children.Add(new TextBlock { Text = "科目名称：", Foreground = ThemeManager.TextSecondaryBrush });
        var tbName = new TextBox { Watermark = "如：编程" };
        panel.Children.Add(tbName);

        panel.Children.Add(new TextBlock { Text = "颜色 HEX：", Foreground = ThemeManager.TextSecondaryBrush });
        var tbColor = new TextBox { Text = "#0078D4" };
        panel.Children.Add(tbColor);

        var btnRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        var ok = new Button
        {
            Content = "添加",
            Background = ThemeManager.AccentBrush,
            Foreground = Brushes.White,
            Padding = new Thickness(14, 6)
        };
        ok.Click += (s, e) =>
        {
            var n = tbName.Text?.Trim();
            var c = tbColor.Text?.Trim();
            if (string.IsNullOrWhiteSpace(n)) return;
            if (string.IsNullOrWhiteSpace(c) || !Color.TryParse(c, out _)) c = "#0078D4";
            if (!ScheduleConfigStore.Subjects.Any(x => x.Name == n))
            {
                ScheduleConfigStore.Subjects.Add(new SubjectConfig { Name = n, ColorHex = c });
                ScheduleConfigStore.NotifyChanged();
            }
            dlg.Close();
        };

        var cancel = new Button
        {
            Content = "取消",
            Background = ThemeManager.WindowBrush,
            Foreground = ThemeManager.TextSecondaryBrush,
            Padding = new Thickness(14, 6)
        };
        cancel.Click += (s, e) => dlg.Close();

        btnRow.Children.Add(ok);
        btnRow.Children.Add(cancel);
        panel.Children.Add(btnRow);

        dlg.Content = panel;
        dlg.ShowDialog(owner);
    }

    private static IBrush ParseBrush(string? hex)
        => Color.TryParse(hex, out var c) ? new SolidColorBrush(c) : new SolidColorBrush(Colors.Gray);
}