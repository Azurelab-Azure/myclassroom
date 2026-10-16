using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using CourseApp.Dialogs;
using CourseApp.Localization;
using CourseApp.Models;
using CourseApp.Services;
using CourseApp.Theme;
using CourseApp.Views;

namespace CourseApp;

/// <summary>
/// 办公模式主窗口。
/// 页面：0 首页 / 1 日程 / 2 待办 / 6 设置。
/// 复用标题栏、侧边栏、灵动岛、今日侧边栏、托盘。
/// </summary>
public class Form2 : Form, IMainForm
{
    // =====================================================
    // 数据
    // =====================================================

    /// <summary>日程列表</summary>
    public List<ScheduleItem> Schedules { get; set; } = new();

    /// <summary>课程（灵动岛 / 侧边栏用，办公模式为空）</summary>
    public List<Course> Courses { get; set; } = new();

    /// <summary>节次（灵动岛 / 侧边栏用，办公模式为空）</summary>
    public List<SectionTime> SectionTimes { get; set; } = new();

    /// <summary>值日生（办公模式为空）</summary>
    public DutyRoster Duty { get; set; } = new();

    /// <summary>班委公告（办公模式为空）</summary>
    public List<ClassCommittee> Committees { get; set; } = new();

    // =====================================================
    // 控件
    // =====================================================

    /// <summary>标题栏</summary>
    private TitleBarView titleBar = null!;

    /// <summary>侧边栏</summary>
    private SidebarView sidebar = null!;

    /// <summary>内容宿主</summary>
    private Panel contentHost = null!;

    /// <summary>办公首页</summary>
    private PageOfficeDashboard pageDashboard = null!;

    /// <summary>办公日程</summary>
    private PageOfficeSchedule pageSchedule = null!;

    /// <summary>办公待办</summary>
    private PageOfficeTodo pageTodo = null!;

    /// <summary>设置页</summary>
    private PageSettings pageSettings = null!;

    // =====================================================
    // 独立浮动窗口
    // =====================================================

    /// <summary>灵动岛窗口</summary>
    private FloatingIslandForm? islandForm;

    /// <summary>今日侧边栏窗口</summary>
    private FloatingTodayForm? todayForm;

    // =====================================================
    // 托盘
    // =====================================================

    /// <summary>托盘图标</summary>
    private NotifyIcon? trayIcon;

    /// <summary>是否真正退出</summary>
    private bool reallyExit = false;

    // =====================================================
    // 状态
    // =====================================================

    /// <summary>全局配置</summary>
    private AppConfig config;

    // =====================================================
    // WM_SETREDRAW
    // =====================================================

    /// <summary>发送窗口消息</summary>
    [DllImport("user32.dll")]
    private static extern int SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

    /// <summary>WM_SETREDRAW 消息常量</summary>
    private const int WM_SETREDRAW = 0x000B;

    // =====================================================
    // 构造
    // =====================================================

    /// <summary>
    /// 构造办公模式主窗口。
    /// </summary>
    /// <param name="cfg">全局配置</param>
    public Form2(AppConfig cfg)
    {
        config = cfg ?? ConfigService.Load();

        // 窗口基础属性
        Text = AppInfo.AppName + " · " + I18n.T("office.title");
        Width = 1280;
        Height = 800;
        MinimumSize = new Size(1000, 600);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.None;
        BackColor = AppTheme.Colors.WindowBg;
        DoubleBuffered = true;
        KeyPreview = true;

        SetStyle(ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.AllPaintingInWmPaint, true);
        UpdateStyles();

        // 标题栏
        titleBar = new TitleBarView(this);
        titleBar.Dock = DockStyle.Top;

        // 侧边栏
        sidebar = new SidebarView();
        sidebar.ItemClicked += idx => SwitchPage(idx);

        // 内容区
        contentHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Colors.WindowBg,
        };
        contentHost.SuspendLayout();

        pageDashboard = new PageOfficeDashboard(this);
        pageSchedule = new PageOfficeSchedule(this);
        pageTodo = new PageOfficeTodo(this);
        pageSettings = new PageSettings(this);

        pageDashboard.Dock = DockStyle.Fill;
        pageSchedule.Dock = DockStyle.Fill;
        pageTodo.Dock = DockStyle.Fill;
        pageSettings.Dock = DockStyle.Fill;

        pageDashboard.Visible = false;
        pageSchedule.Visible = false;
        pageTodo.Visible = false;
        pageSettings.Visible = false;

        contentHost.Controls.Add(pageSettings);
        contentHost.Controls.Add(pageTodo);
        contentHost.Controls.Add(pageSchedule);
        contentHost.Controls.Add(pageDashboard);
        contentHost.ResumeLayout(false);
        contentHost.PerformLayout();

        // 组装
        SuspendLayout();
        Controls.Add(contentHost);
        Controls.Add(sidebar);
        Controls.Add(titleBar);
        ResumeLayout(false);
        PerformLayout();

