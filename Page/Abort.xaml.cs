using System.Windows.Controls;
using System.Windows.Input;

namespace 小科狗配置
{
  /// <summary>
  /// Abort.xaml 的交互逻辑
  /// </summary>
  public partial class Abort : BasePage
  {

    #region 获取GroupBox的Header用于主窗口导航事件
    private void GroupBox_MouseEnter(object sender, MouseEventArgs e)
    {
      if (sender is not GroupBox groupBox) return;
      NameOfSelectedGroupBox = groupBox.Header.ToString();
    }

    #endregion




    public Abort()
    {
      InitializeComponent();

    }



  }
}
