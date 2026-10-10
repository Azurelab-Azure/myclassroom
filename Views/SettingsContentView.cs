using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using CourseApp.Controls;
using CourseApp.Dialogs;
using CourseApp.Localization;
using CourseApp.Services;
using CourseApp.Theme;

namespace CourseApp.Views
{
    /// <summary>
    /// 设置内容区：7 个子页，SetPage 切换。
    /// 顺序：系统 / 个性化 / 应用 / 悬浮组件 / 时间和语言 / 关于 / 危险区。
    /// 主题 / 语言切换后强制重启软件。
    /// </summary>
    public class SettingsContentView : Panel
    {
        /// <summary>主窗口引用</summary>
        private readonly Form1 _owner;

        /// <summary>子页宿主</summary>
        private Panel _host = null!;

        /// <summary>子页数组</summary>
        private Panel[] _pages = null!;

        /// <summary>当前子页索引</summary>
        private int _currentIndex = -1;

        /// <summary>普通行高度</summary>
        private const int RowH = 72;

        /// <summary>堆叠行底部留白</summary>
        private const int StackPadH = 16;

        /// <summary>卡片底部留白</summary>
        private const int CardPadBottom = 20;

        /// <summary>
        /// 构建设置内容区。
        /// </summary>
        /// <param name="owner">主窗口</param>
        public SettingsContentView(Form1 owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Colors.WindowBg;

            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            _host = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Colors.WindowBg,
            };
            Controls.Add(_host);

            BuildPages();
        }

        /// <summary>
        /// 切换子页。
        /// </summary>
        /// <param name="index">子页索引</param>
        public void SetPage(int index)
        {
            if (index < 0 || index >= _pages.Length) return;
            if (_currentIndex == index) return;
            _currentIndex = index;

            _host.SuspendLayout();
            for (int i = 0; i < _pages.Length; i++)
                _pages[i].Visible = (i == index);
            _host.ResumeLayout();
        }

        // =====================================================
        // 7 个子页
        // =====================================================
        /// <summary>
        /// 构建所有子页。
        /// </summary>
        private void BuildPages()
        {
            _pages = new[]
            {
                BuildSystemPage(),
                BuildPersonalizationPage(),
                BuildAppsPage(),
                BuildOverlayPage(),
                BuildTimeLanguagePage(),
                BuildAboutPage(),
                BuildDangerPage(),
            };

            _host.SuspendLayout();
            foreach (var p in _pages)
            {
                p.Dock = DockStyle.Fill;
                p.Visible = false;
                _host.Controls.Add(p);
            }
            _host.ResumeLayout();
        }

        // =====================================================
        // 0. 系统
        // =====================================================
        /// <summary>
        /// 构建"系统"页。
        /// </summary>
        private Panel BuildSystemPage()
        {
            var page = NewPage(
                I18n.T("settings.section.system"),
                I18n.T("settings.section.system.desc"));

            AddCard(page,
                I18n.T("settings.system.dataStore"),
                I18n.T("settings.system.dataStore.desc"), new[]
            {
                Row.Button(Icons.Import,
                    I18n.T("settings.system.openDataDir"),
                    I18n.T("settings.system.openDataDir.desc"),
                    I18n.T("settings.system.open"), OpenDataDir),
            });

            AddCard(page,
                I18n.T("settings.system.backup"),
                I18n.T("settings.system.backup.desc"), new[]
            {
                Row.Button(Icons.Save,
                    I18n.T("settings.system.backupData"),
                    I18n.T("settings.system.backupData.desc"),
                    I18n.T("settings.system.backupBtn"), BackupData),
                Row.Button(Icons.Import,
                    I18n.T("settings.system.restoreData"),
                    I18n.T("settings.system.restoreData.desc"),
                    I18n.T("settings.system.restoreBtn"), RestoreData),
            });

            return page;
        }

