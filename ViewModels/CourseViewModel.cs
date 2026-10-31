using CommunityToolkit.Mvvm.ComponentModel;
using PlanPeopleAvalonia.Models;
using PlanPeopleAvalonia.Services;

namespace PlanPeopleAvalonia.ViewModels;

public partial class CourseViewModel : ObservableObject
{
    public Course Model { get; }
    private readonly ScheduleViewModel _parent;

    [ObservableProperty] 
    private double _visualLeft = -1;

    [ObservableProperty] 
    private double _visualTop = -1;

    public CourseViewModel(Course model, ScheduleViewModel parent)
    {
        Model = model;
        _parent = parent;
        
        _parent.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ScheduleViewModel.CellWidth) || 
                e.PropertyName == nameof(ScheduleViewModel.CellHeight))
            {
                OnPropertyChanged(nameof(Left));
                OnPropertyChanged(nameof(Top));
                OnPropertyChanged(nameof(Width));
                OnPropertyChanged(nameof(Height));
            }
        };
    }

    // ★ 手动触发 Left/Top 通知，配合界面刷新
    public double Left => VisualLeft >= 0 ? VisualLeft : (Model.WeekDay - 1) * _parent.CellWidth;
    public double Top => VisualTop >= 0 ? VisualTop : (Model.TimeStart - 1) * _parent.CellHeight;
    
    public double Width => _parent.CellWidth;
    public double Height => (Model.TimeEnd - Model.TimeStart + 1) * _parent.CellHeight;

    public string Name => Model.Name;
    public string Teacher => Model.Teacher;
    public string ColorHex => ScheduleConfigStore.GetColorForSubject(Model.Name);
}