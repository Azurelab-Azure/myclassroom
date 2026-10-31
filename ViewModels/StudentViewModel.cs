using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using PlanPeopleAvalonia.Models;
using PlanPeopleAvalonia.Services;

namespace PlanPeopleAvalonia.ViewModels;

public partial class StudentViewModel : ViewModelBase
{
    public ObservableCollection<Student> Students { get; } = new();

    [ObservableProperty] 
    private Student? _selectedStudent;

    public StudentViewModel()
    {
        LoadData();
    }

    public void LoadData()
    {
        try
        {
            var list = StudentRepository.LoadStudents() ?? new List<Student>();
            
            if (list.Count == 0)
            {
                list = new List<Student>
                {
                    new() { Id = "2023001", Name = "张三", Duty = "班长" },
                    new() { Id = "2023002", Name = "李四", Duty = "学习委员" }
                };
                StudentRepository.SaveStudents(list);
            }

            var currentSelectedId = SelectedStudent?.Id;

            Students.Clear();
            foreach (var s in list) Students.Add(s);
            
            if (!string.IsNullOrEmpty(currentSelectedId))
                SelectedStudent = Students.FirstOrDefault(x => x.Id == currentSelectedId);
            else
                SelectedStudent = Students.FirstOrDefault();
        }
        catch { }
    }

    public void DeleteStudent(Student student)
    {
        if (student == null) return;
        Students.Remove(student);
        StudentRepository.SaveStudents(Students.ToList());
        SelectedStudent = Students.FirstOrDefault();
    }

    public void AddStudent(Student newStudent)
    {
        Students.Add(newStudent);
        StudentRepository.SaveStudents(Students.ToList());
        SelectedStudent = newStudent;
    }

    public void UpdateStudent(Student updated)
    {
        // 找到列表里真正存在的老对象
        var existing = Students.FirstOrDefault(x => x.Id == updated.Id);
        if (existing != null)
        {
            // 1. 同步属性数据
            existing.Name = updated.Name;
            existing.Duty = updated.Duty;
            existing.Gender = updated.Gender;
            existing.Seat = updated.Seat;
            existing.Level = updated.Level;
            existing.HealthStatus = updated.HealthStatus;
            existing.ParentPhone = updated.ParentPhone;
            existing.Address = updated.Address;
            existing.Info = updated.Info;
            existing.PhotoPath = updated.PhotoPath;
            
            // 2. ★ 核心修复：强制通知 UI 刷新
            // 因为 Student 是普通类，修改属性不会通知 UI，必须让列表重新拉取数据
            var index = Students.IndexOf(existing);
            if (index >= 0)
            {
                Students[index] = null!; 
                Students[index] = existing;
            }

            // 3. 同步给右侧详情卡片
            SelectedStudent = existing;
        }
        
        // 4. 写入本地 JSON
        StudentRepository.SaveStudents(Students.ToList());
    }
}