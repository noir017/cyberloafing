using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Text;

namespace Cyberloafing
{
    sealed class Pager
    {
        readonly List<int> _starts = new List<int>();
        string _text = "";
        public float LineHeight = 16f;

        public int Count
        {
            get { return _starts.Count; }
        }

        public int TextLength
        {
            get { return _text.Length; }
        }

        public string Text
        {
            get { return _text; }
        }

        public void SetText(string text)
        {
            if (text == null) text = "";
            _text = text.Replace("\r\n", "\n").Replace("\r", "\n");
            _starts.Clear();
        }

        public void Rebuild(Graphics g, Font font, int width, bool crisp)
        {
            _starts.Clear();
            g.TextRenderingHint = crisp
                ? TextRenderingHint.SingleBitPerPixelGridFit
                : TextRenderingHint.ClearTypeGridFit;
            LineHeight = font.GetHeight(g);
            if (LineHeight < 1f) LineHeight = font.Size;
            if (_text.Length == 0) return;
            if (width < 8) width = 8;

            using (StringFormat fmt = MeasureFormat())
            {
                float full = Math.Max(1f, g.MeasureString("国", font, 10000, fmt).Width);
                float half = Math.Max(1f, g.MeasureString("A", font, 10000, fmt).Width);
                int i = 0;
                int n = _text.Length;
                while (i < n)
                {
                    _starts.Add(i);
                    if (_text[i] == '\n')
                    {
                        i++;
                        continue;
                    }
                    float used = 0f;
                    int j = i;
                    while (j < n && _text[j] != '\n')
                    {
                        float cw = _text[j] < 128 ? half : full;
                        if (j > i && used + cw > width)
                            break;
                        used += cw;
                        j++;
                    }
                    while (j > i + 1 && g.MeasureString(_text.Substring(i, j - i), font, 10000, fmt).Width > width)
                        j--;
                    if (j <= i) j = i + 1;
                    i = j;
                }
            }
        }

        public string LineContent(int index)
        {
            if (index < 0 || index >= _starts.Count) return "";
            int a = _starts[index];
            int b = (index + 1 < _starts.Count) ? _starts[index + 1] : _text.Length;
            if (a >= _text.Length) return "";
            if (_text[a] == '\n') return "";
            if (b > a && _text[b - 1] == '\n') b--;
            if (b <= a) return "";
            return _text.Substring(a, b - a);
        }

        public string PageText(int lineIndex, int lines)
        {
            if (_starts.Count == 0 || lines <= 0) return "";
            if (lineIndex < 0) lineIndex = 0;
            if (lineIndex >= _starts.Count) lineIndex = _starts.Count - 1;
            int end = lineIndex + lines;
            if (end > _starts.Count) end = _starts.Count;
            StringBuilder sb = new StringBuilder();
            for (int i = lineIndex; i < end; i++)
            {
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(LineContent(i));
            }
            return sb.ToString();
        }

        public int NextIndex(int index, int pageLines)
        {
            int n = _starts.Count;
            if (n == 0) return 0;
            if (pageLines < 1) pageLines = 1;
            if (index < 0) index = 0;
            int last = n - pageLines;
            if (last < 0) last = 0;
            if (index >= last) return index;
            int next = index + pageLines;
            if (next > last) next = last;
            return next;
        }

        public int PrevIndex(int index, int pageLines)
        {
            if (pageLines < 1) pageLines = 1;
            int prev = index - pageLines;
            if (prev < 0) prev = 0;
            return prev;
        }

        public int LastIndex(int pageLines)
        {
            if (pageLines < 1) pageLines = 1;
            int last = _starts.Count - pageLines;
            if (last < 0) last = 0;
            return last;
        }

        public int IndexForOffset(int offset)
        {
            if (_starts.Count == 0) return 0;
            if (offset < 0) offset = 0;
            int lo = 0;
            int hi = _starts.Count - 1;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                if (_starts[mid] <= offset) lo = mid;
                else hi = mid - 1;
            }
            return lo;
        }

        public int OffsetOf(int index)
        {
            if (_starts.Count == 0) return 0;
            if (index < 0) index = 0;
            if (index >= _starts.Count) index = _starts.Count - 1;
            return _starts[index];
        }

        public int LineStart(int index)
        {
            return OffsetOf(index);
        }

        public static StringFormat MeasureFormat()
        {
            StringFormat fmt = (StringFormat)StringFormat.GenericTypographic.Clone();
            fmt.FormatFlags = fmt.FormatFlags | StringFormatFlags.MeasureTrailingSpaces;
            fmt.Trimming = StringTrimming.None;
            return fmt;
        }
    }
}
