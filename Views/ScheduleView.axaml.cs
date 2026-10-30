using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
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

        var topBar = new ScheduleTopBar();
        topBar.SaveClicked += () => { _vm.LoadData(); BuildUI(); };
        topBar.ImportClicked += () => { _vm.LoadData(); BuildUI(); };
        topBar.TabChanged += (tab) =>
        {
            if (tab == "课表") { _vm.LoadData(); BuildUI(); }
            else new ScheduleSettingsView(tab, () => { _subjectPanel.ReloadSubjects(); _vm.LoadData(); BuildUI(); }).ShowDialog((Window)TopLevel.GetTopLevel(this)!);
        };
        Grid.SetRow(topBar, 0);
        leftPanel.Children.Add(topBar);

        var subBar = new ScheduleSubBar(_vm, () => { _vm.LoadData(); BuildUI(); });
        Grid.SetRow(subBar, 1);
        leftPanel.Children.Add(subBar);

        var scroll = new ScrollViewer { Background = ThemeManager.WindowBrush };
        Grid.SetRow(scroll, 2);
        leftPanel.Children.Add(scroll);
        BuildGrid(scroll);

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

        var headerGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("80, *, *, *, *, *, *, *") };
        for (int i = 0; i < 8; i++)
        {
            var border = new Border { BorderBrush = ThemeManager.BorderBrush, BorderThickness = new Thickness(0, 0, 1, 1) };
            Grid.SetColumn(border, i);
            if (i > 0)
            {
                var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Spacing = 4 };
                stack.Children.Add(new TextBlock { Text = i switch { 1 => "周一", 2 => "周二", 3 => "周三", 4 => "周四", 5 => "周五", 6 => "周六", _ => "周日" }, Foreground = ThemeManager.TextSecondaryBrush, HorizontalAlignment = HorizontalAlignment.Center, FontWeight = FontWeight.Bold });
                stack.Children.Add(new TextBlock { Text = DateTime.Now.AddDays(i - 1).ToString("MM/dd"), Foreground = ThemeManager.TextSecondaryBrush, FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center });
                border.Child = stack;
            }
            headerGrid.Children.Add(border);
        }
        Grid.SetRow(headerGrid, 0);
        containerGrid.Children.Add(headerGrid);

        _mainGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("80, *"), [Grid.RowProperty] = 1, Background = Brushes.Transparent };

        var leftTime = new StackPanel();
        for (int i = 1; i <= 8; i++)
        {
            var period = ScheduleConfigStore.Periods.FirstOrDefault(p => p.Section == i);
            string timeStr = period != null ? $"{period.StartTime}\n{period.EndTime}" : "";
            
            var cell = new Border { Height = RowHeight, BorderBrush = ThemeManager.BorderBrush, BorderThickness = new Thickness(0, 0, 1, 1) };
            var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Spacing = 4 };
            stack.Children.Add(new TextBlock { Text = i.ToString(), Foreground = ThemeManager.TextPrimaryBrush, FontSize = 16, FontWeight = FontWeight.Bold });
            stack.Children.Add(new TextBlock { Text = timeStr, Foreground = ThemeManager.TextSecondaryBrush, FontSize = 10, TextAlignment = TextAlignment.Center });
            cell.Child = stack;
            leftTime.Children.Add(cell);
        }
        Grid.SetColumn(leftTime, 0);
        _mainGrid.Children.Add(leftTime);

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

        foreach (var rowData in _vm.GridRows)
        {
            for (int c = 0; c < 7; c++)
            {
                var course = c switch { 0 => rowData.MondayCourse, 1 => rowData.TuesdayCourse, 2 => rowData.WednesdayCourse, 3 => rowData.ThursdayCourse, 4 => rowData.FridayCourse, 5 => rowData.SaturdayCourse, 6 => rowData.SundayCourse, _ => null };
                if (course != null && rowData.SectionNumber == course.TimeStart)
                {
                    int rowSpan = Math.Max(1, course.TimeEnd - course.TimeStart + 1);
                    
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
                        BorderBrush = Brushes.Transparent,
                        Tag = course
                    };

                    var panel = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
                    panel.Children.Add(new TextBlock { Text = course.Name, Foreground = Brushes.White, FontWeight = FontWeight.Bold, TextAlignment = TextAlignment.Center });
                    panel.Children.Add(new TextBlock { Text = course.Teacher, Foreground = Brushes.White, FontSize = 11, TextAlignment = TextAlignment.Center });
                    card.Child = panel;

                    Grid.SetRow(card, course.TimeStart - 1);
                    Grid.SetColumn(card, c);
                    Grid.SetRowSpan(card, rowSpan);
                    rightGrid.Children.Add(card);
                }
            }
        }
        Grid.SetColumn(rightGrid, 1);
        _mainGrid.Children.Add(rightGrid);

        _mainGrid.PointerPressed += OnGridPointerPressed;
        _mainGrid.PointerMoved += OnGridPointerMoved;
        _mainGrid.PointerReleased += OnGridPointerReleased;

        containerGrid.Children.Add(_mainGrid);
    }

    private (int day, int section) GetCellAt(Point p)
    {
        double x = p.X - HeaderWidth;
        if (x < 0) return (-1, -1);
        int day = (int)(x / ((_mainGrid.Bounds.Width - HeaderWidth) / 7)) + 1;
        int section = (int)(p.Y / RowHeight) + 1;
        if (day < 1 || day > 7 || section < 1 || section > 8) return (-1, -1);
        return (day, section);
    }

    private void OnGridPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var pos = e.GetPosition(_mainGrid);
        var (day, section) = GetCellAt(pos);
        if (day == -1) return;

        var hitCourse = CourseRepository.LoadCourses().FirstOrDefault(c => c.WeekDay == day && section >= c.TimeStart && section <= c.TimeEnd);

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            if (hitCourse != null)
            {
                _selectedCourse = hitCourse;
                _isDragging = true;
                _dragStartPoint = pos;

                var cards = _mainGrid.GetVisualDescendants().OfType<Border>().Where(b => b.Tag is Course);
                foreach (var c in cards) c.BorderBrush = Brushes.Transparent;
                var card = cards.FirstOrDefault(b => b.Tag == hitCourse);
                if (card != null) { card.BorderBrush = Brushes.Yellow; _selectedCard = card; }
            }
            else
            {
                var now = DateTime.Now;
                if ((now - _lastClickTime).TotalMilliseconds < 400)
                {
                    _ = ShowEditorDialog(null, day, section);
                    _lastClickTime = DateTime.MinValue;
                }
                else _lastClickTime = now;
            }
            e.Handled = true;
        }
    }

    private void OnGridPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDragging && _selectedCourse != null && _selectedCard != null)
        {
            var pos = e.GetPosition(_mainGrid);
            if (Math.Abs(pos.X - _dragStartPoint.X) > 10 || Math.Abs(pos.Y - _dragStartPoint.Y) > 10)
            {
                _selectedCard.Opacity = 0.5;
            }
        }
    }

    private void OnGridPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDragging && _selectedCourse != null)
        {
            var pos = e.GetPosition(_mainGrid);
            var (day, section) = GetCellAt(pos);

            if (day != -1 && (day != _selectedCourse.WeekDay || section != _selectedCourse.TimeStart))
            {
                var all = CourseRepository.LoadCourses();
                var target = all.FirstOrDefault(c => c.Name == _selectedCourse.Name && c.WeekDay == _selectedCourse.WeekDay && c.TimeStart == _selectedCourse.TimeStart);
                if (target != null)
                {
                    int duration = target.TimeEnd - target.TimeStart;
                    target.WeekDay = day;
                    target.TimeStart = section;
                    target.TimeEnd = section + duration;
                    CourseRepository.SaveCourses(all);
                }
                _vm.LoadData();
                BuildUI();
            }
            else if (_selectedCard != null)
            {
                _selectedCard.Opacity = 1;
            }
            _isDragging = false;
            _selectedCourse = null;
            _selectedCard = null;
        }
    }

    private async Task ShowEditorDialog(Course? existingCourse, int defaultWeekday = 0, int defaultSection = 0)
    {
        var editorVm = new CourseEditorViewModel();
        editorVm.LoadData(existingCourse, CourseRepository.LoadTeachers());
        if (existingCourse == null && defaultWeekday > 0)
        {
            editorVm.WeekDayIndex = defaultWeekday - 1;
            editorVm.StartSection = defaultSection;
            editorVm.EndSection = defaultSection;
        }
        var dlg = new CourseEditorWindow { DataContext = editorVm };
        if (TopLevel.GetTopLevel(this) is Window owner)
        {
            await dlg.ShowDialog(owner);
            _vm.LoadData();
            BuildUI();
        }
    }
}