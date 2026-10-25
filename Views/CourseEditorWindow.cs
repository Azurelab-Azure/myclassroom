using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using PlanPeopleAvalonia.ViewModels;
using PlanPeopleAvalonia.Models;
using TeacherModel = PlanPeopleAvalonia.Models.Teacher; // ★ 别名引入

namespace PlanPeopleAvalonia.Views;

public partial class CourseEditorWindow : Window
{
    public CourseEditorWindow()
    {
        Title = "添加 / 编辑课程";
        Width = 400;
        Height = 520;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;
        Background = ThemeManager.WindowBrush;

        this.DataContextChanged += (s, e) =>
        {
            if (DataContext is CourseEditorViewModel vm) BuildUI(vm);
        };
    }

    private void BuildUI(CourseEditorViewModel vm)
    {
        var root = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto, *, Auto"),
            Margin = new Thickness(24)
        };

        // 标题
        var title = new TextBlock
        {
            Text = "添加 / 编辑课程",
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            Foreground = ThemeManager.TextPrimaryBrush,
            Margin = new Thickness(0, 0, 0, 24)
        };
        Grid.SetRow(title, 0);
        root.Children.Add(title);

        // 表单内容
        var scroll = new ScrollViewer();
        Grid.SetRow(scroll, 1);
        var panel = new StackPanel { Spacing = 16 };
        scroll.Content = panel;
        root.Children.Add(scroll);

        void AddField(string label, Control control)
        {
            var stack = new StackPanel { Spacing = 8 };
            stack.Children.Add(new TextBlock { Text = label, FontWeight = FontWeight.Bold, Foreground = ThemeManager.TextSecondaryBrush });
            stack.Children.Add(control);
            panel.Children.Add(stack);
        }

        // 课程名
        var tbName = new TextBox { Text = vm.CourseName, Watermark = "例如：高等数学" };
        tbName.TextChanged += (s, e) => vm.CourseName = tbName.Text ?? "";
        AddField("课程名称 *", tbName);

        // 教师下拉框
        var cbTeacher = new ComboBox { ItemsSource = vm.AvailableTeachers, SelectedItem = vm.SelectedTeacher, HorizontalAlignment = HorizontalAlignment.Stretch };
        // ★ 核心修复：将 as Teacher 改为 as TeacherModel，避开命名空间冲突
        cbTeacher.SelectionChanged += (s, e) => vm.SelectedTeacher = cbTeacher.SelectedItem as TeacherModel;
        AddField("任课教师", cbTeacher);

        // 教室
        var tbRoom = new TextBox { Text = vm.Classroom, Watermark = "例如：A101" };
        tbRoom.TextChanged += (s, e) => vm.Classroom = tbRoom.Text ?? "";
        AddField("教室", tbRoom);

        // 星期
        var cbWeek = new ComboBox { SelectedIndex = vm.WeekDayIndex, HorizontalAlignment = HorizontalAlignment.Stretch };
        cbWeek.ItemsSource = new[] { "周一", "周二", "周三", "周四", "周五", "周六", "周日" };
        cbWeek.SelectionChanged += (s, e) => vm.WeekDayIndex = cbWeek.SelectedIndex;
        AddField("星期", cbWeek);

        // 节次
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*, *"), ColumnSpacing = 12 };
        var numStart = new NumericUpDown { Value = vm.StartSection, Minimum = 1, Maximum = 8, Increment = 1 };
        numStart.ValueChanged += (s, e) => vm.StartSection = numStart.Value ?? 1;
        var numEnd = new NumericUpDown { Value = vm.EndSection, Minimum = 1, Maximum = 8, Increment = 1 };
        numEnd.ValueChanged += (s, e) => vm.EndSection = numEnd.Value ?? 2;

        var col1 = new StackPanel { Spacing = 8 };
        col1.Children.Add(new TextBlock { Text = "起始节次", FontWeight = FontWeight.Bold, Foreground = ThemeManager.TextSecondaryBrush });
        col1.Children.Add(numStart);
        Grid.SetColumn(col1, 0);

        var col2 = new StackPanel { Spacing = 8 };
        col2.Children.Add(new TextBlock { Text = "结束节次", FontWeight = FontWeight.Bold, Foreground = ThemeManager.TextSecondaryBrush });
        col2.Children.Add(numEnd);
        Grid.SetColumn(col2, 1);

        grid.Children.Add(col1);
        grid.Children.Add(col2);
        panel.Children.Add(grid);

        // 底部按钮
        var btnPanel = new Grid { ColumnDefinitions = new ColumnDefinitions("*, *"), Margin = new Thickness(0, 24, 0, 0) };
        Grid.SetRow(btnPanel, 2);

        var btnCancel = new Button { Content = "取消", HorizontalAlignment = HorizontalAlignment.Stretch };
        btnCancel.Click += (s, e) => Close();
        Grid.SetColumn(btnCancel, 0);
        btnPanel.Children.Add(btnCancel);

        var btnSave = new Button { Content = "保存", Background = ThemeManager.AccentBrush, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(8, 0, 0, 0) };
        btnSave.Click += (s, e) => 
        {
            vm.Save();
            Close();
        };
        Grid.SetColumn(btnSave, 1);
        btnPanel.Children.Add(btnSave);

        root.Children.Add(btnPanel);

        Content = root;
    }
}