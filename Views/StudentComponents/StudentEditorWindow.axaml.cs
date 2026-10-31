using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using PlanPeopleAvalonia.Models;

namespace PlanPeopleAvalonia.Views.StudentComponents;

public partial class StudentEditorWindow : Window
{
    private readonly Student _student;
    private readonly Action _onSaved;
    private string _newPhotoPath = "";

    // XAML 编译器需要无参构造，但我们主要用带参的
    public StudentEditorWindow() : this(new Student(), () => { }) { }

    public StudentEditorWindow(Student student, Action onSaved)
    {
        InitializeComponent();
        
        _student = student;
        _onSaved = onSaved;
        _newPhotoPath = student.PhotoPath ?? "";

        // 加载数据到文本框
        this.FindControl<TextBox>("TbName")!.Text = student.Name;
        this.FindControl<TextBox>("TbId")!.Text = student.Id;
        this.FindControl<TextBox>("TbGender")!.Text = student.Gender;
        this.FindControl<TextBox>("TbDuty")!.Text = student.Duty;
        this.FindControl<TextBox>("TbSeat")!.Text = student.Seat;
        this.FindControl<TextBox>("TbLevel")!.Text = student.Level;
        this.FindControl<TextBox>("TbHealth")!.Text = student.HealthStatus;
        this.FindControl<TextBox>("TbParentPhone")!.Text = student.ParentPhone;
        this.FindControl<TextBox>("TbAddress")!.Text = student.Address;
        this.FindControl<TextBox>("TbInfo")!.Text = student.Info;

        UpdateAvatarPreview();

        // 绑定事件
        this.FindControl<Border>("AvatarBorder")!.PointerPressed += async (s, e) => await SelectPhotoAsync();
        this.FindControl<Button>("BtnChangePhoto")!.Click += async (s, e) => await SelectPhotoAsync();
        this.FindControl<Button>("BtnCancel")!.Click += (s, e) => Close();
        this.FindControl<Button>("BtnSave")!.Click += (s, e) => SaveAndClose();
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
            Title = "选择学生头像",
            AllowMultiple = false,
            FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
        });

        if (files.Count >= 1)
        {
            var file = files[0];
            var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Photos");
            if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);
            
            string ext = Path.GetExtension(file.Name);
            string newFileName = $"student_{Guid.NewGuid().ToString("N").Substring(0, 8)}{ext}";
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
        var id = this.FindControl<TextBox>("TbId")!.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(id)) return;

        _student.Name = name;
        _student.Id = id;
        _student.Gender = this.FindControl<TextBox>("TbGender")!.Text?.Trim() ?? "";
        _student.Duty = this.FindControl<TextBox>("TbDuty")!.Text?.Trim() ?? "";
        _student.Seat = this.FindControl<TextBox>("TbSeat")!.Text?.Trim() ?? "";
        _student.Level = this.FindControl<TextBox>("TbLevel")!.Text?.Trim() ?? "";
        _student.HealthStatus = this.FindControl<TextBox>("TbHealth")!.Text?.Trim() ?? "";
        _student.ParentPhone = this.FindControl<TextBox>("TbParentPhone")!.Text?.Trim() ?? "";
        _student.Address = this.FindControl<TextBox>("TbAddress")!.Text?.Trim() ?? "";
        _student.Info = this.FindControl<TextBox>("TbInfo")!.Text?.Trim() ?? "";
        _student.PhotoPath = _newPhotoPath;

        _onSaved?.Invoke();
        Close();
    }
}