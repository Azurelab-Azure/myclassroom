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
    /// 学生编辑（含个人档案）。
    /// </summary>
    public class StudentEditorDialog : FlatDialogBase
    {
        private readonly Student? _editing;

        private FlatTextBox _idBox = null!;
        private FlatTextBox _nameBox = null!;
        private FlatComboBox _genderBox = null!;
        private FlatTextBox _seatBox = null!;
        private FlatTextBox _birthBox = null!;
        private FlatTextBox _parentBox = null!;
        private FlatTextBox _parentPhoneBox = null!;
        private FlatTextBox _addressBox = null!;
        private FlatTextBox _remarkBox = null!;
        private PictureBox _photoBox = null!;
        private string _photoPath = "";

        public Student? Result { get; private set; }

        public StudentEditorDialog() : this(null) { }

        public StudentEditorDialog(Student? existing)
            : base(existing == null ? I18n.T("student.add") : I18n.T("student.edit"), 520, 700)
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
            AddLabel(I18n.T("student.photo"), x, y);
            _photoBox = new PictureBox
            {
                Left = fieldX, Top = y,
                Width = 100, Height = 120,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = AppTheme.Colors.CardBg,
                BorderStyle = BorderStyle.FixedSingle,
            };
            ContentPanel.Controls.Add(_photoBox);

            var btnPick = new FlatButton
            {
                Text = I18n.T("student.selectPhoto"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Left = fieldX + 110, Top = y + 40,
                Width = 110, Height = 32,
            };
            btnPick.Click += (s, e) => PickPhoto();
            ContentPanel.Controls.Add(btnPick);

            y += 130;

            // ---------- 学号 ----------
            AddLabel(I18n.T("student.id"), x, y);
            _idBox = new FlatTextBox { Left = fieldX, Top = y, Width = fieldW, Height = 32 };
            ContentPanel.Controls.Add(_idBox);
            y += rowH + gap;

            // ---------- 姓名 ----------
            AddLabel(I18n.T("student.name"), x, y);
            _nameBox = new FlatTextBox { Left = fieldX, Top = y, Width = fieldW, Height = 32 };
            ContentPanel.Controls.Add(_nameBox);
            y += rowH + gap;

            // ---------- 性别 ----------
            AddLabel(I18n.T("student.gender"), x, y);
            _genderBox = new FlatComboBox { Left = fieldX, Top = y, Width = 160, Height = 32 };
            _genderBox.SetItems(new IFlatComboItem[]
            {
                new FlatTextItem(I18n.T("student.gender.male")),
                new FlatTextItem(I18n.T("student.gender.female")),
            });
            _genderBox.SelectedIndex = 0;
            ContentPanel.Controls.Add(_genderBox);
            y += rowH + gap;

            // ---------- 座位 ----------
            AddLabel(I18n.T("student.seat"), x, y);
            _seatBox = new FlatTextBox
            {
                Left = fieldX, Top = y, Width = fieldW, Height = 32,
                Placeholder = I18n.T("student.seatHint"),
            };
            ContentPanel.Controls.Add(_seatBox);
            y += rowH + gap;

            // ---------- 出生日期 ----------
            AddLabel(I18n.T("student.birth"), x, y);
            _birthBox = new FlatTextBox
            {
                Left = fieldX, Top = y, Width = fieldW, Height = 32,
                Placeholder = "yyyy-MM-dd",
            };
            ContentPanel.Controls.Add(_birthBox);
            y += rowH + gap;

            // ---------- 家长 ----------
            AddLabel(I18n.T("student.parent"), x, y);
            _parentBox = new FlatTextBox { Left = fieldX, Top = y, Width = fieldW, Height = 32 };
            ContentPanel.Controls.Add(_parentBox);
            y += rowH + gap;

            // ---------- 家长电话 ----------
            AddLabel(I18n.T("student.parentPhone"), x, y);
            _parentPhoneBox = new FlatTextBox { Left = fieldX, Top = y, Width = fieldW, Height = 32 };
            ContentPanel.Controls.Add(_parentPhoneBox);
            y += rowH + gap;

            // ---------- 住址 ----------
            AddLabel(I18n.T("student.address"), x, y);
            _addressBox = new FlatTextBox { Left = fieldX, Top = y, Width = fieldW, Height = 32 };
            ContentPanel.Controls.Add(_addressBox);
            y += rowH + gap;

            // ---------- 备注 ----------
            AddLabel(I18n.T("student.remark"), x, y);
            _remarkBox = new FlatTextBox { Left = fieldX, Top = y, Width = fieldW, Height = 32 };
            ContentPanel.Controls.Add(_remarkBox);
            y += rowH + gap + 10;

            // ---------- 按钮 ----------
            var btnOk = new FlatButton
            {
                Text = I18n.T("dialog.save"),
                ButtonStyle = FlatButtonStyle.Primary,
                Width = 100, Height = 36,
                Left = 220, Top = y,
            };
            btnOk.Click += BtnOk_Click;
            ContentPanel.Controls.Add(btnOk);

            var btnCancel = new FlatButton
            {
                Text = I18n.T("dialog.cancel"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Width = 100, Height = 36,
                Left = 330, Top = y,
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

        private void LoadFrom(Student s)
        {
            _idBox.Text = s.Id ?? "";
            _nameBox.Text = s.Name ?? "";
            _genderBox.SelectedIndex = s.Gender == "female" ? 1 : 0;
            _seatBox.Text = s.Seat ?? "";
            _birthBox.Text = s.BirthDate ?? "";
            _parentBox.Text = s.ParentName ?? "";
            _parentPhoneBox.Text = s.ParentPhone ?? "";
            _addressBox.Text = s.Address ?? "";
            _remarkBox.Text = s.Remark ?? "";

            _photoPath = s.Photo ?? "";
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
                MessageDialog.ShowInfo(I18n.T("common.info"), I18n.T("student.needName"));
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
                string dest = Path.Combine(AppPaths.PhotosDir, "student_" + name + ext);
                try { File.Copy(_photoPath, dest, true); savedPhoto = dest; }
                catch { savedPhoto = ""; }
            }

            if (_editing == null)
            {
                Result = new Student
                {
                    Id = _idBox.Text.Trim(),
                    Name = name,
                    Gender = _genderBox.SelectedIndex == 1 ? "female" : "male",
                    Seat = _seatBox.Text.Trim(),
                    BirthDate = _birthBox.Text.Trim(),
                    ParentName = _parentBox.Text.Trim(),
                    ParentPhone = _parentPhoneBox.Text.Trim(),
                    Address = _addressBox.Text.Trim(),
                    Remark = _remarkBox.Text.Trim(),
                    Photo = savedPhoto ?? "",
                };
            }
            else
            {
                _editing.Id = _idBox.Text.Trim();
                _editing.Name = name;
                _editing.Gender = _genderBox.SelectedIndex == 1 ? "female" : "male";
                _editing.Seat = _seatBox.Text.Trim();
                _editing.BirthDate = _birthBox.Text.Trim();
                _editing.ParentName = _parentBox.Text.Trim();
                _editing.ParentPhone = _parentPhoneBox.Text.Trim();
                _editing.Address = _addressBox.Text.Trim();
                _editing.Remark = _remarkBox.Text.Trim();
                _editing.Photo = savedPhoto ?? "";
                Result = _editing;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}