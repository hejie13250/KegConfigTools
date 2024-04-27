using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace 小科狗配置.Class
{
    public class ClipboardMonitor
    {
        #region Win32 Interface
        [DllImport("user32.dll")]
        public static extern bool EmptyClipboard();
        [DllImport("user32.dll", SetLastError = true)]
        private extern static IntPtr SetClipboardData(uint format, IntPtr handle);
        [DllImport("user32.dll")]
        static extern IntPtr GetClipboardData(uint uFormat);
        [DllImport("user32.dll")]
        static extern bool IsClipboardFormatAvailable(uint format);
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool OpenClipboard(IntPtr hWndNewOwner);
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool CloseClipboard();
        [DllImport("kernel32.dll")]
        static extern IntPtr GlobalLock(IntPtr hMem);
        [DllImport("kernel32.dll")]
        static extern bool GlobalUnlock(IntPtr hMem);

        public const uint CF_UNICODETEXT = 13;
        #endregion

        public static bool CopyToClipboard(uint id, string content)
        {
            if (OpenClipboard(IntPtr.Zero))
            {
                EmptyClipboard();
                IntPtr hmem = Marshal.StringToHGlobalUni(content);
                var ptr = GlobalLock(hmem);
                GlobalUnlock(ptr);
                SetClipboardData(id, ptr);
                CloseClipboard();
                return true;
            }
            return false;
        }

        public static string GetFromClipboard(uint id)
        {
            if (!IsClipboardFormatAvailable(id)) return null;
            if (!OpenClipboard(IntPtr.Zero)) return null;

            string data = null;
            var hGlobal = GetClipboardData(id);
            if (hGlobal != IntPtr.Zero)
            {
                var lpwcstr = GlobalLock(hGlobal);
                if (lpwcstr != IntPtr.Zero)
                {
                    data = Marshal.PtrToStringAuto(lpwcstr);
                    GlobalUnlock(lpwcstr);
                }
            }
            CloseClipboard();

            return data;
        }
    }
}
