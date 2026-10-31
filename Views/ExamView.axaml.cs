using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PlanPeopleAvalonia.Models;
using PlanPeopleAvalonia.ViewModels;
using PlanPeopleAvalonia.Views.ExamComponents;

namespace PlanPeopleAvalonia.Views;

public partial class ExamView : UserControl
{
    private ExamViewModel _vm;

    public ExamView()
    {
        InitializeComponent();
        _vm = new ExamViewModel();
        this.DataContext = _vm;

        // 挂载“录入新考试”按钮逻辑
        var listView = this.FindControl<ExamListView>("ListView");
        var detailView = this.FindControl<ExamDetailView>("DetailView");
        
        if (detailView != null)
        {
            // 监听 DetailView 里的录入分数按钮点击
            var btnEnter = detailView.FindControl<Button>("BtnEnterScores");
            if (btnEnter != null)
            {
                btnEnter.Click += async (s, e) => 
                {
                    if (_vm.SelectedExam == null) return;
                    
                    // 弹出成绩录入窗口
                    var scoreWindow = new ScoreEntryWindow(_vm.SelectedExam, () =>
                    {
                        _vm.SaveExam(_vm.SelectedExam); // 保存后刷新
                    });
                    
                    var parent = TopLevel.GetTopLevel(this) as Window;
                    if (parent != null) await scoreWindow.ShowDialog(parent);
                };
            }
        }
    }

    // 新建考试弹窗
    private async void OnAddClick(object? sender, RoutedEventArgs e)
    {
        var newExam = new ExamRecord { Name = "新考试", Date = DateTime.Now.ToString("yyyy-MM-dd"), Subject = "未设置" };
        
        var dialog = new ExamEditorWindow(newExam, () =>
        {
            _vm.SaveExam(newExam);
        });

        var parent = TopLevel.GetTopLevel(this) as Window;
        if (parent != null) await dialog.ShowDialog(parent);
    }
}