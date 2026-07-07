using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Editor
{
    public static class TextBoxExtensions
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);

        public static void BeginUpdate(this TextBoxBase textBox) => SendMessage(textBox.Handle, 0x000B, IntPtr.Zero, IntPtr.Zero); // WM_SETREDRAW False
        public static void EndUpdate(this TextBoxBase textBox)
        {
            SendMessage(textBox.Handle, 0x000B, new IntPtr(1), IntPtr.Zero); // WM_SETREDRAW True
            textBox.Invalidate();
        }
    }
}
