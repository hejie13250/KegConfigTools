using System.Reflection;
using GroupBox = System.Windows.Controls.GroupBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Path = System.IO.Path;

namespace 小科狗配置.Page
{
  /// <summary>
  /// Abort.xaml 的交互逻辑
  /// </summary>
  public partial class Abort
  {
    #region 获取GroupBox的Header用于主窗口导航事件
    private void GroupBox_MouseEnter(object sender, MouseEventArgs e)
    {
      if (sender is not GroupBox groupBox) return;
      NameOfSelectedGroupBox = groupBox.Header.ToString();
    }

    #endregion

    private readonly string _appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);


    public Abort()
    {
      InitializeComponent();

    }

  }
}
