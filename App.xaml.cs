using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace 小科狗配置
{
  /// <summary>
  /// App.xaml 的交互逻辑
  /// </summary>
  public partial class App : Application
  {
    public new void Exit()
    {
      this.MainWindow.Close();
      Application.Current.Shutdown();
    }
  }

}
