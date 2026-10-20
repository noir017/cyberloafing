using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Cyberloafing
{
    static class Smoke
    {
        public static int Run()
        {
            string log = Path.Combine(Path.GetTempPath(), "cyberloafing-smoke.txt");
            try
            {
                Pager pager = new Pager();
                pager.SetText("甲乙丙丁戊己庚辛壬癸\n第二行ABC\n\n第三章 测试");
                using (Bitmap bmp = new Bitmap(8, 8))
                using (Graphics g = Graphics.FromImage(bmp))
                using (Font font = new Font("Microsoft YaHei", 12f, FontStyle.Regular, GraphicsUnit.Point))
                using (StringFormat fmt = Pager.MeasureFormat())
                {
                    pager.Rebuild(g, font, 80, false);
                    if (pager.Count < 3) return Fail(log, "too few lines " + pager.Count);
                    if (pager.LineStart(0) != 0) return Fail(log, "start");
                    if (pager.PageText(0, 1).Length == 0) return Fail(log, "empty page");
                    if (pager.PrevIndex(pager.NextIndex(0, 1), 1) != 0) return Fail(log, "roundtrip");
                    if (pager.IndexForOffset(0) != 0) return Fail(log, "offset");
                    int end = pager.TextLength;
                    if (pager.IndexForOffset(end) < 0) return Fail(log, "end index");
                    for (int i = 0; i < pager.Count; i++)
                    {
                        string line = pager.LineContent(i);
                        if (line.Length <= 1) continue;
                        float w = g.MeasureString(line, font, 10000, fmt).Width;
                        if (w > 86f) return Fail(log, "overflow " + w.ToString(CultureInfo.InvariantCulture));
                    }
                    if (pager.NextIndex(pager.LastIndex(1), 1) != pager.LastIndex(1))
                        return Fail(log, "last sticks");
                }

                Regex re = new Regex(@"第[0-9０-９零一二三四五六七八九十百千万两]+[章节回部集]");
                if (!re.IsMatch("第三章 测试")) return Fail(log, "chapter regex");

                Settings s = new Settings();
                s.FontColor = Settings.ParseColor("#F3E6D0", Color.White);
                if (s.FontColor.R != 243) return Fail(log, "color");
                s.Opacity = 5;
                s.Clamp();
                if (s.Opacity != 1) return Fail(log, "clamp");

                File.WriteAllText(log, "ok lines=" + pager.Count, Encoding.ASCII);
                return 0;
            }
            catch (Exception ex)
            {
                return Fail(log, ex.ToString());
            }
        }

        static int Fail(string log, string message)
        {
            File.WriteAllText(log, message, Encoding.UTF8);
            return 1;
        }
    }
}
