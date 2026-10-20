using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Cyberloafing
{
    sealed class ReaderForm : Form
    {
        const int GWL_EXSTYLE = -20;
        const int WS_EX_LAYERED = 0x80000;
        const int WS_EX_TRANSPARENT = 0x20;
        const int WS_EX_TOOLWINDOW = 0x80;
        const int LWA_COLORKEY = 0x1;
        const int LWA_ALPHA = 0x2;
        const int WM_HOTKEY = 0x0312;
        const int WM_MOUSEWHEEL = 0x020A;
        const int WM_DPICHANGED = 0x02E0;

        Settings _settings;
        int _dpi = 96;
        readonly Pager _pager = new Pager();
        Font _font;
        string _pageText = "";
        string _bookPath = "";
        bool _loaded;
        readonly ProgressStore _progress = new ProgressStore();
        int _lineIndex;
        bool _drag;
        bool _moved;
        Point _dragStart;
        Point _dragOrigin;
        readonly System.Windows.Forms.Timer _timer = new System.Windows.Forms.Timer();
        readonly NotifyIcon _tray = new NotifyIcon();
        readonly ContextMenuStrip _menu = new ContextMenuStrip();
        readonly ToolStripMenuItem _autoMenu = new ToolStripMenuItem("自动翻页");
        readonly ToolStripMenuItem _modeTranslucent = new ToolStripMenuItem("半透明底");
        readonly ToolStripMenuItem _modeText = new ToolStripMenuItem("只留文字");
        readonly ToolStripMenuItem _clickItem = new ToolStripMenuItem("鼠标穿透");
        string _lastQuery = "";
        bool _hover;
        WheelFilter _wheel;

        public bool ClickThrough
        {
            get { return _settings != null && _settings.ClickThrough; }
        }

        public ReaderForm()
        {
            _settings = Settings.Load(AppPaths.Settings);
            if (!File.Exists(AppPaths.Settings))
                _settings.Save(AppPaths.Settings);

            Text = "摸鱼阅读";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            KeyPreview = true;
            AllowDrop = true;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = _settings.BgColor;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            UpdateStyles();

            ApplyFont();
            BuildMenu();
            _tray.Icon = SystemIcons.Application;
            _tray.Text = "摸鱼阅读";
            _tray.Visible = true;
            _tray.ContextMenuStrip = _menu;
            _tray.DoubleClick += delegate { ToggleVisible(); };

            _timer.Tick += delegate { TurnNext(true); };
            _menu.Opening += delegate { RefreshChecks(); };
            _menu.Opened += delegate { _timer.Stop(); };
            _menu.Closed += delegate { if (!_hover) ApplyTimer(); };

            _wheel = new WheelFilter(this);
            Application.AddMessageFilter(_wheel);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_LAYERED | WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ReadDpi();
            ApplyPlacement();
            RegisterHotkeys();
            ApplyChrome();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            for (int id = 1; id <= 6; id++)
                UnregisterHotKey(Handle, id);
            base.OnHandleDestroyed(e);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            EnsureOnScreen();
            LoadInitialBook();
            ApplyTimer();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                _settings.X = Dip(Left);
                _settings.Y = Dip(Top);
                _settings.Save(AppPaths.Settings);
                SaveProgress();
            }
            catch
            {
            }
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Application.RemoveMessageFilter(_wheel);
            _timer.Stop();
            _timer.Dispose();
            _tray.Visible = false;
            _tray.Dispose();
            if (_font != null) _font.Dispose();
            base.OnFormClosed(e);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(_settings.BgColor);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_font == null) return;
            e.Graphics.TextRenderingHint = _settings.Translucent
                ? TextRenderingHint.ClearTypeGridFit
                : TextRenderingHint.SingleBitPerPixelGridFit;
            string raw = _pageText == null ? "" : _pageText;
            string[] lines = raw.Split('\n');
            float y = Px(1);
            float step = _pager.LineHeight > 1f ? _pager.LineHeight : _font.GetHeight(e.Graphics);
            using (SolidBrush brush = new SolidBrush(_settings.FontColor))
            using (StringFormat fmt = Pager.MeasureFormat())
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    e.Graphics.DrawString(lines[i], _font, brush, Px(2), y, fmt);
                    y += step;
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _drag = true;
                _moved = false;
                _dragStart = Cursor.Position;
                _dragOrigin = Location;
                Capture = true;
            }
            else if (e.Button == MouseButtons.Right)
            {
                _menu.Show(Cursor.Position);
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_drag)
            {
                int dx = Cursor.Position.X - _dragStart.X;
                int dy = Cursor.Position.Y - _dragStart.Y;
                if (Math.Abs(dx) + Math.Abs(dy) > 4) _moved = true;
                if (_moved) Location = new Point(_dragOrigin.X + dx, _dragOrigin.Y + dy);
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && _drag)
            {
                _drag = false;
                Capture = false;
                if (_moved)
                {
                    _settings.X = Dip(Left);
                    _settings.Y = Dip(Top);
                    try { _settings.Save(AppPaths.Settings); } catch { }
                }
                else
                {
                    TurnNext(false);
                }
            }
            base.OnMouseUp(e);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            _timer.Stop();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            if (!_drag) ApplyTimer();
            base.OnMouseLeave(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if ((e.Modifiers & Keys.Alt) == Keys.Alt)
            {
                base.OnKeyDown(e);
                return;
            }
            if (e.Control && e.KeyCode == Keys.O)
            {
                OpenBook();
                e.Handled = true;
                return;
            }
            if (e.Control && e.KeyCode == Keys.F)
            {
                Search(false);
                e.Handled = true;
                return;
            }
            if (e.KeyCode == Keys.F3)
            {
                Search(true);
                e.Handled = true;
                return;
            }
            if (e.KeyCode == Keys.Escape)
            {
                HideReader();
                e.Handled = true;
                return;
            }
            if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Down || e.KeyCode == Keys.Space || e.KeyCode == Keys.PageDown)
                TurnNext(false);
            else if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Up || e.KeyCode == Keys.PageUp)
                TurnPrev();
            else if (e.KeyCode == Keys.Home)
                GoOffset(0);
            else if (e.KeyCode == Keys.End)
                GoEnd();
            base.OnKeyDown(e);
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
            else e.Effect = DragDropEffects.None;
            base.OnDragEnter(e);
        }

        protected override void OnDragDrop(DragEventArgs e)
        {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null && files.Length > 0)
                SwitchTo(files[0]);
            base.OnDragDrop(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_DPICHANGED)
            {
                int dpi = m.WParam.ToInt32() & 0xFFFF;
                if (dpi >= 96 && dpi != _dpi)
                {
                    _dpi = dpi;
                    int offset = _pager.Count == 0 ? 0 : _pager.OffsetOf(_lineIndex);
                    ApplyPlacement();
                    ApplyChrome();
                    if (_font != null) RebuildKeep(offset);
                }
                return;
            }
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == 1) TurnNext(false);
                else if (id == 2) TurnPrev();
                else if (id == 3) ToggleVisible();
                else if (id == 4) Close();
                else if (id == 5) ChangeFont(1);
                else if (id == 6) ChangeFont(-1);
                return;
            }
            base.WndProc(ref m);
        }

        public void OnWheel(int delta)
        {
            if ((ModifierKeys & Keys.Control) == Keys.Control)
                ChangeFont(delta > 0 ? 1 : -1);
            else if (delta > 0)
                TurnPrev();
            else if (delta < 0)
                TurnNext(false);
        }

        void BuildMenu()
        {
            _menu.Font = new Font("Microsoft YaHei", 9f);
            AddItem("选择小说...", delegate { PickNovel(); });
            AddItem("打开文件...", delegate { OpenBook(); });
            AddItem("回到开头", delegate { GoOffset(0); });
            AddItem("跳到末尾", delegate { GoEnd(); });
            AddItem("跳到百分比...", delegate { JumpPercent(); });
            AddItem("查找...", delegate { Search(false); });
            AddItem("查找下一处", delegate { Search(true); });
            AddItem("章节...", delegate { PickChapter(); });
            AddItem("复制这一页", delegate { CopyPage(); });
            _menu.Items.Add(new ToolStripSeparator());

            int[] autos = new int[] { 0, 3000, 5000, 8000, 15000 };
            string[] labels = new string[] { "关闭", "3 秒", "5 秒", "8 秒", "15 秒" };
            for (int i = 0; i < autos.Length; i++)
            {
                int ms = autos[i];
                ToolStripMenuItem item = new ToolStripMenuItem(labels[i]);
                item.Tag = ms;
                item.Click += delegate { SetAuto(ms); };
                _autoMenu.DropDownItems.Add(item);
            }
            _menu.Items.Add(_autoMenu);

            _modeTranslucent.Click += delegate { SetTranslucent(true); };
            _modeText.Click += delegate { SetTranslucent(false); };
            ToolStripMenuItem mode = new ToolStripMenuItem("显示");
            mode.DropDownItems.Add(_modeTranslucent);
            mode.DropDownItems.Add(_modeText);
            _menu.Items.Add(mode);

            _clickItem.Click += delegate
            {
                _settings.ClickThrough = !_settings.ClickThrough;
                ApplyChrome();
                SaveSettings();
            };
            _menu.Items.Add(_clickItem);
            AddItem("隐藏", delegate { HideReader(); });
            AddItem("字号加大", delegate { ChangeFont(1); });
            AddItem("字号减小", delegate { ChangeFont(-1); });
            AddItem("阅读设置...", delegate { EditSettings(); });
            _menu.Items.Add(new ToolStripSeparator());
            AddItem("退出", delegate { Close(); });
        }

        void AddItem(string text, EventHandler onClick)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(text);
            item.Click += onClick;
            _menu.Items.Add(item);
        }

        void RefreshChecks()
        {
            foreach (ToolStripItem raw in _autoMenu.DropDownItems)
            {
                ToolStripMenuItem item = raw as ToolStripMenuItem;
                if (item == null || item.Tag == null) continue;
                item.Checked = (int)item.Tag == _settings.AutoMs;
            }
            _modeTranslucent.Checked = _settings.Translucent;
            _modeText.Checked = !_settings.Translucent;
            _clickItem.Checked = _settings.ClickThrough;
        }

        void LoadInitialBook()
        {
            Library.EnsureFolder();
            try { _progress.Load(AppPaths.Progress); }
            catch { }
            string start = _progress.ResolveCurrent();
            if (string.IsNullOrEmpty(start))
            {
                string[] books = Library.List();
                if (books.Length > 0) start = books[0];
            }
            if (!string.IsNullOrEmpty(start))
                LoadBook(start, _progress.Get(start));
            else
                ShowMessage("把 txt 小说放进 novel 文件夹。\n右键可以选择要读的那一本。");
        }

        void LoadBook(string path, int offset)
        {
            try
            {
                FileInfo info = new FileInfo(path);
                if (!info.Exists)
                {
                    ReportLoadError("找不到文件。");
                    return;
                }
                if (info.Length > 40L * 1024L * 1024L)
                {
                    ReportLoadError("文件超过 40MB，没有载入。");
                    return;
                }
                Cursor = Cursors.WaitCursor;
                string text = ReadText(path);
                _bookPath = path;
                _loaded = true;
                _pager.SetText(text);
                RebuildKeep(offset);
                SaveProgress();
            }
            catch (Exception ex)
            {
                ReportLoadError("没有读成这个文件。\n" + ex.Message);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        void ReportLoadError(string text)
        {
            if (_loaded)
                MessageBox.Show(this, text, "摸鱼阅读");
            else
                ShowMessage(text);
        }

        void ShowMessage(string text)
        {
            _pager.SetText("");
            _pageText = text;
            _lineIndex = 0;
            UpdateTray();
            Invalidate();
        }

        void RebuildKeep(int offset)
        {
            if (_font == null) return;
            int width = ClientSize.Width - Px(8);
            if (width < Px(8)) width = Px(8);
            using (Graphics g = CreateGraphics())
            {
                g.PageUnit = GraphicsUnit.Pixel;
                _pager.Rebuild(g, _font, width, !_settings.Translucent);
            }
            if (offset < 0) offset = 0;
            if (offset > _pager.TextLength) offset = _pager.TextLength;
            _lineIndex = _pager.IndexForOffset(offset);
            ShowPage();
        }

        void ShowPage()
        {
            if (_pager.TextLength == 0)
            {
                if (string.IsNullOrEmpty(_pageText))
                    _pageText = "这个文件是空的";
            }
            else
            {
                _pageText = _pager.PageText(_lineIndex, LinesPerPage());
            }
            UpdateTray();
            Invalidate();
        }

        int LinesPerPage()
        {
            float h = _pager.LineHeight;
            if (h < 1f) h = 16f;
            int n = (int)(ClientSize.Height / h);
            if (n < 1) n = 1;
            return n;
        }

        void TurnNext(bool fromTimer)
        {
            if (_pager.Count == 0) return;
            int next = _pager.NextIndex(_lineIndex, LinesPerPage());
            if (next == _lineIndex)
            {
                if (fromTimer) _timer.Stop();
                return;
            }
            _lineIndex = next;
            ShowPage();
            SaveProgress();
        }

        void TurnPrev()
        {
            if (_pager.Count == 0) return;
            int prev = _pager.PrevIndex(_lineIndex, LinesPerPage());
            if (prev == _lineIndex) return;
            _lineIndex = prev;
            ShowPage();
            SaveProgress();
            ApplyTimer();
        }

        void GoOffset(int offset)
        {
            if (_pager.Count == 0) return;
            _lineIndex = _pager.IndexForOffset(offset);
            ShowPage();
            SaveProgress();
        }

        void GoEnd()
        {
            if (_pager.Count == 0) return;
            _lineIndex = _pager.LastIndex(LinesPerPage());
            ShowPage();
            SaveProgress();
        }

        void OpenBook()
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "打开小说";
                dlg.Filter = "文本|*.txt;*.text;*.md;*.log|所有文件|*.*";
                if (!string.IsNullOrEmpty(_bookPath))
                {
                    string dir = Path.GetDirectoryName(_bookPath);
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                        dlg.InitialDirectory = dir;
                }
                if (string.IsNullOrEmpty(dlg.InitialDirectory))
                    dlg.InitialDirectory = AppPaths.NovelDir;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    SwitchTo(dlg.FileName);
            }
        }

        void PickNovel()
        {
            string chosen = NovelDialog.Pick(this, Library.List(), _bookPath);
            if (!string.IsNullOrEmpty(chosen))
                SwitchTo(chosen);
        }

        void SwitchTo(string path)
        {
            if (!string.IsNullOrEmpty(_bookPath))
            {
                string current = "";
                string next = "";
                try
                {
                    current = Path.GetFullPath(_bookPath);
                    next = Path.GetFullPath(path);
                }
                catch
                {
                    next = path;
                }
                if (current.Length > 0 && string.Equals(current, next, StringComparison.OrdinalIgnoreCase))
                    return;
                SaveProgress();
            }
            LoadBook(path, _progress.Get(path));
        }

        void JumpPercent()
        {
            if (_pager.TextLength == 0) return;
            double current = 100.0 * _pager.OffsetOf(_lineIndex) / _pager.TextLength;
            string initial = current.ToString("0.0", CultureInfo.InvariantCulture);
            string value = AskDialog.Ask(this, "跳到百分比", "输入 0 到 100", initial);
            if (value == null) return;
            double p;
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out p)
                && !double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out p))
            {
                MessageBox.Show(this, "请输入数字。", "摸鱼阅读");
                return;
            }
            if (p < 0) p = 0;
            if (p > 100) p = 100;
            int offset = (int)(_pager.TextLength * (p / 100.0));
            GoOffset(offset);
        }

        void Search(bool again)
        {
            if (_pager.TextLength == 0) return;
            string query = _lastQuery;
            if (!again || string.IsNullOrEmpty(query))
            {
                query = AskDialog.Ask(this, "查找", "查找文字", _lastQuery);
                if (string.IsNullOrEmpty(query)) return;
                _lastQuery = query;
            }
            string text = _pager.Text;
            int from = _pager.OffsetOf(_lineIndex) + 1;
            if (from < 0) from = 0;
            if (from > text.Length) from = text.Length;
            int found = text.IndexOf(query, from, StringComparison.OrdinalIgnoreCase);
            if (found < 0 && from > 0)
                found = text.IndexOf(query, 0, StringComparison.OrdinalIgnoreCase);
            if (found < 0)
            {
                MessageBox.Show(this, "没有找到。", "摸鱼阅读");
                return;
            }
            GoOffset(found);
        }

        void PickChapter()
        {
            if (_pager.TextLength == 0) return;
            List<Chapter> chapters = FindChapters(_pager.Text);
            int offset = ChapterDialog.Pick(this, chapters);
            if (offset >= 0) GoOffset(offset);
        }

        void CopyPage()
        {
            if (string.IsNullOrEmpty(_pageText)) return;
            try { Clipboard.SetText(_pageText); }
            catch { }
        }

        void EditSettings()
        {
            int offset = _pager.OffsetOf(_lineIndex);
            using (SettingsDialog dlg = new SettingsDialog(_settings.Clone()))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                _settings = dlg.Edited;
                ApplyPlacement();
                EnsureOnScreen();
                ApplyFont();
                ApplyChrome();
                RebuildKeep(offset);
                ApplyTimer();
                SaveSettings();
            }
        }

        void SetAuto(int ms)
        {
            _settings.AutoMs = ms;
            _settings.Clamp();
            ApplyTimer();
            SaveSettings();
        }

        void SetTranslucent(bool on)
        {
            if (_settings.Translucent == on) return;
            int offset = _pager.OffsetOf(_lineIndex);
            _settings.Translucent = on;
            ApplyChrome();
            RebuildKeep(offset);
            SaveSettings();
        }

        void ChangeFont(int delta)
        {
            int size = _settings.FontSize + delta;
            if (size < 8) size = 8;
            if (size > 72) size = 72;
            if (size == _settings.FontSize) return;
            int offset = _pager.OffsetOf(_lineIndex);
            _settings.FontSize = size;
            ApplyFont();
            RebuildKeep(offset);
            SaveSettings();
        }

        void ApplyFont()
        {
            Font next;
            try
            {
                next = new Font(_settings.FontName, _settings.FontSize, FontStyle.Regular, GraphicsUnit.Point);
            }
            catch
            {
                next = new Font("Microsoft YaHei", _settings.FontSize, FontStyle.Regular, GraphicsUnit.Point);
            }
            Font old = _font;
            _font = next;
            if (old != null) old.Dispose();
        }

        void ApplyChrome()
        {
            if (!IsHandleCreated) return;
            BackColor = _settings.BgColor;
            int ex = GetWindowLong(Handle, GWL_EXSTYLE);
            ex |= WS_EX_LAYERED | WS_EX_TOOLWINDOW;
            if (_settings.ClickThrough) ex |= WS_EX_TRANSPARENT;
            else ex &= ~WS_EX_TRANSPARENT;
            SetWindowLong(Handle, GWL_EXSTYLE, ex);

            if (_settings.Translucent)
            {
                int alpha = (int)Math.Round(_settings.Opacity * 255.0);
                if (alpha < 40) alpha = 40;
                if (alpha > 255) alpha = 255;
                SetLayeredWindowAttributes(Handle, 0, (byte)alpha, LWA_ALPHA);
            }
            else
            {
                SetLayeredWindowAttributes(Handle, ColorRef(_settings.BgColor), 255, LWA_COLORKEY);
            }
            Invalidate();
        }

        void ApplyTimer()
        {
            if (_hover) return;
            if (_settings.AutoMs >= 300)
            {
                _timer.Interval = _settings.AutoMs;
                _timer.Start();
            }
            else
            {
                _timer.Stop();
            }
        }

        void ToggleVisible()
        {
            if (Visible)
                HideReader();
            else
            {
                Show();
                Activate();
                ApplyChrome();
                Invalidate();
            }
        }

        void HideReader()
        {
            Hide();
        }

        void EnsureOnScreen()
        {
            Rectangle mine = new Rectangle(Left, Top, Math.Max(Width, 20), Math.Max(Height, 20));
            Screen[] screens = Screen.AllScreens;
            for (int i = 0; i < screens.Length; i++)
            {
                if (screens[i].WorkingArea.IntersectsWith(mine))
                    return;
            }
            Rectangle area = Screen.PrimaryScreen.WorkingArea;
            int w = ClientSize.Width;
            int margin = Px(16);
            if (w > area.Width - margin) w = area.Width - margin;
            Left = area.Left + Px(24);
            Top = area.Bottom - Height - Px(8);
            if (w != ClientSize.Width) ClientSize = new Size(w, ClientSize.Height);
            _settings.X = Dip(Left);
            _settings.Y = Dip(Top);
            _settings.SizeX = Dip(ClientSize.Width);
        }

        void ReadDpi()
        {
            uint dpi = GetDpiForWindow(Handle);
            if (dpi < 96) dpi = 96;
            _dpi = (int)dpi;
        }

        void ApplyPlacement()
        {
            int w = Px(_settings.SizeX);
            int h = Px(_settings.SizeY);
            if (w < 1) w = 1;
            if (h < 1) h = 1;
            ClientSize = new Size(w, h);
            Location = new Point(Px(_settings.X), Px(_settings.Y));
        }

        int Px(int dip)
        {
            return (int)Math.Round(dip * (_dpi / 96.0));
        }

        int Dip(int px)
        {
            return (int)Math.Round(px * 96.0 / _dpi);
        }

        void UpdateTray()
        {
            string name = string.IsNullOrEmpty(_bookPath) ? "摸鱼阅读" : Path.GetFileName(_bookPath);
            string tip = name;
            if (_pager.TextLength > 0)
            {
                double pct = 100.0 * _pager.OffsetOf(_lineIndex) / _pager.TextLength;
                tip = string.Format(CultureInfo.InvariantCulture, "{0}  {1:0.0}%", name, pct);
            }
            if (tip.Length > 63) tip = tip.Substring(0, 60) + "...";
            _tray.Text = tip;
        }

        void SaveSettings()
        {
            try { _settings.Save(AppPaths.Settings); }
            catch { }
        }

        void SaveProgress()
        {
            if (!_loaded || string.IsNullOrEmpty(_bookPath)) return;
            try
            {
                _progress.Set(_bookPath, _pager.OffsetOf(_lineIndex));
                _progress.Save(AppPaths.Progress);
            }
            catch
            {
            }
        }

        static string ReadText(string path)
        {
            byte[] data = File.ReadAllBytes(path);
            if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
                return Encoding.UTF8.GetString(data, 3, data.Length - 3);
            if (data.Length >= 2 && data[0] == 0xFF && data[1] == 0xFE)
                return Encoding.Unicode.GetString(data, 2, data.Length - 2);
            if (data.Length >= 2 && data[0] == 0xFE && data[1] == 0xFF)
                return Encoding.BigEndianUnicode.GetString(data, 2, data.Length - 2);
            try
            {
                UTF8Encoding utf8 = new UTF8Encoding(false, true);
                return utf8.GetString(data);
            }
            catch (DecoderFallbackException)
            {
                return Encoding.GetEncoding(936).GetString(data);
            }
        }

        static List<Chapter> FindChapters(string text)
        {
            List<Chapter> list = new List<Chapter>();
            Regex re = new Regex(@"第[0-9０-９零一二三四五六七八九十百千万两]+[章节回部集]");
            Match m = re.Match(text);
            int guard = 0;
            while (m.Success && guard < 5000)
            {
                int lineStart = text.LastIndexOf('\n', m.Index);
                lineStart = lineStart < 0 ? 0 : lineStart + 1;
                int lineEnd = text.IndexOf('\n', m.Index);
                if (lineEnd < 0) lineEnd = text.Length;
                string title = text.Substring(lineStart, lineEnd - lineStart).Trim();
                if (title.Length > 48) title = title.Substring(0, 48);
                if (list.Count == 0 || list[list.Count - 1].Offset != lineStart)
                {
                    Chapter ch = new Chapter();
                    ch.Title = title;
                    ch.Offset = lineStart;
                    list.Add(ch);
                }
                m = m.NextMatch();
                guard++;
            }
            return list;
        }

        void RegisterHotkeys()
        {
            uint mod = 0x0001 | 0x0002 | 0x4000;
            RegisterHotKey(Handle, 1, mod, 0x27);
            RegisterHotKey(Handle, 2, mod, 0x25);
            RegisterHotKey(Handle, 3, mod, 0x48);
            RegisterHotKey(Handle, 4, mod, 0x51);
            RegisterHotKey(Handle, 5, mod, 0x26);
            RegisterHotKey(Handle, 6, mod, 0x28);
        }

        static uint ColorRef(Color c)
        {
            return (uint)(c.R | (c.G << 8) | (c.B << 16));
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll")]
        static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        static extern uint GetDpiForWindow(IntPtr hwnd);

        sealed class WheelFilter : IMessageFilter
        {
            readonly ReaderForm _form;

            public WheelFilter(ReaderForm form)
            {
                _form = form;
            }

            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg != WM_MOUSEWHEEL) return false;
                if (!_form.Visible || _form.ClickThrough) return false;
                if (!_form.Bounds.Contains(Cursor.Position)) return false;
                Form active = Form.ActiveForm;
                if (active != null && active != _form) return false;
                int wparam = m.WParam.ToInt32();
                int delta = (short)((wparam >> 16) & 0xFFFF);
                _form.OnWheel(delta);
                return true;
            }
        }
    }
}
