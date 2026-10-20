using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Cyberloafing
{
    static class DialogDpi
    {
        [DllImport("user32.dll")]
        static extern uint GetDpiForSystem();

        public static void Apply(Form form)
        {
            form.AutoScaleMode = AutoScaleMode.None;
            uint dpi = GetDpiForSystem();
            if (dpi <= 96) return;
            float scale = dpi / 96f;
            form.Scale(new SizeF(scale, scale));
        }
    }
}
