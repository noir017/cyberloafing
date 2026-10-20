using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Cyberloafing
{
    sealed class ChapterDialog : Form
    {
        readonly List<Chapter> _chapters;
        readonly ListBox _list = new ListBox();
        public int Chosen = -1;

        ChapterDialog(List<Chapter> chapters)
        {
            _chapters = chapters;
            Text = "章节";
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
            title.Text = chapters.Count == 0 ? "没有章节标题" : "章节";
            title.Font = new Font("KaiTi", 18f, FontStyle.Regular, GraphicsUnit.Point);
            title.ForeColor = Ink.Brass;
            title.BackColor = Ink.Bg;
            title.Bounds = new Rectangle(18, 12, 380, 30);
            Controls.Add(title);

            if (chapters.Count == 0)
            {
                Label empty = new Label();
                empty.Text = "按“第一章”“第2回”这种标题识别。这本里没有找到。";
                empty.ForeColor = Ink.Muted;
                empty.BackColor = Ink.Bg;
                empty.Bounds = new Rectangle(20, 58, 380, 40);
                Controls.Add(empty);
            }

            _list.Visible = chapters.Count > 0;
            _list.Bounds = new Rectangle(20, 56, 380, 340);
            _list.BackColor = Ink.Surface;
            _list.ForeColor = Ink.Text;
            _list.BorderStyle = BorderStyle.None;
            _list.IntegralHeight = false;
            _list.DrawMode = DrawMode.OwnerDrawFixed;
            _list.ItemHeight = 28;
            _list.DrawItem += DrawRow;
            for (int i = 0; i < chapters.Count; i++)
                _list.Items.Add(chapters[i].Title);
            _list.DoubleClick += delegate { Accept(); };
            _list.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter) Accept();
            };
            Controls.Add(_list);

            Button cancel = new Button();
            cancel.Text = "关闭";
            cancel.DialogResult = DialogResult.Cancel;
            cancel.Bounds = new Rectangle(310, 412, 90, 32);
            cancel.FlatStyle = FlatStyle.Flat;
            cancel.FlatAppearance.BorderColor = Ink.Line;
            cancel.BackColor = Ink.Surface;
            cancel.ForeColor = Ink.Text;
            Controls.Add(cancel);
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
            if (_list.SelectedIndex < 0 || _list.SelectedIndex >= _chapters.Count) return;
            Chosen = _chapters[_list.SelectedIndex].Offset;
            DialogResult = DialogResult.OK;
            Close();
        }

        public static int Pick(IWin32Window owner, List<Chapter> chapters)
        {
            using (ChapterDialog dlg = new ChapterDialog(chapters))
            {
                if (dlg.ShowDialog(owner) != DialogResult.OK) return -1;
                return dlg.Chosen;
            }
        }
    }
}
