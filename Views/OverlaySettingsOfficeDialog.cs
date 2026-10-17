using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using CourseApp.Controls;
using CourseApp.Localization;
using CourseApp.Models;
using CourseApp.Services;
using CourseApp.Theme;
using CourseApp.Views;

namespace CourseApp.Dialogs
{
    /// <summary>
    /// 办公模式专属的悬浮组件设置弹窗。
    /// 提供办公相关的模块选择。
    /// </summary>
    public class OverlaySettingsOfficeDialog : FlatDialogBase
    {
        private OverlayConfig _config;
        private FlatButton _btnIsland = null!;
        private FlatButton _btnSidebar = null!;
        private FlatButton _btnNews = null!;
        private FlatButton _btnImportExport = null!;

        private Panel _contentHost = null!;
        private int _currentGroup = 0;

        public OverlaySettingsOfficeDialog()
            : base(I18n.T("overlay.title"), 760, 640)
        {
            _config = OverlayConfigService.Load();
            ContentPanel.Padding = new Padding(0);
            BuildUI();
        }

        private void BuildUI()
        {
            // 底部按钮栏
            var bottomBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = AppTheme.Colors.WindowBg,
            };
            ContentPanel.Controls.Add(bottomBar);

            var btnCancel = new FlatButton
            {
                Text = I18n.T("dialog.cancel"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Width = 100, Height = 36,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            bottomBar.Controls.Add(btnCancel);

            var btnOk = new FlatButton
            {
                Text = I18n.T("dialog.save"),
                ButtonStyle = FlatButtonStyle.Primary,
                Width = 100, Height = 36,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            btnOk.Click += (s, e) => Save();
            bottomBar.Controls.Add(btnOk);

            bottomBar.Resize += (s, e) =>
            {
                btnCancel.Left = bottomBar.Width - btnCancel.Width - 20;
                btnCancel.Top = (bottomBar.Height - btnCancel.Height) / 2;
                btnOk.Left = btnCancel.Left - btnOk.Width - 12;
                btnOk.Top = btnCancel.Top;
            };

            // 左侧导航栏
            var navBar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 160,
                BackColor = AppTheme.Colors.WindowBg,
            };
            ContentPanel.Controls.Add(navBar);
            navBar.BringToFront();

            _btnIsland = MakeNavButton(I18n.T("overlay.island"), 16, navBar);
            _btnIsland.Click += (s, e) => SwitchGroup(0);

            _btnSidebar = MakeNavButton(I18n.T("overlay.sidebar"), 60, navBar);
            _btnSidebar.Click += (s, e) => SwitchGroup(1);

            _btnNews = MakeNavButton(I18n.T("overlay.news"), 104, navBar);
            _btnNews.Click += (s, e) => SwitchGroup(2);

            _btnImportExport = MakeNavButton(I18n.T("overlay.importExport"), 148, navBar);
            _btnImportExport.Click += (s, e) => SwitchGroup(3);

            // 右侧内容区
            _contentHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Colors.WindowBg,
                AutoScroll = true,
                Padding = new Padding(20, 16, 20, 16),
            };
            ContentPanel.Controls.Add(_contentHost);
            _contentHost.BringToFront();

            SwitchGroup(0);
        }

        private FlatButton MakeNavButton(string text, int top, Panel parent)
        {
            var btn = new FlatButton
            {
                Text = text,
                ButtonStyle = FlatButtonStyle.Subtle,
                Left = 8, Top = top,
                Width = 144, Height = 36,
            };
            parent.Controls.Add(btn);
            return btn;
        }

