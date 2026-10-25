using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using PlanPeopleAvalonia.Services;
using PlanPeopleAvalonia.ViewModels;
using TeacherModel = PlanPeopleAvalonia.Models.Teacher;

namespace PlanPeopleAvalonia.Views;

public class TeacherEditorWindow : Window
{
    private readonly TeacherModel? _existing;
    private readonly Action _onSaved;

    private readonly TextBox _tbName, _tbSubject, _tbTitle, _tbPhone, _tbEmail, _tbOffice, _tbEmployeeId, _tbRemark;
    private readonly ComboBox _cbGender, _cbEducation;
    private readonly Border _avatarBorder;
    private readonly TextBlock _avatarInitial;
    private readonly Image _avatarImage;
    private string _currentPhotoPath = "";

    private static readonly string[] GenderOptions = { "男", "女", "其他" };
    private static readonly string[] EducationOptions = { "本科", "硕士", "博士", "专科", "其他" };

    public TeacherEditorWindow(TeacherModel? existing, Action onSaved)
    {
        _existing = existing;
        _onSaved = onSaved ?? (() => { });
        _currentPhotoPath = existing?.PhotoPath ?? "";

        Title = existing == null ? "添加教师" : "编辑教师";
        Width = 560;
        Height = 720;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = ThemeManager.WindowBrush;

        var root = new Grid { RowDefinitions = new RowDefinitions("Auto, *, Auto"), Margin = new Thickness(24) };
        root.Children.Add(new TextBlock { Text = existing == null ? "新增教师档案" : $"编辑：{existing.Name}", FontSize = 20, FontWeight = FontWeight.Bold, Foreground = ThemeManager.TextPrimaryBrush, Margin = new Thickness(0, 0, 0, 16), [Grid.RowProperty] = 0 });

        var form = new StackPanel { Spacing = 10 };
        var headerGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("120, *"), Margin = new Thickness(0, 0, 0, 8) };

        _avatarImage = new Image { Stretch = Stretch.UniformToFill, IsVisible = false };
        _avatarInitial = new TextBlock { FontSize = 36, FontWeight = FontWeight.Bold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var avatarLayer = new Grid(); avatarLayer.Children.Add(_avatarInitial); avatarLayer.Children.Add(_avatarImage);
        _avatarBorder = new Border { Width = 96, Height = 96, CornerRadius = new CornerRadius(48), Background = ThemeManager.AccentBrush, ClipToBounds = true, Cursor = new Cursor(StandardCursorType.Hand), Child = avatarLayer, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, [Grid.ColumnProperty] = 0 };
        _avatarBorder.PointerPressed += async (s, e) => await PickPhotoAsync();
        headerGrid.Children.Add(_avatarBorder);

        var headerRight = new StackPanel { Spacing = 8, Margin = new Thickness(16, 0, 0, 0), [Grid.ColumnProperty] = 1 };
        _tbName = AddField(headerRight, "姓名 *", existing?.Name);

        var photoBtns = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var btnPick = SmallBtn("选择照片"); btnPick.Click += async (s, e) => await PickPhotoAsync(); photoBtns.Children.Add(btnPick);
        var btnClear = SmallBtn("清除照片"); btnClear.Click += (s, e) => { _currentPhotoPath = ""; RefreshAvatar(); }; photoBtns.Children.Add(btnClear);
        headerRight.Children.Add(photoBtns);
        headerGrid.Children.Add(headerRight);
        form.Children.Add(headerGrid);

        var twoCol = new Grid { ColumnDefinitions = new ColumnDefinitions("*, *"), RowDefinitions = new RowDefinitions("Auto, Auto, Auto, Auto, Auto") };
        _tbEmployeeId = AddFieldAt(twoCol, 0, 0, "工号", existing?.EmployeeId);
        _tbSubject = AddFieldAt(twoCol, 0, 1, "学科", existing?.Subject);
        _tbTitle = AddFieldAt(twoCol, 1, 0, "职称", existing?.Title);
        _cbGender = AddComboAt(twoCol, 1, 1, "性别", GenderOptions, existing?.Gender);
        _tbPhone = AddFieldAt(twoCol, 2, 0, "电话", existing?.Phone);
        _tbEmail = AddFieldAt(twoCol, 2, 1, "邮箱", existing?.Email);
        _tbOffice = AddFieldAt(twoCol, 3, 0, "办公室", existing?.Office);
        _cbEducation = AddComboAt(twoCol, 3, 1, "学历", EducationOptions, existing?.Education);
        form.Children.Add(twoCol);

        form.Children.Add(new TextBlock { Text = "备注", Foreground = ThemeManager.TextSecondaryBrush, FontSize = 13, Margin = new Thickness(0, 8, 0, 0) });
        _tbRemark = new TextBox { Text = existing?.Remark ?? "", Background = ThemeManager.CardBrush, Foreground = ThemeManager.TextPrimaryBrush, CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 8), AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 80 };
        form.Children.Add(_tbRemark);

