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
    /// Win11 设置内容区：8 个子页，SetPage 切换。
    /// 每行按实际高度累加，选择器采用"标题在上、控件在下"的堆叠布局。
    /// 主题 / 语言切换后强制重启软件。
    /// </summary>
    public class SettingsContentView : Panel
    {
        private readonly Form1 _owner;
        private Panel _host = null!;
        private Panel[] _pages = null!;
        private int _currentIndex = -1;

        private const int RowH = 72;
        private const int StackPadH = 16;
        private const int CardPadBottom = 20;

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
        // 8 个子页
        // =====================================================
        private void BuildPages()
        {
            _pages = new[]
            {
                BuildSystemPage(),
                BuildPersonalizationPage(),
                BuildAppsPage(),
                BuildIslandPage(),
                BuildSidebarPage(),
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
        // 系统
        // =====================================================
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
        // 个性化
        // =====================================================
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
        // 应用
        // =====================================================
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
        // 灵动岛（含今日侧边栏）
        // =====================================================
        private Panel BuildIslandPage()
        {
            var page = NewPage(
                I18n.T("settings.section.island"),
                I18n.T("settings.section.island.desc"));

            // ---------- 灵动岛：尺寸 ----------
            AddCard(page,
                I18n.T("settings.island.size"),
                I18n.T("settings.island.size.desc"), new[]
            {
                Row.Number(I18n.T("settings.island.width"),
                    I18n.T("settings.island.width.desc"),
                    ConfigService.Load().IslandWidth, 120, 600, v =>
                    {
                        var cfg = ConfigService.Load();
                        cfg.IslandWidth = v;
                        ConfigService.Save(cfg);
                        _owner.ApplyIslandConfig();
                    }),
                Row.Number(I18n.T("settings.island.height"),
                    I18n.T("settings.island.height.desc"),
                    ConfigService.Load().IslandHeight, 24, 120, v =>
                    {
                        var cfg = ConfigService.Load();
                        cfg.IslandHeight = v;
                        ConfigService.Save(cfg);
                        _owner.ApplyIslandConfig();
                    }),
            });

            // ---------- 灵动岛：内容 ----------
            AddCard(page,
                I18n.T("settings.island.content"),
                I18n.T("settings.island.content.desc"), new[]
            {
                Row.Stack(
                    I18n.T("settings.island.content.label"),
                    I18n.T("settings.island.content.desc2"),
                    () =>
                {
                    var sel = new CardSelector();
                    sel.Width = 2 * 128 + 8;
                    sel.SetItems(new[]
                    {
                        new CardSelector.Item
                        {
                            Text = I18n.T("settings.island.content.course"),
                            PreviewDrawer = IslandPreviewDrawer(false),
                        },
                        new CardSelector.Item
                        {
                            Text = I18n.T("settings.island.content.news"),
                            PreviewDrawer = IslandPreviewDrawer(true),
                        },
                    });
                    sel.SelectedIndex = ConfigService.Load().IslandShowNews ? 1 : 0;
                    sel.SelectedIndexChanged += (s, e) =>
                    {
                        var cfg = ConfigService.Load();
                        cfg.IslandShowNews = (sel.SelectedIndex == 1);
                        ConfigService.Save(cfg);
                        _owner.ApplyIslandConfig();
                    };
                    return sel;
                }),
            });

            // ---------- 灵动岛：新闻 ----------
            AddCard(page,
                I18n.T("settings.island.news"),
                I18n.T("settings.island.news.desc"), new[]
            {
                Row.StackText(
                    I18n.T("settings.island.newsUrl"),
                    I18n.T("settings.island.newsUrl.desc"),
                    ConfigService.Load().IslandNewsUrl ?? "",
                    text =>
                    {
                        var cfg = ConfigService.Load();
                        cfg.IslandNewsUrl = text;
                        ConfigService.Save(cfg);
                    }),
                Row.Button(Icons.Info,
                    I18n.T("settings.island.newsPreset"),
                    I18n.T("settings.island.newsPreset.desc"),
                    I18n.T("settings.island.newsPresetBtn"), ShowPresetSources),
                Row.Number(I18n.T("settings.island.newsInterval"),
                    I18n.T("settings.island.newsInterval.desc"),
                    ConfigService.Load().IslandNewsIntervalSec, 3, 120, v =>
                    {
                        var cfg = ConfigService.Load();
                        cfg.IslandNewsIntervalSec = v;
                        ConfigService.Save(cfg);
                        _owner.ApplyIslandConfig();
                    }),
            });

            // ---------- 今日侧边栏 ----------
            AddCard(page,
                I18n.T("settings.todaySidebar"),
                I18n.T("settings.todaySidebar.desc"), new[]
            {
                Row.Stack(
                    I18n.T("settings.todaySidebar.visible"),
                    I18n.T("settings.todaySidebar.visibleDesc"),
                    () =>
                {
                    var sel = new CardSelector();
                    sel.Width = 2 * 128 + 8;
                    sel.SetItems(new[]
                    {
                        new CardSelector.Item { Text = I18n.T("common.show") },
                        new CardSelector.Item { Text = I18n.T("common.hide") },
                    });
                    sel.SelectedIndex = ConfigService.Load().TodaySidebarVisible ? 0 : 1;
                    sel.SelectedIndexChanged += (s, e) =>
                    {
                        var cfg = ConfigService.Load();
                        cfg.TodaySidebarVisible = (sel.SelectedIndex == 0);
                        ConfigService.Save(cfg);
                        _owner.ApplyTodaySidebarConfig();
                    };
                    return sel;
                }),

                Row.Number(
                    I18n.T("settings.todaySidebar.posX"),
                    I18n.T("settings.todaySidebar.posXDesc"),
                    Math.Max(0, ConfigService.Load().TodaySidebarX), 0, 4000, v =>
                {
                    var cfg = ConfigService.Load();
                    cfg.TodaySidebarX = v;
                    ConfigService.Save(cfg);
                    _owner.ApplyTodaySidebarConfig();
                }),

                Row.Number(
                    I18n.T("settings.todaySidebar.posY"),
                    I18n.T("settings.todaySidebar.posYDesc"),
                    Math.Max(0, ConfigService.Load().TodaySidebarY), 0, 4000, v =>
                {
                    var cfg = ConfigService.Load();
                    cfg.TodaySidebarY = v;
                    ConfigService.Save(cfg);
                    _owner.ApplyTodaySidebarConfig();
                }),
            });

            return page;
        }

        // =====================================================
        // 侧边栏
        // =====================================================
        private Panel BuildSidebarPage()
        {
            var page = NewPage(
                I18n.T("settings.section.sidebar"),
                I18n.T("settings.section.sidebar.desc"));

            AddCard(page,
                I18n.T("settings.sidebar.position"),
                I18n.T("settings.sidebar.position.desc"), new[]
            {
                Row.Stack(
                    I18n.T("settings.sidebar.position.label"),
                    I18n.T("settings.sidebar.position.desc2"),
                    () =>
                {
                    var sel = new CardSelector();
                    sel.Width = 4 * 128 + 3 * 8;
                    sel.SetItems(new[]
                    {
                        new CardSelector.Item
                        {
                            Text = I18n.T("settings.sidebar.position.left"),
                            PreviewDrawer = SidebarPositionPreviewDrawer("left"),
                        },
                        new CardSelector.Item
                        {
                            Text = I18n.T("settings.sidebar.position.right"),
                            PreviewDrawer = SidebarPositionPreviewDrawer("right"),
                        },
                        new CardSelector.Item
                        {
                            Text = I18n.T("settings.sidebar.position.top"),
                            PreviewDrawer = SidebarPositionPreviewDrawer("top"),
                        },
                        new CardSelector.Item
                        {
                            Text = I18n.T("settings.sidebar.position.bottom"),
                            PreviewDrawer = SidebarPositionPreviewDrawer("bottom"),
                        },
                    });
                    sel.SelectedIndex = ReadSidebarPosIndex();
                    sel.SelectedIndexChanged += (s, e) =>
                    {
                        var cfg = ConfigService.Load();
                        cfg.SidebarPosition = new[] { "left", "right", "top", "bottom" }[sel.SelectedIndex];
                        ConfigService.Save(cfg);
                        _owner.ApplySidebarConfig();
                    };
                    return sel;
                }),
            });

            AddCard(page,
                I18n.T("settings.sidebar.align"),
                I18n.T("settings.sidebar.align.desc"), new[]
            {
                Row.Stack(
                    I18n.T("settings.sidebar.alignPrimary"),
                    I18n.T("settings.sidebar.alignPrimary.desc"),
                    () =>
                {
                    var sel = new CardSelector();
                    sel.Width = 3 * 128 + 2 * 8;
                    sel.SetItems(new[]
                    {
                        new CardSelector.Item
                        {
                            Text = I18n.T("settings.sidebar.align.start"),
                            PreviewDrawer = AlignPreviewDrawer("start"),
                        },
                        new CardSelector.Item
                        {
                            Text = I18n.T("settings.sidebar.align.center"),
                            PreviewDrawer = AlignPreviewDrawer("center"),
                        },
                        new CardSelector.Item
                        {
                            Text = I18n.T("settings.sidebar.align.end"),
                            PreviewDrawer = AlignPreviewDrawer("end"),
                        },
                    });
                    sel.SelectedIndex = ReadAlignIndex("SidebarAlignPrimary");
                    sel.SelectedIndexChanged += (s, e) =>
                    {
                        var cfg = ConfigService.Load();
                        cfg.SidebarAlignPrimary = new[] { "start", "center", "end" }[sel.SelectedIndex];
                        ConfigService.Save(cfg);
                        _owner.ApplySidebarConfig();
                    };
                    return sel;
                }),

                Row.Stack(
                    I18n.T("settings.sidebar.alignSecondary"),
                    I18n.T("settings.sidebar.alignSecondary.desc"),
                    () =>
                {
                    var sel = new CardSelector();
                    sel.Width = 3 * 128 + 2 * 8;
                    sel.SetItems(new[]
                    {
                        new CardSelector.Item
                        {
                            Text = I18n.T("settings.sidebar.align.start"),
                            PreviewDrawer = AlignPreviewDrawer("start"),
                        },
                        new CardSelector.Item
                        {
                            Text = I18n.T("settings.sidebar.align.center"),
                            PreviewDrawer = AlignPreviewDrawer("center"),
                        },
                        new CardSelector.Item
                        {
                            Text = I18n.T("settings.sidebar.align.end"),
                            PreviewDrawer = AlignPreviewDrawer("end"),
                        },
                    });
                    sel.SelectedIndex = ReadAlignIndex("SidebarAlignSecondary");
                    sel.SelectedIndexChanged += (s, e) =>
                    {
                        var cfg = ConfigService.Load();
                        cfg.SidebarAlignSecondary = new[] { "start", "center", "end" }[sel.SelectedIndex];
                        ConfigService.Save(cfg);
                        _owner.ApplySidebarConfig();
                    };
                    return sel;
                }),
            });

            return page;
        }

        // =====================================================
        // 时间和语言
        // =====================================================
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
        // 关于
        // =====================================================
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

            return page;
        }

        // =====================================================
        // 危险区
        // =====================================================
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

        private static Action<Graphics, Rectangle> IslandPreviewDrawer(bool showNews)
        {
            return (g, rect) =>
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var colors = AppTheme.Colors;

                using (var path = GraphicsExtensions.GetRoundPath(rect, 6))
                using (var brush = new SolidBrush(colors.CardBg))
                    g.FillPath(brush, path);

                int pillW = rect.Width - 20;
                int pillH = 14;
                var pill = new Rectangle(
                    rect.X + (rect.Width - pillW) / 2,
                    rect.Y + (rect.Height - pillH) / 2,
                    pillW, pillH);

                using (var path = GraphicsExtensions.GetRoundPath(pill, pillH / 2))
                using (var brush = new SolidBrush(colors.Accent))
                    g.FillPath(brush, path);

                int dotSize = 4;
                var dot = new Rectangle(pill.X + 8, pill.Y + (pillH - dotSize) / 2, dotSize, dotSize);
                using (var brush = new SolidBrush(Color.White))
                    g.FillEllipse(brush, dot);

                var line = new Rectangle(pill.X + 18, pill.Y + pillH / 2 - 1, pill.Width - 30, 2);
                using (var brush = new SolidBrush(Color.White))
                    g.FillRectangle(brush, line);

                string label = showNews ? "📰" : "📅";
                var textRect = new Rectangle(rect.X, rect.Y + rect.Height - 18, rect.Width, 16);
                TextRenderer.DrawText(g, label, AppTheme.SmallFont, textRect, colors.TextPrimary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix);

                using (var path = GraphicsExtensions.GetRoundPath(
                    new Rectangle(rect.X, rect.Y, rect.Width - 1, rect.Height - 1), 6))
                using (var pen = new Pen(colors.CardBorder, 1f))
                    g.DrawPath(pen, path);
            };
        }

        private static Action<Graphics, Rectangle> SidebarPositionPreviewDrawer(string pos)
        {
            return (g, rect) =>
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var colors = AppTheme.Colors;

                using (var path = GraphicsExtensions.GetRoundPath(rect, 6))
                using (var brush = new SolidBrush(colors.WindowBg))
                    g.FillPath(brush, path);

                int barThickness = 12;
                Rectangle bar;

                switch (pos)
                {
                    case "right":
                        bar = new Rectangle(rect.Right - barThickness, rect.Y, barThickness, rect.Height);
                        break;
                    case "top":
                        bar = new Rectangle(rect.X, rect.Y, rect.Width, barThickness);
                        break;
                    case "bottom":
                        bar = new Rectangle(rect.X, rect.Bottom - barThickness, rect.Width, barThickness);
                        break;
                    case "left":
                    default:
                        bar = new Rectangle(rect.X, rect.Y, barThickness, rect.Height);
                        break;
                }

                using (var path = GraphicsExtensions.GetRoundPath(rect, 6))
                {
                    var old = g.Clip;
                    g.SetClip(path);
                    using (var brush = new SolidBrush(colors.Accent))
                        g.FillRectangle(brush, bar);
                    g.Clip = old;
                }

                using (var path = GraphicsExtensions.GetRoundPath(
                    new Rectangle(rect.X, rect.Y, rect.Width - 1, rect.Height - 1), 6))
                using (var pen = new Pen(colors.CardBorder, 1f))
                    g.DrawPath(pen, path);
            };
        }

        private static Action<Graphics, Rectangle> AlignPreviewDrawer(string align)
        {
            return (g, rect) =>
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var colors = AppTheme.Colors;

                using (var path = GraphicsExtensions.GetRoundPath(rect, 6))
                using (var brush = new SolidBrush(colors.WindowBg))
                    g.FillPath(brush, path);

                int dotSize = 6;
                int gap = 4;
                int totalW = 3 * dotSize + 2 * gap;

                int startX;
                switch (align)
                {
                    case "center": startX = rect.X + (rect.Width - totalW) / 2; break;
                    case "end": startX = rect.Right - totalW - 4; break;
                    default: startX = rect.X + 4; break;
                }

                int y = rect.Y + (rect.Height - dotSize) / 2;

                for (int i = 0; i < 3; i++)
                {
                    int x = startX + i * (dotSize + gap);
                    var dot = new Rectangle(x, y, dotSize, dotSize);
                    using var brush = new SolidBrush(colors.Accent);
                    g.FillEllipse(brush, dot);
                }

                using (var path = GraphicsExtensions.GetRoundPath(
                    new Rectangle(rect.X, rect.Y, rect.Width - 1, rect.Height - 1), 6))
                using (var pen = new Pen(colors.CardBorder, 1f))
                    g.DrawPath(pen, path);
            };
        }

        // =====================================================
        // 逻辑
        // =====================================================
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

        private void AskRestart()
        {
            var owner = _owner;
            if (owner == null || owner.IsDisposed) return;

            MessageDialog.ShowInfo(
                I18n.T("common.info"),
                I18n.T("msg.needRestart"));

            owner.Restart();
        }

        private int ReadSidebarPosIndex()
        {
            var p = ConfigService.Load().SidebarPosition?.ToLowerInvariant() ?? "left";
            return p switch { "right" => 1, "top" => 2, "bottom" => 3, _ => 0 };
        }

        private int ReadAlignIndex(string key)
        {
            var cfg = ConfigService.Load();
            var v = key == "SidebarAlignPrimary" ? cfg.SidebarAlignPrimary : cfg.SidebarAlignSecondary;
            return (v ?? "start").ToLowerInvariant() switch { "center" => 1, "end" => 2, _ => 0 };
        }

        // =====================================================
        // 页面 / 卡片
        // =====================================================
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
        private class Row
        {
            public string Title = "";
            public string Desc = "";
            public bool IsStacked = false;
            public Func<Control?> ControlFactory = null!;

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

            public static Row Stack(string title, string desc, Func<Control?> factory)
            {
                return new Row
                {
                    Title = title, Desc = desc,
                    IsStacked = true,
                    ControlFactory = factory,
                };
            }

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

        private void BackupData()
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string folder = Path.Combine(desktop,
                    "CourseApp-Backup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
                Directory.CreateDirectory(folder);
                foreach (var f in new[] { "courses.json", "sections.json", "teachers.json", "config.json" })
                {
                    var src = Path.Combine(AppPaths.BaseDir, f);
                    if (File.Exists(src)) File.Copy(src, Path.Combine(folder, f), true);
                }
                MessageDialog.ShowInfo(I18n.T("common.success"),
                    string.Format(I18n.T("msg.backupDone"), folder));
            }
            catch (Exception ex) { MessageDialog.ShowError(I18n.T("common.error"), ex.Message); }
        }

        private void RestoreData()
        {
            string folder = FileBrowserDialog.PickFolder(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            if (string.IsNullOrEmpty(folder)) return;

            try
            {
                int n = 0;
                foreach (var f in new[] { "courses.json", "sections.json", "teachers.json", "config.json" })
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

        private void ShowPresetSources()
        {
            using var dlg = new NewsSourceDialog(ConfigService.Load().IslandNewsUrl ?? "");
            if (dlg.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(dlg.ResultUrl))
            {
                var cfg = ConfigService.Load();
                cfg.IslandNewsUrl = dlg.ResultUrl;
                ConfigService.Save(cfg);
                _owner.ApplyIslandConfig();
                MessageDialog.ShowInfo(I18n.T("common.info"), I18n.T("msg.newsSourceChanged"));
            }
        }

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

        private void ExportCsv()
        {
            using var dlg = new ExportCsvDialog(AppPaths.BaseDir, "courses.csv");
            if (dlg.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(dlg.ResultPath))
            {
                CsvService.Save(dlg.ResultPath, _owner.Courses);
                MessageDialog.ShowInfo(I18n.T("common.success"), I18n.T("dialog.exported"));
            }
        }

        private void SaveAllData()
        {
            _owner.SaveAll();
            MessageDialog.ShowInfo(I18n.T("common.success"), I18n.T("dialog.saved"));
        }

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

        private void ShowAbout()
        {
            using var dlg = new AboutDialog();
            dlg.ShowDialog(this);
        }

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