        // =====================================================
        // 1. 个性化
        // =====================================================
        /// <summary>
        /// 构建"个性化"页。
        /// </summary>
        private Panel BuildPersonalizationPage()
        {
            var page = NewPage(
                I18n.T("settings.section.personalization"),
                I18n.T("settings.section.personalization.desc"));

            AddCard(page,
                I18n.T("settings.personalization.mode"),
                I18n.T("settings.personalization.mode.desc"), new[]
            {
                Row.Stack(
                    I18n.T("settings.personalization.mode.label"),
                    I18n.T("settings.personalization.mode.desc2"),
                    () =>
                {
                    var sel = new CardSelector();
                    sel.Width = 5 * 128 + 4 * 8;

                    sel.SetItems(new[]
                    {
                        new CardSelector.Item
                        {
                            Text = I18n.T("settings.theme.light"),
                            PreviewDrawer = ThemePreviewDrawer("light"),
                        },
                        new CardSelector.Item
                        {
                            Text = I18n.T("settings.theme.dark"),
                            PreviewDrawer = ThemePreviewDrawer("dark"),
                        },
                        new CardSelector.Item
                        {
                            Text = I18n.T("settings.theme.clear"),
                            PreviewDrawer = ThemePreviewDrawer("clear"),
                        },
                        new CardSelector.Item
                        {
                            Text = I18n.T("settings.theme.fresh"),
                            PreviewDrawer = ThemePreviewDrawer("fresh"),
                        },
                        new CardSelector.Item
                        {
                            Text = I18n.T("settings.theme.system"),
                            PreviewDrawer = ThemePreviewDrawer("system"),
                        },
                    });
                    sel.SelectedIndex = GetThemeIndex();
                    sel.SelectedIndexChanged += (s, e) => ApplyTheme(sel.SelectedIndex);
                    return sel;
                }),
            });

            return page;
        }

        // =====================================================
        // 2. 应用
        // =====================================================
        /// <summary>
        /// 构建"应用"页。
        /// </summary>
        private Panel BuildAppsPage()
        {
            var page = NewPage(
                I18n.T("settings.section.apps"),
                I18n.T("settings.section.apps.desc"));

            AddCard(page,
                I18n.T("settings.apps.courseData"),
                I18n.T("settings.apps.courseData.desc"), new[]
            {
                Row.Button(Icons.Import,
                    I18n.T("settings.apps.import"),
                    I18n.T("settings.apps.import.desc"),
                    I18n.T("settings.apps.importBtn"), ImportCsv),
                Row.Button(Icons.Export,
                    I18n.T("settings.apps.export"),
                    I18n.T("settings.apps.export.desc"),
                    I18n.T("settings.apps.exportBtn"), ExportCsv),
                Row.Button(Icons.Save,
                    I18n.T("settings.apps.save"),
                    I18n.T("settings.apps.save.desc"),
                    I18n.T("settings.apps.saveBtn"), SaveAllData),
            });

            return page;
        }

        // =====================================================
        // 3. 悬浮组件
        // =====================================================
        /// <summary>
        /// 构建"悬浮组件"页。
        /// </summary>
        private Panel BuildOverlayPage()
        {
            var page = NewPage(
                I18n.T("settings.section.overlay"),
                I18n.T("settings.section.overlay.desc"));

            AddCard(page,
                I18n.T("overlay.title"),
                I18n.T("overlay.desc"), new[]
            {
                Row.Button(Icons.Settings,
                    I18n.T("overlay.openSettings"),
                    I18n.T("overlay.openSettings.desc"),
                    I18n.T("overlay.openSettingsBtn"), OpenOverlaySettings),
            });

            return page;
        }

