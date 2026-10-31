using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters; // ★ 修复 CS0246：ContentPresenter 所在命名空间
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using PlanPeopleAvalonia.ViewModels;

namespace PlanPeopleAvalonia.Views.ScheduleComponents;

public partial class ScheduleGrid : UserControl
{
    private CourseViewModel? _draggedCourse;
    private ContentPresenter? _draggedContainer; 
    private bool _isDragging;
    private Point _clickOffset;
    private Point _startPoint;
    private DateTime _lastClickTime = DateTime.MinValue;
    private Point _lastClickPosition;

    public ScheduleGrid()
    {
         InitializeComponent();
        
        // 纯代码挂载，拦截所有底层拦截
        this.AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        this.AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
        this.AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        var grid = this.FindControl<Grid>("MainGrid");
        if (grid == null || DataContext is not ScheduleViewModel vm) return;

        var pos = e.GetPosition(grid);
        
        var itemsControl = this.FindControl<ItemsControl>("CourseItemsControl");
        if (itemsControl == null) return;

        _draggedCourse = null;
        _draggedContainer = null;

        for (int i = 0; i < vm.CurrentCourses.Count; i++)
        {
            var course = vm.CurrentCourses[i];
            if (pos.X >= course.Left && pos.X <= course.Left + course.Width &&
                pos.Y >= course.Top && pos.Y <= course.Top + course.Height)
            {
                _draggedCourse = course;
                // 获取生成它的容器 UI 元素（加上 as 强制转换，修复 CS1503）
                _draggedContainer = itemsControl.ContainerFromIndex(i) as ContentPresenter;
                break;
            }
        }

        // 修复 CS0019：正确处理 null 判断
        if (_draggedCourse != null && _draggedContainer != null)
        {
            _startPoint = pos;
            _clickOffset = new Point(pos.X - _draggedCourse.Left, pos.Y - _draggedCourse.Top);
            
            e.Pointer.Capture(grid);
            e.Handled = true;
        }
        else
        {
            _lastClickPosition = pos;
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_draggedCourse == null || _draggedContainer == null) return;

        var grid = this.FindControl<Grid>("MainGrid");
        if (grid == null) return;

        var pos = e.GetPosition(grid);
        
        if (Math.Sqrt(Math.Pow(pos.X - _startPoint.X, 2) + Math.Pow(pos.Y - _startPoint.Y, 2)) > 5)
        {
            _isDragging = true;
        }

        if (_isDragging)
        {
            double targetLeft = Math.Clamp(pos.X - _clickOffset.X, 0, grid.Bounds.Width - _draggedCourse.Width);
            double targetTop = Math.Clamp(pos.Y - _clickOffset.Y, 0, grid.Bounds.Height - _draggedCourse.Height);
            
            // 直接强改 UI 控件的位置
            Canvas.SetLeft(_draggedContainer, targetLeft);
            Canvas.SetTop(_draggedContainer, targetTop);
            
            // 同步数据，防止松手后弹回
            _draggedCourse.VisualLeft = targetLeft;
            _draggedCourse.VisualTop = targetTop;
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (DataContext is not ScheduleViewModel vm) return;
        var grid = this.FindControl<Grid>("MainGrid");
        if (grid == null) return;

        e.Pointer.Capture(null);

        // ★ 修复 CS0103：把 pos 的作用域提前，保证双击判断也能用
        var pos = e.GetPosition(grid);

        if (_isDragging && _draggedCourse != null)
        {
            // 计算最终吸附的格子
            int col = Math.Clamp((int)Math.Round(_draggedCourse.VisualLeft / vm.CellWidth), 0, 6);
            int row = Math.Clamp((int)Math.Round(_draggedCourse.VisualTop / vm.CellHeight), 0, vm.TotalSections - 1);
            
            int span = _draggedCourse.Model.TimeEnd - _draggedCourse.Model.TimeStart;

            // 更新模型数据
            _draggedCourse.Model.WeekDay = col + 1;
            _draggedCourse.Model.TimeStart = row + 1;
            _draggedCourse.Model.TimeEnd = Math.Min(vm.TotalSections, _draggedCourse.Model.TimeStart + span);

            _draggedCourse.VisualLeft = -1;
            _draggedCourse.VisualTop = -1;
            
            vm.SaveData();
            vm.RefreshCurrentWeek(); // 强制刷新，回到正确的网格中

            _isDragging = false;
            _draggedCourse = null;
            _draggedContainer = null;
            e.Handled = true;
            return;
        }

        // 未拖拽，双击判断
        _draggedCourse = null;
        _draggedContainer = null;
        _isDragging = false;

        var timeDiff = DateTime.Now - _lastClickTime;
        var posDiff = Math.Sqrt(Math.Pow(pos.X - _lastClickPosition.X, 2) + Math.Pow(pos.Y - _lastClickPosition.Y, 2));

        if (timeDiff.TotalMilliseconds < 300 && posDiff < 10)
        {
            _lastClickTime = DateTime.MinValue;
            int col = Math.Clamp((int)(pos.X / vm.CellWidth), 0, 6);
            int row = Math.Clamp((int)(pos.Y / vm.CellHeight), 0, vm.TotalSections - 1);
            this.FindAncestorOfType<ScheduleView>()?.CreateNewCourse(col + 1, row + 1);
        }
        else
        {
            _lastClickTime = DateTime.Now;
        }
    }
}