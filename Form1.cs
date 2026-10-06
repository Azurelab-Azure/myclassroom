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

public class Form1 : Form
{
    // =====================================================
    // 数据
    // =====================================================
    public List<Course> Courses { get; set; } = new();
    public List<SectionTime> SectionTimes { get; set; } = new();
    public List<Teacher> Teachers { get; set; } = new();
    public List<Student> Students { get; set; } = new();
    public DutyRoster Duty { get; set; } = new();
    public List<ExamRecord> Exams { get; set; } = new();
    public SeatLayout SeatLayout { get; set; } = new();

    // =====================================================
    // 控件
    // =====================================================
    private TitleBarView titleBar = null!;
    private SidebarView sidebar = null!;
    private Panel contentHost = null!;
    private PageDashboard pageDashboard = null!;
    private PageHome pageHome = null!;
    private PageTeachers pageTeachers = null!;
    private PageStudents pageStudents = null!;
    private PageScores pageScores = null!;
    private PageSettings pageSettings = null!;

    // =====================================================
    // 独立浮动窗口
    // =====================================================
    private FloatingIslandForm? islandForm;
    private FloatingTodayForm? todayForm;

    // =====================================================
    // 托盘
    // =====================================================
    private NotifyIcon? trayIcon;
    private bool reallyExit = false;

    // =====================================================
    // 状态
    // =====================================================
    private AppConfig config;
    private int currentWeek = 1;

    // =====================================================
    // WM_SETREDRAW
    // =====================================================
    [DllImport("user32.dll")]
    private static extern int SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

    private const int WM_SETREDRAW = 0x000B;

    // =====================================================
    // 构造
    // =====================================================
    public Form1(AppConfig cfg)
    {
        config = cfg ?? ConfigService.Load();

        // ---------- 窗口 ----------
        Text = AppInfo.AppName;
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

        // ---------- 标题栏 ----------
        titleBar = new TitleBarView(this);
        titleBar.Dock = DockStyle.Top;

        // ---------- 侧边栏 ----------
        sidebar = new SidebarView();
        sidebar.ItemClicked += idx => SwitchPage(idx);

        // ---------- 内容区 ----------
        contentHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Colors.WindowBg,
        };
        contentHost.SuspendLayout();

        pageDashboard = new PageDashboard(this);
        pageHome = new PageHome();
        pageTeachers = new PageTeachers(this);
        pageStudents = new PageStudents(this);
        pageScores = new PageScores(this);
        pageSettings = new PageSettings(this);

        pageDashboard.Dock = DockStyle.Fill;
        pageHome.Dock = DockStyle.Fill;
        pageTeachers.Dock = DockStyle.Fill;
        pageStudents.Dock = DockStyle.Fill;
        pageScores.Dock = DockStyle.Fill;
        pageSettings.Dock = DockStyle.Fill;

        pageDashboard.Visible = false;
        pageHome.Visible = false;
        pageTeachers.Visible = false;
        pageStudents.Visible = false;
        pageScores.Visible = false;
        pageSettings.Visible = false;

        // 加的顺序：后加的在前
        contentHost.Controls.Add(pageSettings);
        contentHost.Controls.Add(pageScores);
        contentHost.Controls.Add(pageStudents);
        contentHost.Controls.Add(pageTeachers);
        contentHost.Controls.Add(pageHome);
        contentHost.Controls.Add(pageDashboard);
        contentHost.ResumeLayout(false);
        contentHost.PerformLayout();

        // ---------- 组装 ----------
        SuspendLayout();
        Controls.Add(contentHost);
        Controls.Add(sidebar);
        Controls.Add(titleBar);
        ResumeLayout(false);
        PerformLayout();

        // ---------- 灵动岛 ----------
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

        // ---------- 今日侧边栏 ----------
        if (config.TodaySidebarVisible)
            CreateTodayForm();

        // ---------- 托盘 ----------
        BuildTrayIcon();

        // ---------- 事件 ----------
        Resize += (s, e) =>
        {
            if (WindowState == FormWindowState.Minimized) Hide();
        };
        FormClosing += Form1_FormClosing;
        AppTheme.ThemeChanged += OnThemeChanged;

        WireUpHomePage();

        // ---------- 加载 ----------
        LoadAll();
        RefreshAll();

