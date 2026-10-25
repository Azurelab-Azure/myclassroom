using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using PlanPeopleAvalonia.Models;
using PlanPeopleAvalonia.Services;
using PlanPeopleAvalonia.ViewModels;

namespace PlanPeopleAvalonia.Views;

public partial class TeacherView : UserControl
{
    private readonly TeacherTopBar _topBar;
    private readonly TeacherListView _listView;
    private readonly TeacherDetailView _detailView;
    
    private Teacher? _currentSelectedTeacher;

    public TeacherView()
    {
        _detailView = new TeacherDetailView();
        _listView = new TeacherListView();
        _topBar = new TeacherTopBar();

        // ★ 核心修复 1：接收单击 -> 刷新详情
        _listView.TeacherSelected += (t) => 
        {
            _currentSelectedTeacher = t;
            _detailView.ShowTeacher(t); 
        };

        // ★ 核心修复 2：接收双击 -> 弹出编辑
        _listView.TeacherDoubleClicked += (t) => 
        {
            OpenEditor(t);
        };

        _topBar.AddClicked += () => OpenEditor(null);

        // 组装
        var body = new Grid { ColumnDefinitions = new ColumnDefinitions("280, *") };
        
        var leftBorder = new Border 
        { 
            Background = ThemeManager.WindowBrush, 
            BorderBrush = ThemeManager.BorderBrush, 
            BorderThickness = new Thickness(0, 0, 1, 0), 
            Child = _listView,
            [Grid.ColumnProperty] = 0
        };
        body.Children.Add(leftBorder);

        _detailView.HorizontalAlignment = HorizontalAlignment.Stretch;
        _detailView.VerticalAlignment = VerticalAlignment.Stretch;
        Grid.SetColumn(_detailView, 1);
        body.Children.Add(_detailView);

        var root = new Grid { RowDefinitions = new RowDefinitions("Auto, *") };
        Grid.SetRow(_topBar, 0);
        root.Children.Add(_topBar);
        Grid.SetRow(body, 1);
        root.Children.Add(body);

        Content = root;
        
        ReloadTeachers();
    }

    private void ReloadTeachers()
    {
        try
        {
            var list = CourseRepository.LoadTeachers() ?? new List<Teacher>();
            _listView.SetTeachers(list);
            
            if (_currentSelectedTeacher != null)
            {
                var current = list.Find(t => t.Name == _currentSelectedTeacher.Name);
                if (current != null)
                {
                    _currentSelectedTeacher = current;
                    _detailView.ShowTeacher(current);
                }
                else
                {
                    _currentSelectedTeacher = null;
                    _detailView.Clear();
                }
            }
            else
            {
                _detailView.Clear();
            }
        }
        catch { }
    }

    private void OpenEditor(Teacher? teacher)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner) return;

        var win = new TeacherEditorWindow(teacher, () =>
        {
            ReloadTeachers();
        });
        win.ShowDialog(owner);
    }
}