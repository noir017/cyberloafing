using System;
using System.Drawing;
using System.Windows.Forms;

namespace Cyberloafing
{
    sealed class NovelDialog : Form
    {
        readonly string[] _paths;
        readonly ListBox _list = new ListBox();
        public string Chosen = "";

        NovelDialog(string[] paths, string currentPath)
        {
            _paths = paths;
            Text = "选择小说";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Ink.Bg;
            ForeColor = Ink.Text;
            Font = new Font("Microsoft YaHei", 9f);
            ClientSize = new Size(420, 460);

            Label title = new Label();
            title.Text = paths.Length == 0 ? "novel 文件夹是空的" : "选择小说";
            title.Font = new Font("KaiTi", 18f, FontStyle.Regular, GraphicsUnit.Point);
            title.ForeColor = Ink.Brass;
            title.BackColor = Ink.Bg;
            title.Bounds = new Rectangle(18, 12, 380, 30);
            Controls.Add(title);

            if (paths.Length == 0)
            {
                Label empty = new Label();
                empty.Text = "把 txt 文件放进程序旁边的 novel 文件夹，再打开这个列表。";
                empty.ForeColor = Ink.Muted;
                empty.BackColor = Ink.Bg;
                empty.Bounds = new Rectangle(20, 58, 380, 48);
                Controls.Add(empty);
            }

            _list.Visible = paths.Length > 0;
            _list.Bounds = new Rectangle(20, 56, 380, 340);
            _list.BackColor = Ink.Surface;
            _list.ForeColor = Ink.Text;
            _list.BorderStyle = BorderStyle.None;
            _list.IntegralHeight = false;
            _list.DrawMode = DrawMode.OwnerDrawFixed;
            _list.ItemHeight = 28;
            _list.DrawItem += DrawRow;
            for (int i = 0; i < paths.Length; i++)
            {
                _list.Items.Add(Library.KeyFor(paths[i]));
                if (!string.IsNullOrEmpty(currentPath)
                    && string.Equals(Library.KeyFor(paths[i]), Library.KeyFor(currentPath), StringComparison.OrdinalIgnoreCase))
                    _list.SelectedIndex = i;
            }
            _list.DoubleClick += delegate { Accept(); };
            _list.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter) Accept();
            };
            Controls.Add(_list);

            Button cancel = new Button();
            cancel.Text = "取消";
            cancel.DialogResult = DialogResult.Cancel;
            cancel.Bounds = new Rectangle(210, 412, 90, 32);
            cancel.FlatStyle = FlatStyle.Flat;
            cancel.FlatAppearance.BorderColor = Ink.Line;
            cancel.BackColor = Ink.Surface;
            cancel.ForeColor = Ink.Text;
            Controls.Add(cancel);

            Button open = new Button();
            open.Text = "打开";
            open.Bounds = new Rectangle(310, 412, 90, 32);
            open.FlatStyle = FlatStyle.Flat;
            open.FlatAppearance.BorderSize = 0;
            open.BackColor = Ink.Brass;
            open.ForeColor = Ink.BrassInk;
            open.Click += delegate { Accept(); };
            Controls.Add(open);
            AcceptButton = open;
            CancelButton = cancel;
            DialogDpi.Apply(this);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            int row = Font.Height + 12;
            if (row < 28) row = 28;
            _list.ItemHeight = row;
        }

        void DrawRow(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            bool on = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            using (SolidBrush bg = new SolidBrush(on ? Ink.Brass : Ink.Surface))
                e.Graphics.FillRectangle(bg, e.Bounds);
            using (SolidBrush fg = new SolidBrush(on ? Ink.BrassInk : Ink.Text))
            {
                int top = e.Bounds.Top + Math.Max(0, (e.Bounds.Height - e.Font.Height) / 2);
                e.Graphics.DrawString(_list.Items[e.Index].ToString(), e.Font, fg, e.Bounds.Left + 12, top);
            }
        }

        void Accept()
        {
            if (_list.SelectedIndex < 0 || _list.SelectedIndex >= _paths.Length) return;
            Chosen = _paths[_list.SelectedIndex];
            DialogResult = DialogResult.OK;
            Close();
        }

        public static string Pick(IWin32Window owner, string[] paths, string currentPath)
        {
            using (NovelDialog dlg = new NovelDialog(paths, currentPath))
            {
                if (dlg.ShowDialog(owner) != DialogResult.OK) return "";
                return dlg.Chosen ?? "";
            }
        }
    }
}
