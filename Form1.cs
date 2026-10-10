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
/// 主窗口。
/// 负责：页面切换、数据加载保存、灵动岛、今日侧边栏、托盘、主题切换。
/// 页面：0 首页 / 1 主页 / 2 教师 / 3 学生 / 4 评价 / 5 成绩 / 6 设置。
/// </summary>
public class Form1 : Form
{
    // =====================================================
    // 数据
    // =====================================================

    /// <summary>课程列表</summary>
    public List<Course> Courses { get; set; } = new();

    /// <summary>节次时间列表</summary>
    public List<SectionTime> SectionTimes { get; set; } = new();

    /// <summary>教师列表</summary>
    public List<Teacher> Teachers { get; set; } = new();

    /// <summary>学生列表</summary>
    public List<Student> Students { get; set; } = new();

    /// <summary>值日生表</summary>
    public DutyRoster Duty { get; set; } = new();

    /// <summary>考试记录</summary>
    public List<ExamRecord> Exams { get; set; } = new();

    /// <summary>座位表布局</summary>
    public SeatLayout SeatLayout { get; set; } = new();

    /// <summary>班委公告</summary>
    public List<ClassCommittee> Committees { get; set; } = new();

    /// <summary>课代表列表</summary>
    public List<CourseRepresentative> CourseRepresentatives { get; set; } = new();

    /// <summary>积分记录</summary>
    public List<ScoreRecord> ScoreRecords { get; set; } = new();

    /// <summary>积分分类</summary>
    public List<ScoreCategory> ScoreCategories { get; set; } = new();

    // =====================================================
    // 控件
    // =====================================================

    /// <summary>标题栏</summary>
    private TitleBarView titleBar = null!;

    /// <summary>左侧导航栏</summary>
    private SidebarView sidebar = null!;

    /// <summary>内容宿主</summary>
    private Panel contentHost = null!;

    /// <summary>首页仪表盘</summary>
    private PageDashboard pageDashboard = null!;

    /// <summary>主页课表</summary>
    private PageHome pageHome = null!;

    /// <summary>教师页</summary>
    private PageTeachers pageTeachers = null!;

    /// <summary>学生页</summary>
    private PageStudents pageStudents = null!;

    /// <summary>评价页</summary>
    private PagePoints pagePoints = null!;

    /// <summary>成绩页</summary>
    private PageExam pageExam = null!;

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

    /// <summary>是否真正退出（区分最小化到托盘）</summary>
    private bool reallyExit = false;

    // =====================================================
    // 状态
    // =====================================================

    /// <summary>全局配置</summary>
    private AppConfig config;