        private void SwitchGroup(int idx)
        {
            _currentGroup = idx;
            _btnIsland.ButtonStyle = idx == 0 ? FlatButtonStyle.Primary : FlatButtonStyle.Subtle;
            _btnSidebar.ButtonStyle = idx == 1 ? FlatButtonStyle.Primary : FlatButtonStyle.Subtle;
            _btnNews.ButtonStyle = idx == 2 ? FlatButtonStyle.Primary : FlatButtonStyle.Subtle;
            _btnImportExport.ButtonStyle = idx == 3 ? FlatButtonStyle.Primary : FlatButtonStyle.Subtle;

            _contentHost.Controls.Clear();
            _contentHost.Invalidate();

            switch (idx)
            {
                case 0: BuildIslandGroup(); break;
                case 1: BuildSidebarGroup(); break;
                case 2: BuildNewsGroup(); break;
                case 3: BuildImportExportGroup(); break;
            }
        }

        private void BuildIslandGroup()
        {
            int y = 0;
            y = AddGroupTitle(y, I18n.T("overlay.island"));
            y = AddToggleRow(y, I18n.T("overlay.enabled"), _config.IslandEnabled, v => { _config.IslandEnabled = v; });

            y = AddSectionLabel(y, I18n.T("overlay.mode"));
            y = AddChoiceRow(y, new[]
            {
                (I18n.T("overlay.mode.island"), "island"),
                (I18n.T("overlay.mode.notch"), "notch"),
                (I18n.T("overlay.mode.topbar"), "topbar"),
            }, _config.IslandMode, v => { _config.IslandMode = v; });

            y = AddNumberRow(y, I18n.T("overlay.width"), _config.IslandWidth, 160, 1200, v => { _config.IslandWidth = v; });
            y = AddNumberRow(y, I18n.T("overlay.height"), _config.IslandHeight, 24, 120, v => { _config.IslandHeight = v; });

            y = AddColorRow(y, I18n.T("overlay.bg"), _config.IslandBg, v => { _config.IslandBg = v; });
            y = AddColorRow(y, I18n.T("overlay.fg"), _config.IslandFg, v => { _config.IslandFg = v; });

            y = AddSectionLabel(y, I18n.T("overlay.modules"));
            y = AddModuleRow(y, _config.IslandModules);

            y = AddSectionLabel(y, I18n.T("overlay.preview"));
            AddPreview(y, true);
        }

        private void BuildSidebarGroup()
        {
            int y = 0;
            y = AddGroupTitle(y, I18n.T("overlay.sidebar"));
            y = AddToggleRow(y, I18n.T("overlay.enabled"), _config.SidebarEnabled, v => { _config.SidebarEnabled = v; });

            y = AddNumberRow(y, I18n.T("overlay.width"), _config.SidebarWidth, 20, 120, v => { _config.SidebarWidth = v; });
            y = AddNumberRow(y, I18n.T("overlay.height"), _config.SidebarHeight, 120, 1200, v => { _config.SidebarHeight = v; });

            y = AddColorRow(y, I18n.T("overlay.bg"), _config.SidebarBg, v => { _config.SidebarBg = v; });
            y = AddColorRow(y, I18n.T("overlay.fg"), _config.SidebarFg, v => { _config.SidebarFg = v; });

            y = AddSectionLabel(y, I18n.T("overlay.modules"));
            y = AddModuleRow(y, _config.SidebarModules);

            y = AddSectionLabel(y, I18n.T("overlay.preview"));
            AddPreview(y, false);
        }

        private void BuildNewsGroup()
        {
            int y = 0;
            y = AddGroupTitle(y, I18n.T("overlay.news"));
            y = AddTextRow(y, I18n.T("overlay.newsUrl"), _config.NewsUrl, v => _config.NewsUrl = v);
            y = AddNumberRow(y, I18n.T("overlay.newsInterval"), _config.NewsIntervalSec, 3, 120, v => _config.NewsIntervalSec = v);
        }