        var scroll = new ScrollViewer { Content = form, [Grid.RowProperty] = 1 };
        root.Children.Add(scroll);

        var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 20, 0, 0), [Grid.RowProperty] = 2 };
        var cancel = new Button { Content = "取消", Padding = new Thickness(20, 8), CornerRadius = new CornerRadius(8), Background = ThemeManager.CardBrush, Foreground = ThemeManager.TextPrimaryBrush };
        cancel.Click += (s, e) => Close(); btnRow.Children.Add(cancel);
        var save = new Button { Content = "保存", Padding = new Thickness(20, 8), CornerRadius = new CornerRadius(8), Background = ThemeManager.AccentBrush, Foreground = Brushes.White, FontWeight = FontWeight.Bold };
        save.Click += (s, e) => SaveAndClose(); btnRow.Children.Add(save);
        root.Children.Add(btnRow);
        Content = root;
        RefreshAvatar();
    }

    private static TextBox AddField(StackPanel parent, string label, string? value) { parent.Children.Add(new TextBlock { Text = label, Foreground = ThemeManager.TextSecondaryBrush, FontSize = 13 }); var tb = new TextBox { Text = value ?? "", Watermark = $"请输入{label.Replace("*", "").Trim()}", Background = ThemeManager.CardBrush, Foreground = ThemeManager.TextPrimaryBrush, CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 8) }; parent.Children.Add(tb); return tb; }
    private static TextBox AddFieldAt(Grid grid, int row, int col, string label, string? value) { var wrap = new StackPanel { Spacing = 4, Margin = new Thickness(col == 0 ? 0 : 6, 0, col == 0 ? 6 : 0, 10) }; wrap.Children.Add(new TextBlock { Text = label, Foreground = ThemeManager.TextSecondaryBrush, FontSize = 13 }); var tb = new TextBox { Text = value ?? "", Background = ThemeManager.CardBrush, Foreground = ThemeManager.TextPrimaryBrush, CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 8) }; wrap.Children.Add(tb); Grid.SetRow(wrap, row); Grid.SetColumn(wrap, col); grid.Children.Add(wrap); return tb; }
    private static ComboBox AddComboAt(Grid grid, int row, int col, string label, string[] items, string? value) { var wrap = new StackPanel { Spacing = 4, Margin = new Thickness(col == 0 ? 0 : 6, 0, col == 0 ? 6 : 0, 10) }; wrap.Children.Add(new TextBlock { Text = label, Foreground = ThemeManager.TextSecondaryBrush, FontSize = 13 }); var cb = new ComboBox { ItemsSource = items, HorizontalAlignment = HorizontalAlignment.Stretch, Background = ThemeManager.CardBrush, Foreground = ThemeManager.TextPrimaryBrush, CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 6) }; if (!string.IsNullOrWhiteSpace(value) && items.Contains(value)) cb.SelectedItem = value; wrap.Children.Add(cb); Grid.SetRow(wrap, row); Grid.SetColumn(wrap, col); grid.Children.Add(wrap); return cb; }
    private static Button SmallBtn(string text) => new() { Content = text, Padding = new Thickness(12, 4), CornerRadius = new CornerRadius(6), FontSize = 12, Background = ThemeManager.CardBrush, Foreground = ThemeManager.TextPrimaryBrush };

    private async System.Threading.Tasks.Task PickPhotoAsync() { var top = TopLevel.GetTopLevel(this); if (top == null) return; var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "选择照片", AllowMultiple = false, FileTypeFilter = new[] { new FilePickerFileType("图片") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg" } } } }); if (files.Count == 0) return; var local = files[0].TryGetLocalPath(); if (string.IsNullOrEmpty(local) || !File.Exists(local)) return; try { var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PlanPeople", "photos"); Directory.CreateDirectory(dir); var baseName = !string.IsNullOrWhiteSpace(_tbEmployeeId.Text) ? _tbEmployeeId.Text!.Trim() : (!string.IsNullOrWhiteSpace(_tbName.Text) ? _tbName.Text!.Trim() : "teacher"); var ext = Path.GetExtension(local); var target = Path.Combine(dir, $"{baseName}_{DateTime.Now:yyyyMMddHHmmss}{ext}"); File.Copy(local, target, overwrite: true); _currentPhotoPath = target; RefreshAvatar(); } catch (Exception ex) { ShowError(ex.Message); } }
    private void RefreshAvatar() { if (!string.IsNullOrWhiteSpace(_currentPhotoPath) && File.Exists(_currentPhotoPath)) { try { _avatarImage.Source = new Bitmap(_currentPhotoPath); _avatarImage.IsVisible = true; _avatarInitial.IsVisible = false; return; } catch { } } _avatarImage.IsVisible = false; _avatarInitial.IsVisible = true; var n = _tbName?.Text?.Trim(); _avatarInitial.Text = string.IsNullOrWhiteSpace(n) ? "?" : n.Substring(0, 1); }

    private void SaveAndClose()
    {
        var name = _tbName.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name)) { ShowError("姓名不能为空"); return; }
        var newTeacher = new TeacherModel
        {
            Name = name, Subject = _tbSubject.Text?.Trim() ?? "", Title = _tbTitle.Text?.Trim() ?? "",
            Gender = _cbGender.SelectedItem as string ?? "", Phone = _tbPhone.Text?.Trim() ?? "",
            Email = _tbEmail.Text?.Trim() ?? "", Office = _tbOffice.Text?.Trim() ?? "",
            EmployeeId = _tbEmployeeId.Text?.Trim() ?? "", Education = _cbEducation.SelectedItem as string ?? "",
            Remark = _tbRemark.Text?.Trim() ?? "", PhotoPath = _currentPhotoPath
        };
        try
        {
            var list = CourseRepository.LoadTeachers() ?? new List<TeacherModel>();
            if (_existing != null)
            {
                int idx = list.IndexOf(_existing);
                if (idx >= 0) list[idx] = newTeacher;
                else list.Add(newTeacher);
            }
            else list.Add(newTeacher);
            CourseRepository.SaveTeachers(list);
            _onSaved();
            Close();
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }
    private void ShowError(string msg) { if (TopLevel.GetTopLevel(this) is not Window owner) return; var dlg = new Window { Title = "提示", Width = 320, Height = 150, WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = ThemeManager.WindowBrush }; var p = new StackPanel { Margin = new Thickness(20), Spacing = 12 }; p.Children.Add(new TextBlock { Text = msg, Foreground = ThemeManager.TextPrimaryBrush, TextWrapping = TextWrapping.Wrap }); var ok = new Button { Content = "确定", HorizontalAlignment = HorizontalAlignment.Right, Background = ThemeManager.AccentBrush, Foreground = Brushes.White, Padding = new Thickness(16, 6), CornerRadius = new CornerRadius(6) }; ok.Click += (s, e) => dlg.Close(); p.Children.Add(ok); dlg.Content = p; dlg.ShowDialog(owner); }
}