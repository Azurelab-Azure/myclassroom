using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using PlanPeopleAvalonia.Models;
using PlanPeopleAvalonia.Services;
using PlanPeopleAvalonia.ViewModels;
using PlanPeopleAvalonia.Views.Components;

namespace PlanPeopleAvalonia.Views;

public partial class ScheduleView : UserControl
{
    private ScheduleViewModel _vm = null!;
    private ScheduleSubjectPanel _subjectPanel = null!;
    private Grid _mainGrid = null!;

    // 拖拽相关状态
    private Course? _selectedCourse = null;
    private Border? _selectedCard = null;
    private bool _isDragging = false;
    private Point _dragStartPoint;
    private DateTime _lastClickTime = DateTime.MinValue;

    private const int HeaderWidth = 80;
    private const int RowHeight = 80;

    public ScheduleView()
    {
        this.DataContextChanged += (s, e) =>
        {
            if (DataContext is ScheduleViewModel vm)
            {
                _vm = vm;
                BuildUI();
            }
        };
    }

    private void BuildUI()
    {
        var root = new Grid { Background = ThemeManager.WindowBrush, ColumnDefinitions = new ColumnDefinitions("*, 240") };
        this.Content = root;

        var leftPanel = new Grid { RowDefinitions = new RowDefinitions("Auto, Auto, *") };
        Grid.SetColumn(leftPanel, 0);
        root.Children.Add(leftPanel);

        // 1. 顶部栏 (TopBar)
        var topBar = new ScheduleTopBar();
        topBar.SaveClicked += () => { 
            _vm.LoadData(); 
            BuildUI(); 
        };
        topBar.ImportClicked += () => { 
            _vm.LoadData(); 
            BuildUI(); 
        };
        topBar.TabChanged += (tab) =>
        {
            if (tab == "课表") 
            { 
                _vm.LoadData(); 
                BuildUI(); 
            }
            else 
            {
                // ★ 联动：打开设置弹窗，保存后触发刷新
                new ScheduleSettingsView(tab, () => 
                { 
                    _subjectPanel.ReloadSubjects(); 
                    _vm.LoadData(); 
                    BuildUI(); 
                }).ShowDialog((Window)TopLevel.GetTopLevel(this)!);
            }
        };
        Grid.SetRow(topBar, 0);
        leftPanel.Children.Add(topBar);

        // 2. 副工具栏 (SubBar)
        var subBar = new ScheduleSubBar(_vm, () => { 
            _vm.LoadData(); 
            BuildUI(); 
        });
        Grid.SetRow(subBar, 1);
        leftPanel.Children.Add(subBar);

        // 3. 课表核心网格
        var scroll = new ScrollViewer { Background = ThemeManager.WindowBrush };
        Grid.SetRow(scroll, 2);
        leftPanel.Children.Add(scroll);
        BuildGrid(scroll);

        // 4. 右侧科目面板 (SubjectPanel)
        _subjectPanel = new ScheduleSubjectPanel();
        _subjectPanel.SubjectClicked += (sub) =>
        {
            var teachers = CourseRepository.LoadTeachers();
            var matchedTeacher = teachers.FirstOrDefault(t => sub.Contains(t.Subject) || t.Subject.Contains(sub))?.Name ?? "";
            
            int day = 1, sec = 1;
            var existing = CourseRepository.LoadCourses();
            bool found = false;
            for (int d = 1; d <= 7 && !found; d++)
            {
                for (int s = 1; s <= 8 && !found; s++)
                {
                    if (!existing.Any(c => c.WeekDay == d && s >= c.TimeStart && s <= c.TimeEnd))
                    {
                        day = d; sec = s; found = true;
                    }
                }
            }
            existing.Add(new Course { Name = sub, Teacher = matchedTeacher, WeekDay = day, TimeStart = sec, TimeEnd = sec, Weeks = Enumerable.Range(1, 20).ToList() });
            CourseRepository.SaveCourses(existing);
            _vm.LoadData();
            BuildUI();
        };
        Grid.SetColumn(_subjectPanel, 1);
        root.Children.Add(_subjectPanel);
    }

