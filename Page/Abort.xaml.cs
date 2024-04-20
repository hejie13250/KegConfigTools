using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Page = System.Windows.Controls.Page;

namespace 小科狗配置
{
  /// <summary>
  /// Abort.xaml 的交互逻辑
  /// </summary>
  public partial class Abort : Page, INotifyPropertyChanged
  {

    #region 获取GroupBox的Header用于主窗口导航事件
    public event EventHandler<string> NameOfSelectedGroupBoxChanged;

    private string _NameOfSelectedGroupBox;
    public string NameOfSelectedGroupBox
    {
      get { return _NameOfSelectedGroupBox; }
      set
      {
        if (_NameOfSelectedGroupBox != value)
        {
          _NameOfSelectedGroupBox = value;
          OnPropertyChanged(nameof(NameOfSelectedGroupBox));
          // 触发事件
          NameOfSelectedGroupBoxChanged?.Invoke(this, _NameOfSelectedGroupBox);
        }
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


    private void GroupBox_MouseEnter(object sender, MouseEventArgs e)
    {
      if (sender is not GroupBox groupBox) return;
      NameOfSelectedGroupBox = groupBox.Header.ToString();
    }

    #endregion




    public Abort()
    {
      InitializeComponent();
      versionLabel.Content = $"当前版本：v {GetAssemblyVersion()}";
    }

    // 获取版本号
    public string GetAssemblyVersion()
    {
      Assembly assembly = Assembly.GetExecutingAssembly();
      Version version = assembly.GetName().Version;
      return version.ToString().Substring(0, 3);
    }



  }
}
