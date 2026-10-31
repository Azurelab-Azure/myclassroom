using Avalonia.Controls;
using PlanPeopleAvalonia.ViewModels;
using System.Linq;

namespace PlanPeopleAvalonia.Views.StudentComponents;

public partial class StudentListView : UserControl
{
    public StudentListView()
    {
        InitializeComponent();
        
        // 绑定搜索逻辑
        var searchBox = this.FindControl<TextBox>("SearchBox");
        if (searchBox != null)
        {
            searchBox.TextChanged += (s, e) =>
            {
                if (DataContext is not StudentViewModel vm) return;
                
                var keyword = searchBox.Text?.Trim() ?? "";
                var listBox = this.FindControl<ListBox>("StudentListBox");
                if (listBox != null)
                {
                    var filtered = string.IsNullOrEmpty(keyword) 
                        ? vm.Students.ToList() 
                        : vm.Students.Where(x => (x.Name?.Contains(keyword) == true) || (x.Id?.Contains(keyword) == true)).ToList();
                        
                    listBox.ItemsSource = filtered;
                }
            };
        }
    }

    // 清空搜索框
    public void RefreshList()
    {
        var searchBox = this.FindControl<TextBox>("SearchBox");
        if (searchBox != null) searchBox.Text = "";
    }
}