        // 默认显示首页
        SwitchPage(0);
    }

    // =====================================================
    // 今日侧边栏
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

        todayForm.SetData(Courses, SectionTimes, Duty, currentWeek);
        todayForm.Show();
    }

    public void ApplyTodaySidebarConfig()
    {
        var cfg = ConfigService.Load();

        if (cfg.TodaySidebarVisible)
        {
            CreateTodayForm();

            if (todayForm != null && !todayForm.Visible)
                todayForm.Show();

            todayForm?.SetData(Courses, SectionTimes, Duty, currentWeek);
            todayForm?.RefreshPosition();
        }
        else
        {
            todayForm?.Hide();
        }
    }

    // =====================================================
    // 页面切换（6 路）
    // =====================================================
    private void SwitchPage(int idx)
    {
        pageDashboard.Visible = (idx == 0);
        pageHome.Visible = (idx == 1);
        pageTeachers.Visible = (idx == 2);
        pageStudents.Visible = (idx == 3);
        pageScores.Visible = (idx == 4);
        pageSettings.Visible = (idx == 5);

        if (idx == 0) pageDashboard.BringToFront();
        else if (idx == 1) pageHome.BringToFront();
        else if (idx == 2) pageTeachers.BringToFront();
        else if (idx == 3) pageStudents.BringToFront();
        else if (idx == 4) pageScores.BringToFront();
        else pageSettings.BringToFront();

        sidebar.SelectedIndex = idx;
    }

    /// <summary>供 PageDashboard 调用。</summary>
    public void SwitchPagePublic(int idx) => SwitchPage(idx);

    // =====================================================
    // 主页事件
    // =====================================================
    private void WireUpHomePage()
    {
        pageHome.Toolbar.PrevWeekClicked += () =>
        {
            if (currentWeek > 1) { currentWeek--; RefreshAll(); }
        };
        pageHome.Toolbar.NextWeekClicked += () =>
        {
            currentWeek++;
            RefreshAll();
        };

        pageHome.ScheduleGrid.CellDoubleClicked += (day, section) => OnCellDoubleClicked(day, section);
        pageHome.ScheduleGrid.CourseEditRequested += c => ShowCourseEditor(c, c.WeekDay, c.TimeStart);
        pageHome.ScheduleGrid.CourseDeleteRequested += c => DeleteCourse(c);
        pageHome.ScheduleGrid.CourseCopyRequested += c =>
        {
            MessageDialog.ShowInfo(I18n.T("common.info"),
                string.Format(I18n.T("msg.courseCopied"), c.Name));
        };

        pageHome.ScheduleGrid.CourseSwapped += (courseA, courseB, newDay, newStart, newEnd) =>
        {
            SwapCourses(courseA, courseB, newDay, newStart, newEnd);
        };
    }

    // =====================================================
    // 加载 / 保存
    // =====================================================
    public void LoadAll()
    {
        Courses = CourseRepository.LoadCourses();
        SectionTimes = CourseRepository.LoadSections();
        Teachers = CourseRepository.LoadTeachers();
        Students = StudentRepository.LoadStudents();
        Duty = StudentRepository.LoadDuty();
        Exams = StudentRepository.LoadExams();
        SeatLayout = StudentRepository.LoadSeatLayout();
    }

    public void SaveAll()
    {
        CourseRepository.SaveCourses(Courses);
        CourseRepository.SaveSections(SectionTimes);
        CourseRepository.SaveTeachers(Teachers);
        StudentRepository.SaveStudents(Students);
        StudentRepository.SaveDuty(Duty);
        StudentRepository.SaveExams(Exams);
        StudentRepository.SaveSeatLayout(SeatLayout);
        ConfigService.Save(config);
    }

    // =====================================================
    // 刷新
    // =====================================================
    public void RefreshAll()
    {
        // 首页
        pageDashboard.SetData(Courses, SectionTimes, Duty, currentWeek);

        // 主页
        pageHome.Toolbar.SetWeek(currentWeek);
        pageHome.ScheduleGrid.SetData(SectionTimes, Courses, currentWeek);

        // 教师页
        pageTeachers.SetData(Teachers);

        // 学生页
        pageStudents.SetData(Students);

        // 成绩页
        pageScores.SetData(Exams);

        // 灵动岛
        islandForm?.SetData(SectionTimes, Courses, currentWeek);

        // 今日侧边栏
        todayForm?.SetData(Courses, SectionTimes, Duty, currentWeek);
    }

    // =====================================================
    // 应用配置
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

    // =====================================================
    // 重启
    // =====================================================
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
    // 课程编辑
    // =====================================================
    private void OnCellDoubleClicked(int day, int section)
    {
        var existing = FindCourseAt(day, section);
        ShowCourseEditor(existing, day, section);
    }

    private Course? FindCourseAt(int day, int section)
    {
        foreach (var c in Courses)
        {
            if (c.WeekDay == day
                && c.Weeks != null && c.Weeks.Contains(currentWeek)
                && section >= c.TimeStart && section <= c.TimeEnd)
                return c;
        }
        return null;
    }

    private void ShowCourseEditor(Course? existing, int day, int section)
    {
        using var dlg = new CourseEditorDialog(existing, day, section, Teachers, currentWeek);
        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.ResultCourse != null)
        {
            if (existing == null) Courses.Add(dlg.ResultCourse);
            SaveAll();
            RefreshAll();
        }
    }

    private void DeleteCourse(Course c)
    {
        if (!MessageDialog.Confirm(I18n.T("common.confirm"),
            string.Format(I18n.T("msg.confirmDeleteCourse"), c.Name)))
            return;
        Courses.Remove(c);
        SaveAll();
        RefreshAll();
    }

    private void SwapCourses(Course courseA, Course? courseB, int newDay, int newStart, int newEnd)
    {
        int oldDayA = courseA.WeekDay;
        int oldStartA = courseA.TimeStart;
        int oldEndA = courseA.TimeEnd;

        courseA.WeekDay = newDay;
        courseA.TimeStart = newStart;
        courseA.TimeEnd = newEnd;

        if (courseB != null)
        {
            courseB.WeekDay = oldDayA;
            courseB.TimeStart = oldStartA;
            courseB.TimeEnd = oldEndA;
        }

        SaveAll();
        RefreshAll();

        MessageDialog.ShowInfo(
            I18n.T("common.success"),
            courseB != null ? I18n.T("swap.done") : I18n.T("swap.moved"));
    }

    // =====================================================
    // 主题
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

    // =====================================================
    // 托盘
    // =====================================================
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
        menu.Items.Add(I18n.T("nav.home"), null, (s, e) => { RestoreFromTray(); SwitchPage(1); });
        menu.Items.Add(I18n.T("nav.teachers"), null, (s, e) => { RestoreFromTray(); SwitchPage(2); });
        menu.Items.Add(I18n.T("nav.students"), null, (s, e) => { RestoreFromTray(); SwitchPage(3); });
        menu.Items.Add(I18n.T("nav.scores"), null, (s, e) => { RestoreFromTray(); SwitchPage(4); });
        menu.Items.Add(I18n.T("nav.settings"), null, (s, e) => { RestoreFromTray(); SwitchPage(5); });
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

    private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
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
        }
        base.Dispose(disposing);
    }
}