        // 灵动岛
        islandForm = new FloatingIslandForm();
        islandForm.IslandClicked += () =>
        {
            if (WindowState == FormWindowState.Minimized)
                WindowState = FormWindowState.Normal;
            Show();
            Activate();
        };
        islandForm.ExitRequested += () => ExitApp();
        islandForm.Show();

        // 今日侧边栏
        if (config.TodaySidebarVisible)
            CreateTodayForm();

        // 托盘
        BuildTrayIcon();

        // 事件
        Resize += (s, e) =>
        {
            if (WindowState == FormWindowState.Minimized) Hide();
        };
        FormClosing += Form2_FormClosing;
        AppTheme.ThemeChanged += OnThemeChanged;

        // 加载
        LoadAll();
        RefreshAll();

        // 默认显示首页
        SwitchPage(0);
    }

    // =====================================================
    // 今日侧边栏
    // =====================================================

    /// <summary>
    /// 创建今日侧边栏窗口。
    /// </summary>
    private void CreateTodayForm()
    {
        if (todayForm != null && !todayForm.IsDisposed) return;

        todayForm = new FloatingTodayForm();
        todayForm.OpenMainRequested += () =>
        {
            if (WindowState == FormWindowState.Minimized)
                WindowState = FormWindowState.Normal;
            Show();
            Activate();
        };
        todayForm.ExitRequested += () => ExitApp();

        todayForm.ApplyConfig();
        todayForm.SetData(Courses, SectionTimes, Duty, 1, Committees);
        todayForm.Show();
    }

    /// <summary>
    /// 应用今日侧边栏配置。
    /// </summary>
    public void ApplyTodaySidebarConfig()
    {
        var overlayCfg = OverlayConfigService.Load();

        if (overlayCfg.SidebarEnabled)
        {
            CreateTodayForm();

            if (todayForm != null && !todayForm.Visible)
                todayForm.Show();

            todayForm?.ApplyConfig();
            todayForm?.SetData(Courses, SectionTimes, Duty, 1, Committees);
        }
        else
        {
            todayForm?.Hide();
        }
    }

    // =====================================================
    // 页面切换
    // =====================================================

    /// <summary>
    /// 切换页面。
    /// </summary>
    /// <param name="idx">0 首页 / 1 日程 / 2 待办 / 6 设置</param>
    private void SwitchPage(int idx)
    {
        pageDashboard.Visible = (idx == 0);
        pageSchedule.Visible = (idx == 1);
        pageTodo.Visible = (idx == 2);
        pageSettings.Visible = (idx == 6);

        if (idx == 0) pageDashboard.BringToFront();
        else if (idx == 1) pageSchedule.BringToFront();
        else if (idx == 2) pageTodo.BringToFront();
        else pageSettings.BringToFront();

        sidebar.SelectedIndex = idx;
    }

    /// <summary>
    /// 供页面调用切换。
    /// </summary>
    public void SwitchPagePublic(int idx) => SwitchPage(idx);

    // =====================================================
    // 加载 / 保存
    // =====================================================

    /// <summary>
    /// 加载所有数据。
    /// </summary>
    public void LoadAll()
    {
        Schedules = ScheduleRepository.Load();
    }

    /// <summary>
    /// 保存所有数据。
    /// </summary>
    public void SaveAll()
    {
        ScheduleRepository.Save(Schedules);
        ConfigService.Save(config);
    }

    // =====================================================
    // 刷新
    // =====================================================

    /// <summary>
    /// 刷新所有页面和浮动窗口。
    /// </summary>
    public void RefreshAll()
    {
        pageDashboard.SetData(Schedules);
        pageSchedule.SetData(Schedules);
        pageTodo.SetData(Schedules);

        islandForm?.SetData(SectionTimes, Courses, 1, Duty, Committees);
        todayForm?.SetData(Courses, SectionTimes, Duty, 1, Committees);
    }

    // =====================================================
    // 应用配置
    // =====================================================

    /// <summary>
    /// 应用灵动岛配置。
    /// </summary>
    public void ApplyIslandConfig()
    {
        islandForm?.ApplyConfig();
    }

    /// <summary>
    /// 应用侧边栏配置。
    /// </summary>
    public void ApplySidebarConfig()
    {
        SuspendLayout();
        try
        {
            sidebar.ApplyConfig();
        }
        finally
        {
            ResumeLayout(true);
        }

        Invalidate(true);
    }

    // =====================================================
    // 日程编辑
    // =====================================================

    /// <summary>
    /// 打开日程编辑弹窗。
    /// </summary>
    /// <param name="existing">正在编辑的日程，为 null 表示新建</param>
    public void OpenScheduleEditor(ScheduleItem? existing)
    {
        using var dlg = new ScheduleItemEditorDialog(existing);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        if (dlg.Result == null) return;

        if (existing == null)
            Schedules.Add(dlg.Result);

        SaveAll();
        RefreshAll();
    }

    /// <summary>
    /// 删除日程。
    /// </summary>
    public void DeleteSchedule(ScheduleItem item)
    {
        if (!MessageDialog.Confirm(I18n.T("common.confirm"),
            string.Format(I18n.T("schedule.confirmDelete"), item.Title)))
            return;

        Schedules.RemoveAll(x => x.Id == item.Id);
        SaveAll();
        RefreshAll();
    }

    /// <summary>
    /// 切换日程完成状态。
    /// </summary>
    public void ToggleScheduleDone(ScheduleItem item)
    {
        var target = Schedules.Find(x => x.Id == item.Id);
        if (target == null) return;

        target.Done = !target.Done;
        SaveAll();
        RefreshAll();
    }

    // =====================================================
    // 重启
    // =====================================================

    /// <summary>
    /// 重启软件。
    /// </summary>
    public void Restart()
    {
        try { islandForm?.Close(); } catch { }
        try { todayForm?.Close(); } catch { }

        try
        {
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
                trayIcon = null;
            }
        }
        catch { }

        try
        {
            var exe = Application.ExecutablePath;
            if (System.IO.File.Exists(exe))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exe,
                    UseShellExecute = true,
                    WorkingDirectory = System.IO.Path.GetDirectoryName(exe) ?? "",
                });
            }
        }
        catch { }

        reallyExit = true;

        var t = new System.Windows.Forms.Timer { Interval = 500 };
        t.Tick += (s, e) =>
        {
            t.Stop();
            Close();
        };
        t.Start();
    }

    // =====================================================
    // 主题
    // =====================================================

    /// <summary>
    /// 主题变化时重绘整个窗口。
    /// </summary>
    private void OnThemeChanged()
    {
        if (!IsHandleCreated) return;

        SendMessage(this.Handle, WM_SETREDRAW, 0, 0);

        SuspendLayout();
        try
        {
            BackColor = AppTheme.Colors.WindowBg;
            contentHost.BackColor = AppTheme.Colors.WindowBg;

            RedrawChildren(this);
        }
        finally
        {
            ResumeLayout(false);
            SendMessage(this.Handle, WM_SETREDRAW, 1, 0);
            Refresh();
        }
    }

    /// <summary>
    /// 递归重绘子控件。
    /// </summary>
    private void RedrawChildren(Control parent)
    {
        foreach (Control c in parent.Controls)
        {
            c.Invalidate();
            RedrawChildren(c);
        }
    }

    // =====================================================
    // 托盘
    // =====================================================

    /// <summary>
    /// 构建托盘图标和右键菜单。
    /// </summary>
    private void BuildTrayIcon()
    {
        trayIcon = new NotifyIcon { Text = AppInfo.AppName, Visible = true };
        try
        {
            if (System.IO.File.Exists(AppPaths.AppIconPath))
                trayIcon.Icon = new Icon(AppPaths.AppIconPath);
            else
                trayIcon.Icon = SystemIcons.Application;
        }
        catch { trayIcon.Icon = SystemIcons.Application; }

        trayIcon.DoubleClick += (s, e) => RestoreFromTray();

        var menu = new ContextMenuStrip();
        menu.Items.Add(I18n.T("tray.open"), null, (s, e) => RestoreFromTray());
        menu.Items.Add(I18n.T("nav.dashboard"), null, (s, e) => { RestoreFromTray(); SwitchPage(0); });
        menu.Items.Add(I18n.T("nav.schedule"), null, (s, e) => { RestoreFromTray(); SwitchPage(1); });
        menu.Items.Add(I18n.T("nav.todo"), null, (s, e) => { RestoreFromTray(); SwitchPage(2); });
        menu.Items.Add(I18n.T("nav.settings"), null, (s, e) => { RestoreFromTray(); SwitchPage(6); });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(I18n.T("schedule.new"), null, (s, e) =>
        {
            RestoreFromTray();
            SwitchPage(1);
            OpenScheduleEditor(null);
        });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(I18n.T("menu.about"), null, (s, e) =>
        {
            using var dlg = new AboutDialog();
            dlg.ShowDialog(this);
        });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(I18n.T("common.exit"), null, (s, e) => ExitApp());
        trayIcon.ContextMenuStrip = menu;
    }

    /// <summary>
    /// 从托盘恢复主窗口。
    /// </summary>
    private void RestoreFromTray()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    /// <summary>
    /// 退出软件。
    /// </summary>
    private void ExitApp()
    {
        reallyExit = true;
        Close();
    }

    /// <summary>
    /// 窗体关闭事件。
    /// </summary>
    private void Form2_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!reallyExit)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        SaveAll();

        try { islandForm?.Close(); } catch { }
        try { todayForm?.Close(); } catch { }

        if (trayIcon != null)
        {
            trayIcon.Visible = false;
            trayIcon.Dispose();
        }
    }

    /// <summary>
    /// 释放资源。
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            AppTheme.ThemeChanged -= OnThemeChanged;
            trayIcon?.Dispose();
            islandForm?.Dispose();
            todayForm?.Dispose();
        }
        base.Dispose(disposing);
    }
}