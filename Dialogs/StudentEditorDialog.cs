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
    /// 学生编辑弹窗。
    /// 包含：基本信息、等级与职务、综合评价、健康状况、学籍档案、家长信息、积分徽章。
    /// 内容超过窗口高度时自动滚动。
    /// </summary>
    public class StudentEditorDialog : FlatDialogBase
    {
        /// <summary>正在编辑的学生。为 null 表示新建。</summary>
        private readonly Student? _editing;

        // ---------- 基本信息 ----------
        /// <summary>学号输入框</summary>
        private FlatTextBox _idBox = null!;

        /// <summary>姓名输入框</summary>
        private FlatTextBox _nameBox = null!;

        /// <summary>性别下拉框</summary>
        private FlatComboBox _genderBox = null!;

        /// <summary>座位输入框</summary>
        private FlatTextBox _seatBox = null!;

        /// <summary>出生日期输入框</summary>
        private FlatTextBox _birthBox = null!;

        // ---------- 等级与职务 ----------
        /// <summary>等级下拉框</summary>
        private FlatComboBox _levelBox = null!;

        /// <summary>等级分输入框</summary>
        private FlatNumberBox _levelScoreBox = null!;

        /// <summary>职务输入框</summary>
        private FlatTextBox _dutyBox = null!;

        // ---------- 综合评价 ----------
        /// <summary>综合评分输入框</summary>
        private FlatNumberBox _overallScoreBox = null!;

        /// <summary>教师评语输入框</summary>
        private FlatTextBox _overallCommentBox = null!;

        // ---------- 健康状况 ----------
        /// <summary>健康状况下拉框</summary>
        private FlatComboBox _healthStatusBox = null!;

        /// <summary>健康说明输入框</summary>
        private FlatTextBox _healthNoteBox = null!;

        // ---------- 学籍档案 ----------
        /// <summary>档案号输入框</summary>
        private FlatTextBox _archiveNoBox = null!;

        /// <summary>入学日期输入框</summary>
        private FlatTextBox _enrollmentBox = null!;

        /// <summary>政治面貌输入框</summary>
        private FlatTextBox _politicalBox = null!;

        /// <summary>籍贯输入框</summary>
        private FlatTextBox _hometownBox = null!;

        /// <summary>民族输入框</summary>
        private FlatTextBox _ethnicityBox = null!;

        // ---------- 家长信息 ----------
        /// <summary>家长姓名输入框</summary>
        private FlatTextBox _parentBox = null!;

        /// <summary>家长电话输入框</summary>
        private FlatTextBox _parentPhoneBox = null!;

        /// <summary>住址输入框</summary>
        private FlatTextBox _addressBox = null!;

        /// <summary>备注输入框</summary>
        private FlatTextBox _remarkBox = null!;

        // ---------- 积分 / 徽章 ----------
        /// <summary>累计积分显示框（只读）</summary>
        private FlatNumberBox _totalPointsBox = null!;

        /// <summary>徽章下拉框</summary>
        private FlatComboBox _badgeBox = null!;

        // ---------- 照片 ----------
        /// <summary>照片预览框</summary>
        private PictureBox _photoBox = null!;

        /// <summary>当前照片路径</summary>
        private string _photoPath = "";

        /// <summary>编辑结果。确定后有效。</summary>
        public Student? Result { get; private set; }

        /// <summary>
        /// 无参构造：新建学生。
        /// </summary>
        public StudentEditorDialog() : this(null) { }

        /// <summary>
        /// 构造学生编辑弹窗。
        /// </summary>
        /// <param name="existing">正在编辑的学生，为 null 表示新建</param>
        public StudentEditorDialog(Student? existing)
            : base(existing == null ? I18n.T("student.add") : I18n.T("student.edit"), 640, 760)
        {
            _editing = existing;

            // 启用滚动模式
            EnableScroll();

            BuildUI();
            if (existing != null) LoadFrom(existing);
        }

        /// <summary>
        /// 构建界面。
        /// </summary>
        private void BuildUI()
        {
            int x = 20;
            int y = 10;
            int fieldX = 130;
            int fieldW = 440;
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
            ScrollPanel!.Controls.Add(_photoBox);

            var btnPick = new FlatButton
            {
                Text = I18n.T("student.selectPhoto"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Left = fieldX + 110, Top = y + 44,
                Width = 120, Height = 32,
            };
            btnPick.Click += (s, e) => PickPhoto();
            ScrollPanel.Controls.Add(btnPick);

            y += 130;

            // ---------- 基本信息 ----------
            y = AddSection(y, I18n.T("student.section.basic"));

            y = AddField(I18n.T("student.id"), ref _idBox, x, y, fieldX, fieldW);
            y = AddField(I18n.T("student.name"), ref _nameBox, x, y, fieldX, fieldW);

            AddLabel(I18n.T("student.gender"), x, y);
            _genderBox = new FlatComboBox { Left = fieldX, Top = y, Width = 160, Height = 32 };
            _genderBox.SetItems(new IFlatComboItem[]
            {
                new FlatTextItem(I18n.T("student.gender.male")),
                new FlatTextItem(I18n.T("student.gender.female")),
            });
            _genderBox.SelectedIndex = 0;
            ScrollPanel.Controls.Add(_genderBox);
            y += rowH + gap;

            y = AddField(I18n.T("student.seat"), ref _seatBox, x, y, fieldX, fieldW);
            y = AddField(I18n.T("student.birth"), ref _birthBox, x, y, fieldX, fieldW);

            // ---------- 等级与职务 ----------
            y = AddSection(y, I18n.T("student.section.level"));

            AddLabel(I18n.T("student.level"), x, y);
            _levelBox = new FlatComboBox { Left = fieldX, Top = y, Width = 160, Height = 32 };
            _levelBox.SetItems(new IFlatComboItem[]
            {
                new FlatTextItem("优"),
                new FlatTextItem("良"),
                new FlatTextItem("中"),
                new FlatTextItem("待提高"),
            });
            ScrollPanel.Controls.Add(_levelBox);
            y += rowH + gap;

            AddLabel(I18n.T("student.levelScore"), x, y);
            _levelScoreBox = new FlatNumberBox
            {
                Left = fieldX, Top = y, Width = 120, Height = 32,
                Minimum = 0, Maximum = 100, Value = 0,
            };
            ScrollPanel.Controls.Add(_levelScoreBox);
            y += rowH + gap;

            y = AddField(I18n.T("student.duty"), ref _dutyBox, x, y, fieldX, fieldW);

            // ---------- 积分 / 徽章 ----------
            y = AddSection(y, I18n.T("student.section.points"));

            AddLabel(I18n.T("student.totalPoints"), x, y);
            _totalPointsBox = new FlatNumberBox
            {
                Left = fieldX, Top = y, Width = 120, Height = 32,
                Minimum = 0, Maximum = 9999, Value = 0,
            };
            ScrollPanel.Controls.Add(_totalPointsBox);
            y += rowH + gap;

            AddLabel(I18n.T("student.badge"), x, y);
            _badgeBox = new FlatComboBox { Left = fieldX, Top = y, Width = 200, Height = 32 };
            _badgeBox.SetItems(new IFlatComboItem[]
            {
                new FlatTextItem(I18n.T("badge.none")),
                new FlatTextItem(I18n.T("badge.threeGood")),
                new FlatTextItem(I18n.T("badge.progress")),
                new FlatTextItem(I18n.T("badge.labor")),
                new FlatTextItem(I18n.T("badge.discipline")),
                new FlatTextItem(I18n.T("badge.helper")),
            });
            _badgeBox.SelectedIndex = 0;
            ScrollPanel.Controls.Add(_badgeBox);
            y += rowH + gap;

            // ---------- 综合评价 ----------
            y = AddSection(y, I18n.T("student.section.overall"));

            AddLabel(I18n.T("student.overallScore"), x, y);
            _overallScoreBox = new FlatNumberBox
            {
                Left = fieldX, Top = y, Width = 120, Height = 32,
                Minimum = 0, Maximum = 100, Value = 0,
            };
            ScrollPanel.Controls.Add(_overallScoreBox);
            y += rowH + gap;

            AddLabel(I18n.T("student.overallComment"), x, y);
            _overallCommentBox = new FlatTextBox { Left = fieldX, Top = y, Width = fieldW, Height = 32 };
            ScrollPanel.Controls.Add(_overallCommentBox);
            y += rowH + gap;

            // ---------- 健康状况 ----------
            y = AddSection(y, I18n.T("student.section.health"));

            AddLabel(I18n.T("student.healthStatus"), x, y);
            _healthStatusBox = new FlatComboBox { Left = fieldX, Top = y, Width = 200, Height = 32 };
            _healthStatusBox.SetItems(new IFlatComboItem[]
            {
                new FlatTextItem("健康"),
                new FlatTextItem("近视"),
                new FlatTextItem("过敏"),
                new FlatTextItem("哮喘"),
                new FlatTextItem("其他"),
            });
            ScrollPanel.Controls.Add(_healthStatusBox);
            y += rowH + gap;

            y = AddField(I18n.T("student.healthNote"), ref _healthNoteBox, x, y, fieldX, fieldW);

            // ---------- 学籍档案 ----------
            y = AddSection(y, I18n.T("student.section.archive"));

            y = AddField(I18n.T("student.archiveNo"), ref _archiveNoBox, x, y, fieldX, fieldW);
            y = AddField(I18n.T("student.enrollment"), ref _enrollmentBox, x, y, fieldX, fieldW);
            y = AddField(I18n.T("student.political"), ref _politicalBox, x, y, fieldX, fieldW);
            y = AddField(I18n.T("student.hometown"), ref _hometownBox, x, y, fieldX, fieldW);
            y = AddField(I18n.T("student.ethnicity"), ref _ethnicityBox, x, y, fieldX, fieldW);

            // ---------- 家长信息 ----------
            y = AddSection(y, I18n.T("student.section.parent"));

            y = AddField(I18n.T("student.parent"), ref _parentBox, x, y, fieldX, fieldW);
            y = AddField(I18n.T("student.parentPhone"), ref _parentPhoneBox, x, y, fieldX, fieldW);
            y = AddField(I18n.T("student.address"), ref _addressBox, x, y, fieldX, fieldW);
            y = AddField(I18n.T("student.remark"), ref _remarkBox, x, y, fieldX, fieldW);

            y += 10;

            // ---------- 按钮 ----------
            var btnOk = new FlatButton
            {
                Text = I18n.T("dialog.save"),
                ButtonStyle = FlatButtonStyle.Primary,
                Width = 100, Height = 36,
                Left = 340, Top = y,
            };
            btnOk.Click += BtnOk_Click;
            ScrollPanel.Controls.Add(btnOk);

            var btnCancel = new FlatButton
            {
                Text = I18n.T("dialog.cancel"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Width = 100, Height = 36,
                Left = 450, Top = y,
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            ScrollPanel.Controls.Add(btnCancel);

            // 告诉滚动宿主内容实际高度
            SetScrollContentHeight(y + btnOk.Height + 40);
        }

        /// <summary>
        /// 添加分区标题。
        /// </summary>
        /// <param name="y">当前纵向位置</param>
        /// <param name="title">标题文字</param>
        /// <returns>新的纵向位置</returns>
        private int AddSection(int y, string title)
        {
            y += 10;
            var lbl = new Label
            {
                Text = title,
                Font = new Font(AppTheme.BodyFont.FontFamily, 13f, FontStyle.Bold),
                ForeColor = AppTheme.Colors.Accent,
                Left = 20, Top = y,
                Width = 560, Height = 24,
                BackColor = Color.Transparent,
                AutoSize = false,
            };
            ScrollPanel!.Controls.Add(lbl);
            return y + 30;
        }

        /// <summary>
        /// 添加标签 + 文本框行。
        /// </summary>
        private int AddField(string label, ref FlatTextBox box, int x, int y, int fieldX, int fieldW)
        {
            AddLabel(label, x, y);
            box = new FlatTextBox { Left = fieldX, Top = y, Width = fieldW, Height = 32 };
            ScrollPanel!.Controls.Add(box);
            return y + 44;
        }

        /// <summary>
        /// 添加字段标签。
        /// </summary>
        private void AddLabel(string text, int x, int y)
        {
            ScrollPanel!.Controls.Add(new Label
            {
                Text = text,
                Left = x, Top = y + 6, Width = 100,
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.Colors.TextPrimary,
                BackColor = Color.Transparent,
                AutoSize = false,
            });
        }

        /// <summary>
        /// 从学生对象加载数据到界面。
        /// </summary>
        private void LoadFrom(Student s)
        {
            _idBox.Text = s.Id ?? "";
            _nameBox.Text = s.Name ?? "";
            _genderBox.SelectedIndex = s.Gender == "female" ? 1 : 0;
            _seatBox.Text = s.Seat ?? "";
            _birthBox.Text = s.BirthDate ?? "";

            _levelBox.SelectedIndex = s.Level switch
            {
                "优" => 0, "良" => 1, "中" => 2, "待提高" => 3, _ => -1
            };
            _levelScoreBox.Value = s.LevelScore;
            _dutyBox.Text = s.Duty ?? "";

            _totalPointsBox.Value = Math.Max(0, Math.Min(9999, s.TotalPoints));
            _badgeBox.SelectedIndex = s.Badge switch
            {
                "三好学生" => 1,
                "进步之星" => 2,
                "劳动模范" => 3,
                "纪律标兵" => 4,
                "助人之星" => 5,
                _ => 0
            };

            _overallScoreBox.Value = s.OverallScore;
            _overallCommentBox.Text = s.OverallComment ?? "";

            _healthStatusBox.SelectedIndex = s.HealthStatus switch
            {
                "健康" => 0, "近视" => 1, "过敏" => 2, "哮喘" => 3, "其他" => 4, _ => -1
            };
            _healthNoteBox.Text = s.HealthNote ?? "";

            _archiveNoBox.Text = s.ArchiveNo ?? "";
            _enrollmentBox.Text = s.EnrollmentDate ?? "";
            _politicalBox.Text = s.PoliticalStatus ?? "";
            _hometownBox.Text = s.Hometown ?? "";
            _ethnicityBox.Text = s.Ethnicity ?? "";

            _parentBox.Text = s.ParentName ?? "";
            _parentPhoneBox.Text = s.ParentPhone ?? "";
            _addressBox.Text = s.Address ?? "";
            _remarkBox.Text = s.Remark ?? "";

            _photoPath = s.Photo ?? "";
            LoadPhotoPreview(_photoPath);
        }

        /// <summary>
        /// 加载照片预览。
        /// </summary>
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

        /// <summary>
        /// 弹出文件对话框选择照片。
        /// </summary>
        private void PickPhoto()
        {
            using var ofd = new OpenFileDialog { Filter = "图片|*.jpg;*.jpeg;*.png;*.bmp" };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                _photoPath = ofd.FileName;
                LoadPhotoPreview(_photoPath);
            }
        }

        /// <summary>
        /// 点击确定：校验并保存。
        /// </summary>
        private void BtnOk_Click(object? sender, EventArgs e)
        {
            string name = _nameBox.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageDialog.ShowInfo(I18n.T("common.info"), I18n.T("student.needName"));
                return;
            }

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

            var target = _editing ?? new Student();
            target.Id = _idBox.Text.Trim();
            target.Name = name;
            target.Gender = _genderBox.SelectedIndex == 1 ? "female" : "male";
            target.Seat = _seatBox.Text.Trim();
            target.BirthDate = _birthBox.Text.Trim();

            target.Level = _levelBox.SelectedIndex >= 0 ? _levelBox.SelectedText : "";
            target.LevelScore = _levelScoreBox.Value;
            target.Duty = _dutyBox.Text.Trim();

            target.TotalPoints = _totalPointsBox.Value;
            target.Badge = _badgeBox.SelectedIndex > 0 ? _badgeBox.SelectedText : "";

            target.OverallScore = _overallScoreBox.Value;
            target.OverallComment = _overallCommentBox.Text.Trim();

            target.HealthStatus = _healthStatusBox.SelectedIndex >= 0 ? _healthStatusBox.SelectedText : "";
            target.HealthNote = _healthNoteBox.Text.Trim();

            target.ArchiveNo = _archiveNoBox.Text.Trim();
            target.EnrollmentDate = _enrollmentBox.Text.Trim();
            target.PoliticalStatus = _politicalBox.Text.Trim();
            target.Hometown = _hometownBox.Text.Trim();
            target.Ethnicity = _ethnicityBox.Text.Trim();

            target.ParentName = _parentBox.Text.Trim();
            target.ParentPhone = _parentPhoneBox.Text.Trim();
            target.Address = _addressBox.Text.Trim();
            target.Remark = _remarkBox.Text.Trim();
            target.Photo = savedPhoto ?? "";

            Result = target;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}