using Avalonia.Controls;        // ★ 核心修复：补上 UserControl 所在的命名空间
using Avalonia.Interactivity;   // 用于 RoutedEventArgs
using Avalonia.VisualTree;      // 用于 FindAncestorOfType
using PlanPeopleAvalonia.Views; // 用于查找 TeacherView

namespace PlanPeopleAvalonia.Views.TeacherComponents;

public partial class TeacherTopBar : UserControl
{
    public TeacherTopBar()
    {
        InitializeComponent();
    }

    // 点击编辑按钮事件
    private void OnEditClick(object? sender, RoutedEventArgs e)
    {
        // 向上查找父级 TeacherView，调用它的弹窗方法
        var parentView = this.FindAncestorOfType<TeacherView>();
        parentView?.ShowEditTeacherDialog();
    }
}