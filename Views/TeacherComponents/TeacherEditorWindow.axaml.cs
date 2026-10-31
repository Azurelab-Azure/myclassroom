using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using PlanPeopleAvalonia.Models;

namespace PlanPeopleAvalonia.Views.TeacherComponents;

public partial class TeacherEditorWindow : Window
{
    private readonly Teacher _teacher;
    private readonly Action _onSaved;
    private string _newPhotoPath = "";

    // 无参构造：供 XAML 预览器使用
    public TeacherEditorWindow() : this(new Teacher(), () => { }) { }

    public TeacherEditorWindow(Teacher teacher, Action onSaved)
    {
        InitializeComponent();
        
        _teacher = teacher;
        _onSaved = onSaved;
        _newPhotoPath = teacher.PhotoPath ?? teacher.Photo ?? "";

        // 1. 加载基础数据到控件
        this.FindControl<TextBox>("TbName")!.Text = teacher.Name;
        this.FindControl<TextBox>("TbSubject")!.Text = teacher.Subject;
        this.FindControl<TextBox>("TbTitle")!.Text = teacher.Title;
        this.FindControl<TextBox>("TbId")!.Text = teacher.EmployeeId;
        this.FindControl<TextBox>("TbGender")!.Text = teacher.Gender;
        this.FindControl<TextBox>("TbPhone")!.Text = teacher.Phone;
        this.FindControl<TextBox>("TbEmail")!.Text = teacher.Email;
        this.FindControl<TextBox>("TbOffice")!.Text = teacher.Office;
        this.FindControl<TextBox>("TbEducation")!.Text = teacher.Education;
        this.FindControl<TextBox>("TbPolitical")!.Text = teacher.PoliticalStatus;
        this.FindControl<TextBox>("TbHometown")!.Text = teacher.Hometown;
        this.FindControl<TextBox>("TbInfo")!.Text = teacher.Info;

        UpdateAvatarPreview();

        // 2. 挂载事件
        this.FindControl<Border>("AvatarBorder")!.PointerPressed += async (s, e) => await SelectPhotoAsync();
        this.FindControl<Button>("BtnChangePhoto")!.Click += async (s, e) => await SelectPhotoAsync();
        this.FindControl<Button>("BtnCancel")!.Click += (s, e) => Close();
        this.FindControl<Button>("BtnSave")!.Click += (s, e) => SaveAndClose();
        this.FindControl<Button>("BtnPoster")!.Click += (s, e) => GeneratePoster();
    }

    private void UpdateAvatarPreview()
    {
        var preview = this.FindControl<Image>("AvatarPreview")!;
        var placeholder = this.FindControl<TextBlock>("AvatarPlaceholder")!;

        if (!string.IsNullOrEmpty(_newPhotoPath) && File.Exists(_newPhotoPath))
        {
            preview.Source = new Bitmap(_newPhotoPath);
            preview.IsVisible = true;
            placeholder.IsVisible = false;
        }
        else
        {
            preview.Source = null;
            preview.IsVisible = false;
            placeholder.IsVisible = true;
        }
    }

    private async System.Threading.Tasks.Task SelectPhotoAsync()
    {
        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择教师头像",
            AllowMultiple = false,
            FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
        });

        if (files.Count >= 1)
        {
            var file = files[0];
            var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Photos");
            if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);
            
            string ext = Path.GetExtension(file.Name);
            string newFileName = $"teacher_{Guid.NewGuid().ToString("N").Substring(0, 8)}{ext}";
            string destPath = Path.Combine(dataDir, newFileName);
            
            using (var stream = await file.OpenReadAsync())
            using (var destStream = File.Create(destPath))
            {
                await stream.CopyToAsync(destStream);
            }

            _newPhotoPath = destPath;
            UpdateAvatarPreview();
        }
    }

    private void SaveAndClose()
    {
        var name = this.FindControl<TextBox>("TbName")!.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return;

        // 同步数据回模型
        _teacher.Name = name;
        _teacher.Subject = this.FindControl<TextBox>("TbSubject")!.Text?.Trim() ?? "";
        _teacher.Title = this.FindControl<TextBox>("TbTitle")!.Text?.Trim() ?? "";
        _teacher.EmployeeId = this.FindControl<TextBox>("TbId")!.Text?.Trim() ?? "";
        _teacher.Gender = this.FindControl<TextBox>("TbGender")!.Text?.Trim() ?? "";
        _teacher.Phone = this.FindControl<TextBox>("TbPhone")!.Text?.Trim() ?? "";
        _teacher.Email = this.FindControl<TextBox>("TbEmail")!.Text?.Trim() ?? "";
        _teacher.Office = this.FindControl<TextBox>("TbOffice")!.Text?.Trim() ?? "";
        _teacher.Education = this.FindControl<TextBox>("TbEducation")!.Text?.Trim() ?? "";
        _teacher.PoliticalStatus = this.FindControl<TextBox>("TbPolitical")!.Text?.Trim() ?? "";
        _teacher.Hometown = this.FindControl<TextBox>("TbHometown")!.Text?.Trim() ?? "";
        _teacher.Info = this.FindControl<TextBox>("TbInfo")!.Text?.Trim() ?? "";
        _teacher.PhotoPath = _newPhotoPath;
        _teacher.Photo = _newPhotoPath; // 兼容旧字段

        _onSaved?.Invoke();
        Close();
    }

    private void GeneratePoster()
    {
        var posterWindow = new Window { Title = "教师海报预览", Width = 500, Height = 600, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var posterPanel = new StackPanel { Background = Avalonia.Media.Brushes.White, Margin = new Avalonia.Thickness(20) };
        
        if (!string.IsNullOrEmpty(_newPhotoPath) && File.Exists(_newPhotoPath))
        {
            posterPanel.Children.Add(new Image { Source = new Bitmap(_newPhotoPath), Width = 120, Height = 120, ClipToBounds = true, Margin = new Avalonia.Thickness(0, 20, 0, 10) });
        }
        
        posterPanel.Children.Add(new TextBlock { Text = this.FindControl<TextBox>("TbName")!.Text, FontSize = 28, FontWeight = Avalonia.Media.FontWeight.Bold, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, Margin = new Avalonia.Thickness(0, 10, 0, 5) });
        posterPanel.Children.Add(new TextBlock { Text = $"{this.FindControl<TextBox>("TbSubject")!.Text} | {this.FindControl<TextBox>("TbTitle")!.Text}", FontSize = 16, Foreground = Avalonia.Media.Brushes.Gray, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 0, 20) });
        posterPanel.Children.Add(new TextBlock { Text = this.FindControl<TextBox>("TbInfo")!.Text, FontSize = 14, TextWrapping = Avalonia.Media.TextWrapping.Wrap, TextAlignment = Avalonia.Media.TextAlignment.Center, Margin = new Avalonia.Thickness(20) });
        
        posterWindow.Content = posterPanel;
        posterWindow.ShowDialog(this);
    }
}