using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using CourseApp.Controls;
using CourseApp.Localization;
using CourseApp.Models;
using CourseApp.Theme;

namespace CourseApp.Dialogs
{
    public class SectionTimeEditorDialog : FlatDialogBase
    {
        private const int MinNormalSections = 3;

        private readonly List<SectionTime> _sections;

        private FlatScrollBar _scrollBar = null!;
        private Panel _listHost = null!;
        private FlatNumberBox _countBox = null!;
        private bool _updatingCount = false;

        private int _scrollY = 0;
        private int _rowHeight = 40;

        private int _dragIndex = -1;
        private int _dragTargetIndex = -1;
        private bool _dragging = false;
        private int _dragMouseY = 0;

        public List<SectionTime> Result { get; private set; }

        public SectionTimeEditorDialog(List<SectionTime> sections)
            : base(I18n.T("section.title"), 640, 600)
        {
            _sections = sections?.Select(s => new SectionTime
            {
                Type = s.Type,
                Section = s.Section,
                Name = s.Name,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
            }).ToList() ?? new List<SectionTime>();

            EnsureMinNormalSections();
            Result = _sections;

            BuildUI();
            RebuildRows();
            SyncCountBox();
        }

        private void EnsureMinNormalSections()
        {
            int normalCount = _sections.Count(s => s.Type == "normal");
            while (normalCount < MinNormalSections)
            {
                int next = GetNextSectionNumber();
                _sections.Add(new SectionTime
                {
                    Type = "normal",
                    Section = next,
                    Name = "",
                    StartTime = "",
                    EndTime = "",
                });
                normalCount++;
            }
        }

        private int GetNextSectionNumber()
        {
            var normals = _sections.Where(s => s.Type == "normal")
                                   .Select(s => s.Section).OrderBy(x => x).ToList();
            if (normals.Count == 0) return 1;
            return normals.Max() + 1;
        }

