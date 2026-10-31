using System;
using Avalonia.Controls;
using Avalonia.VisualTree; // ★ 修复：必须引入这个才能用 FindAncestorOfType
using PlanPeopleAvalonia.ViewModels;


namespace PlanPeopleAvalonia.Views.ScheduleComponents;

// ★ 确保类名和 XAML 里的 x:Class 完全一致！
public partial class ScheduleSubjectPanel : UserControl
{
    public ScheduleSubjectPanel()
    {
         InitializeComponent(); // ★ 确保有这一行
    }

    /// <summary>
    /// 点击右侧科目列表时触发
    /// </summary>
    private void SubjectList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is string subject && DataContext is ScheduleViewModel vm)
        {
            // 1. 向上寻找父级视图（ScheduleView）
            var parent = this.FindAncestorOfType<ScheduleView>();
            
            // 2. 调用父级视图的快捷添加方法
            parent?.HandleQuickAddCourse(subject);
            
            // 3. 取消选中状态，方便下次点击同一个科目时还能触发
            listBox.SelectedItem = null;
        }
    }
}