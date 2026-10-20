using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace Cyberloafing
{
    sealed class SettingsDialog : Form
    {
        public Settings Edited;
        readonly TextBox _x = new TextBox();
        readonly TextBox _y = new TextBox();
        readonly TextBox _w = new TextBox();
        readonly TextBox _h = new TextBox();
        readonly TextBox _fontName = new TextBox();
        readonly TextBox _fontSize = new TextBox();
        readonly TextBox _fontColor = new TextBox();
        readonly TextBox _bgColor = new TextBox();
        readonly TextBox _opacity = new TextBox();
        readonly TextBox _auto = new TextBox();
        readonly CheckBox _translucent = new CheckBox();
        readonly CheckBox _click = new CheckBox();

        public SettingsDialog(Settings edited)
        {
            Edited = edited;
            Text = "阅读设置";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Ink.Bg;
            ForeColor = Ink.Text;
            Font = new Font("Microsoft YaHei", 9f);
            ClientSize = new Size(460, 468);

            Label title = new Label();
            title.Text = "阅读条";
            title.Font = new Font("KaiTi", 20f, FontStyle.Regular, GraphicsUnit.Point);
            title.ForeColor = Ink.Brass;
            title.BackColor = Ink.Bg;
            title.Bounds = new Rectangle(22, 14, 200, 34);
            Controls.Add(title);

            Label hint = Hint("拖动屏幕上的文字可以挪位置。窗口加高后，一页会显示多行。", 50);
            Controls.Add(hint);

            AddPair("位置", "X", _x, edited.X.ToString(CultureInfo.InvariantCulture), "Y", _y, edited.Y.ToString(CultureInfo.InvariantCulture), 82);
            AddPair("尺寸", "宽", _w, edited.SizeX.ToString(CultureInfo.InvariantCulture), "高", _h, edited.SizeY.ToString(CultureInfo.InvariantCulture), 118);

            Label fontLabel = FieldLabel("字体", 154);
            Controls.Add(fontLabel);
            StyleBox(_fontName, edited.FontName);
            _fontName.Bounds = new Rectangle(78, 154, 180, 24);
            Controls.Add(_fontName);
            StyleBox(_fontSize, edited.FontSize.ToString(CultureInfo.InvariantCulture));
            _fontSize.Bounds = new Rectangle(266, 154, 48, 24);
            Controls.Add(_fontSize);
            Button pickFont = BrassButton("选择", 322, 152, 112);
            pickFont.Click += delegate { ChooseFont(); };
            Controls.Add(pickFont);

            Label colorLabel = FieldLabel("颜色", 196);
            Controls.Add(colorLabel);
            StyleBox(_fontColor, Settings.FormatColor(edited.FontColor));
            _fontColor.Bounds = new Rectangle(78, 196, 110, 24);
            Controls.Add(_fontColor);
            Button pickFg = QuietButton("字色", 196, 196, 70);
            pickFg.Click += delegate { ChooseColor(_fontColor, edited.FontColor); };
            Controls.Add(pickFg);
            StyleBox(_bgColor, Settings.FormatColor(edited.BgColor));
            _bgColor.Bounds = new Rectangle(274, 196, 90, 24);
            Controls.Add(_bgColor);
            Button pickBg = QuietButton("底色", 372, 196, 64);
            pickBg.Click += delegate { ChooseColor(_bgColor, edited.BgColor); };
            Controls.Add(pickBg);

            _translucent.Text = "半透明底";
            _translucent.Checked = edited.Translucent;
            _translucent.ForeColor = Ink.Text;
            _translucent.BackColor = Ink.Bg;
            _translucent.Bounds = new Rectangle(74, 236, 110, 24);
            Controls.Add(_translucent);
            Label opLabel = FieldLabel("不透明度", 236);
            opLabel.Bounds = new Rectangle(190, 236, 72, 24);
            Controls.Add(opLabel);
            StyleBox(_opacity, edited.Opacity.ToString("0.##", CultureInfo.InvariantCulture));
            _opacity.Bounds = new Rectangle(266, 236, 64, 24);
            Controls.Add(_opacity);

            Label autoLabel = FieldLabel("自动翻页", 276);
            autoLabel.Bounds = new Rectangle(22, 276, 84, 24);
            Controls.Add(autoLabel);
            StyleBox(_auto, edited.AutoMs.ToString(CultureInfo.InvariantCulture));
            _auto.Bounds = new Rectangle(110, 276, 72, 24);
            Controls.Add(_auto);
            Label autoHint = Hint("毫秒，0 为关闭", 276);
            autoHint.Bounds = new Rectangle(190, 276, 250, 24);
            Controls.Add(autoHint);

            _click.Text = "鼠标穿透，点下去会落到后面的窗口";
            _click.Checked = edited.ClickThrough;
            _click.ForeColor = Ink.Text;
            _click.BackColor = Ink.Bg;
            _click.Bounds = new Rectangle(74, 316, 360, 24);
            Controls.Add(_click);

            Label keys = Hint("Ctrl+Alt+左/右 翻页    Ctrl+Alt+H 隐藏    Ctrl+Alt+Q 退出", 358);
            keys.ForeColor = Ink.Muted;
            Controls.Add(keys);

            Button cancel = QuietButton("取消", 248, 408, 90);
            cancel.DialogResult = DialogResult.Cancel;
            Controls.Add(cancel);
            Button ok = BrassButton("保存", 348, 408, 90);
            ok.Click += delegate { TrySave(); };
            Controls.Add(ok);
            AcceptButton = ok;
            CancelButton = cancel;
            DialogDpi.Apply(this);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _x.SelectionStart = _x.TextLength;
            _x.SelectionLength = 0;
        }

        void TrySave()
        {
            int x, y, w, h, fontSize, auto;
            if (!TryReadInt(_x, "位置 X", out x)) return;
            if (!TryReadInt(_y, "位置 Y", out y)) return;
            if (!TryReadInt(_w, "宽度", out w)) return;
            if (!TryReadInt(_h, "高度", out h)) return;
            if (!TryReadInt(_fontSize, "字号", out fontSize)) return;
            if (!TryReadInt(_auto, "自动翻页", out auto)) return;
            double opacity;
            if (!double.TryParse(_opacity.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out opacity)
                && !double.TryParse(_opacity.Text.Trim(), NumberStyles.Float, CultureInfo.CurrentCulture, out opacity))
            {
                MessageBox.Show(this, "不透明度要是数字，例如 0.5。", "阅读设置");
                return;
            }
            Edited.X = x;
            Edited.Y = y;
            Edited.SizeX = w;
            Edited.SizeY = h;
            Edited.FontName = _fontName.Text.Trim();
            Edited.FontSize = fontSize;
            Edited.FontColor = Settings.ParseColor(_fontColor.Text, Edited.FontColor);
            Edited.BgColor = Settings.ParseColor(_bgColor.Text, Edited.BgColor);
            Edited.Translucent = _translucent.Checked;
            Edited.Opacity = opacity;
            Edited.ClickThrough = _click.Checked;
            Edited.AutoMs = auto;
            Edited.Clamp();
            DialogResult = DialogResult.OK;
            Close();
        }

        bool TryReadInt(TextBox box, string name, out int n)
        {
            if (int.TryParse(box.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out n))
            {
                box.BackColor = Ink.Surface;
                return true;
            }
            box.BackColor = Color.FromArgb(92, 48, 36);
            MessageBox.Show(this, name + "要是整数。", "阅读设置");
            n = 0;
            return false;
        }

        void ChooseFont()
        {
            using (FontDialog dlg = new FontDialog())
            {
                dlg.ShowColor = false;
                dlg.Font = new Font(Edited.FontName, Math.Max(8, Edited.FontSize), FontStyle.Regular, GraphicsUnit.Point);
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                _fontName.Text = dlg.Font.Name;
                _fontSize.Text = ((int)Math.Round(dlg.Font.Size)).ToString(CultureInfo.InvariantCulture);
            }
        }

        void ChooseColor(TextBox box, Color current)
        {
            using (ColorDialog dlg = new ColorDialog())
            {
                dlg.Color = Settings.ParseColor(box.Text, current);
                dlg.FullOpen = true;
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                box.Text = Settings.FormatColor(dlg.Color);
            }
        }

        void AddPair(string name, string a, TextBox boxA, string va, string b, TextBox boxB, string vb, int y)
        {
            Controls.Add(FieldLabel(name, y));
            Label la = Hint(a, y);
            la.Bounds = new Rectangle(78, y, 24, 24);
            Controls.Add(la);
            StyleBox(boxA, va);
            boxA.Bounds = new Rectangle(104, y, 72, 24);
            Controls.Add(boxA);
            Label lb = Hint(b, y);
            lb.Bounds = new Rectangle(196, y, 24, 24);
            Controls.Add(lb);
            StyleBox(boxB, vb);
            boxB.Bounds = new Rectangle(222, y, 72, 24);
            Controls.Add(boxB);
        }

        static Label FieldLabel(string text, int y)
        {
            Label label = new Label();
            label.Text = text;
            label.ForeColor = Ink.Muted;
            label.BackColor = Ink.Bg;
            label.Bounds = new Rectangle(22, y, 52, 24);
            label.TextAlign = ContentAlignment.MiddleLeft;
            return label;
        }

        static Label Hint(string text, int y)
        {
            Label label = new Label();
            label.Text = text;
            label.ForeColor = Ink.Muted;
            label.BackColor = Ink.Bg;
            label.Bounds = new Rectangle(22, y, 416, 22);
            return label;
        }

        static void StyleBox(TextBox box, string text)
        {
            box.Text = text;
            box.BorderStyle = BorderStyle.FixedSingle;
            box.BackColor = Ink.Surface;
            box.ForeColor = Ink.Text;
        }

        static Button BrassButton(string text, int x, int y, int w)
        {
            Button button = new Button();
            button.Text = text;
            button.Bounds = new Rectangle(x, y, w, 32);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = Ink.Brass;
            button.ForeColor = Ink.BrassInk;
            return button;
        }

        static Button QuietButton(string text, int x, int y, int w)
        {
            Button button = new Button();
            button.Text = text;
            button.Bounds = new Rectangle(x, y, w, 32);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Ink.Line;
            button.BackColor = Ink.Surface;
            button.ForeColor = Ink.Text;
            return button;
        }
    }
}
