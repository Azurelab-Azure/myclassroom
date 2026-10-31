using System;
using Avalonia.Controls;
using PlanPeopleAvalonia.ViewModels;
using PlanPeopleAvalonia.Services;
using PlanPeopleAvalonia.Views.TeacherComponents;

namespace PlanPeopleAvalonia.Views;

public partial class TeacherView : UserControl
{
    public TeacherView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 供顶部工具栏调用的编辑教师弹窗
    /// </summary>
    public async void ShowEditTeacherDialog()
    {
        // 1. 安全断言：防止 DataContext 缺失或未选中教师导致空引用
        if (DataContext is not TeacherViewModel vm) return;
        if (vm.SelectedTeacher == null) return;

        // 2. 实例化全新的 XAML 档案弹窗
        var dialog = new TeacherEditorWindow(vm.SelectedTeacher, () =>
        {
            try
            {
                // 保存到本地 JSON
                CourseRepository.SaveTeachers(vm.Teachers);
                
                // 刷新 ViewModel，确保列表和右侧详情同步更新
                vm.LoadData();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[保存教师失败] {ex.Message}");
            }
        });

        // 3. 获取父窗口并显示弹窗
        // TopLevel.GetTopLevel(this) 比 direct Window 更安全，兼容多平台
        var parentWindow = TopLevel.GetTopLevel(this) as Window;
        if (parentWindow != null)
        {
            await dialog.ShowDialog(parentWindow);
        }
        else
        {
            // 极端情况：如果找不到父窗口（比如在主窗口加载前被调用），直接 Show 避免崩溃
            dialog.Show();
        }
    }
}