using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using CourseApp.Controls;
using CourseApp.Localization;
using CourseApp.Models;
using CourseApp.Services;
using CourseApp.Theme;

namespace CourseApp.Dialogs
{
    /// <summary>
    /// 添加 / 编辑教师弹窗。
    /// </summary>
    public class TeacherEditorDialog : FlatDialogBase
    {
        private readonly Teacher? _editing;

        private FlatTextBox _nameBox = null!;
        private FlatTextBox _titleBox = null!;
        private FlatTextBox _subjectBox = null!;
        private FlatTextBox _infoBox = null!;
        private FlatTextBox _phoneBox = null!;
        private FlatTextBox _emailBox = null!;
        private FlatTextBox _officeBox = null!;
        private FlatTextBox _remarkBox = null!;
        private PictureBox _photoBox = null!;
        private string _photoPath = "";

        public Teacher? Result { get; private set; }

        public TeacherEditorDialog() : this(null) { }

        public TeacherEditorDialog(Teacher? existing)
            : base(existing == null ? I18n.T("teacher.add") : I18n.T("teacher.edit"), 520, 700)
        {
            _editing = existing;
            BuildUI();
            if (existing != null) LoadFrom(existing);
        }

        private void BuildUI()
        {
            int x = 20;
            int y = 10;
            int fieldX = 130;
            int fieldW = 340;
            int rowH = 36;
            int gap = 8;

            // ---------- 照片 ----------
            AddLabel(I18n.T("teacher.photo"), x, y);
            _photoBox = new PictureBox
            {
                Left = fieldX, Top = y,
                Width = 120, Height = 140,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = AppTheme.Colors.CardBg,
                BorderStyle = BorderStyle.FixedSingle,
            };
            ContentPanel.Controls.Add(_photoBox);

            var btnPick = new FlatButton
            {
                Text = I18n.T("teacher.selectPhoto"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Left = fieldX + 130, Top = y + 50,
                Width = 120, Height = 34,
            };
            btnPick.Click += (s, e) => PickPhoto();
            ContentPanel.Controls.Add(btnPick);

            y += 150;

            // ---------- 姓名 ----------
            AddLabel(I18n.T("teacher.name"), x, y);
            _nameBox = new FlatTextBox { Left = fieldX, Top = y, Width = fieldW, Height = 32 };
            ContentPanel.Controls.Add(_nameBox);
            y += rowH + gap;

            // ---------- 职称 ----------
            AddLabel(I18n.T("teacher.field.title"), x, y);
            _titleBox = new FlatTextBox { Left = fieldX, Top = y, Width = fieldW, Height = 32 };
            ContentPanel.Controls.Add(_titleBox);
            y += rowH + gap;

            // ---------- 学科 ----------
            AddLabel(I18n.T("teacher.field.subject"), x, y);
            _subjectBox = new FlatTextBox { Left = fieldX, Top = y, Width = fieldW, Height = 32 };
            ContentPanel.Controls.Add(_subjectBox);
            y += rowH + gap;

            // ---------- 电话 ----------
            AddLabel(I18n.T("teacher.field.phone"), x, y);
            _phoneBox = new FlatTextBox { Left = fieldX, Top = y, Width = fieldW, Height = 32 };
            ContentPanel.Controls.Add(_phoneBox);
            y += rowH + gap;

            // ---------- 邮箱 ----------
            AddLabel(I18n.T("teacher.field.email"), x, y);
            _emailBox = new FlatTextBox { Left = fieldX, Top = y, Width = fieldW, Height = 32 };
            ContentPanel.Controls.Add(_emailBox);
            y += rowH + gap;

            // ---------- 办公室 ----------
            AddLabel(I18n.T("teacher.field.office"), x, y);
            _officeBox = new FlatTextBox { Left = fieldX, Top = y, Width = fieldW, Height = 32 };
            ContentPanel.Controls.Add(_officeBox);
            y += rowH + gap;

            // ---------- 简介 ----------
            AddLabel(I18n.T("teacher.field.info"), x, y);
            _infoBox = new FlatTextBox { Left = fieldX, Top = y, Width = fieldW, Height = 32 };
            ContentPanel.Controls.Add(_infoBox);
            y += rowH + gap;

            // ---------- 备注 ----------
            AddLabel(I18n.T("teacher.field.remark"), x, y);
            _remarkBox = new FlatTextBox { Left = fieldX, Top = y, Width = fieldW, Height = 32 };
            ContentPanel.Controls.Add(_remarkBox);
            y += rowH + gap + 10;

            // ---------- 按钮 ----------
            var btnOk = new FlatButton
            {
                Text = I18n.T("dialog.save"),
                ButtonStyle = FlatButtonStyle.Primary,
                Width = 100, Height = 36,
                Left = 250, Top = y,
            };
            btnOk.Click += BtnOk_Click;
            ContentPanel.Controls.Add(btnOk);

            var btnCancel = new FlatButton
            {
                Text = I18n.T("dialog.cancel"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Width = 100, Height = 36,
                Left = 360, Top = y,
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            ContentPanel.Controls.Add(btnCancel);
        }

        private void AddLabel(string text, int x, int y)
        {
            ContentPanel.Controls.Add(new Label
            {
                Text = text,
                Left = x, Top = y + 6, Width = 100,
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.Colors.TextPrimary,
                BackColor = Color.Transparent,
                AutoSize = false,
            });
        }

        private void LoadFrom(Teacher t)
        {
            _nameBox.Text = t.Name ?? "";
            _titleBox.Text = t.Title ?? "";
            _subjectBox.Text = t.Subject ?? "";
            _infoBox.Text = t.Info ?? "";
            _phoneBox.Text = t.Phone ?? "";
            _emailBox.Text = t.Email ?? "";
            _officeBox.Text = t.Office ?? "";
            _remarkBox.Text = t.Remark ?? "";

            _photoPath = t.Photo ?? "";
            LoadPhotoPreview(_photoPath);
        }

        private void LoadPhotoPreview(string path)
        {
            try
            {
                _photoBox.Image?.Dispose();
                _photoBox.Image = null;
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    _photoBox.Image = Image.FromStream(fs);
                }
            }
            catch { }
        }

        private void PickPhoto()
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "图片|*.jpg;*.jpeg;*.png;*.bmp",
            };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                _photoPath = ofd.FileName;
                LoadPhotoPreview(_photoPath);
            }
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            string name = _nameBox.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageDialog.ShowInfo(I18n.T("dialog.info"), I18n.T("dialog.needTeacherName"));
                return;
            }

