using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace 小科狗配置
{
  /// <summary>
  /// Instructions.xaml 的交互逻辑
  /// </summary>
  public partial class Instructions : Window
  {
    public Instructions()
    {
      InitializeComponent();
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
      // 只有当用户按下左键时才处理
      if (e.LeftButton == MouseButtonState.Pressed)
      {
        // 调用API使窗口跟随鼠标移动
        DragMove();
      }
    }
    // 确定
    private void Button_Click(object sender, RoutedEventArgs e)
    {
      this.Close();
    }
  }
}
