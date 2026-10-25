using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PlanPeopleAvalonia.ViewModels;

public partial class StudentBadge : ObservableObject
{
    [ObservableProperty] private string _name = "";
    
    // ★ 纯矢量 SVG Path 数据（Material Design 标准格式）
    [ObservableProperty] private string _iconData = ""; 
    
    [ObservableProperty] private string _color = "#FFD700";

    public static List<StudentBadge> GetDefaultBadges() => new()
    {
        
    };
}