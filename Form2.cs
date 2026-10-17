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
/// </summary>
public class Form2 : Form, IMainForm
{
    // =====================================================
    // 数据
    // =====================================================

    /// <summary>日程列表</summary>
    public List<ScheduleItem> Schedules { get; set; } = new();

    // IMainForm 接口要求的空数据（办公模式不使用）
    public List<Course> Courses { get; set; } = new();
    public List<SectionTime> SectionTimes { get; set; } = new();

    /// <summary>值日生（办公模式为空）</summary>
    public DutyRoster Duty { get; set; } = new();

    /// <summary>班委公告（办公模式为空）</summary>
    public List<ClassCommittee> Committees { get; set; } = new();

    // =====================================================
    // 控件
    // =====================================================

    private TitleBarView titleBar = null!;
    private SidebarView sidebar = null!;
    private Panel contentHost = null!;
    private PageOfficeDashboard pageDashboard = null!;
    private PageOfficeSchedule pageSchedule = null!;
    private PageOfficeTodo pageTodo = null!;
    /// <summary>设置页</summary>
private PageSettingsOffice pageSettings = null!;

    // =====================================================
    // 独立浮动窗口
    // =====================================================

    private FloatingIslandForm? islandForm;
    private FloatingTodayForm? todayForm;

    // =====================================================
    // 托盘与提醒
    // =====================================================

    private NotifyIcon? trayIcon;
    private ReminderService? _reminderService;
    private bool reallyExit = false;

    // =====================================================
    // 状态
    // =====================================================

    private AppConfig config;

    [DllImport("user32.dll")]
    private static extern int SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);
    private const int WM_SETREDRAW = 0x000B;

    // =====================================================
    // 构造
    // =====================================================

    public Form2(AppConfig cfg)
    {
        config = cfg ?? ConfigService.Load();

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
        pageSettings = new PageSettingsOffice(this);

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

        // 加载数据
        LoadAll();
        
        // 初始化提醒服务
        _reminderService = new ReminderService();
        _reminderService.SetData(Schedules);

                // 绑定快捷键
        KeyDown += Form2_KeyDown;

        // 刷新全部界面（此时会同步灵动岛）
        RefreshAll();

        // 默认显示首页
        SwitchPage(0);
    }

    // =====================================================
    // 侧边栏 & 页面切换
    // =====================================================

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
        todayForm.SetOfficeData(Schedules);
        todayForm.Show();
    }

    public void ApplyTodaySidebarConfig()
    {
        var overlayCfg = OverlayConfigService.Load();

        if (overlayCfg.SidebarEnabled)
        {
            CreateTodayForm();

            if (todayForm != null && !todayForm.Visible)
                todayForm.Show();

            todayForm?.ApplyConfig();
            todayForm?.SetOfficeData(Schedules);
        }
        else
        {
            todayForm?.Hide();
        }
    }

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

    public void SwitchPagePublic(int idx) => SwitchPage(idx);

    // =====================================================
    // 加载 / 保存 / 刷新
    // =====================================================

    public void LoadAll()
    {
        Schedules = ScheduleRepository.Load();
    }

    public void SaveAll()
    {
        ScheduleRepository.Save(Schedules);
        ConfigService.Save(config);
    }

    /// <summary>
    /// 刷新界面。核心方法，必须调用 RefreshOverlay。
    /// </summary>
    public void RefreshAll()
    {
        pageDashboard.SetData(Schedules);
        pageSchedule.SetData(Schedules);
        pageTodo.SetData(Schedules);

        // 强制同步灵动岛和侧边栏
        RefreshOverlay();

        // 更新提醒服务的数据源
        _reminderService?.SetData(Schedules);
    }

    /// <summary>
    /// 同步浮动窗口数据。
    /// </summary>
    private void RefreshOverlay()
    {
        islandForm?.SetOfficeData(Schedules);
        todayForm?.SetOfficeData(Schedules);
    }

    // =====================================================
    // 日程操作
    // =====================================================

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

    public void DeleteSchedule(ScheduleItem item)
    {
        if (!MessageDialog.Confirm(I18n.T("common.confirm"),
            string.Format(I18n.T("schedule.confirmDelete"), item.Title)))
            return;

        Schedules.RemoveAll(x => x.Id == item.Id);
        SaveAll();
        RefreshAll();
    }

    public void ToggleScheduleDone(ScheduleItem item)
    {
        var target = Schedules.Find(x => x.Id == item.Id);
        if (target == null) return;

        target.Done = !target.Done;
        SaveAll();
        RefreshAll();
    }

    // =====================================================
    // 配置 & 重启
    // =====================================================

    public void ApplyIslandConfig()
    {
        islandForm?.ApplyConfig();
    }

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
    // 主题 & 托盘
    // =====================================================

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

    private void RedrawChildren(Control parent)
    {
        foreach (Control c in parent.Controls)
        {
            c.Invalidate();
            RedrawChildren(c);
        }
    }

        /// <summary>
    /// 全局快捷键处理。
    /// </summary>
    private void Form2_KeyDown(object? sender, KeyEventArgs e)
    {
        // Ctrl + N 新建日程
        if (e.Control && e.KeyCode == Keys.N)
        {
            OpenScheduleEditor(null);
            e.Handled = true;
        }
    }

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
        menu.Items.Add(I18n.T("common.exit"), null, (s, e) => ExitApp());
        trayIcon.ContextMenuStrip = menu;
    }

    private void RestoreFromTray()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void ExitApp()
    {
        reallyExit = true;
        Close();
    }

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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            AppTheme.ThemeChanged -= OnThemeChanged;
            trayIcon?.Dispose();
            islandForm?.Dispose();
            todayForm?.Dispose();
            _reminderService?.Dispose();
        }
        base.Dispose(disposing);
    }
}