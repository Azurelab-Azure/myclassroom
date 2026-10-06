using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using CourseApp.Controls;
using CourseApp.Localization;
using CourseApp.Models;
using CourseApp.Theme;

namespace CourseApp.Dialogs
{
    /// <summary>
    /// 教师详情弹窗。
    /// </summary>
    public class TeacherDetailDialog : FlatDialogBase
    {
        private readonly Teacher _teacher;
        private Image? _photo;

        public TeacherDetailDialog(Teacher teacher)
            : base(I18n.T("teacher.detail"), 480, 620)
        {
            _teacher = teacher ?? throw new ArgumentNullException(nameof(teacher));
            LoadPhoto();
            BuildUI();
        }

        private void LoadPhoto()
        {
            try
            {
                if (string.IsNullOrEmpty(_teacher.Photo)) return;
                if (!File.Exists(_teacher.Photo)) return;
                using var fs = new FileStream(_teacher.Photo, FileMode.Open, FileAccess.Read, FileShare.Read);
                _photo = Image.FromStream(fs);
            }
            catch { }
        }

        private void BuildUI()
        {
            int x = 20;
            int y = 10;

            // ---------- 头像 ----------
            var avatar = new PhotoAvatar
            {
                Left = x, Top = y,
                Width = 120, Height = 120,
                Image = _photo,
                Initial = string.IsNullOrEmpty(_teacher.Name) ? "?" : _teacher.Name.Substring(0, 1),
            };
            ContentPanel.Controls.Add(avatar);

            // ---------- 姓名 ----------
            ContentPanel.Controls.Add(new Label
            {
                Text = _teacher.Name ?? "",
                Font = new Font(AppTheme.BodyFont.FontFamily, 20f, FontStyle.Bold),
                ForeColor = AppTheme.Colors.TextPrimary,
                Left = x + 140, Top = y + 10,
                Width = ClientSize.Width - x - 160,
                Height = 36,
                BackColor = Color.Transparent,
                AutoSize = false,
            });

            // ---------- 学科 · 职称 ----------
            string sub = "";
            if (!string.IsNullOrEmpty(_teacher.Subject)) sub += _teacher.Subject;
            if (!string.IsNullOrEmpty(_teacher.Title))
                sub += (sub.Length > 0 ? " · " : "") + _teacher.Title;

            ContentPanel.Controls.Add(new Label
            {
                Text = sub,
                Font = new Font(AppTheme.BodyFont.FontFamily, 12f),
                ForeColor = AppTheme.Colors.Accent,
                Left = x + 140, Top = y + 52,
                Width = ClientSize.Width - x - 160,
                Height = 24,
                BackColor = Color.Transparent,
                AutoSize = false,
            });

            y += 140;

            // ---------- 详情行 ----------
            y = AddDetailRow(I18n.T("teacher.field.phone"), _teacher.Phone, x, y);
            y = AddDetailRow(I18n.T("teacher.field.email"), _teacher.Email, x, y);
            y = AddDetailRow(I18n.T("teacher.field.office"), _teacher.Office, x, y);
            y = AddDetailRow(I18n.T("teacher.field.info"), _teacher.Info, x, y);
            y = AddDetailRow(I18n.T("teacher.field.remark"), _teacher.Remark, x, y);

            // ---------- 关闭按钮 ----------
            var btnClose = new FlatButton
            {
                Text = I18n.T("common.close"),
                ButtonStyle = FlatButtonStyle.Primary,
                Width = 100, Height = 36,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            btnClose.Click += (s, e) => Close();
            ContentPanel.Controls.Add(btnClose);

            ContentPanel.Resize += (s, e) =>
            {
                btnClose.Left = ContentPanel.Width - btnClose.Width - 20;
                btnClose.Top = ContentPanel.Height - btnClose.Height - 20;
            };
        }

        private int AddDetailRow(string label, string? value, int x, int y)
        {
            ContentPanel.Controls.Add(new Label
            {
                Text = label,
                Font = AppTheme.SmallFont,
                ForeColor = AppTheme.Colors.TextSecondary,
                Left = x, Top = y,
                Width = 80, Height = 22,
                BackColor = Color.Transparent,
                AutoSize = false,
            });

            ContentPanel.Controls.Add(new Label
            {
                Text = string.IsNullOrEmpty(value) ? "—" : value,
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.Colors.TextPrimary,
                Left = x + 88, Top = y,
                Width = ClientSize.Width - x - 120,
                Height = 22,
                BackColor = Color.Transparent,
                AutoSize = false,
            });

            return y + 30;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _photo?.Dispose();
                _photo = null;
            }
            base.Dispose(disposing);
        }

        // =====================================================
        // 头像控件
        // =====================================================
        private class PhotoAvatar : Panel
        {
            public Image? Image;
            public string Initial = "?";

            public PhotoAvatar()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.ResizeRedraw, true);
                BackColor = Color.Transparent;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                var rect = new Rectangle(0, 0, Width - 1, Height - 1);
                using var path = GraphicsExtensions.GetRoundPath(rect, 12);

                if (Image != null)
                {
                    var old = g.Clip;
                    g.SetClip(path);
                    try
                    {
                        float scale = Math.Max((float)rect.Width / Image.Width,
                                               (float)rect.Height / Image.Height);
                        int w = (int)(Image.Width * scale);
                        int h = (int)(Image.Height * scale);
                        int x = (rect.Width - w) / 2;
                        int y = (rect.Height - h) / 2;
                        g.DrawImage(Image, new Rectangle(x, y, w, h));
                    }
                    finally { g.Clip = old; }
                }
                else
                {
                    using var bg = new SolidBrush(AppTheme.Colors.HoverBg);
                    g.FillPath(bg, path);

                    using var font = new Font(AppTheme.BodyFont.FontFamily,
                        rect.Height * 0.4f, FontStyle.Bold);
                    TextRenderer.DrawText(g, Initial, font, rect,
                        AppTheme.Colors.TextSecondary,
                        TextFormatFlags.HorizontalCenter |
                        TextFormatFlags.VerticalCenter |
                        TextFormatFlags.NoPrefix);
                }
            }
        }
    }
}