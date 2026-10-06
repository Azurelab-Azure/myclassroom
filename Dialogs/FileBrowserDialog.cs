using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using CourseApp.Controls;
using CourseApp.Localization;
using CourseApp.Services;
using CourseApp.Theme;

namespace CourseApp.Dialogs
{
    public class FileBrowserDialog : FlatDialogBase
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(
            string pszPath, uint dwFileAttributes,
            ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        private const uint SHGFI_ICON = 0x100;
        private const uint SHGFI_SMALLICON = 0x1;
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x80;

        private const int SidebarW = 180;
        private const int ToolbarH = 48;
        private const int HeaderH = 34;
        private const int RowH = 34;
        private const int PadLeft = 12;

        private readonly bool _folderMode;
        private readonly string _filter;

        private List<FileEntry> _entries = new();
        private string _currentPath = "";
        private int _selectedIndex = -1;
        private int _hoverIndex = -1;
        private int _scrollY = 0;
        private int _sortColumn = 0;
        private bool _sortAsc = true;

        private FlatScrollBar _scrollBar = null!;
        private DoubleBufferedPanel _listHost = null!;
        private FlatTextBox _pathBox = null!;
        private FlatButton _btnUp = null!;
        private FlatButton _btnRefresh = null!;
        private FlatButton _btnHome = null!;
        private FlatButton _btnOpen = null!;
        private DoubleBufferedPanel _sidebar = null!;
        private List<SidebarItem> _sidebarItems = new();
        private int _sidebarHoverIndex = -1;

        public string ResultPath { get; private set; } = "";

        private class FileEntry
        {
            public string FullPath = "";
            public string Name = "";
            public string TypeName = "";
            public string Modified = "";
            public bool IsFolder;
            public Icon? Icon;
            public DateTime ModifiedDt;
        }

        private class SidebarItem
        {
            public string Name = "";
            public string Path = "";
            public Rectangle Rect;
            public Icon? Icon;
        }

        // =====================================================
        // 构造
        // =====================================================
        public FileBrowserDialog(bool folderMode, string startPath, string filter)
            : base(folderMode ? I18n.T("fileDialog.titlePickFolder") : I18n.T("fileDialog.titleOpen"), 900, 620)
        {
            _folderMode = folderMode;
            _filter = filter ?? "*.*";

            ContentPanel.Padding = new Padding(0);

            BuildUI();
            LoadDir(startPath);
        }

        public static string PickFile(string startPath, string filter)
        {
            using var dlg = new FileBrowserDialog(false, startPath, filter);
            return dlg.ShowDialog() == DialogResult.OK ? dlg.ResultPath : "";
        }

        public static string PickFolder(string startPath)
        {
            using var dlg = new FileBrowserDialog(true, startPath, "");
            return dlg.ShowDialog() == DialogResult.OK ? dlg.ResultPath : "";
        }

        // =====================================================
        // 构建
        // =====================================================
        private void BuildUI()
        {
            BuildSidebar();
            BuildToolbar();
            BuildListHost();
            BuildBottomButtons();
        }

        // ---------- 侧边栏 ----------
        private void BuildSidebar()
        {
            _sidebar = new DoubleBufferedPanel
            {
                Left = 0, Top = 0,
                Width = SidebarW,
                Height = ContentPanel.Height,
                BackColor = AppTheme.Colors.WindowBg,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left,
            };
            _sidebar.Paint += Sidebar_Paint;
            _sidebar.MouseDown += Sidebar_MouseDown;
            _sidebar.MouseMove += Sidebar_MouseMove;
            _sidebar.MouseLeave += (s, e) => SetSidebarHover(-1);
            ContentPanel.Controls.Add(_sidebar);

            void Add(string name, string path)
            {
                var item = new SidebarItem { Name = name, Path = path };
                item.Icon = GetIcon(path, true);
                _sidebarItems.Add(item);
            }

            Add(I18n.T("fileDialog.sidebarDesktop"),
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            Add(I18n.T("fileDialog.sidebarDownloads"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"));
            Add(I18n.T("fileDialog.sidebarDocuments"),
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            Add(I18n.T("fileDialog.sidebarPictures"),
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));

            try
            {
                foreach (var drive in DriveInfo.GetDrives())
                {
                    try
                    {
                        if (!drive.IsReady) continue;
                        string letter = drive.Name.TrimEnd('\\');
                        Add(letter + "\\", drive.Name);
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void Sidebar_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var colors = AppTheme.Colors;

            int y = 8;
            const int itemH = 36;
            const int gap = 2;

            for (int i = 0; i < _sidebarItems.Count; i++)
            {
                var it = _sidebarItems[i];
                var rect = new Rectangle(8, y, SidebarW - 16, itemH);
                it.Rect = rect;

                if (i == _sidebarHoverIndex)
                {
                    using var bg = new SolidBrush(colors.HoverBg);
                    using var path = GraphicsExtensions.GetRoundPath(rect, 6);
                    g.FillPath(bg, path);
                }

                int iconSize = 20;
                var iconRect = new Rectangle(rect.X + 10, rect.Y + (rect.Height - iconSize) / 2, iconSize, iconSize);
                if (it.Icon != null)
                    g.DrawIcon(it.Icon, iconRect);

                var textRect = new Rectangle(rect.X + 38, rect.Y, rect.Width - 46, rect.Height);
                TextRenderer.DrawText(g, it.Name, AppTheme.BodyFont, textRect, colors.TextPrimary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

                y += itemH + gap;
            }
        }

        private void Sidebar_MouseDown(object? sender, MouseEventArgs e)
        {
            foreach (var it in _sidebarItems)
            {
                if (it.Rect.Contains(e.Location) && Directory.Exists(it.Path))
                {
                    LoadDir(it.Path);
                    return;
                }
            }
        }

        private void Sidebar_MouseMove(object? sender, MouseEventArgs e)
        {
            int newHover = -1;
            for (int i = 0; i < _sidebarItems.Count; i++)
            {
                if (_sidebarItems[i].Rect.Contains(e.Location)) { newHover = i; break; }
            }
            SetSidebarHover(newHover);
        }

        private void SetSidebarHover(int newIndex)
        {
            if (_sidebarHoverIndex == newIndex) return;

            const int itemH = 36;
            const int gap = 2;
            int baseY = 8;

            int old = _sidebarHoverIndex;
            _sidebarHoverIndex = newIndex;

            if (old >= 0 && old < _sidebarItems.Count)
            {
                int y = baseY + old * (itemH + gap);
                _sidebar.Invalidate(new Rectangle(0, y, SidebarW, itemH));
            }
            if (newIndex >= 0 && newIndex < _sidebarItems.Count)
            {
                int y = baseY + newIndex * (itemH + gap);
                _sidebar.Invalidate(new Rectangle(0, y, SidebarW, itemH));
            }
        }

        // ---------- 工具栏 ----------
        private void BuildToolbar()
        {
            int top = 10;
            int h = 32;
            int left = SidebarW + PadLeft;

            _btnUp = new FlatButton
            {
                Text = "↑",
                ButtonStyle = FlatButtonStyle.Secondary,
                Left = left, Top = top, Width = h, Height = h,
            };
            _btnUp.Click += (s, e) => GoUp();
            ContentPanel.Controls.Add(_btnUp);

            _btnRefresh = new FlatButton
            {
                Text = "↻",
                ButtonStyle = FlatButtonStyle.Secondary,
                Left = left + h + 4, Top = top, Width = h, Height = h,
            };
            _btnRefresh.Click += (s, e) => LoadDir(_currentPath);
            ContentPanel.Controls.Add(_btnRefresh);

            _btnHome = new FlatButton
            {
                Text = "⌂",
                ButtonStyle = FlatButtonStyle.Secondary,
                Left = left + (h + 4) * 2, Top = top, Width = h, Height = h,
            };
            _btnHome.Click += (s, e) => LoadDir(AppPaths.BaseDir);
            ContentPanel.Controls.Add(_btnHome);

            int pathLeft = left + (h + 4) * 3 + 4;
            int openW = 80;
            _pathBox = new FlatTextBox
            {
                Left = pathLeft, Top = top,
                Width = ContentPanel.Width - pathLeft - 20 - openW - 8,
                Height = h,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            _pathBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    var p = _pathBox.Text.Trim();
                    if (Directory.Exists(p)) LoadDir(p);
                }
            };
            ContentPanel.Controls.Add(_pathBox);

            _btnOpen = new FlatButton
            {
                Text = I18n.T("menu.open"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Left = ContentPanel.Width - 20 - openW,
                Top = top,
                Width = openW,
                Height = h,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };
            _btnOpen.Click += (s, e) =>
            {
                var p = _pathBox.Text.Trim();
                if (Directory.Exists(p)) LoadDir(p);
            };
            ContentPanel.Controls.Add(_btnOpen);
        }

        // ---------- 列表 ----------
        private void BuildListHost()
        {
            int left = SidebarW + PadLeft;
            int top = ToolbarH + 8;

            _listHost = new DoubleBufferedPanel
            {
                Left = left,
                Top = top,
                Width = ContentPanel.Width - left - 20 - 12,
                Height = ContentPanel.Height - top - 80,
                BackColor = AppTheme.Colors.CardBg,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            };
            _listHost.Paint += ListHost_Paint;
            _listHost.MouseDown += ListHost_MouseDown;
            _listHost.MouseMove += ListHost_MouseMove;
            _listHost.MouseLeave += (s, e) => SetHover(-1);
            _listHost.MouseDoubleClick += ListHost_MouseDoubleClick;
            _listHost.MouseWheel += ListHost_MouseWheel;
            ContentPanel.Controls.Add(_listHost);

            _scrollBar = new FlatScrollBar
            {
                Left = ContentPanel.Width - 20 - 8,
                Top = top,
                Width = 8,
                Height = _listHost.Height,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right,
            };
            _scrollBar.ValueChanged += (s, e) =>
            {
                _scrollY = _scrollBar.Value;
                _listHost.Invalidate();
            };
            ContentPanel.Controls.Add(_scrollBar);
        }

        // ---------- 底部 ----------
        private void BuildBottomButtons()
        {
            var btnOk = new FlatButton
            {
                Text = _folderMode ? I18n.T("fileDialog.titlePickFolder") : I18n.T("menu.open"),
                ButtonStyle = FlatButtonStyle.Primary,
                Width = 120, Height = 36,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            btnOk.Click += (s, e) => ConfirmSelection();
            ContentPanel.Controls.Add(btnOk);

            var btnCancel = new FlatButton
            {
                Text = I18n.T("dialog.cancel"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Width = 100, Height = 36,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            ContentPanel.Controls.Add(btnCancel);

            void LayoutBottom()
            {
                int y = ContentPanel.Height - 48;
                btnCancel.Left = ContentPanel.Width - btnCancel.Width - 20;
                btnCancel.Top = y;
                btnOk.Left = btnCancel.Left - btnOk.Width - 12;
                btnOk.Top = y;
            }
            LayoutBottom();
            ContentPanel.Resize += (s, e) => LayoutBottom();
        }

        // =====================================================
        // 加载
        // =====================================================
        private void LoadDir(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;

            _currentPath = path;
            _entries.Clear();
            _selectedIndex = -1;
            _hoverIndex = -1;
            _scrollY = 0;

            try
            {
                foreach (var dir in Directory.GetDirectories(path))
                {
                    var di = new DirectoryInfo(dir);
                    _entries.Add(new FileEntry
                    {
                        FullPath = dir,
                        Name = di.Name,
                        TypeName = I18n.T("fileDialog.folder"),
                        Modified = di.LastWriteTime.ToString("yyyy-MM-dd HH:mm"),
                        ModifiedDt = di.LastWriteTime,
                        IsFolder = true,
                        Icon = GetIcon(dir, true),
                    });
                }

                if (!_folderMode)
                {
                    string[] patterns = _filter.Contains("|")
                        ? _filter.Split('|')[1].Split(';')
                        : new[] { _filter };

                    foreach (var p in patterns)
                    {
                        foreach (var file in Directory.GetFiles(path, p.Trim()))
                        {
                            var fi = new FileInfo(file);
                            _entries.Add(new FileEntry
                            {
                                FullPath = file,
                                Name = fi.Name,
                                TypeName = fi.Extension.ToUpper() + " " + I18n.T("fileDialog.fileSuffix"),
                                Modified = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm"),
                                ModifiedDt = fi.LastWriteTime,
                                IsFolder = false,
                                Icon = GetIcon(file, false),
                            });
                        }
                    }
                }

                ApplySort();
            }
            catch { }

            _pathBox.Text = path;
            UpdateScrollBar();
            _listHost.Invalidate();
        }

        private void ApplySort()
        {
            IEnumerable<FileEntry> q = _entries;
            switch (_sortColumn)
            {
                case 0:
                    q = _sortAsc
                        ? _entries.OrderByDescending(x => x.IsFolder).ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                        : _entries.OrderByDescending(x => x.IsFolder).ThenByDescending(x => x.Name, StringComparer.OrdinalIgnoreCase);
                    break;
                case 1:
                    q = _sortAsc
                        ? _entries.OrderByDescending(x => x.IsFolder).ThenBy(x => x.TypeName, StringComparer.OrdinalIgnoreCase)
                        : _entries.OrderByDescending(x => x.IsFolder).ThenByDescending(x => x.TypeName, StringComparer.OrdinalIgnoreCase);
                    break;
                case 2:
                    q = _sortAsc
                        ? _entries.OrderByDescending(x => x.IsFolder).ThenBy(x => x.ModifiedDt)
                        : _entries.OrderByDescending(x => x.IsFolder).ThenByDescending(x => x.ModifiedDt);
                    break;
            }
            _entries = q.ToList();
        }

        private void SortBy(int column)
        {
            if (_sortColumn == column) _sortAsc = !_sortAsc;
            else { _sortColumn = column; _sortAsc = true; }
            ApplySort();
            _listHost.Invalidate();
        }

        private void GoUp()
        {
            try
            {
                var parent = Directory.GetParent(_currentPath);
                if (parent != null && parent.Exists) LoadDir(parent.FullName);
            }
            catch { }
        }

        private void ConfirmSelection()
        {
            if (_folderMode)
            {
                ResultPath = _currentPath;
            }
            else
            {
                if (_selectedIndex >= 0 && _selectedIndex < _entries.Count)
                {
                    var en = _entries[_selectedIndex];
                    if (en.IsFolder) { LoadDir(en.FullPath); return; }
                    ResultPath = en.FullPath;
                }
                else
                {
                    ResultPath = _currentPath;
                }
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        // =====================================================
        // 图标
        // =====================================================
        private Icon? GetIcon(string path, bool isFolder)
        {
            try
            {
                var shinfo = new SHFILEINFO();
                uint flags = SHGFI_ICON | SHGFI_SMALLICON;
                uint attr = isFolder ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL;
                SHGetFileInfo(path, attr, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);
                if (shinfo.hIcon != IntPtr.Zero)
                    return Icon.FromHandle(shinfo.hIcon);
            }
            catch { }
            return null;
        }

        // =====================================================
        // 绘制
        // =====================================================
        private void ListHost_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var colors = AppTheme.Colors;

            using (var bg = new SolidBrush(colors.CardBg))
                g.FillRectangle(bg, _listHost.ClientRectangle);

            var headerRect = new Rectangle(0, 0, _listHost.Width, HeaderH);
            using (var hbg = new SolidBrush(colors.GridHeaderBg))
                g.FillRectangle(hbg, headerRect);

            using (var pen = new Pen(colors.Divider, 1f))
                g.DrawLine(pen, 0, headerRect.Bottom - 1, _listHost.Width, headerRect.Bottom - 1);

            int typeW = 120;
            int dateW = 160;
            int nameW = _listHost.Width - typeW - dateW;
            if (nameW < 100) nameW = 100;

            int typeX = nameW;
            int dateX = nameW + typeW;

            DrawHeaderColumn(g, I18n.T("fileDialog.colName"), 12, nameW - 12, 0);
            DrawHeaderColumn(g, I18n.T("fileDialog.colType"), typeX + 8, typeW - 8, 1);
            DrawHeaderColumn(g, I18n.T("fileDialog.colModified"), dateX + 8, dateW - 8, 2);

            int y = HeaderH - _scrollY;
            for (int i = 0; i < _entries.Count; i++)
            {
                var en = _entries[i];
                var rowRect = new Rectangle(0, y, _listHost.Width, RowH);

                if (rowRect.Bottom > HeaderH && rowRect.Top < _listHost.Height)
                    DrawRow(g, en, i, rowRect, nameW, typeW, dateW, typeX, dateX);

                y += RowH;
            }
        }

        private void DrawHeaderColumn(Graphics g, string text, int x, int w, int colIdx)
        {
            var colors = AppTheme.Colors;
            var rect = new Rectangle(x, 0, w, HeaderH);

            string label = text;
            if (_sortColumn == colIdx) label += _sortAsc ? " ↑" : " ↓";

            TextRenderer.DrawText(g, label, AppTheme.BodyFont, rect, colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        private void DrawRow(Graphics g, FileEntry en, int index, Rectangle rect,
            int nameW, int typeW, int dateW, int typeX, int dateX)
        {
            var colors = AppTheme.Colors;

            bool selected = (index == _selectedIndex);
            bool hover = (index == _hoverIndex);

            if (selected)
            {
                using var bg = new SolidBrush(colors.SelectedBg);
                g.FillRectangle(bg, rect);
            }
            else if (hover)
            {
                using var bg = new SolidBrush(colors.HoverBg);
                g.FillRectangle(bg, rect);
            }

            int iconSize = 20;
            var iconRect = new Rectangle(14, rect.Y + (rect.Height - iconSize) / 2, iconSize, iconSize);
            if (en.Icon != null)
                g.DrawIcon(en.Icon, iconRect);

            var nameCol = new Rectangle(44, rect.Y, nameW - 52, rect.Height);
            TextRenderer.DrawText(g, en.Name, AppTheme.BodyFont, nameCol, colors.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            var typeCol = new Rectangle(typeX + 8, rect.Y, typeW - 16, rect.Height);
            TextRenderer.DrawText(g, en.TypeName, AppTheme.SmallFont, typeCol, colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            var dateCol = new Rectangle(dateX + 8, rect.Y, dateW - 16, rect.Height);
            TextRenderer.DrawText(g, en.Modified, AppTheme.SmallFont, dateCol, colors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        // =====================================================
        // 鼠标
        // =====================================================
        private int HitTestRow(Point p)
        {
            if (p.Y < HeaderH) return -1;
            int idx = (p.Y - HeaderH + _scrollY) / RowH;
            if (idx < 0 || idx >= _entries.Count) return -1;
            return idx;
        }

        private int HitTestHeader(Point p)
        {
            if (p.Y >= HeaderH) return -1;
            int typeW = 120;
            int dateW = 160;
            int nameW = _listHost.Width - typeW - dateW;
            if (nameW < 100) nameW = 100;

            if (p.X < nameW) return 0;
            if (p.X < nameW + typeW) return 1;
            return 2;
        }

        private void SetHover(int newIndex)
        {
            if (_hoverIndex == newIndex) return;

            int old = _hoverIndex;
            _hoverIndex = newIndex;

            if (old >= 0 && old < _entries.Count)
            {
                int y = HeaderH - _scrollY + old * RowH;
                _listHost.Invalidate(new Rectangle(0, y, _listHost.Width, RowH));
            }

            if (newIndex >= 0 && newIndex < _entries.Count)
            {
                int y = HeaderH - _scrollY + newIndex * RowH;
                _listHost.Invalidate(new Rectangle(0, y, _listHost.Width, RowH));
            }
        }

        private void ListHost_MouseDown(object? sender, MouseEventArgs e)
        {
            int hcol = HitTestHeader(e.Location);
            if (hcol >= 0) { SortBy(hcol); return; }

            int idx = HitTestRow(e.Location);
            if (idx < 0) return;

            if (e.Button == MouseButtons.Left)
            {
                _selectedIndex = idx;
                _listHost.Invalidate();
            }
            else if (e.Button == MouseButtons.Right)
            {
                _selectedIndex = idx;
                _listHost.Invalidate();
                ShowRowMenu(_entries[idx], e.Location);
            }
        }

        private void ListHost_MouseMove(object? sender, MouseEventArgs e)
        {
            SetHover(HitTestRow(e.Location));
        }

        private void ListHost_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            int idx = HitTestRow(e.Location);
            if (idx < 0) return;

            var en = _entries[idx];
            if (en.IsFolder)
            {
                LoadDir(en.FullPath);
            }
            else
            {
                ResultPath = en.FullPath;
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void ListHost_MouseWheel(object? sender, MouseEventArgs e)
        {
            _scrollY -= Math.Sign(e.Delta) * 60;
            ClampScroll();
            _scrollBar.Value = _scrollY;
            _scrollBar.Wake();
            _listHost.Invalidate();
        }

        private void ClampScroll()
        {
            int contentH = _entries.Count * RowH;
            int maxScroll = Math.Max(0, contentH - (_listHost.Height - HeaderH));
            if (_scrollY < 0) _scrollY = 0;
            if (_scrollY > maxScroll) _scrollY = maxScroll;
        }

        private void UpdateScrollBar()
        {
            int contentH = _entries.Count * RowH;
            bool need = contentH > _listHost.Height - HeaderH;
            _scrollBar.Visible = need;
            if (need)
            {
                _scrollBar.Maximum = contentH;
                _scrollBar.LargeChange = _listHost.Height - HeaderH;
                _scrollBar.Value = _scrollY;
            }
            else _scrollY = 0;
        }

        // =====================================================
        // 右键菜单
        // =====================================================
        private void ShowRowMenu(FileEntry en, Point localPt)
        {
            var menu = new FlatContextMenu();

            if (en.IsFolder)
            {
                menu.AddItem(FlatMenuItem.Create(I18n.T("menu.open"),
                    () => LoadDir(en.FullPath), Icons.Folder));
                menu.AddItem(FlatMenuItem.Create(I18n.T("menu.openContainingFolder"),
                    () => OpenInExplorer(en.FullPath), Icons.Folder));
            }
            else
            {
                menu.AddItem(FlatMenuItem.Create(I18n.T("menu.open"),
                    () => { ResultPath = en.FullPath; DialogResult = DialogResult.OK; Close(); },
                    Icons.FileIcon));
                menu.AddItem(FlatMenuItem.Create(I18n.T("menu.openContainingFolder"),
                    () => OpenInExplorer("/select,\"" + en.FullPath + "\""), Icons.Folder));
                menu.AddSeparator();
                menu.AddItem(FlatMenuItem.CreateDanger(I18n.T("menu.delete"),
                    () => DeleteEntry(en), Icons.Delete));
            }

            menu.ShowAt(_listHost, _listHost.PointToScreen(localPt));
        }

        private void OpenInExplorer(string arg)
        {
            try { System.Diagnostics.Process.Start("explorer.exe", arg); } catch { }
        }

        private void DeleteEntry(FileEntry en)
        {
            var ok = MessageDialog.Confirm(I18n.T("dialog.confirm"),
                string.Format(I18n.T("msg.confirmDelete"), en.Name));
            if (!ok) return;
            try
            {
                if (en.IsFolder) Directory.Delete(en.FullPath, true);
                else File.Delete(en.FullPath);
                LoadDir(_currentPath);
            }
            catch { }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var en in _entries) en.Icon = null;
                foreach (var it in _sidebarItems) it.Icon = null;
            }
            base.Dispose(disposing);
        }
    }
}