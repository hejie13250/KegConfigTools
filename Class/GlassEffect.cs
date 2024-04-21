using System;
using System.Runtime.InteropServices;

namespace 小科狗配置
{


  public class GlassEffect
  {
    [DllImport("Dwmapi.dll")]
    public static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

    [DllImport("Dwmapi.dll")]
    public static extern int DwmEnableBlurBehindWindow(IntPtr hWnd, ref DWM_BLURBEHIND pBlurBehind);

    [StructLayout(LayoutKind.Sequential)]
    public struct MARGINS
    {
      public int cxLeftWidth;
      public int cxRightWidth;
      public int cyTopHeight;
      public int cyBottomHeight;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DWM_BLURBEHIND
    {
      public uint dwFlags;
      public bool fEnable;
      public IntPtr hRgnBlur;
      public bool fTransitionOnMaximized;
    }
  }

}
