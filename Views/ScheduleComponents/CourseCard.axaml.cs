using Avalonia.Controls;
using Avalonia.Interactivity;
using PlanPeopleAvalonia.ViewModels;
using Avalonia.VisualTree;

namespace PlanPeopleAvalonia.Views.ScheduleComponents;

public partial class CourseCard : UserControl
{
    public CourseCard()
    {
       InitializeComponent();
    }

    // 右键菜单事件会向外抛出（你可以通过事件中心、或者向上查找父级 View 处理）
    private void EditCourse_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CourseViewModel vm)
        {
            // 向上查找 ScheduleView 进行处理
            var parent = this.FindAncestorOfType<ScheduleView>();
            parent?.HandleEditCourse(vm);
        }
    }

    private void DeleteCourse_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CourseViewModel vm)
        {
            var parent = this.FindAncestorOfType<ScheduleView>();
            parent?.HandleDeleteCourse(vm);
        }
    }
}