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
    private static Mutex mutex = null;

    protected override void OnStartup(StartupEventArgs e)
    {
      const string appName = "小科狗配置";
      bool createdNew;

      mutex = new Mutex(true, appName, out createdNew);

      if (!createdNew)
      {
        // 应用程序的另一个实例已经在运行
        //MessageBox.Show("应用程序已在运行。");
        Current.Shutdown(); // 关闭当前实例
        return;
      }

      base.OnStartup(e);
      // 其他启动逻辑
    }



    //public new void Exit()
    //{
    //  this.MainWindow.Close();
    //  Application.Current.Shutdown();
    //}
  }

}
