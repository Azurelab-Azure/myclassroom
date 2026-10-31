using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using PlanPeopleAvalonia.Models;
using PlanPeopleAvalonia.Services;
using AppConfig = PlanPeopleAvalonia.Services.AppConfig;

namespace PlanPeopleAvalonia.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    public ObservableCollection<string> Categories { get; } = new() { "基础与通用", "外观与主题", "课表专属", "灵动岛与悬浮", "安全与高级" };
    
    [ObservableProperty] private string _selectedCategory = "基础与通用";
    public ObservableCollection<SettingItem> CurrentItems { get; } = new();

    private AppConfig _config;

    public SettingsViewModel()
    {
        _config = ConfigService.Load();
        ShowCategory(_selectedCategory);
    }

    partial void OnSelectedCategoryChanged(string value) => ShowCategory(value);

    private void ShowCategory(string category)
    {
        CurrentItems.Clear();

        switch (category)
        {
            case "基础与通用":
                CurrentItems.Add(new("Language", "界面语言", "选择软件使用的默认语言", _config.Language, "dropdown", new[] { "zh-CN", "en-US" }));
                CurrentItems.Add(new("AutoCheckUpdate", "自动检查更新", "启动时检查是否有新版本", _config.AutoCheckUpdate));
                CurrentItems.Add(new("StartMinimized", "启动时最小化", "启动后直接进入系统托盘", _config.StartMinimized));
                CurrentItems.Add(new("RememberLastPage", "记住上次页面", "重启软件后回到关闭前的页面", _config.RememberLastPage));
                CurrentItems.Add(new("EnableDoubleClick", "启用双击操作", "双击网格可直接新建", _config.EnableDoubleClick));
                CurrentItems.Add(new("AutoSaveOnExit", "退出时自动保存", "关闭软件时自动保存", _config.AutoSaveOnExit));
                CurrentItems.Add(new("AutoSaveInterval", "自动保存间隔(分)", "每隔几分钟自动保存", _config.AutoSaveInterval, "number"));
                break;

            case "外观与主题":
                CurrentItems.Add(new("Theme", "应用主题", "浅色/深色/清爽/护眼", _config.Theme, "dropdown", new[] { "light", "dark", "clear", "fresh" }));
                CurrentItems.Add(new("AccentColor", "强调色", "如 #0078D4", _config.AccentColor, "text"));
                CurrentItems.Add(new("WindowFontSize", "字体大小", "整体界面的缩放比例", _config.WindowFontSize, "number"));
                CurrentItems.Add(new("FontFamily", "字体", "全局字体名称", _config.FontFamily, "text"));
                CurrentItems.Add(new("EnableBlur", "亚克力模糊效果", "开启窗口毛玻璃", _config.EnableBlur));
                CurrentItems.Add(new("ShowBorder", "显示窗口边框", "是否显示黑色边框", _config.ShowBorder));
                CurrentItems.Add(new("CompactMode", "紧凑模式", "减小控件间距", _config.CompactMode));
                CurrentItems.Add(new("ShowAvatar", "显示头像", "在列表中展示用户头像", _config.ShowAvatar));
                break;

            case "课表专属":
                CurrentItems.Add(new("TotalSections", "每天总节数", "课表最大支持节数", _config.TotalSections, "number"));
                CurrentItems.Add(new("DaysPerWeek", "每周上课天数", "通常为 5 或 7", _config.DaysPerWeek, "number"));
                CurrentItems.Add(new("CellWidth", "课表列宽", "单个格子的像素宽", _config.CellWidth, "number"));
                CurrentItems.Add(new("CellHeight", "课表行高", "单个格子的像素高", _config.CellHeight, "number"));
                CurrentItems.Add(new("ShowWeekends", "显示周末", "是否在课表显示周六周日", _config.ShowWeekends));
                CurrentItems.Add(new("ShowNonCurrentWeekCourses", "显示非本周课程", "以灰色展示其他周课程", _config.ShowNonCurrentWeekCourses));
                CurrentItems.Add(new("HighlightCurrentDay", "高亮当前天", "突出显示今天", _config.HighlightCurrentDay));
                CurrentItems.Add(new("DefaultCourseColor", "默认课程色", "新课程的颜色", _config.DefaultCourseColor, "text"));
                CurrentItems.Add(new("EnableConflictCheck", "开启碰撞检测", "拖拽时检测时间冲突", _config.EnableConflictCheck));
                CurrentItems.Add(new("EnableDragDrop", "允许鼠标拖拽", "是否启用拖拽调课", _config.EnableDragDrop));
                break;

            case "灵动岛与悬浮":
                CurrentItems.Add(new("EnableIsland", "启用桌面灵动岛", "显示上课提醒", _config.EnableIsland));
                CurrentItems.Add(new("EnableFloatingWindow", "启用桌面浮窗", "显示今日课表悬浮窗", _config.EnableFloatingWindow));
                CurrentItems.Add(new("IslandTheme", "灵动岛配色", "dark / light", _config.IslandTheme, "dropdown", new[] { "dark", "light" }));
                CurrentItems.Add(new("IslandWidth", "灵动岛宽度", "像素", _config.IslandWidth, "number"));
                CurrentItems.Add(new("IslandHeight", "灵动岛高度", "像素", _config.IslandHeight, "number"));
                CurrentItems.Add(new("IslandShowWeather", "显示天气", "在岛上展示天气", _config.IslandShowWeather));
                CurrentItems.Add(new("IslandShowTime", "显示倒计时", "展示课程倒计时", _config.IslandShowTime));
                CurrentItems.Add(new("FloatingTopMost", "浮窗置顶", "一直显示在最上层", _config.FloatingTopMost));
                CurrentItems.Add(new("FloatingOpacity", "浮窗透明度", "0.0 - 1.0", _config.FloatingOpacity, "number"));
                break;

            case "安全与高级":
                CurrentItems.Add(new("RequirePasswordOnCourseEdit", "编辑需密码", "修改课表需要验证", _config.RequirePasswordOnCourseEdit));
                CurrentItems.Add(new("EnableDataEncryption", "数据加密", "本地 JSON 加密存储", _config.EnableDataEncryption));
                CurrentItems.Add(new("DataBackupPath", "备份路径", "自动备份的文件夹", _config.DataBackupPath, "text"));
                CurrentItems.Add(new("AutoBackupOnStartup", "启动自动备份", "每次打开自动生成备份", _config.AutoBackupOnStartup));
                CurrentItems.Add(new("DebugMode", "调试模式", "在控制台输出日志", _config.DebugMode));
                CurrentItems.Add(new("EnableTelemetry", "发送匿名数据", "帮助改进软件", _config.EnableTelemetry));
                break;
        }
    }

    public void SaveSettings()
    {
        // 把 UI 修改的值写回配置模型
        foreach (var item in CurrentItems)
        {
            var prop = typeof(AppConfig).GetProperty(item.Key);
            if (prop != null && item.Value != null)
            {
                try
                {
                    // 处理 NumericUpDown (decimal -> int/double)
                    object finalVal = item.Value;
                    if (prop.PropertyType == typeof(int) && finalVal is decimal d) finalVal = (int)d;
                    if (prop.PropertyType == typeof(double) && finalVal is decimal d2) finalVal = (double)d2;
                    
                    prop.SetValue(_config, finalVal);
                }
                catch { }
            }
        }
        ConfigService.Save(_config);
    }
}