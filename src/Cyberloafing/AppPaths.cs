using System.IO;
using System.Windows.Forms;

namespace Cyberloafing
{
    static class AppPaths
    {
        public static string Dir
        {
            get { return Path.GetDirectoryName(Application.ExecutablePath); }
        }

        public static string Settings
        {
            get { return Path.Combine(Dir, "settings.ini"); }
        }

        public static string Progress
        {
            get { return Path.Combine(Dir, "progress.ini"); }
        }

        public static string NovelDir
        {
            get { return Path.Combine(Dir, "novel"); }
        }

        public static string Error
        {
            get { return Path.Combine(Dir, "error.log"); }
        }
    }
}