        /// <summary>
        /// 打开悬浮组件设置弹窗。
        /// </summary>
        private void OpenOverlaySettings()
        {
            using var dlg = new OverlaySettingsDialog();
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _owner.ApplyIslandConfig();
                _owner.ApplyTodaySidebarConfig();
            }
        }

        // =====================================================
        // 4. 时间和语言
        // =====================================================
        /// <summary>
        /// 构建"时间和语言"页。
        /// </summary>
        private Panel BuildTimeLanguagePage()
        {
            var page = NewPage(
                I18n.T("settings.section.timelanguage"),
                I18n.T("settings.section.timelanguage.desc"));

            AddCard(page,
                I18n.T("settings.lang.language"),
                I18n.T("settings.lang.language.desc"), new[]
            {
                Row.Custom(
                    I18n.T("settings.lang.language.label"),
                    I18n.T("settings.lang.language.desc2"),
                    () =>
                {
                    var box = new FlatComboBox
                    {
                        Width = 360,
                        Height = 32,
                    };

                    var items = new List<IFlatComboItem>();
                    var langs = LangService.GetAll();

                    int selectedIndex = 0;
                    for (int i = 0; i < langs.Count; i++)
                    {
                        var l = langs[i];

                        string display = l.Name;
                        if (!string.IsNullOrEmpty(l.NativeName) && l.NativeName != l.Name)
                            display += " / " + l.NativeName;
                        display += "  (" + l.Code + ")";

                        items.Add(new FlatTextItem(display));

                        if (l.Code == I18n.CurrentLang)
                            selectedIndex = i;
                    }

                    box.SetItems(items);
                    box.SelectedIndex = selectedIndex;
                    box.SelectedIndexChanged += (s, e) =>
                    {
                        if (box.SelectedIndex < 0) return;
                        var selected = LangService.GetAll()[box.SelectedIndex];
                        ApplyLanguage(selected.Code);
                    };
                    return box;
                }),
            });

            AddCard(page,
                I18n.T("settings.lang.section"),
                I18n.T("settings.lang.section.desc"), new[]
            {
                Row.Button(Icons.Calendar,
                    I18n.T("settings.lang.editSection"),
                    I18n.T("settings.lang.editSection.desc"),
                    I18n.T("settings.lang.editSectionBtn"), EditSectionTime),
            });

            return page;
        }

        // =====================================================
        // 5. 关于
        // =====================================================
        /// <summary>
        /// 构建"关于"页。
        /// </summary>
        private Panel BuildAboutPage()
        {
            var page = NewPage(
                I18n.T("settings.section.about"),
                I18n.T("settings.section.about.desc"));

            AddCard(page,
                I18n.T("settings.about.section"),
                I18n.T("settings.about.section.desc"), new[]
            {
                Row.Button(Icons.Info,
                    I18n.T("settings.about.view"),
                    I18n.T("settings.about.view.desc"),
                    I18n.T("settings.about.viewBtn"), ShowAbout),
            });

            AddCard(page,
                I18n.T("settings.update"),
                I18n.T("settings.update.desc"), new[]
            {
                Row.Stack(
                    I18n.T("settings.update.autoCheck"),
                    I18n.T("settings.update.autoCheckDesc"),
                    () =>
                {
                    var sel = new CardSelector();
                    sel.Width = 2 * 128 + 8;
                    sel.SetItems(new[]
                    {
                        new CardSelector.Item { Text = I18n.T("common.show") },
                        new CardSelector.Item { Text = I18n.T("common.hide") },
                    });
                    sel.SelectedIndex = ConfigService.Load().AutoCheckUpdate ? 0 : 1;
                    sel.SelectedIndexChanged += (s, e) =>
                    {
                        var cfg = ConfigService.Load();
                        cfg.AutoCheckUpdate = (sel.SelectedIndex == 0);
                        ConfigService.Save(cfg);
                    };
                    return sel;
                }),

                Row.Button(Icons.Refresh,
                    I18n.T("settings.update.checkNow"),
                    I18n.T("settings.update.checkNowDesc"),
                    I18n.T("settings.update.checkBtn"), () => CheckUpdateNow()),
            });

            return page;
        }

        // =====================================================
        // 6. 危险区
        // =====================================================
        /// <summary>
        /// 构建"危险区"页。
        /// </summary>
        private Panel BuildDangerPage()
        {
            var page = NewPage(
                I18n.T("settings.section.danger"),
                I18n.T("settings.section.danger.desc"));

            AddCard(page,
                I18n.T("settings.danger.clear"),
                I18n.T("settings.danger.clear.desc"), new[]
            {
                Row.Button(Icons.Delete,
                    I18n.T("settings.danger.clearBtn"),
                    I18n.T("settings.danger.clearBtn.desc"),
                    I18n.T("settings.danger.clearBtn.btn"), ClearCourses, danger: true),
            }, danger: true);

            return page;
        }

        // =====================================================
        // 预览图工厂
        // =====================================================
        /// <summary>
        /// 主题预览绘制器。
        /// </summary>
        private static Action<Graphics, Rectangle> ThemePreviewDrawer(string themeName)
        {
            return (g, rect) =>
            {
                ThemeColors tc = themeName switch
                {
                    "dark" => ThemeColors.Dark(),
                    "clear" => ThemeColors.Clear(),
                    "fresh" => ThemeColors.Fresh(),
                    "system" => ThemeColors.Light(),
                    _ => ThemeColors.Light(),
                };

                g.SmoothingMode = SmoothingMode.AntiAlias;

                using (var path = GraphicsExtensions.GetRoundPath(rect, 6))
                using (var brush = new SolidBrush(tc.WindowBg))
                    g.FillPath(brush, path);

                var topBar = new Rectangle(rect.X, rect.Y, rect.Width, 14);
                using (var brush = new SolidBrush(tc.CardBg))
                {
                    using var clip = GraphicsExtensions.GetRoundPath(rect, 6);
                    var old = g.Clip;
                    g.SetClip(clip);
                    g.FillRectangle(brush, topBar);
                    g.Clip = old;
                }

                var sidebar = new Rectangle(rect.X, rect.Y + 14, 14, rect.Height - 14);
                using (var brush = new SolidBrush(tc.CardBg))
                {
                    using var clip = GraphicsExtensions.GetRoundPath(rect, 6);
                    var old = g.Clip;
                    g.SetClip(clip);
                    g.FillRectangle(brush, sidebar);
                    g.Clip = old;
                }

                var block = new Rectangle(rect.X + 24, rect.Y + 26, 30, 20);
                using (var brush = new SolidBrush(tc.Accent))
                using (var path = GraphicsExtensions.GetRoundPath(block, 3))
                    g.FillPath(brush, path);

                var line = new Rectangle(rect.X + 60, rect.Y + 32, rect.Width - 72, 4);
                using (var brush = new SolidBrush(tc.TextPrimary))
                using (var path = GraphicsExtensions.GetRoundPath(line, 2))
                    g.FillPath(brush, path);

                using (var path = GraphicsExtensions.GetRoundPath(
                    new Rectangle(rect.X, rect.Y, rect.Width - 1, rect.Height - 1), 6))
                using (var pen = new Pen(tc.CardBorder, 1f))
                    g.DrawPath(pen, path);
            };
        }

        // =====================================================
        // 逻辑
        // =====================================================
        /// <summary>
        /// 读取当前主题索引。
        /// </summary>
        private int GetThemeIndex()
        {
            var t = (ConfigService.Load().Theme ?? "light").ToLowerInvariant();
            return t switch
            {
                "dark" => 1,
                "clear" => 2,
                "fresh" => 3,
                "system" => 4,
                _ => 0,
            };
        }

        /// <summary>
        /// 应用主题，保存后重启。
        /// </summary>
        private void ApplyTheme(int idx)
        {
            var cfg = ConfigService.Load();
            switch (idx)
            {
                case 0: cfg.Theme = "light";  break;
                case 1: cfg.Theme = "dark";   break;
                case 2: cfg.Theme = "clear";  break;
                case 3: cfg.Theme = "fresh";  break;
                case 4: cfg.Theme = "system"; break;
            }
            ConfigService.Save(cfg);

            AskRestart();
        }

        /// <summary>
        /// 应用语言，保存后重启。
        /// </summary>
        private void ApplyLanguage(string langCode)
        {
            if (string.IsNullOrEmpty(langCode)) return;
            if (langCode == I18n.CurrentLang) return;

            var langFile = AppPaths.LangFile(langCode);
            if (!System.IO.File.Exists(langFile))
            {
                MessageDialog.ShowError(I18n.T("common.error"),
                    "找不到语言文件：\n" + langFile);
                return;
            }

            var cfg = ConfigService.Load();
            cfg.Language = langCode;
            ConfigService.Save(cfg);

            AskRestart();
        }

        /// <summary>
        /// 询问重启。
        /// </summary>
        private void AskRestart()
        {
            var owner = _owner;
            if (owner == null || owner.IsDisposed) return;

            MessageDialog.ShowInfo(
                I18n.T("common.info"),
                I18n.T("msg.needRestart"));

            owner.Restart();
        }

        /// <summary>
        /// 检查更新。
        /// </summary>
        private async void CheckUpdateNow()
        {
            try
            {
                var release = await UpdateService.CheckForUpdateAsync();

                if (release == null)
                {
                    MessageDialog.ShowInfo(I18n.T("common.info"), I18n.T("update.upToDate"));
                    return;
                }

                using var dlg = new UpdateDialog(release);
                dlg.ShowDialog(this);
            }
            catch
            {
                MessageDialog.ShowError(I18n.T("common.error"), I18n.T("update.checkFailed"));
            }
        }

        // =====================================================
        // 页面 / 卡片
        // =====================================================
        /// <summary>
        /// 创建新页面。
        /// </summary>
        private Panel NewPage(string title, string desc)
        {
            var page = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Colors.WindowBg,
                AutoScroll = true,
                Padding = new Padding(32, 24, 32, 24),
            };

            page.Controls.Add(new Label
            {
                Text = title,
                Font = new Font(AppTheme.BodyFont.FontFamily, 22f, FontStyle.Bold),
                ForeColor = AppTheme.Colors.TextPrimary,
                Left = 32, Top = 24, Width = 600, Height = 40,
                BackColor = Color.Transparent,
                AutoSize = false,
            });
            page.Controls.Add(new Label
            {
                Text = desc,
                Font = new Font(AppTheme.BodyFont.FontFamily, 10f),
                ForeColor = AppTheme.Colors.TextSecondary,
                Left = 32, Top = 66, Width = 600, Height = 22,
                BackColor = Color.Transparent,
                AutoSize = false,
            });
            return page;
        }

        /// <summary>
        /// 向页面添加卡片。
        /// </summary>
        private Panel AddCard(Panel page, string title, string desc, Row[] rows, bool danger = false)
        {
            int top = 110;
            foreach (Control c in page.Controls)
                if (c is Panel p && (p.Tag as string ?? "").EndsWith("card"))
                    top = Math.Max(top, p.Bottom + 20);

            int cardW = Math.Max(400, page.Width - 64);

            var card = new Panel
            {
                Left = 32, Top = top,
                Width = cardW,
                Height = 100,
                BackColor = Color.Transparent,
                Tag = danger ? "danger-card" : "card",
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            card.Paint += Card_Paint;
            page.Controls.Add(card);

            card.Controls.Add(new Label
            {
                Text = title,
                Font = new Font(AppTheme.BodyFont.FontFamily, 12f, FontStyle.Bold),
                ForeColor = AppTheme.Colors.TextPrimary,
                Left = 20, Top = 14, Width = cardW - 40, Height = 24,
                BackColor = Color.Transparent,
                AutoSize = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            });

            card.Controls.Add(new Label
            {
                Text = desc,
                Font = AppTheme.SmallFont,
                ForeColor = AppTheme.Colors.TextSecondary,
                Left = 20, Top = 36, Width = cardW - 40, Height = 18,
                BackColor = Color.Transparent,
                AutoSize = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            });

            int innerW = cardW - 40;
            int y = 60;

            for (int i = 0; i < rows.Length; i++)
            {
                int rowH = AddRowAt(card, y, innerW, rows[i], i > 0);
                y += rowH;
            }

            card.Height = y + CardPadBottom;
            return card;
        }

        /// <summary>
        /// 添加一行。
        /// </summary>
        private int AddRowAt(Panel card, int y, int innerW, Row row, bool withDivider)
        {
            if (withDivider)
            {
                card.Controls.Add(new Panel
                {
                    Left = 20, Top = y,
                    Width = innerW, Height = 1,
                    BackColor = AppTheme.Colors.Divider,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                });
            }

            // 堆叠行
            if (row.IsStacked)
            {
                card.Controls.Add(new Label
                {
                    Text = row.Title,
                    Font = AppTheme.BodyFont,
                    ForeColor = AppTheme.Colors.TextPrimary,
                    Left = 20, Top = y + 12, Width = innerW - 40, Height = 22,
                    BackColor = Color.Transparent,
                    AutoSize = false,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                });

                int ctrlTop = y + 38;
                if (!string.IsNullOrEmpty(row.Desc))
                {
                    card.Controls.Add(new Label
                    {
                        Text = row.Desc,
                        Font = AppTheme.SmallFont,
                        ForeColor = AppTheme.Colors.TextSecondary,
                        Left = 20, Top = y + 34, Width = innerW - 40, Height = 18,
                        BackColor = Color.Transparent,
                        AutoSize = false,
                        Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    });
                    ctrlTop = y + 58;
                }

                var ctrl = row.ControlFactory?.Invoke();
                if (ctrl != null)
                {
                    ctrl.Left = 20;
                    ctrl.Top = ctrlTop;
                    ctrl.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                    card.Controls.Add(ctrl);

                    return (ctrlTop - y) + ctrl.Height + StackPadH;
                }

                return ctrlTop - y + StackPadH;
            }

            // 普通行
            card.Controls.Add(new Label
            {
                Text = row.Title,
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.Colors.TextPrimary,
                Left = 20, Top = y + 14, Width = innerW - 400, Height = 22,
                BackColor = Color.Transparent,
                AutoSize = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            });

            card.Controls.Add(new Label
            {
                Text = row.Desc,
                Font = AppTheme.SmallFont,
                ForeColor = AppTheme.Colors.TextSecondary,
                Left = 20, Top = y + 38, Width = innerW - 400, Height = 18,
                BackColor = Color.Transparent,
                AutoSize = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            });

            var ctrl2 = row.ControlFactory?.Invoke();
            if (ctrl2 != null)
            {
                ctrl2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                ctrl2.Top = y + (RowH - ctrl2.Height) / 2;
                ctrl2.Left = card.Width - 20 - ctrl2.Width;
                card.Controls.Add(ctrl2);
            }

            return RowH;
        }

        /// <summary>
        /// 卡片绘制。
        /// </summary>
        private void Card_Paint(object? sender, PaintEventArgs e)
        {
            var panel = sender as Panel;
            if (panel == null || panel.Width <= 2 || panel.Height <= 2) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            bool danger = (panel.Tag as string) == "danger-card";
            var colors = AppTheme.Colors;

            var rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
            using var path = GraphicsExtensions.GetRoundPath(rect, 8);

            using (var bg = new SolidBrush(colors.CardBg))
                g.FillPath(bg, path);

            Color border = danger ? colors.Danger : colors.CardBorder;
            using (var pen = new Pen(border, 1f))
                g.DrawPath(pen, path);
        }

        // =====================================================
        // Row
        // =====================================================
        /// <summary>
        /// 设置行数据。
        /// </summary>
        private class Row
        {
            /// <summary>标题</summary>
            public string Title = "";

            /// <summary>描述</summary>
            public string Desc = "";

            /// <summary>是否堆叠布局</summary>
            public bool IsStacked = false;

            /// <summary>控件工厂</summary>
            public Func<Control?> ControlFactory = null!;

            /// <summary>
            /// 普通按钮行。
            /// </summary>
            public static Row Button(string icon, string title, string desc,
                string btnText, Action onClick, bool danger = false)
            {
                return new Row
                {
                    Title = title, Desc = desc,
                    ControlFactory = () =>
                    {
                        var btn = new FlatButton
                        {
                            Text = btnText,
                            ButtonStyle = danger ? FlatButtonStyle.Danger : FlatButtonStyle.Secondary,
                            Width = 88, Height = 32,
                        };
                        btn.Click += (s, e) => onClick();
                        return btn;
                    },
                };
            }

            /// <summary>
            /// 数字行。
            /// </summary>
            public static Row Number(string title, string desc,
                int value, int min, int max, Action<int> onChange)
            {
                return new Row
                {
                    Title = title, Desc = desc,
                    ControlFactory = () =>
                    {
                        var box = new FlatNumberBox
                        {
                            Width = 120, Height = 32,
                            Minimum = min, Maximum = max,
                            Value = Math.Max(min, Math.Min(max, value)),
                        };
                        box.ValueChanged += (s, e) => onChange(box.Value);
                        return box;
                    },
                };
            }

            /// <summary>
            /// 堆叠行。
            /// </summary>
            public static Row Stack(string title, string desc, Func<Control?> factory)
            {
                return new Row
                {
                    Title = title, Desc = desc,
                    IsStacked = true,
                    ControlFactory = factory,
                };
            }

            /// <summary>
            /// 堆叠文本行。
            /// </summary>
            public static Row StackText(string title, string desc,
                string value, Action<string> onChanged)
            {
                return new Row
                {
                    Title = title, Desc = desc,
                    IsStacked = true,
                    ControlFactory = () =>
                    {
                        var box = new FlatTextBox
                        {
                            Width = 400, Height = 32,
                            Text = value ?? "",
                        };
                        box.TextChanged += (s, e) => onChanged(box.Text);
                        return box;
                    },
                };
            }

            /// <summary>
            /// 自定义行。
            /// </summary>
            public static Row Custom(string title, string desc, Func<Control?> factory)
            {
                return new Row
                {
                    Title = title, Desc = desc,
                    ControlFactory = factory,
                };
            }
        }

        // =====================================================
        // 动作
        // =====================================================
        /// <summary>
        /// 打开数据目录。
        /// </summary>
        private void OpenDataDir()
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = AppPaths.BaseDir,
                    UseShellExecute = true,
                });
            }
            catch (Exception ex) { MessageDialog.ShowError(I18n.T("common.error"), ex.Message); }
        }

        /// <summary>
        /// 备份数据。
        /// </summary>
        private void BackupData()
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string folder = Path.Combine(desktop,
                    "CourseApp-Backup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
                Directory.CreateDirectory(folder);
                foreach (var f in new[] { "courses.json", "sections.json", "teachers.json", "config.json", "students.json", "duty.json", "exams.json", "seats.json", "committees.json", "representatives.json", "scores.json", "score_categories.json", "overlay.json" })
                {
                    var src = Path.Combine(AppPaths.BaseDir, f);
                    if (File.Exists(src)) File.Copy(src, Path.Combine(folder, f), true);
                }
                MessageDialog.ShowInfo(I18n.T("common.success"),
                    string.Format(I18n.T("msg.backupDone"), folder));
            }
            catch (Exception ex) { MessageDialog.ShowError(I18n.T("common.error"), ex.Message); }
        }

        /// <summary>
        /// 恢复数据。
        /// </summary>
        private void RestoreData()
        {
            string folder = FileBrowserDialog.PickFolder(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            if (string.IsNullOrEmpty(folder)) return;

            try
            {
                int n = 0;
                foreach (var f in new[] { "courses.json", "sections.json", "teachers.json", "config.json", "students.json", "duty.json", "exams.json", "seats.json", "committees.json", "representatives.json", "scores.json", "score_categories.json", "overlay.json" })
                {
                    var src = Path.Combine(folder, f);
                    if (File.Exists(src))
                    {
                        File.Copy(src, Path.Combine(AppPaths.BaseDir, f), true);
                        n++;
                    }
                }
                _owner.LoadAll();
                _owner.RefreshAll();
                MessageDialog.ShowInfo(I18n.T("common.success"),
                    string.Format(I18n.T("msg.restoreDone"), n));
            }
            catch (Exception ex) { MessageDialog.ShowError(I18n.T("common.error"), ex.Message); }
        }

        /// <summary>
        /// 导入 CSV 课程。
        /// </summary>
        private void ImportCsv()
        {
            string path = FileBrowserDialog.PickFile(AppPaths.BaseDir, "CSV 文件|*.csv");
            if (string.IsNullOrEmpty(path)) return;
            _owner.Courses = CsvService.Load(path);
            _owner.SaveAll();
            _owner.RefreshAll();
            MessageDialog.ShowInfo(I18n.T("common.success"),
                string.Format(I18n.T("dialog.imported"), _owner.Courses.Count));
        }

        /// <summary>
        /// 导出 CSV 课程。
        /// </summary>
        private void ExportCsv()
        {
            using var dlg = new ExportCsvDialog(AppPaths.BaseDir, "courses.csv");
            if (dlg.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(dlg.ResultPath))
            {
                CsvService.Save(dlg.ResultPath, _owner.Courses);
                MessageDialog.ShowInfo(I18n.T("common.success"), I18n.T("dialog.exported"));
            }
        }

        /// <summary>
        /// 立即保存。
        /// </summary>
        private void SaveAllData()
        {
            _owner.SaveAll();
            MessageDialog.ShowInfo(I18n.T("common.success"), I18n.T("dialog.saved"));
        }

        /// <summary>
        /// 编辑节次时间。
        /// </summary>
        private void EditSectionTime()
        {
            using var dlg = new SectionTimeEditorDialog(_owner.SectionTimes);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _owner.SectionTimes = dlg.Result;
                _owner.SaveAll();
                _owner.RefreshAll();
            }
        }

        /// <summary>
        /// 显示关于弹窗。
        /// </summary>
        private void ShowAbout()
        {
            using var dlg = new AboutDialog();
            dlg.ShowDialog(this);
        }

        /// <summary>
        /// 清空所有课程。
        /// </summary>
        private void ClearCourses()
        {
            if (!MessageDialog.Confirm(I18n.T("common.confirm"), I18n.T("msg.confirmClearCourses")))
                return;
            _owner.Courses.Clear();
            _owner.SaveAll();
            _owner.RefreshAll();
            MessageDialog.ShowInfo(I18n.T("common.success"), I18n.T("msg.dataCleared"));
        }
    }
}