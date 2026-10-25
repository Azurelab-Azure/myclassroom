using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using PlanPeopleAvalonia.Models;
using PlanPeopleAvalonia.Services;
using PlanPeopleAvalonia.ViewModels;
using PlanPeopleAvalonia.Views.StudentComponents;

namespace PlanPeopleAvalonia.Views;

public partial class StudentView : UserControl
{
    private readonly StudentTopBar _topBar;
    private readonly StudentListView _listView;
    private readonly StudentDetailView _detailView;

    private List<Student> _allStudents = new();
    private string _currentSearch = "";

    public StudentView()
    {
        _detailView = new StudentDetailView();
        _listView = new StudentListView();
        _topBar = new StudentTopBar();

        // ★ 绑定事件 (确保判空，防止右侧白屏)
        _listView.StudentSelected += t => 
        {
            if (t == null) _detailView.Clear();
            else _detailView.ShowStudent(t);
        };
        _topBar.AddClicked += OnAddClicked;
        _topBar.SearchTextChanged += OnSearch;

        var body = new Grid { ColumnDefinitions = new ColumnDefinitions("280, *") };
        var leftBorder = new Border { Background = ThemeManager.WindowBrush, BorderBrush = ThemeManager.BorderBrush, BorderThickness = new Thickness(0, 0, 1, 0), Child = _listView };
        Grid.SetColumn(leftBorder, 0);
        body.Children.Add(leftBorder);

        Grid.SetColumn(_detailView, 1);
        body.Children.Add(_detailView);

        var root = new Grid { RowDefinitions = new RowDefinitions("Auto, *") };
        Grid.SetRow(_topBar, 0);
        root.Children.Add(_topBar);
        Grid.SetRow(body, 1);
        root.Children.Add(body);

        Content = root;
        ReloadStudents();
    }

    private void ReloadStudents()
    {
        try
        {
            _allStudents = StudentRepository.LoadStudents() ?? new List<Student>();
            ApplyFilter();
        }
        catch { }
    }

    private void OnSearch(string keyword)
    {
        _currentSearch = keyword ?? "";
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var filtered = string.IsNullOrWhiteSpace(_currentSearch) 
            ? _allStudents 
            : _allStudents.Where(s => (s.Name?.Contains(_currentSearch) == true) || (s.Id?.Contains(_currentSearch) == true)).ToList();
            
        _listView.SetStudents(filtered);
        if (filtered.Count == 0) _detailView.Clear();
    }

    private void OnAddClicked()
    {
        if (TopLevel.GetTopLevel(this) is Window owner)
            new StudentEditorWindow(null, ReloadStudents).ShowDialog(owner);
    }
}