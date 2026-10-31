using CommunityToolkit.Mvvm.ComponentModel;

namespace PlanPeopleAvalonia.ViewModels;

/// <summary>
/// 背景网格单元（画线用的空白格）。
/// 因为课表尺寸固定，所以直接在构造函数里算死位置。
/// </summary>
public partial class CellViewModel : ObservableObject
{
    // 行号（0 开始）和列号（0 开始）
    public int Row { get; }
    public int Col { get; }

    // 用于拖拽悬停高亮（预留）
    [ObservableProperty] private bool _isHovered;

    // 绝对定位坐标和宽高（直接绑定给 Canvas 和 Border）
    public double Left { get; }
    public double Top { get; }
    public double Width { get; }
    public double Height { get; }

    public CellViewModel(int row, int col, ScheduleViewModel parent)
    {
        Row = row;
        Col = col;
        
        // 直接根据父级的固定像素尺寸计算位置
        Left = col * parent.CellWidth;
        Top = row * parent.CellHeight;
        Width = parent.CellWidth;
        Height = parent.CellHeight;
    }
}