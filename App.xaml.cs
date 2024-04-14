using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace 小科狗配置
{
  /// <summary>
  /// App.xaml 的交互逻辑
  /// </summary>
  public partial class App : Application
  {
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    protected override void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);

      _ = new Mutex(true, "小科狗配置", out bool isNewInstance);

      if (!isNewInstance)
      {
        // 已有实例运行，将焦点设置到已有实例
        IntPtr mainWindowHandle = Process.GetCurrentProcess().MainWindowHandle;
        if (mainWindowHandle != IntPtr.Zero)
        {
          SetForegroundWindow(mainWindowHandle);
        }
        Shutdown();
      }
    }


    public new void Exit()
    {
      this.MainWindow.Close();
      Application.Current.Shutdown();
    }
  }

}
