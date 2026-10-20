using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Cyberloafing
{
    sealed class ProgressStore
    {
        readonly Dictionary<string, int> _offsets = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public string Current = "";

        public int Get(string fullPath)
        {
            int offset;
            if (_offsets.TryGetValue(Library.KeyFor(fullPath), out offset))
                return offset;
            return 0;
        }

        public void Set(string fullPath, int offset)
        {
            if (offset < 0) offset = 0;
            string key = Library.KeyFor(fullPath);
            _offsets[key] = offset;
            Current = key;
        }

        public string ResolveCurrent()
        {
            string path = Library.FullPath(Current);
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                return path;
            return "";
        }

        public void Load(string path)
        {
            _offsets.Clear();
            Current = "";
            if (!File.Exists(path)) return;

            string legacyFile = "";
            int legacyOffset = -1;
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string key = line.Substring(0, eq).Trim();
                string val = line.Substring(eq + 1).Trim();
                if (key == "current")
                    Current = val;
                else if (key == "offset")
                    legacyOffset = ParseOffset(val);
                else if (key == "file")
                    legacyFile = val;
                else if (key == "item")
                    ReadItem(val);
            }

            if (_offsets.Count == 0 && legacyFile.Length > 0)
            {
                string key = Library.KeyFor(legacyFile);
                if (!File.Exists(legacyFile))
                {
                    string moved = Path.Combine(AppPaths.NovelDir, Path.GetFileName(legacyFile));
                    if (File.Exists(moved))
                        key = Library.KeyFor(moved);
                }
                _offsets[key] = legacyOffset < 0 ? 0 : legacyOffset;
                Current = key;
            }
        }

        public void Save(string path)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("current=" + Current);
            List<string> keys = new List<string>(_offsets.Keys);
            keys.Sort(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];
                sb.AppendLine("item=" + key + "|" + _offsets[key].ToString(CultureInfo.InvariantCulture));
            }
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        }

        void ReadItem(string val)
        {
            int bar = val.LastIndexOf('|');
            if (bar <= 0) return;
            string key = val.Substring(0, bar);
            int offset = ParseOffset(val.Substring(bar + 1));
            if (key.Length == 0 || offset < 0) return;
            _offsets[key] = offset;
        }

        static int ParseOffset(string val)
        {
            int n;
            if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out n))
                return n;
            return -1;
        }
    }
}