    private void BuildGrid(ScrollViewer scroll)
    {
        var containerGrid = new Grid { RowDefinitions = new RowDefinitions("Auto, Auto") };
        scroll.Content = containerGrid;

        // ================= 1. 表头行 (周一 ~ 周日) =================
        var headerGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("80, *, *, *, *, *, *, *") };
        for (int i = 0; i < 8; i++)
        {
            var border = new Border { BorderBrush = ThemeManager.BorderBrush, BorderThickness = new Thickness(0, 0, 1, 1) };
            Grid.SetColumn(border, i);
            if (i > 0)
            {
                var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Spacing = 4 };
                stack.Children.Add(new TextBlock { Text = i switch { 1 => "周一", 2 => "周二", 3 => "周三", 4 => "周四", 5 => "周五", 6 => "周六", _ => "周日" }, Foreground = ThemeManager.TextPrimaryBrush, HorizontalAlignment = HorizontalAlignment.Center, FontWeight = FontWeight.Bold });
                stack.Children.Add(new TextBlock { Text = DateTime.Now.AddDays(i - 1).ToString("MM/dd"), Foreground = ThemeManager.TextSecondaryBrush, FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center });
                border.Child = stack;
            }
            headerGrid.Children.Add(border);
        }
        Grid.SetRow(headerGrid, 0);
        containerGrid.Children.Add(headerGrid);

        // ================= 2. 主体网格 (时间列 + 课表网格) =================
        _mainGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("80, *"), [Grid.RowProperty] = 1, Background = Brushes.Transparent };

        // 2.1 左侧时间列
        var leftTime = new StackPanel();
        for (int i = 1; i <= 8; i++)
        {
            // ★ 完美对接 ScheduleConfigStore 中的时间段配置
            var period = ScheduleConfigStore.Periods.FirstOrDefault(p => p.Section == i);
            string timeStr = period != null ? $"{period.StartTime}\n{period.EndTime}" : "";
            
            var cell = new Border { Height = RowHeight, BorderBrush = ThemeManager.BorderBrush, BorderThickness = new Thickness(0, 0, 1, 1) };
            var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Spacing = 4 };
            
            stack.Children.Add(new TextBlock { Text = i.ToString(), Foreground = ThemeManager.TextPrimaryBrush, FontSize = 16, FontWeight = FontWeight.Bold, HorizontalAlignment = HorizontalAlignment.Center });
            stack.Children.Add(new TextBlock { Text = timeStr, Foreground = ThemeManager.TextSecondaryBrush, FontSize = 10, TextAlignment = TextAlignment.Center });
            
            cell.Child = stack;
            leftTime.Children.Add(cell);
        }
        Grid.SetColumn(leftTime, 0);
        _mainGrid.Children.Add(leftTime);

        // 2.2 右侧课程网格 (7列 x 8行背景)
        var rightGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*, *, *, *, *, *, *"), [Grid.ColumnProperty] = 1 };
        for (int r = 0; r < 8; r++)
        {
            rightGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(RowHeight) });
            for (int c = 0; c < 7; c++)
            {
                var cell = new Border { BorderBrush = ThemeManager.BorderBrush, BorderThickness = new Thickness(0, 0, 1, 1), Background = Brushes.Transparent };
                Grid.SetRow(cell, r);
                Grid.SetColumn(cell, c);
                rightGrid.Children.Add(cell);
            }
        }

        // 2.3 渲染课程卡片
        foreach (var rowData in _vm.GridRows)
        {
            for (int c = 0; c < 7; c++)
            {
                var course = c switch { 0 => rowData.MondayCourse, 1 => rowData.TuesdayCourse, 2 => rowData.WednesdayCourse, 3 => rowData.ThursdayCourse, 4 => rowData.FridayCourse, 5 => rowData.SaturdayCourse, 6 => rowData.SundayCourse, _ => null };
                
                // 只有在起始节次渲染卡片
                if (course != null && rowData.SectionNumber == course.TimeStart)
                {
                    int rowSpan = Math.Max(1, course.TimeEnd - course.TimeStart + 1);
                    
                    // ★ 完美对接 ScheduleConfigStore 科目颜色
                    string colorHex = ScheduleConfigStore.GetColorForSubject(course.Name);
                    var card = new Border
                    {
                        Background = new SolidColorBrush(Color.Parse(colorHex)),
                        CornerRadius = new CornerRadius(6),
                        Margin = new Thickness(3),
                        VerticalAlignment = VerticalAlignment.Stretch,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        ZIndex = 10,
                        Cursor = new Cursor(StandardCursorType.Hand),
                        BorderThickness = new Thickness(2),
                        BorderBrush = Brushes.Transparent, // 选中时变为白色
                        Tag = course
                    };

                    var panel = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Spacing = 2 };
                    panel.Children.Add(new TextBlock { Text = course.Name, Foreground = Brushes.White, FontWeight = FontWeight.Bold, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap });
                    if (!string.IsNullOrWhiteSpace(course.Teacher))
                    {
                        panel.Children.Add(new TextBlock { Text = course.Teacher, Foreground = Brushes.White, FontSize = 11, TextAlignment = TextAlignment.Center });
                    }
                    card.Child = panel;

                    Grid.SetRow(card, course.TimeStart - 1);
                    Grid.SetColumn(card, c);
                    Grid.SetRowSpan(card, rowSpan);
                    rightGrid.Children.Add(card);

                    // 绑定交互事件
                    card.PointerPressed += OnCoursePointerPressed;
                    card.PointerMoved += OnCoursePointerMoved;
                    card.PointerReleased += OnCoursePointerReleased;
                }
            }
        }
        Grid.SetColumn(rightGrid, 1);
        _mainGrid.Children.Add(rightGrid);

        containerGrid.Children.Add(_mainGrid);
    }

    // ================== 拖拽与双击交互逻辑 ==================

    private void OnCoursePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border card || card.Tag is not Course course) return;

        // 双击检测
        var currentTime = DateTime.Now;
        if (currentTime - _lastClickTime < TimeSpan.FromMilliseconds(300) && _selectedCard == card)
        {
            // 双击触发编辑
            OpenEditWindow(course);
            _lastClickTime = DateTime.MinValue;
            return;
        }
        _lastClickTime = currentTime;

        // 选中高亮
        if (_selectedCard != null) _selectedCard.BorderBrush = Brushes.Transparent;
        _selectedCard = card;
        _selectedCourse = course;
        _selectedCard.BorderBrush = Brushes.White;

        // 准备拖拽
        _isDragging = false;
        _dragStartPoint = e.GetPosition(this);
        e.Pointer.Capture(card);
    }

    private void OnCoursePointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is not Border card || _selectedCourse == null) return;
        
        var currentPoint = e.GetPosition(this);
        if (!_isDragging && Math.Abs(currentPoint.Y - _dragStartPoint.Y) > 10)
        {
            _isDragging = true;
        }

        if (_isDragging)
        {
            // 在拖拽时给一个半透明效果
            card.Opacity = 0.5;
        }
    }

    private void OnCoursePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is not Border card) return;
        card.Opacity = 1.0;
        e.Pointer.Capture(null);

        if (_isDragging && _selectedCourse != null)
        {
            // 计算释放时的目标网格
            var pos = e.GetPosition(_mainGrid);
            int targetCol = (int)(pos.X / ((_mainGrid.Bounds.Width - HeaderWidth) / 7));
            int targetRow = (int)(pos.Y / RowHeight);

            if (targetCol >= 0 && targetCol < 7 && targetRow >= 0 && targetRow < 8)
            {
                if (ScheduleConfigStore.LockPublished) return; // 锁定已发布课表

                var courses = CourseRepository.LoadCourses();
                var targetCourse = courses.FirstOrDefault(c => c.WeekDay == targetCol + 1 && c.TimeStart == targetRow + 1);
                
                if (targetCourse != null)
                {
                    if (ScheduleConfigStore.DragSwapEnabled)
                    {
                        // 交换课时
                        int oldDay = _selectedCourse.WeekDay;
                        int oldStart = _selectedCourse.TimeStart;
                        int oldEnd = _selectedCourse.TimeEnd;
                        
                        _selectedCourse.WeekDay = targetCourse.WeekDay;
                        _selectedCourse.TimeStart = targetCourse.TimeStart;
                        _selectedCourse.TimeEnd = targetCourse.TimeEnd;
                        
                        targetCourse.WeekDay = oldDay;
                        targetCourse.TimeStart = oldStart;
                        targetCourse.TimeEnd = oldEnd;
                    }
                    else if (ScheduleConfigStore.ConflictWarnEnabled)
                    {
                        // 冲突提示
                        ShowMessage("排课冲突", "该时间段已有其他课程！");
                        _isDragging = false;
                        return;
                    }
                }
                else
                {
                    // 移动到空白位置
                    _selectedCourse.WeekDay = targetCol + 1;
                    _selectedCourse.TimeStart = targetRow + 1;
                    _selectedCourse.TimeEnd = targetRow + 1 + (_selectedCourse.TimeEnd - _selectedCourse.TimeStart);
                }
                
                CourseRepository.SaveCourses(courses);
                _vm.LoadData();
                BuildUI();
            }
        }
        _isDragging = false;
    }

    private void OpenEditWindow(Course course)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner) return;
        
        // 简单示例：双击后提示
        ShowMessage("课程信息", $"课程：{course.Name}\n教师：{course.Teacher}\n时间：周{course.WeekDay} 第{course.TimeStart}-{course.TimeEnd}节");
    }

    private async void ShowMessage(string title, string content)
    {
        if (TopLevel.GetTopLevel(this) is Window owner)
        {
            var dlg = new Window
            {
                Title = title,
                Width = 320,
                Height = 160,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = ThemeManager.WindowBrush
            };
            var panel = new StackPanel { Margin = new Thickness(20), Spacing = 16 };
            panel.Children.Add(new TextBlock { Text = content, Foreground = ThemeManager.TextPrimaryBrush, TextWrapping = TextWrapping.Wrap });
            var okBtn = new Button { Content = "确定", Background = ThemeManager.AccentBrush, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Right, Padding = new Thickness(16, 6) };
            okBtn.Click += (s, e) => dlg.Close();
            panel.Children.Add(okBtn);
            dlg.Content = panel;
            await dlg.ShowDialog(owner);
        }
    }
}