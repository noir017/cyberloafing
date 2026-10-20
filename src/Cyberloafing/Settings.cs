using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;

namespace Cyberloafing
{
    sealed class Settings
    {
        public int X = 80;
        public int Y = 960;
        public int SizeX = 680;
        public int SizeY = 52;
        public string FontName = "Microsoft YaHei";
        public int FontSize = 12;
        public Color FontColor = Color.White;
        public Color BgColor = Color.Black;
        public bool Translucent = true;
        public double Opacity = 0.5;
        public bool ClickThrough = false;
        public int AutoMs = 0;

        public static Settings Load(string path)
        {
            Settings s = new Settings();
            if (!File.Exists(path))
                return s;
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = StripComment(lines[i]);
                if (line.Length == 0)
                    continue;
                int eq = line.IndexOf('=');
                if (eq <= 0)
                    continue;
                string key = line.Substring(0, eq).Trim();
                string val = line.Substring(eq + 1).Trim();
                s.Apply(key, val);
            }
            s.Clamp();
            return s;
        }

        public void Save(string path)
        {
            Clamp();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("x_offset=" + X.ToString(CultureInfo.InvariantCulture) + "  // top-left, pixels at 100% scale");
            sb.AppendLine("y_offset=" + Y.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("size_x=" + SizeX.ToString(CultureInfo.InvariantCulture) + "  // width, pixels at 100% scale");
            sb.AppendLine("size_y=" + SizeY.ToString(CultureInfo.InvariantCulture) + "  // height, pixels at 100% scale; taller shows more lines");
            sb.AppendLine("font_name=" + FontName);
            sb.AppendLine("font_size=" + FontSize.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("font_color=" + FormatColor(FontColor));
            sb.AppendLine("bg_color=" + FormatColor(BgColor));
            sb.AppendLine("translucent=" + (Translucent ? "1" : "0") + "  // 1 tinted background, 0 text only");
            sb.AppendLine("opacity=" + Opacity.ToString("0.##", CultureInfo.InvariantCulture) + "  // 0.15 to 1 when translucent=1");
            sb.AppendLine("click_through=" + (ClickThrough ? "1" : "0") + "  // 1 sends clicks to the window behind");
            sb.AppendLine("auto_ms=" + AutoMs.ToString(CultureInfo.InvariantCulture) + "  // auto page interval, 0 off");
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        }

        public Settings Clone()
        {
            Settings s = new Settings();
            s.X = X;
            s.Y = Y;
            s.SizeX = SizeX;
            s.SizeY = SizeY;
            s.FontName = FontName;
            s.FontSize = FontSize;
            s.FontColor = FontColor;
            s.BgColor = BgColor;
            s.Translucent = Translucent;
            s.Opacity = Opacity;
            s.ClickThrough = ClickThrough;
            s.AutoMs = AutoMs;
            return s;
        }

        void Apply(string key, string val)
        {
            if (key == "x_offset") X = ParseInt(val, X);
            else if (key == "y_offset") Y = ParseInt(val, Y);
            else if (key == "size_x") SizeX = ParseInt(val, SizeX);
            else if (key == "size_y") SizeY = ParseInt(val, SizeY);
            else if (key == "font_name" && val.Length > 0) FontName = val;
            else if (key == "font_size") FontSize = ParseInt(val, FontSize);
            else if (key == "font_color") FontColor = ParseColor(val, FontColor);
            else if (key == "bg_color") BgColor = ParseColor(val, BgColor);
            else if (key == "translucent") Translucent = val == "1" || val == "true";
            else if (key == "opacity") Opacity = ParseDouble(val, Opacity);
            else if (key == "click_through") ClickThrough = val == "1" || val == "true";
            else if (key == "auto_ms") AutoMs = ParseInt(val, AutoMs);
        }

        public void Clamp()
        {
            if (SizeX < 160) SizeX = 160;
            if (SizeX > 4000) SizeX = 4000;
            if (SizeY < 18) SizeY = 18;
            if (SizeY > 2000) SizeY = 2000;
            if (FontSize < 8) FontSize = 8;
            if (FontSize > 72) FontSize = 72;
            if (FontName == null || FontName.Trim().Length == 0) FontName = "Microsoft YaHei";
            if (Opacity < 0.15) Opacity = 0.15;
            if (Opacity > 1) Opacity = 1;
            if (AutoMs < 0) AutoMs = 0;
            if (AutoMs > 0 && AutoMs < 300) AutoMs = 300;
            if (AutoMs > 120000) AutoMs = 120000;
        }

        static string StripComment(string line)
        {
            if (line == null) return "";
            int mark = line.IndexOf("//");
            if (mark >= 0) line = line.Substring(0, mark);
            return line.Trim();
        }

        static int ParseInt(string val, int fallback)
        {
            int n;
            if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out n))
                return n;
            return fallback;
        }

        static double ParseDouble(string val, double fallback)
        {
            double n;
            if (double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out n))
                return n;
            if (double.TryParse(val, NumberStyles.Float, CultureInfo.CurrentCulture, out n))
                return n;
            return fallback;
        }

        public static Color ParseColor(string val, Color fallback)
        {
            if (val == null) return fallback;
            val = val.Trim();
            if (val.Length == 0) return fallback;
            if (val.StartsWith("#") && (val.Length == 7 || val.Length == 9))
            {
                int rgb;
                if (!int.TryParse(val.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out rgb))
                    return fallback;
                if (val.Length == 7)
                    return Color.FromArgb(255, (rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255);
                return Color.FromArgb((rgb >> 24) & 255, (rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255);
            }
            Color named = Color.FromName(val);
            if (!named.IsKnownColor)
                return fallback;
            return named;
        }

        public static string FormatColor(Color color)
        {
            if (color.IsNamedColor && color.A == 255)
                return color.Name.ToLowerInvariant();
            return string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B);
        }
    }
}