    /// <summary>当前周次</summary>
    private int currentWeek = 1;

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
    /// 构造主窗口。
    /// </summary>
    /// <param name="cfg">全局配置</param>
    public Form1(AppConfig cfg)
    {
        config = cfg ?? ConfigService.Load();

        // 窗口基础属性
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

        pageDashboard = new PageDashboard(this);
        pageHome = new PageHome();
        pageTeachers = new PageTeachers(this);
        pageStudents = new PageStudents(this);
        pagePoints = new PagePoints(this);
        pageExam = new PageExam(this);
        pageSettings = new PageSettings(this);

        pageDashboard.Dock = DockStyle.Fill;
        pageHome.Dock = DockStyle.Fill;
        pageTeachers.Dock = DockStyle.Fill;
        pageStudents.Dock = DockStyle.Fill;
        pagePoints.Dock = DockStyle.Fill;
        pageExam.Dock = DockStyle.Fill;
        pageSettings.Dock = DockStyle.Fill;

        pageDashboard.Visible = false;
        pageHome.Visible = false;
        pageTeachers.Visible = false;
        pageStudents.Visible = false;
        pagePoints.Visible = false;
        pageExam.Visible = false;
        pageSettings.Visible = false;

        // 后加的在前
        contentHost.Controls.Add(pageSettings);
        contentHost.Controls.Add(pageExam);
        contentHost.Controls.Add(pagePoints);
        contentHost.Controls.Add(pageStudents);
        contentHost.Controls.Add(pageTeachers);
        contentHost.Controls.Add(pageHome);
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
        FormClosing += Form1_FormClosing;
        AppTheme.ThemeChanged += OnThemeChanged;

        WireUpHomePage();

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
    /// 创建今日侧边栏窗口（如果尚未创建）。
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
    todayForm.SetData(Courses, SectionTimes, Duty, currentWeek, Committees);
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
        todayForm?.SetData(Courses, SectionTimes, Duty, currentWeek, Committees);
    }
    else
    {
        todayForm?.Hide();
    }
}

    // =====================================================
    // 页面切换（7 路）
    // =====================================================

    /// <summary>
    /// 切换页面。
    /// </summary>
    /// <param name="idx">0 首页 / 1 主页 / 2 教师 / 3 学生 / 4 评价 / 5 成绩 / 6 设置</param>
    private void SwitchPage(int idx)
    {
        pageDashboard.Visible = (idx == 0);
        pageHome.Visible = (idx == 1);
        pageTeachers.Visible = (idx == 2);
        pageStudents.Visible = (idx == 3);
        pagePoints.Visible = (idx == 4);
        pageExam.Visible = (idx == 5);
        pageSettings.Visible = (idx == 6);

        if (idx == 0) pageDashboard.BringToFront();
        else if (idx == 1) pageHome.BringToFront();
        else if (idx == 2) pageTeachers.BringToFront();
        else if (idx == 3) pageStudents.BringToFront();
        else if (idx == 4) pagePoints.BringToFront();
        else if (idx == 5) pageExam.BringToFront();
        else pageSettings.BringToFront();

        sidebar.SelectedIndex = idx;
    }

    /// <summary>
    /// 供 PageDashboard 调用切换页面。
    /// </summary>
    public void SwitchPagePublic(int idx) => SwitchPage(idx);

    // =====================================================
    // 主页事件
    // =====================================================

    /// <summary>
    /// 绑定主页事件。
    /// </summary>
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

        pageHome.ScheduleGrid.ManageRepresentativesRequested += () =>
        {
            ManageRepresentatives();
        };
    }

    // =====================================================
    // 加载 / 保存
    // =====================================================

    /// <summary>
    /// 加载全部数据。
    /// </summary>
    public void LoadAll()
    {
        Courses = CourseRepository.LoadCourses();
        SectionTimes = CourseRepository.LoadSections();
        Teachers = CourseRepository.LoadTeachers();
        Students = StudentRepository.LoadStudents();
        Duty = StudentRepository.LoadDuty();
        Exams = StudentRepository.LoadExams();
        SeatLayout = StudentRepository.LoadSeatLayout();
        Committees = ClassCommitteeRepository.Load();
        CourseRepresentatives = CourseRepresentativeRepository.Load();
        ScoreRecords = ScoreRepository.LoadRecords();
        ScoreCategories = ScoreRepository.LoadCategories();

        ScoreRepository.RecalculateStudentPoints(Students, ScoreRecords);
    }

    /// <summary>
    /// 保存全部数据。
    /// </summary>
    public void SaveAll()
    {
        CourseRepository.SaveCourses(Courses);
        CourseRepository.SaveSections(SectionTimes);
        CourseRepository.SaveTeachers(Teachers);
        StudentRepository.SaveStudents(Students);
        StudentRepository.SaveDuty(Duty);
        StudentRepository.SaveExams(Exams);
        StudentRepository.SaveSeatLayout(SeatLayout);
        ClassCommitteeRepository.Save(Committees);
        CourseRepresentativeRepository.Save(CourseRepresentatives);
        ScoreRepository.SaveRecords(ScoreRecords);
        ScoreRepository.SaveCategories(ScoreCategories);
        ConfigService.Save(config);
    }

    // =====================================================
    // 刷新
    // =====================================================

    /// <summary>
    /// 刷新全部页面和浮动窗口。
    /// </summary>
    public void RefreshAll()
    {
        // 首页
pageDashboard.SetData(
    Courses,
    SectionTimes,
    Duty,
    currentWeek,
    Committees,
    CourseRepresentatives,
    Students,
    ScoreRecords);

        // 主页
        pageHome.Toolbar.SetWeek(currentWeek);
        pageHome.ScheduleGrid.SetData(SectionTimes, Courses, currentWeek);
        pageHome.ScheduleGrid.SetRepresentatives(CourseRepresentatives);

        // 教师页
        pageTeachers.SetData(Teachers);

        // 学生页
        // 学生页
pageStudents.SetData(Students, ScoreRecords, ScoreCategories);

        // 评价页
        pagePoints.SetData(Students, ScoreRecords, ScoreCategories);

        // 成绩页
        pageExam.SetData(Exams, Students);

        // 灵动岛
        islandForm?.SetData(SectionTimes, Courses, currentWeek, Duty, Committees);

        // 今日侧边栏
        todayForm?.SetData(Courses, SectionTimes, Duty, currentWeek, Committees);
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
    // 课程编辑
    // =====================================================

    /// <summary>
    /// 双击课表格子。
    /// </summary>
    private void OnCellDoubleClicked(int day, int section)
    {
        var existing = FindCourseAt(day, section);
        ShowCourseEditor(existing, day, section);
    }

    /// <summary>
    /// 查找指定位置的课程。
    /// </summary>
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

    /// <summary>
    /// 显示课程编辑弹窗。
    /// </summary>
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

    /// <summary>
    /// 删除课程。
    /// </summary>
    private void DeleteCourse(Course c)
    {
        if (!MessageDialog.Confirm(I18n.T("common.confirm"),
            string.Format(I18n.T("msg.confirmDeleteCourse"), c.Name)))
            return;
        Courses.Remove(c);
        SaveAll();
        RefreshAll();
    }

    /// <summary>
    /// 交换两门课程的位置。
    /// </summary>
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
    // 课代表管理
    // =====================================================

    /// <summary>
    /// 打开课代表管理弹窗。
    /// </summary>
    public void ManageRepresentatives()
    {
        using var dlg = new CourseRepresentativeDialog(Courses, Students, CourseRepresentatives);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            CourseRepresentatives = dlg.Result;
            SaveAll();
            RefreshAll();
        }
    }

    // =====================================================
    // 班委公告管理
    // =====================================================

    /// <summary>
    /// 新建班委公告。
    /// </summary>
    public void AddCommitteePublic()
    {
        using var dlg = new ClassCommitteeEditorDialog();
        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Result != null)
        {
            Committees.Add(dlg.Result);
            SaveAll();
            RefreshAll();
        }
    }

    /// <summary>
    /// 编辑班委公告。
    /// </summary>
    public void EditCommitteePublic(ClassCommittee c)
    {
        using var dlg = new ClassCommitteeEditorDialog(c);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            SaveAll();
            RefreshAll();
        }
    }

    /// <summary>
    /// 删除班委公告。
    /// </summary>
    public void DeleteCommitteePublic(ClassCommittee c)
    {
        if (!MessageDialog.Confirm(I18n.T("common.confirm"),
            string.Format(I18n.T("committee.confirmDelete"), c.Title)))
            return;
        Committees.Remove(c);
        SaveAll();
        RefreshAll();
    }

    // =====================================================
    // 积分管理
    // =====================================================

    /// <summary>
    /// 打开加分 / 减分弹窗。
    /// </summary>
    /// <param name="positive">true 加分 / false 减分</param>
    public void OpenScoreEntry(bool positive)
    {
        using var dlg = new ScoreEntryDialog(Students, ScoreCategories, positive);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            ScoreRecords.AddRange(dlg.Result);
            ScoreRepository.RecalculateStudentPoints(Students, ScoreRecords);
            SaveAll();
            RefreshAll();
        }
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
        menu.Items.Add(I18n.T("nav.home"), null, (s, e) => { RestoreFromTray(); SwitchPage(1); });
        menu.Items.Add(I18n.T("nav.teachers"), null, (s, e) => { RestoreFromTray(); SwitchPage(2); });
        menu.Items.Add(I18n.T("nav.students"), null, (s, e) => { RestoreFromTray(); SwitchPage(3); });
        menu.Items.Add(I18n.T("nav.points"), null, (s, e) => { RestoreFromTray(); SwitchPage(4); });
        menu.Items.Add(I18n.T("nav.exam"), null, (s, e) => { RestoreFromTray(); SwitchPage(5); });
        menu.Items.Add(I18n.T("nav.settings"), null, (s, e) => { RestoreFromTray(); SwitchPage(6); });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(I18n.T("points.add"), null, (s, e) =>
        {
            RestoreFromTray();
            SwitchPage(4);
            OpenScoreEntry(true);
        });
        menu.Items.Add(I18n.T("points.subtract"), null, (s, e) =>
        {
            RestoreFromTray();
            SwitchPage(4);
            OpenScoreEntry(false);
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
    /// 窗体关闭事件：未真正退出时隐藏到托盘。
    /// </summary>
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