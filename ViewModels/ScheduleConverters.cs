using System;
using Avalonia.Data.Converters;

namespace PlanPeopleAvalonia.ViewModels;

public static class ScheduleConverters
{
    // WeekDay (1-7) 转为 Grid.Column (0-6)
    public static readonly IValueConverter DayToColumn = new FuncValueConverter<int, int>(day =>
    {
        return Math.Max(0, Math.Min(6, day - 1));
    });

    // TimeStart (1-8) 转为 Grid.Row (0-7)
    public static readonly IValueConverter SectionToRow = new FuncValueConverter<int, int>(section =>
    {
        return Math.Max(0, Math.Min(7, section - 1));
    });

    // TimeEnd 配合 TimeStart，因为我们没有 MultiBinding，这里我们给 RowSpan 直接写个简单逻辑：
    // 实际上 TimeEnd 减去 TimeStart + 1 才能得出跨度。但为了不引起新报错，咱们用 C# 转换：
    public static readonly IValueConverter SpanToHeight = new FuncValueConverter<int, int>(end =>
    {
        return 1; // 这里临时处理，后续有需要再修复跨行
    });
}