        private void BuildImportExportGroup()
        {
            int y = 0;
            y = AddGroupTitle(y, I18n.T("overlay.importExport"));

            var btnExport = new FlatButton
            {
                Text = I18n.T("overlay.export"),
                ButtonStyle = FlatButtonStyle.Primary,
                Left = 0, Top = y,
                Width = 160, Height = 36,
            };
            btnExport.Click += (s, e) => ExportConfig();
            _contentHost.Controls.Add(btnExport);
            y += 50;

            var btnImport = new FlatButton
            {
                Text = I18n.T("overlay.import"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Left = 0, Top = y,
                Width = 160, Height = 36,
            };
            btnImport.Click += (s, e) => ImportConfig();
            _contentHost.Controls.Add(btnImport);
        }

        private int AddGroupTitle(int y, string text)
        {
            var lbl = new Label
            {
                Text = text,
                Font = new Font(AppTheme.BodyFont.FontFamily, 18f, FontStyle.Bold),
                ForeColor = AppTheme.Colors.TextPrimary,
                Left = 0, Top = y,
                Width = _contentHost.Width - 40, Height = 36,
                BackColor = Color.Transparent,
                AutoSize = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            _contentHost.Controls.Add(lbl);
            return y + 46;
        }

        private int AddSectionLabel(int y, string text)
        {
            var lbl = new Label
            {
                Text = text,
                Font = new Font(AppTheme.BodyFont.FontFamily, 12f, FontStyle.Bold),
                ForeColor = AppTheme.Colors.TextPrimary,
                Left = 0, Top = y + 8,
                Width = _contentHost.Width - 40, Height = 22,
                BackColor = Color.Transparent,
                AutoSize = false,
            };
            _contentHost.Controls.Add(lbl);
            return y + 34;
        }

        private int AddToggleRow(int y, string label, bool value, Action<bool> onChanged)
        {
            var lbl = new Label
            {
                Text = label,
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.Colors.TextPrimary,
                Left = 0, Top = y + 8,
                Width = 200, Height = 22,
                BackColor = Color.Transparent,
                AutoSize = false,
            };
            _contentHost.Controls.Add(lbl);

            var toggle = new WinUI3Toggle
            {
                Left = 220, Top = y + 4,
                Checked = value,
            };
            toggle.CheckedChanged += (s, e) => onChanged(toggle.Checked);
            _contentHost.Controls.Add(toggle);

            return y + 42;
        }

        private int AddNumberRow(int y, string label, int value, int min, int max, Action<int> onChanged)
        {
            var lbl = new Label
            {
                Text = label,
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.Colors.TextPrimary,
                Left = 0, Top = y + 8,
                Width = 200, Height = 22,
                BackColor = Color.Transparent,
                AutoSize = false,
            };
            _contentHost.Controls.Add(lbl);

            var box = new FlatNumberBox
            {
                Left = 220, Top = y,
                Width = 120, Height = 32,
                Minimum = min, Maximum = max,
                Value = Math.Max(min, Math.Min(max, value)),
            };
            box.ValueChanged += (s, e) => onChanged(box.Value);
            _contentHost.Controls.Add(box);

            return y + 42;
        }

        private int AddTextRow(int y, string label, string value, Action<string> onChanged)
        {
            var lbl = new Label
            {
                Text = label,
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.Colors.TextPrimary,
                Left = 0, Top = y + 8,
                Width = 200, Height = 22,
                BackColor = Color.Transparent,
                AutoSize = false,
            };
            _contentHost.Controls.Add(lbl);

            var box = new FlatTextBox
            {
                Left = 220, Top = y,
                Width = _contentHost.Width - 260, Height = 32,
                Text = value ?? "",
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            box.TextChanged += (s, e) => onChanged(box.Text);
            _contentHost.Controls.Add(box);

            return y + 42;
        }

        private int AddColorRow(int y, string label, string hex, Action<string> onChanged)
        {
            var lbl = new Label
            {
                Text = label,
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.Colors.TextPrimary,
                Left = 0, Top = y + 8,
                Width = 200, Height = 22,
                BackColor = Color.Transparent,
                AutoSize = false,
            };
            _contentHost.Controls.Add(lbl);

            var preview = new Panel
            {
                Left = 220, Top = y,
                Width = 40, Height = 32,
                BackColor = OverlayRenderer.ParseColor(hex, Color.Gray),
            };
            preview.Paint += (s, e) =>
            {
                using var pen = new Pen(AppTheme.Colors.CardBorder, 1f);
                e.Graphics.DrawRectangle(pen, 0, 0, preview.Width - 1, preview.Height - 1);
            };
            _contentHost.Controls.Add(preview);

            var box = new FlatTextBox
            {
                Left = 268, Top = y,
                Width = 120, Height = 32,
                Text = hex ?? "#000000",
            };
            box.TextChanged += (s, e) =>
            {
                preview.BackColor = OverlayRenderer.ParseColor(box.Text, Color.Gray);
                onChanged(box.Text);
            };
            _contentHost.Controls.Add(box);

            return y + 42;
        }

        private int AddChoiceRow(int y, (string Label, string Value)[] choices, string current, Action<string> onChanged)
        {
            int x = 0;
            foreach (var (label, value) in choices)
            {
                var btn = new FlatButton
                {
                    Text = label,
                    ButtonStyle = value == current ? FlatButtonStyle.Primary : FlatButtonStyle.Secondary,
                    Left = x, Top = y,
                    Width = 100, Height = 32,
                };
                var v = value;
                btn.Click += (s, e) =>
                {
                    onChanged(v);
                    SwitchGroup(_currentGroup);
                };
                _contentHost.Controls.Add(btn);
                x += 110;
            }
            return y + 42;
        }

        private int AddModuleRow(int y, List<string> selected)
        {
            var hint = new Label
            {
                Text = I18n.T("overlay.modules.hint"),
                Font = AppTheme.SmallFont,
                ForeColor = AppTheme.Colors.TextSecondary,
                Left = 0, Top = y,
                Width = _contentHost.Width - 40, Height = 20,
                BackColor = Color.Transparent,
                AutoSize = false,
            };
            _contentHost.Controls.Add(hint);
            y += 24;

            int x = 0;
            foreach (var m in OverlayConfig.OfficeModules)
            {
                bool isSelected = selected.Contains(m);

                var btn = new FlatButton
                {
                    Text = OverlayRenderer.ModuleDisplayName(m),
                    ButtonStyle = isSelected ? FlatButtonStyle.Primary : FlatButtonStyle.Secondary,
                    Left = x, Top = y,
                    Width = 130, Height = 32,
                };
                var mod = m;
                btn.Click += (s, e) =>
                {
                    if (selected.Contains(mod)) selected.Remove(mod);
                    else selected.Add(mod);
                    SwitchGroup(_currentGroup);
                };
                _contentHost.Controls.Add(btn);
                x += 140;
            }

            return y + 42;
        }

        private void AddPreview(int y, bool isIsland)
        {
            var preview = new Panel
            {
                Left = 0, Top = y,
                Width = _contentHost.Width - 40,
                Height = isIsland ? 80 : 200,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            preview.Paint += (s, e) => DrawPreview(e.Graphics, preview.ClientRectangle, isIsland);
            _contentHost.Controls.Add(preview);
        }

        private void DrawPreview(Graphics g, Rectangle rect, bool isIsland)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

                        var ctx = new OverlayContext
            {
                Now = DateTime.Now,
                CurrentNews = "示例新闻标题",
                CurrentApp = "chrome",
                WeatherText = "晴 25°C",
                // 提供一些假课程防止没有数据时出错，但默认不勾选 Schedule 模块就不会显示
                Courses = new List<Course> 
                {
                    new Course { Name = "语文", TimeStart = 1, TimeEnd = 1, WeekDay = 1, Weeks = new List<int> { 1 } }
                },
                Sections = new List<SectionTime> 
                {
                    new SectionTime { Type = "normal", Section = 1, StartTime = "08:00", EndTime = "08:45" }
                },
                CurrentWeek = 1,
                Schedules = new List<ScheduleItem>
                {
                    new ScheduleItem { Title = "写周报", IsTodo = true, Done = false, Date = DateTime.Today.ToString("yyyy-MM-dd") },
                    new ScheduleItem { Title = "项目会议", StartTime = "09:00", EndTime = "10:00", Date = DateTime.Today.ToString("yyyy-MM-dd"), IsTodo = false }
                }
            };

            if (isIsland)
            {
                Color bg = OverlayRenderer.ParseColor(_config.IslandBg, Color.FromArgb(0x1A, 0x1A, 0x1A));
                Color fg = OverlayRenderer.ParseColor(_config.IslandFg, Color.FromArgb(0xF0, 0xF0, 0xF0));

                int w = Math.Min(rect.Width - 20, _config.IslandWidth);
                int h = Math.Max(24, _config.IslandHeight);
                int x = (rect.Width - w) / 2;
                int y = (rect.Height - h) / 2;

                var islandRect = new Rectangle(x, y, w, h);

                using (var path = GraphicsExtensions.GetRoundPath(islandRect, h / 2))
                using (var brush = new SolidBrush(bg))
                    g.FillPath(brush, path);

                var parts = _config.IslandModules
                    .Select(m => OverlayRenderer.RenderText(m, ctx))
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();

                string text = string.Join("  ·  ", parts);
                if (string.IsNullOrEmpty(text)) text = "预览";

                var textRect = new Rectangle(islandRect.X + 16, islandRect.Y, islandRect.Width - 32, islandRect.Height);
                TextRenderer.DrawText(g, text, AppTheme.SmallFont, textRect, fg,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
            else
            {
                Color bg = OverlayRenderer.ParseColor(_config.SidebarBg, Color.White);
                Color fg = OverlayRenderer.ParseColor(_config.SidebarFg, Color.Black);

                int w = Math.Max(20, _config.SidebarWidth);
                int h = Math.Max(120, _config.SidebarHeight);
                if (h > rect.Height) h = rect.Height;

                int x = (rect.Width - w) / 2;
                int y = (rect.Height - h) / 2;

                var barRect = new Rectangle(x, y, w, h);

                using (var path = GraphicsExtensions.GetRoundPath(barRect, 6))
                using (var brush = new SolidBrush(bg))
                    g.FillPath(brush, path);

                int cy = barRect.Y + 8;
                foreach (var m in _config.SidebarModules)
                {
                    string icon = m switch
                    {
                        "News" => "闻",
                        "OfficeSchedule" => "程",
                        "OfficeTodo" => "办",
                        "Weather" => "天",
                        "App" => "应",
                        _ => "·",
                    };

                    using var font = new Font(AppTheme.BodyFont.FontFamily, 9f, FontStyle.Bold);
                    var iconRect = new Rectangle(barRect.X, cy, barRect.Width, 20);
                    TextRenderer.DrawText(g, icon, font, iconRect, fg,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                        TextFormatFlags.NoPrefix);
                    cy += 26;

                    if (cy > barRect.Bottom - 20) break;
                }
            }
        }

        private void Save()
        {
            OverlayConfigService.Save(_config);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ExportConfig()
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "JSON|*.json",
                FileName = "overlay.json",
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;

            if (OverlayConfigService.Export(sfd.FileName, _config))
            {
                MessageDialog.ShowInfo(I18n.T("common.success"), I18n.T("overlay.exported"));
            }
        }

        private void ImportConfig()
        {
            using var ofd = new OpenFileDialog { Filter = "JSON|*.json" };
            if (ofd.ShowDialog() != DialogResult.OK) return;

            var imported = OverlayConfigService.Import(ofd.FileName);
            if (imported == null)
            {
                MessageDialog.ShowError(I18n.T("common.error"), I18n.T("overlay.importFailed"));
                return;
            }

            _config = imported;
            OverlayConfigService.Save(_config);

            MessageDialog.ShowInfo(I18n.T("common.success"), I18n.T("overlay.imported"));
            SwitchGroup(_currentGroup);
        }
    }
}