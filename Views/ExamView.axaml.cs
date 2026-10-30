using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using PlanPeopleAvalonia.Models;
using PlanPeopleAvalonia.Services;
using PlanPeopleAvalonia.ViewModels;
using PlanPeopleAvalonia.Views.Exam; // ★ 统一引入

namespace PlanPeopleAvalonia.Views;

public partial class ExamView : UserControl
{
    private readonly ExamTopBar _topBar;
    private readonly ExamListView _listView;
    private readonly ExamDetailView _detailView;

    public ExamView()
    {
        _detailView = new ExamDetailView();
        _listView = new ExamListView();
        _topBar = new ExamTopBar();

        _listView.ExamSelected += e => 
        {
            if (e == null) _detailView.Clear();
            else _detailView.ShowExam(e);
        };
        
        _topBar.AddClicked += OnAddClicked;
        _detailView.SetUpdateCallback(ReloadExams);

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
        ReloadExams();
    }

    private void ReloadExams()
    {
        try
        {
            var exams = StudentRepository.LoadExams() ?? new List<ExamRecord>();
            _listView.SetExams(exams);
            _detailView.Clear();
        }
        catch { }
    }

    private void OnAddClicked()
    {
        if (TopLevel.GetTopLevel(this) is Window owner)
            new ExamEditorWindow(ReloadExams).ShowDialog(owner);
    }
}