        private void BuildUI()
        {
            var topBar = new Panel
            {
                Left = 20, Top = 6,
                Width = ClientSize.Width - 40, Height = 48,
                BackColor = Color.Transparent,
            };
            ContentPanel.Controls.Add(topBar);

            topBar.Controls.Add(new Label
            {
                Text = I18n.T("section.total"),
                Left = 0, Top = 14, Width = 60,
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.Colors.TextPrimary,
                BackColor = Color.Transparent,
                AutoSize = false,
            });

            _countBox = new FlatNumberBox
            {
                Left = 64, Top = 8, Width = 80, Height = 32,
                Minimum = MinNormalSections, Maximum = 30, Value = MinNormalSections,
            };
            _countBox.ValueChanged += (s, e) =>
            {
                if (_updatingCount) return;
                ApplyNormalCount(_countBox.Value);
            };
            topBar.Controls.Add(_countBox);

            var btnAddSpecial = new FlatButton
            {
                Text = I18n.T("section.special"),
                Icon = Icons.Add, IconSize = 14,
                ButtonStyle = FlatButtonStyle.Secondary,
                Left = 160, Top = 8, Width = 120, Height = 32,
            };
            btnAddSpecial.Click += (s, e) => { AddSpecial(); RebuildRows(); };
            topBar.Controls.Add(btnAddSpecial);

            _listHost = new Panel
            {
                Left = 20, Top = 62,
                Width = ClientSize.Width - 40 - 12,
                Height = ClientSize.Height - 62 - 80,
                BackColor = AppTheme.Colors.CardBg,
            };
            _listHost.Paint += ListHost_Paint;
            _listHost.MouseDown += ListHost_MouseDown;
            _listHost.MouseMove += ListHost_MouseMove;
            _listHost.MouseUp += ListHost_MouseUp;
            _listHost.MouseWheel += ListHost_MouseWheel;
            _listHost.MouseLeave += (s, e) => { if (_dragging) EndDrag(false); };
            ContentPanel.Controls.Add(_listHost);

            _scrollBar = new FlatScrollBar
            {
                Left = ClientSize.Width - 20 - 8,
                Top = 62, Width = 8,
                Height = ClientSize.Height - 62 - 80,
            };
            _scrollBar.ValueChanged += (s, e) =>
            {
                _scrollY = _scrollBar.Value;
                _listHost.Invalidate();
            };
            ContentPanel.Controls.Add(_scrollBar);

            var btnSave = new FlatButton
            {
                Text = I18n.T("dialog.save"),
                ButtonStyle = FlatButtonStyle.Primary,
                Width = 100, Height = 36,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            btnSave.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
            ContentPanel.Controls.Add(btnSave);

            var btnCancel = new FlatButton
            {
                Text = I18n.T("dialog.cancel"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Width = 100, Height = 36,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            ContentPanel.Controls.Add(btnCancel);

            ContentPanel.Resize += (s, e) =>
            {
                _listHost.Width = ContentPanel.Width - 20 - 12;
                _listHost.Height = ContentPanel.Height - 62 - 80;
                _scrollBar.Left = ContentPanel.Width - 20 - 8;
                _scrollBar.Height = ContentPanel.Height - 62 - 80;

                btnCancel.Left = ContentPanel.Width - btnCancel.Width - 20;
                btnCancel.Top = ContentPanel.Height - btnCancel.Height - 20;
                btnSave.Left = btnCancel.Left - btnSave.Width - 12;
                btnSave.Top = btnCancel.Top;
            };
        }

        private void ApplyNormalCount(int target)
        {
            int currentNormal = _sections.Count(s => s.Type == "normal");

            if (target > currentNormal)
            {
                int toAdd = target - currentNormal;
                for (int i = 0; i < toAdd; i++)
                {
                    int next = GetNextSectionNumber();
                    _sections.Add(new SectionTime
                    {
                        Type = "normal",
                        Section = next,
                        Name = "",
                        StartTime = "",
                        EndTime = "",
                    });
                }
            }
            else if (target < currentNormal)
            {
                int toRemove = currentNormal - target;
                var normalsDesc = _sections
                    .Select((s, idx) => new { s, idx })
                    .Where(x => x.s.Type == "normal")
                    .OrderByDescending(x => x.s.Section)
                    .Take(toRemove)
                    .Select(x => x.idx)
                    .OrderByDescending(x => x)
                    .ToList();

                foreach (var idx in normalsDesc)
                    if (idx >= 0 && idx < _sections.Count) _sections.RemoveAt(idx);
            }

            RebuildRows();
            SyncCountBox();
        }

        private void SyncCountBox()
        {
            _updatingCount = true;
            try
            {
                int count = _sections.Count(s => s.Type == "normal");
                if (count < MinNormalSections) count = MinNormalSections;
                if (count > _countBox.Maximum) count = _countBox.Maximum;
                _countBox.Value = count;
            }
            finally { _updatingCount = false; }
        }

        private void AddSpecial()
        {
            _sections.Add(new SectionTime
            {
                Type = "special",
                Section = 0,
                Name = I18n.T("section.special"),
                StartTime = "",
                EndTime = "",
            });
        }

        private void RebuildRows()
        {
            int contentH = _sections.Count * _rowHeight;
            _scrollBar.Maximum = Math.Max(contentH, _listHost.Height);
            _scrollBar.LargeChange = _listHost.Height;
            _scrollBar.Value = 0;
            _scrollY = 0;
            _listHost.Invalidate();
        }

        private void ListHost_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var colors = AppTheme.Colors;

            using (var bg = new SolidBrush(colors.CardBg))
                g.FillRectangle(bg, _listHost.ClientRectangle);

            int y = -_scrollY;

            for (int i = 0; i < _sections.Count; i++)
            {
                var st = _sections[i];
                var rowRect = new Rectangle(0, y, _listHost.Width, _rowHeight);

                if (rowRect.Bottom > 0 && rowRect.Top < _listHost.Height)
                {
                    int alpha = (_dragging && i == _dragIndex) ? 100 : 255;
                    DrawRow(g, st, i, rowRect, alpha);
                }
                y += _rowHeight;
            }

            if (_dragging && _dragTargetIndex >= 0)
            {
                int lineY;
                if (_dragTargetIndex <= 0) lineY = -_scrollY;
                else if (_dragTargetIndex >= _sections.Count) lineY = -_scrollY + _sections.Count * _rowHeight;
                else lineY = -_scrollY + _dragTargetIndex * _rowHeight;

                if (lineY >= 0 && lineY <= _listHost.Height)
                {
                    using var pen = new Pen(colors.Accent, 2f);
                    g.DrawLine(pen, 4, lineY, _listHost.Width - 4, lineY);
                    using var brush = new SolidBrush(colors.Accent);
                    g.FillEllipse(brush, 2, lineY - 3, 6, 6);
                    g.FillEllipse(brush, _listHost.Width - 8, lineY - 3, 6, 6);
                }
            }
        }

        private void DrawRow(Graphics g, SectionTime st, int index, Rectangle rect, int alpha)
        {
            var colors = AppTheme.Colors;
            bool isSpecial = st.Type == "special";

            Color textColor1 = isSpecial ? colors.Accent : colors.TextPrimary;
            Color textColor2 = colors.TextPrimary;

            if (alpha < 255)
            {
                textColor1 = Color.FromArgb(alpha, textColor1);
                textColor2 = Color.FromArgb(alpha, textColor2);
            }

            using (var pen = new Pen(Color.FromArgb(alpha, colors.Divider), 1f))
                g.DrawLine(pen, 12, rect.Bottom - 1, rect.Right - 12, rect.Bottom - 1);

            DrawDragHandle(g, new Rectangle(4, rect.Y + rect.Height / 2 - 6, 6, 12), alpha);

            string leftText = isSpecial ? "◆" : string.Format(I18n.T("schedule.section"), st.Section);
            var leftRect = new Rectangle(18, rect.Y, 60, rect.Height);
            TextRenderer.DrawText(g, leftText, AppTheme.BodyFont, leftRect, textColor1,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            var nameRect = new Rectangle(80, rect.Y, 120, rect.Height);
            if (isSpecial && !string.IsNullOrEmpty(st.Name))
            {
                TextRenderer.DrawText(g, st.Name, AppTheme.BodyFont, nameRect, textColor2,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }

            var startRect = new Rectangle(210, rect.Y, 80, rect.Height);
            TextRenderer.DrawText(g, st.StartTime ?? "", AppTheme.BodyFont, startRect, textColor2,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            var endRect = new Rectangle(300, rect.Y, 80, rect.Height);
            TextRenderer.DrawText(g, st.EndTime ?? "", AppTheme.BodyFont, endRect, textColor2,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            int btnX = rect.Right - 100;
            int btnY = rect.Y + (rect.Height - 24) / 2;

            DrawSmallIcon(g, Icons.Delete, new Rectangle(btnX + 56, btnY, 24, 24), CanDelete(index), alpha);
        }

        private void DrawDragHandle(Graphics g, Rectangle rect, int alpha)
        {
            var color = Color.FromArgb((int)(alpha * 0.5f), AppTheme.Colors.TextSecondary);
            using var brush = new SolidBrush(color);

            for (int i = 0; i < 3; i++)
            {
                int y = rect.Y + i * 5;
                g.FillEllipse(brush, rect.X, y, 2, 2);
                g.FillEllipse(brush, rect.X + 4, y, 2, 2);
            }
        }

        private void DrawSmallIcon(Graphics g, string icon, Rectangle rect, bool enabled, int alpha = 255)
        {
            var color = enabled ? AppTheme.Colors.TextSecondary : AppTheme.Colors.TextDisabled;
            if (alpha < 255) color = Color.FromArgb(alpha, color);
            IconRenderer.Draw(g, icon, rect, color, 14);
        }

        private bool CanDelete(int index)
        {
            var st = _sections[index];
            if (st.Type == "special") return true;
            int normalCount = _sections.Count(s => s.Type == "normal");
            return normalCount > MinNormalSections;
        }

        private void ListHost_MouseDown(object? sender, MouseEventArgs e)
        {
            int index = (e.Y + _scrollY) / _rowHeight;
            if (index < 0 || index >= _sections.Count) return;
            var rect = new Rectangle(0, index * _rowHeight - _scrollY, _listHost.Width, _rowHeight);

            if (e.Button == MouseButtons.Right)
            {
                ShowRowMenu(index, e.Location);
                return;
            }

            int btnX = rect.Right - 100;
            int btnY = rect.Y + (rect.Height - 24) / 2;
            if (new Rectangle(btnX + 56, btnY, 24, 24).Contains(e.Location))
            {
                TryDelete(index);
                return;
            }

            if (e.X < 20)
            {
                _dragIndex = index;
                _dragTargetIndex = index;
                _dragging = true;
                _dragMouseY = e.Y;
                _listHost.Cursor = Cursors.Hand;
                _listHost.Invalidate();
                return;
            }

            if (e.Clicks == 2) EditRow(index);
        }

        private void ListHost_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!_dragging) return;
            _dragMouseY = e.Y;

            int rawIdx = (e.Y + _scrollY) / _rowHeight;
            int rowOffset = (e.Y + _scrollY) % _rowHeight;
            if (rowOffset > _rowHeight / 2) rawIdx++;

            if (rawIdx < 0) rawIdx = 0;
            if (rawIdx > _sections.Count) rawIdx = _sections.Count;

            if (rawIdx != _dragTargetIndex)
            {
                _dragTargetIndex = rawIdx;
                _listHost.Invalidate();
            }
        }

        private void ListHost_MouseUp(object? sender, MouseEventArgs e)
        {
            if (!_dragging) return;
            EndDrag(true);
        }

        private void EndDrag(bool commit)
        {
            if (!_dragging) return;

            if (commit && _dragIndex >= 0 && _dragTargetIndex >= 0)
            {
                int from = _dragIndex;
                int to = _dragTargetIndex;

                if (to != from && to != from + 1)
                {
                    var item = _sections[from];
                    _sections.RemoveAt(from);
                    if (to > from) to--;
                    if (to < 0) to = 0;
                    if (to > _sections.Count) to = _sections.Count;
                    _sections.Insert(to, item);
                }
            }

            _dragging = false;
            _dragIndex = -1;
            _dragTargetIndex = -1;
            _listHost.Cursor = Cursors.Default;
            _listHost.Invalidate();
        }

        private void ListHost_MouseWheel(object? sender, MouseEventArgs e)
        {
            _scrollY -= Math.Sign(e.Delta) * 60;
            int maxScroll = Math.Max(0, _sections.Count * _rowHeight - _listHost.Height);
            if (_scrollY < 0) _scrollY = 0;
            if (_scrollY > maxScroll) _scrollY = maxScroll;
            _scrollBar.Value = _scrollY;
            _scrollBar.Wake();
            _listHost.Invalidate();
        }

        private void TryDelete(int index)
        {
            if (!CanDelete(index))
            {
                MessageDialog.ShowInfo(I18n.T("dialog.info"),
                    string.Format(I18n.T("msg.minSections"), MinNormalSections));
                return;
            }
            _sections.RemoveAt(index);
            RebuildRows();
            SyncCountBox();
        }

        private void ShowRowMenu(int index, Point localPoint)
        {
            var menu = new FlatContextMenu();
            menu.AddItem(FlatMenuItem.Create(I18n.T("menu.edit"), () => EditRow(index), Icons.Edit));
            menu.AddSeparator();
            menu.AddItem(FlatMenuItem.Create(I18n.T("section.moveUp"), () => MoveRow(index, -1), Icons.ArrowUp));
            menu.AddItem(FlatMenuItem.Create(I18n.T("section.moveDown"), () => MoveRow(index, 1), Icons.ArrowDown));
            menu.AddSeparator();

            var delItem = FlatMenuItem.CreateDanger(I18n.T("menu.delete"), () => TryDelete(index), Icons.Delete);
            delItem.Enabled = CanDelete(index);
            menu.AddItem(delItem);

            menu.ShowAt(_listHost, _listHost.PointToScreen(localPoint));
        }

        private void MoveRow(int index, int delta)
        {
            int target = index + delta;
            if (target < 0 || target >= _sections.Count) return;
            var t = _sections[index];
            _sections[index] = _sections[target];
            _sections[target] = t;
            _listHost.Invalidate();
        }

        private void EditRow(int index)
        {
            var st = _sections[index];
            using var dlg = new SectionRowEditDialog(st);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _sections[index] = dlg.Result;
                _listHost.Invalidate();
                SyncCountBox();
            }
        }
    }

    // ============ 单行编辑弹窗 ============
    internal class SectionRowEditDialog : FlatDialogBase
    {
        public SectionTime Result { get; private set; } = new SectionTime();

        public SectionRowEditDialog(SectionTime st)
            : base(I18n.T("section.title"), 420, 380)
        {
            var typeBox = new FlatComboBox { Left = 110, Top = 10, Width = 240, Height = 32 };
            typeBox.SetItems(new IFlatComboItem[]
            {
                new FlatTextItem(I18n.T("section.normal")),
                new FlatTextItem(I18n.T("section.special")),
            });
            typeBox.SelectedIndex = st.Type == "special" ? 1 : 0;
            AddLabel(I18n.T("section.label"), 10);
            ContentPanel.Controls.Add(typeBox);

            var nameBox = new FlatTextBox { Left = 110, Top = 54, Width = 240, Height = 32 };
            nameBox.Text = st.Name ?? "";
            AddLabel(I18n.T("teacher.name"), 54);
            ContentPanel.Controls.Add(nameBox);

            var startBox = new FlatTimeBox { Left = 110, Top = 98, Width = 240, Height = 32 };
            startBox.Value = st.StartTime ?? "";
            AddLabel(I18n.T("section.start"), 98);
            ContentPanel.Controls.Add(startBox);

            var endBox = new FlatTimeBox { Left = 110, Top = 142, Width = 240, Height = 32 };
            endBox.Value = st.EndTime ?? "";
            AddLabel(I18n.T("section.end"), 142);
            ContentPanel.Controls.Add(endBox);

            var btnOk = new FlatButton
            {
                Text = I18n.T("dialog.ok"),
                ButtonStyle = FlatButtonStyle.Primary,
                Width = 100, Height = 36, Left = 140, Top = 250,
            };
            btnOk.Click += (s, e) =>
            {
                bool isSpecial = typeBox.SelectedIndex == 1;
                Result = new SectionTime
                {
                    Type = isSpecial ? "special" : "normal",
                    Section = isSpecial ? 0 : st.Section,
                    Name = isSpecial ? nameBox.Text.Trim() : "",
                    StartTime = startBox.Value,
                    EndTime = endBox.Value,
                };
                DialogResult = DialogResult.OK;
                Close();
            };
            ContentPanel.Controls.Add(btnOk);

            var btnCancel = new FlatButton
            {
                Text = I18n.T("dialog.cancel"),
                ButtonStyle = FlatButtonStyle.Secondary,
                Width = 100, Height = 36, Left = 250, Top = 250,
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            ContentPanel.Controls.Add(btnCancel);
        }

        private void AddLabel(string text, int top)
        {
            ContentPanel.Controls.Add(new Label
            {
                Text = text, Left = 20, Top = top + 6, Width = 80,
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.Colors.TextPrimary,
                BackColor = Color.Transparent,
                AutoSize = false,
            });
        }
    }
}