            // 处理照片：拷贝到 photos/ 目录
            string savedPhoto = _photoPath;
            if (!string.IsNullOrEmpty(_photoPath)
                && !_photoPath.StartsWith(AppPaths.PhotosDir, StringComparison.OrdinalIgnoreCase)
                && File.Exists(_photoPath))
            {
                AppPaths.EnsureAll();
                string ext = Path.GetExtension(_photoPath);
                string dest = Path.Combine(AppPaths.PhotosDir, name + ext);
                try { File.Copy(_photoPath, dest, true); savedPhoto = dest; }
                catch { savedPhoto = ""; }
            }

            if (_editing == null)
            {
                Result = new Teacher
                {
                    Name = name,
                    Title = _titleBox.Text.Trim(),
                    Subject = _subjectBox.Text.Trim(),
                    Info = _infoBox.Text.Trim(),
                    Phone = _phoneBox.Text.Trim(),
                    Email = _emailBox.Text.Trim(),
                    Office = _officeBox.Text.Trim(),
                    Remark = _remarkBox.Text.Trim(),
                    Photo = savedPhoto ?? "",
                };
            }
            else
            {
                _editing.Name = name;
                _editing.Title = _titleBox.Text.Trim();
                _editing.Subject = _subjectBox.Text.Trim();
                _editing.Info = _infoBox.Text.Trim();
                _editing.Phone = _phoneBox.Text.Trim();
                _editing.Email = _emailBox.Text.Trim();
                _editing.Office = _officeBox.Text.Trim();
                _editing.Remark = _remarkBox.Text.Trim();
                _editing.Photo = savedPhoto ?? "";
                Result = _editing;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}