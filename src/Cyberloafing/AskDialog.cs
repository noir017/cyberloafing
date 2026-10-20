using System;
using System.Drawing;
using System.Windows.Forms;

namespace Cyberloafing
{
    sealed class AskDialog : Form
    {
        readonly TextBox _box = new TextBox();
        public string Value;

        AskDialog(string title, string caption, string initial)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Ink.Bg;
            ForeColor = Ink.Text;
            Font = new Font("Microsoft YaHei", 9f);
            ClientSize = new Size(360, 148);

            Label heading = new Label();
            heading.Text = title;
            heading.Font = new Font("KaiTi", 18f, FontStyle.Regular, GraphicsUnit.Point);
            heading.ForeColor = Ink.Brass;
            heading.BackColor = Ink.Bg;
            heading.Bounds = new Rectangle(18, 12, 320, 30);
            Controls.Add(heading);

            Label cap = new Label();
            cap.Text = caption;
            cap.ForeColor = Ink.Muted;
            cap.BackColor = Ink.Bg;
            cap.Bounds = new Rectangle(20, 48, 320, 20);
            Controls.Add(cap);

            _box.Text = initial ?? "";
            _box.BorderStyle = BorderStyle.FixedSingle;
            _box.BackColor = Ink.Surface;
            _box.ForeColor = Ink.Text;
            _box.Bounds = new Rectangle(20, 72, 320, 24);
            Controls.Add(_box);

            Button cancel = new Button();
            cancel.Text = "取消";
            cancel.DialogResult = DialogResult.Cancel;
            cancel.Bounds = new Rectangle(168, 106, 80, 30);
            cancel.FlatStyle = FlatStyle.Flat;
            cancel.FlatAppearance.BorderColor = Ink.Line;
            cancel.BackColor = Ink.Surface;
            cancel.ForeColor = Ink.Text;
            Controls.Add(cancel);

            Button ok = new Button();
            ok.Text = "确定";
            ok.Bounds = new Rectangle(258, 106, 80, 30);
            ok.FlatStyle = FlatStyle.Flat;
            ok.FlatAppearance.BorderSize = 0;
            ok.BackColor = Ink.Brass;
            ok.ForeColor = Ink.BrassInk;
            ok.Click += delegate
            {
                Value = _box.Text;
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(ok);
            AcceptButton = ok;
            CancelButton = cancel;
            DialogDpi.Apply(this);
        }

        public static string Ask(IWin32Window owner, string title, string caption, string initial)
        {
            using (AskDialog dlg = new AskDialog(title, caption, initial))
            {
                if (dlg.ShowDialog(owner) != DialogResult.OK) return null;
                return dlg.Value == null ? "" : dlg.Value.Trim();
            }
        }
    }
}
