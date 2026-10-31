using CommunityToolkit.Mvvm.ComponentModel;

namespace PlanPeopleAvalonia.ViewModels;

public partial class SettingItem : ObservableObject
{
    public string Key { get; }
    public string Title { get; }
    public string Description { get; }
    
    // 控制 UI 显示哪个控件
    public string ItemType { get; } // "switch", "text", "number", "dropdown"
    
    // 用于绑定的值
    [ObservableProperty] private object? _value;
    
    // 如果是下拉框，用于存放选项
    public string[]? Options { get; }

    public SettingItem(string key, string title, string desc, object? value, string type = "switch", string[]? options = null)
    {
        Key = key;
        Title = title;
        Description = desc;
        Value = value;
        ItemType = type;
        Options = options;
    }

    public bool IsSwitch => ItemType == "switch";
    public bool IsText => ItemType == "text";
    public bool IsNumber => ItemType == "number";
    public bool IsDropdown => ItemType == "dropdown";
}