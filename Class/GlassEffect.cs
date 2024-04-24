using System;
using System.Runtime.InteropServices;

namespace 小科狗配置.Class
{


  public class GlassEffect
  {
    [DllImport("Dwmapi.dll")]
    public static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref Margins pMarInset);

    [DllImport("Dwmapi.dll")]
    public static extern int DwmEnableBlurBehindWindow(IntPtr hWnd, ref DwmBlurbehind pBlurBehind);

    [StructLayout(LayoutKind.Sequential)]
    public struct Margins
    {
      public int cxLeftWidth;
      public int cxRightWidth;
      public int cyTopHeight;
      public int cyBottomHeight;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DwmBlurbehind
    {
      public uint dwFlags;
      public bool fEnable;
      public IntPtr hRgnBlur;
      public bool fTransitionOnMaximized;
    }
  }

}
