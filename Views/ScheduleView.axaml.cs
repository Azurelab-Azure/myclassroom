using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PlanPeopleAvalonia.Models;
using PlanPeopleAvalonia.ViewModels;

namespace PlanPeopleAvalonia.Views;

public partial class ScheduleView : UserControl
{
    public ScheduleView()
    {
         InitializeComponent();
    }

    // ================= 响应子组件抛出的操作 =================

    public async void CreateNewCourse(int weekDay, int section)
    {
        if (DataContext is not ScheduleViewModel vm) return;
        var course = new Course { WeekDay = weekDay, TimeStart = section, TimeEnd = section, Weeks = new System.Collections.Generic.List<int> { vm.CurrentWeek } };
        await ShowCourseDialog(vm, course, true);
    }

    public async void HandleQuickAddCourse(string subjectName)
    {
        if (DataContext is not ScheduleViewModel vm) return;
        var course = new Course { Name = subjectName, WeekDay = 1, TimeStart = 1, TimeEnd = 1, Weeks = new System.Collections.Generic.List<int> { vm.CurrentWeek } };
        await ShowCourseDialog(vm, course, true);
    }

    public async void HandleEditCourse(CourseViewModel courseVm)
    {
        if (DataContext is not ScheduleViewModel vm) return;
        var editCourse = new Course
        {
            Name = courseVm.Model.Name,
            Teacher = courseVm.Model.Teacher,
            Classroom = courseVm.Model.Classroom,
            WeekDay = courseVm.Model.WeekDay,
            TimeStart = courseVm.Model.TimeStart,
            TimeEnd = courseVm.Model.TimeEnd,
            Weeks = new System.Collections.Generic.List<int>(courseVm.Model.Weeks)
        };
        await ShowCourseDialog(vm, editCourse, false, courseVm);
    }

    public void HandleDeleteCourse(CourseViewModel courseVm)
    {
        if (DataContext is ScheduleViewModel vm)
        {
            vm.DeleteCourse(courseVm);
        }
    }

