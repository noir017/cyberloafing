using System;
using System.Collections.Generic;
using System.IO;

namespace Cyberloafing
{
    static class Library
    {
        public static void EnsureFolder()
        {
            if (!Directory.Exists(AppPaths.NovelDir))
                Directory.CreateDirectory(AppPaths.NovelDir);
        }

        public static string[] List()
        {
            EnsureFolder();
            string[] files = Directory.GetFiles(AppPaths.NovelDir, "*.txt", SearchOption.AllDirectories);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            return files;
        }

        public static string KeyFor(string fullPath)
        {
            string full = Path.GetFullPath(fullPath);
            string root = Path.GetFullPath(AppPaths.NovelDir);
            if (!root.EndsWith("\\"))
                root = root + "\\";
            if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return full.Substring(root.Length).Replace('\\', '/');
            return full;
        }

        public static string FullPath(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            if (Path.IsPathRooted(key))
                return key;
            return Path.Combine(AppPaths.NovelDir, key.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