    public void OnQuickAddClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ScheduleViewModel vm)
            CreateNewCourse(1, 1);
    }

    // ================= 各种弹窗实现 =================

    public async void OpenSectionSettingsDialog()
    {
        if (DataContext is not ScheduleViewModel vm) return;

        var dialog = new Window { Title = "节次与时间设置", Width = 400, Height = 500, WindowStartupLocation = WindowStartupLocation.CenterOwner, CanResize = false };
        var panel = new StackPanel { Margin = new Thickness(20), Spacing = 15 };
        
        var sectionPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10 };
        sectionPanel.Children.Add(new TextBlock { Text = "每天总节数：", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
        
        // 限制最小值为 2，防止减到 0 或 1 导致 UI 崩溃
        var numSections = new NumericUpDown { Minimum = 2, Maximum = 20, Value = vm.TotalSections, Increment = 1 };
        sectionPanel.Children.Add(numSections);
        panel.Children.Add(sectionPanel);

        var timeScroll = new ScrollViewer { Height = 300 };
        var timeStack = new StackPanel { Spacing = 10 };
        var timeBoxes = new System.Collections.Generic.Dictionary<int, (TextBox Start, TextBox End)>();

        for (int i = 1; i <= vm.TotalSections; i++)
        {
            var timeRow = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10 };
            timeRow.Children.Add(new TextBlock { Text = $"第 {i} 节：", Width = 60, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
            
            // ★ 安全获取原有的时间
            var existing = vm.SectionTimes.Count >= i ? vm.SectionTimes[i-1] : null;
            var txtStart = new TextBox { Text = existing?.StartTime ?? "08:00", Width = 100 };
            var txtEnd = new TextBox { Text = existing?.EndTime ?? "08:45", Width = 100 };
            
            timeRow.Children.Add(txtStart);
            timeRow.Children.Add(new TextBlock { Text = "-", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
            timeRow.Children.Add(txtEnd);
            timeStack.Children.Add(timeRow);
            timeBoxes[i] = (txtStart, txtEnd);
        }
        timeScroll.Content = timeStack;
        panel.Children.Add(timeScroll);

        var btnPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, Spacing = 10 };
        var btnCancel = new Button { Content = "取消", Width = 80 };
        var btnSave = new Button { Content = "保存并应用", Width = 100, Classes = { "accent" } };

        btnCancel.Click += (s, args) => dialog.Close();
        btnSave.Click += (s, args) =>
        {
            try
            {
                // ★ 核心防崩溃逻辑：先清空，再重新生成
                vm.TotalSections = (int)(numSections.Value ?? 8);
                vm.SectionTimes.Clear();
                
                for (int i = 1; i <= vm.TotalSections; i++)
                {
                    // 判空保护
                    string st = string.IsNullOrWhiteSpace(timeBoxes[i].Start.Text) ? "00:00" : timeBoxes[i].Start.Text!;
                    string et = string.IsNullOrWhiteSpace(timeBoxes[i].End.Text) ? "00:00" : timeBoxes[i].End.Text!;
                    
                    vm.SectionTimes.Add(new SectionTime { Section = i, StartTime = st, EndTime = et });
                }
                
                // 重新生成背景网格
                vm.InitializeGrid();
            }
            catch (Exception ex)
            {
                // 如果发生异常，打印出来，不让程序闪退
                Console.WriteLine($"保存设置出错: {ex.Message}");
            }
            finally
            {
                dialog.Close();
            }
        };

        btnPanel.Children.Add(btnCancel);
        btnPanel.Children.Add(btnSave);
        panel.Children.Add(btnPanel);

        dialog.Content = panel;
        var parentWindow = TopLevel.GetTopLevel(this) as Window;
        if (parentWindow != null) await dialog.ShowDialog(parentWindow);
    }
    public async void OpenStyleSettingsDialog()
    {
        if (DataContext is not ScheduleViewModel vm) return;

        var dialog = new Window { Title = "课表样式设置", Width = 320, Height = 300, WindowStartupLocation = WindowStartupLocation.CenterOwner, CanResize = false };
        var panel = new StackPanel { Margin = new Thickness(20), Spacing = 20 };
        
        var heightPanel = new StackPanel { Spacing = 5 };
        heightPanel.Children.Add(new TextBlock { Text = "课表行高 (像素)", FontWeight = Avalonia.Media.FontWeight.SemiBold });
        var sliderHeight = new Slider { Minimum = 50, Maximum = 150, Value = vm.CellHeight, TickFrequency = 10 };
        heightPanel.Children.Add(sliderHeight);
        panel.Children.Add(heightPanel);

        var widthPanel = new StackPanel { Spacing = 5 };
        widthPanel.Children.Add(new TextBlock { Text = "课表列宽 (像素)", FontWeight = Avalonia.Media.FontWeight.SemiBold });
        var sliderWidth = new Slider { Minimum = 80, Maximum = 200, Value = vm.CellWidth, TickFrequency = 10 };
        widthPanel.Children.Add(sliderWidth);
        panel.Children.Add(widthPanel);

        var btnPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, Spacing = 10 };
        var btnCancel = new Button { Content = "取消", Width = 80 };
        var btnApply = new Button { Content = "应用", Width = 80, Classes = { "accent" } };

        btnCancel.Click += (s, args) => dialog.Close();
        btnApply.Click += (s, args) =>
        {
            vm.CellWidth = sliderWidth.Value;
            vm.CellHeight = sliderHeight.Value;
            dialog.Close();
        };

        btnPanel.Children.Add(btnCancel);
        btnPanel.Children.Add(btnApply);
        panel.Children.Add(btnPanel);
        dialog.Content = panel;

        var parentWindow = TopLevel.GetTopLevel(this) as Window;
        if (parentWindow != null) await dialog.ShowDialog(parentWindow);
    }

    private async System.Threading.Tasks.Task ShowCourseDialog(ScheduleViewModel vm, Course courseData, bool isNew, CourseViewModel? originalVm = null)
    {
        var dialog = new Window { Title = isNew ? "新建课程" : "编辑课程", Width = 340, Height = 360, WindowStartupLocation = WindowStartupLocation.CenterOwner, CanResize = false };
        var panel = new StackPanel { Margin = new Thickness(24), Spacing = 16 };
        
        panel.Children.Add(new TextBlock { Text = "课程名称", FontWeight = Avalonia.Media.FontWeight.SemiBold, Foreground = Avalonia.Media.Brushes.Gray });
        var txtName = new TextBox { Text = courseData.Name, Watermark = "例如：高等数学", Height = 36 };
        panel.Children.Add(txtName);

        panel.Children.Add(new TextBlock { Text = "任课教师", FontWeight = Avalonia.Media.FontWeight.SemiBold, Foreground = Avalonia.Media.Brushes.Gray });
        var txtTeacher = new TextBox { Text = courseData.Teacher, Watermark = "例如：张老师", Height = 36 };
        panel.Children.Add(txtTeacher);

        panel.Children.Add(new TextBlock { Text = "上课地点", FontWeight = Avalonia.Media.FontWeight.SemiBold, Foreground = Avalonia.Media.Brushes.Gray });
        var txtRoom = new TextBox { Text = courseData.Classroom, Watermark = "例如：A101", Height = 36 };
        panel.Children.Add(txtRoom);

        var btnPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, Spacing = 10, Margin = new Thickness(0, 10, 0, 0) };
        var btnCancel = new Button { Content = "取消", Width = 80, Height = 36, Background = Avalonia.Media.Brushes.LightGray };
        var btnSave = new Button { Content = "保存", Width = 80, Height = 36, Classes = { "accent" } };

        btnCancel.Click += (s, args) => dialog.Close();
        btnSave.Click += (s, args) =>
        {
            if (string.IsNullOrWhiteSpace(txtName.Text)) return;

            if (isNew)
            {
                courseData.Name = txtName.Text.Trim();
                courseData.Teacher = string.IsNullOrWhiteSpace(txtTeacher.Text) ? "待定" : txtTeacher.Text.Trim();
                courseData.Classroom = string.IsNullOrWhiteSpace(txtRoom.Text) ? "未知" : txtRoom.Text.Trim();
                vm.AddCourse(courseData);
            }
            else
            {
                if (originalVm != null)
                {
                    originalVm.Model.Name = txtName.Text.Trim();
                    originalVm.Model.Teacher = string.IsNullOrWhiteSpace(txtTeacher.Text) ? "待定" : txtTeacher.Text.Trim();
                    originalVm.Model.Classroom = string.IsNullOrWhiteSpace(txtRoom.Text) ? "未知" : txtRoom.Text.Trim();
                    vm.SaveData();
                    vm.RefreshCurrentWeek();
                }
            }
            dialog.Close();
        };

        btnPanel.Children.Add(btnCancel);
        btnPanel.Children.Add(btnSave);
        panel.Children.Add(btnPanel);
        dialog.Content = panel;
        
        var parentWindow = TopLevel.GetTopLevel(this) as Window;
        if (parentWindow != null) await dialog.ShowDialog(parentWindow);
    }
}