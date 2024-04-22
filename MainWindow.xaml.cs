using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Button = System.Windows.Controls.Button;
using Clipboard = System.Windows.Clipboard;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using FormsDialogResult = System.Windows.Forms.DialogResult;
using GroupBox = System.Windows.Controls.GroupBox;
using Label = System.Windows.Controls.Label;
using ListView = System.Windows.Controls.ListView;
using ListViewItem = System.Windows.Controls.ListViewItem;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Path = System.IO.Path;
using Point = System.Windows.Point;
using RadioButton = System.Windows.Controls.RadioButton;
using TextBox = System.Windows.Controls.TextBox;
using Thumb = System.Windows.Controls.Primitives.Thumb;
using Window = System.Windows.Window;
using System.Windows.Navigation;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;
using System.Windows.Interop;
using System.Xml.Linq;

namespace 小科狗配置
{
  /// <summary>
  /// MainWindow.xaml 的交互逻辑
  /// </summary>
  /// 
  public partial class MainWindow : Window
  {
    static readonly string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    StackPanel leftStackPanel;

    readonly string[] _方案设置页面 = new string[] { "候选框配色", "候选框样式", "字体和渲染", "码元设置", "标点设置", "动作设置", "顶功设置", "上屏设置", "中英切换", "翻页按键", "词语联想", "码表调频与检索", "重复上屏", "自动造词", "临时码表检索", "引导码表检索" };
    readonly string[] _全局设置页面 = new string[] { "状态条", "在线查找", "外部工具", "快捷命令", "快捷键", "自启动应用", "定时关机", "其它选项" };
    readonly string[] _帮助页面     = new string[] { "关于", "全局设置说明" };
    readonly string[] _打字统计     = new string[] { "曲线图", "今日和累计数据" };
    readonly string[] _码表设置     = new string[] { "码表修改" };

    #region 初始化
    public MainWindow()
    {
      if (!Directory.Exists($"{appPath}\\configs")) Directory.CreateDirectory($"{appPath}\\configs");
      InitializeComponent();

      title.Text = $"{title.Text} v {GetAssemblyVersion()}";  // 标题加上版本号
      Base.GetKegPath();                                      // 获取小科狗主程序目录

      this.Width    = 930;
      this.Height   = 800;
      frame1.Height = this.Height - 50;
      frame2.Height = this.Height - 50;
      frame3.Height = this.Height - 50;
      frame4.Height = this.Height - 50;
      frame5.Height = this.Height - 50;

      方案设置页面();

      frame1.Navigated += Frame1_Navigated;
      frame2.Navigated += Frame2_Navigated;
      frame3.Navigated += Frame3_Navigated;
      frame4.Navigated += Frame4_Navigated;
      frame5.Navigated += Frame5_Navigated;

    }

    // 获取版本号
    public string GetAssemblyVersion()
    {
      Assembly assembly = Assembly.GetExecutingAssembly();
      Version  version  = assembly.GetName().Version;
      return   version.ToString().Substring(0, 5);
    }
    #endregion

    #region 点击 RadioButton 跳转到指定的 GroupBox
    private void LeftRadioButton1_Click(object sender, RoutedEventArgs e)
    {
      RadioButton radioButton = sender as RadioButton;
      StackPanel stackPanel   = radioButton.Content as StackPanel;
      TextBlock textBlock     = stackPanel.Children.OfType<TextBlock>().FirstOrDefault();

      switch (textBlock.Text)
      {
        case "方案设置": 方案设置页面(); break;
        case "全局设置": 全局设置页面(); break;
        case "帮助"    : 帮助页面();     break;
        case "打字统计": 打字统计();     break;
        case "码表设置": 码表设置();     break;
      }
    }

    private void LeftRadioButton2_Click(object sender, RoutedEventArgs e)
    {
      RadioButton radioButton = sender as RadioButton;
      StackPanel stackPanel   = radioButton.Content as StackPanel;
      TextBlock textBlock     = stackPanel.Children.OfType<TextBlock>().FirstOrDefault();
      string name             = textBlock.Text;

      if (_方案设置页面.Contains(name)) { 方案设置页面();   ScrollViewerOffset(name, 1); }
      if (_全局设置页面.Contains(name)) { 全局设置页面();   ScrollViewerOffset(name, 2); }
      if (_帮助页面    .Contains(name)) { 帮助页面    ();   ScrollViewerOffset(name, 3); }
      if (_打字统计    .Contains(name)) { 打字统计    ();   ScrollViewerOffset(name, 4); }
      if (_码表设置    .Contains(name)) { 码表设置    ();   ScrollViewerOffset(name, 5); }

    }


    private void 方案设置页面()
    {
      SetPageVisibility(
      Visibility.Visible,
      Visibility.Collapsed,
      Visibility.Collapsed, 
      Visibility.Collapsed,
      Visibility.Collapsed,
      leftStackPanel1
      ); 
    }

    private void 全局设置页面()
    {
      SetPageVisibility(
      Visibility.Collapsed,
      Visibility.Visible,
      Visibility.Collapsed,
      Visibility.Collapsed,
      Visibility.Collapsed,
      leftStackPanel2
      );
    }

    private void 帮助页面()
    {
      SetPageVisibility(
      Visibility.Collapsed,
      Visibility.Collapsed, 
      Visibility.Visible,
      Visibility.Collapsed, 
      Visibility.Collapsed, 
      leftStackPanel3
      );
    }

    private void 打字统计()
    {
      SetPageVisibility(
      Visibility.Collapsed,
      Visibility.Collapsed, 
      Visibility.Collapsed,
      Visibility.Visible,
      Visibility.Collapsed,
      leftStackPanel4
      );
    }

    private void 码表设置()
    {
      SetPageVisibility(
      Visibility.Collapsed, 
      Visibility.Collapsed, 
      Visibility.Collapsed, 
      Visibility.Collapsed, 
      Visibility.Visible, 
      leftStackPanel5
      );
      
    }

    private void SetPageVisibility(Visibility f1, Visibility f2, Visibility f3, Visibility f4, Visibility f5, StackPanel activePanel)
    {
      frame1.Width = f1 == Visibility.Visible ? 770 : 0;
      frame2.Width = f2 == Visibility.Visible ? 770 : 0;
      frame3.Width = f3 == Visibility.Visible ? 770 : 0;
      frame4.Width = f4 == Visibility.Visible ? 770 : 0;
      frame5.Width = f5 == Visibility.Visible ? 770 : 0;

      frame1.Visibility = f1;
      frame2.Visibility = f2;
      frame3.Visibility = f3;
      frame4.Visibility = f4;
      frame5.Visibility = f5;

      leftStackPanel1.Visibility = Visibility.Collapsed;
      leftStackPanel2.Visibility = Visibility.Collapsed;
      leftStackPanel3.Visibility = Visibility.Collapsed;
      leftStackPanel4.Visibility = Visibility.Collapsed;
      leftStackPanel5.Visibility = Visibility.Collapsed;

      activePanel.Visibility = Visibility.Visible;
      leftStackPanel = activePanel;
    }

    /// <summary>
    /// 点击左侧的控件偏移右侧滚动条（滚动页面）
    /// </summary>
    /// <param name="text">左侧被点击的控件名</param>
    /// <param name="n">右侧第几个滚动条</param>
    private void ScrollViewerOffset(string content, int n)
    {
      GroupBox groupBox = null;

      // 根据n的值，选择正确的Frame
      if (n == 1)
      {
        var page = frame1.Content as Page;
        var groupBoxStackPanel = page?.FindName("groupBoxStackPanel") as StackPanel;
        groupBox = FindGroupBox(content, groupBoxStackPanel);
      }
      else if (n == 2)
      {
        var page = frame2.Content as Page;
        var groupBoxStackPanel = page?.FindName("groupBoxStackPanel") as StackPanel;
        groupBox = FindGroupBox(content, groupBoxStackPanel);
      }
      else if (n == 3)
      {
        var page = frame3.Content as Page;
        var groupBoxStackPanel = page?.FindName("groupBoxStackPanel") as StackPanel;
        groupBox = FindGroupBox(content, groupBoxStackPanel);
      }
      else if (n == 4)
      {
        var page = frame4.Content as Page;
        var groupBoxStackPanel = page?.FindName("groupBoxStackPanel") as StackPanel;
        groupBox = FindGroupBox(content, groupBoxStackPanel);
      }
      else if (n == 5)
      {
        var page = frame5.Content as Page;
        var groupBoxStackPanel = page?.FindName("groupBoxStackPanel") as StackPanel;
        groupBox = FindGroupBox(content, groupBoxStackPanel);
      }
      // 如果找到了GroupBox，将其滚动到视图中
      groupBox?.BringIntoView();
    }


    /// <summary>
    /// 查找指定容器 StackPanel 内指定 content 的 GroupBox
    /// </summary>
    /// <param name="content">GroupBox的content</param>
    /// <param name="stackPanel">GroupBox所在的StackPanel容器</param>
    /// <returns></returns>
    private GroupBox FindGroupBox(string content, StackPanel groupBoxStackPanel)
    {
      foreach (var child in groupBoxStackPanel.Children)
      {
        if (child is GroupBox groupBox && groupBox.Header.ToString() == content)
        {
          return groupBox;
        }
      }
      return null;
    }

    #endregion

    #region 鼠标移到 GroupBox 时选中指定 RadioButton
    private void Frame1_Navigated(object sender, NavigationEventArgs e)
    {
      if (e.Content is SchemeSetting page)
        page.NameOfSelectedGroupBoxChanged += Frame_NameOfSelectedGroupBoxChanged;
    }
    private void Frame2_Navigated(object sender, NavigationEventArgs e)
    {
      if (e.Content is GlobalSetting page)
        page.NameOfSelectedGroupBoxChanged += Frame_NameOfSelectedGroupBoxChanged;
    }
    private void Frame3_Navigated(object sender, NavigationEventArgs e)
    {
      if (e.Content is Abort page)
        page.NameOfSelectedGroupBoxChanged += Frame_NameOfSelectedGroupBoxChanged;
    }
    private void Frame4_Navigated(object sender, NavigationEventArgs e)
    {
      if (e.Content is KegStatistics page)
        page.NameOfSelectedGroupBoxChanged += Frame_NameOfSelectedGroupBoxChanged;
    }
    private void Frame5_Navigated(object sender, NavigationEventArgs e)
    {
      if (e.Content is KegStatistics page)
        page.NameOfSelectedGroupBoxChanged += Frame_NameOfSelectedGroupBoxChanged;
    }


    private void Frame_NameOfSelectedGroupBoxChanged(object sender, string e)
    {
      foreach (var child in leftStackPanel.Children)
      {
        if (child is RadioButton radioButton)
        {
          var textBlock = FindChildTextBlock(radioButton);
          if (textBlock != null && textBlock.Text.Equals(e, StringComparison.OrdinalIgnoreCase))
            radioButton.IsChecked = true;
          else
            radioButton.IsChecked = false;
        }
      }
    }



    private TextBlock FindChildTextBlock(DependencyObject parent)
    {
      for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
      {
        var child = VisualTreeHelper.GetChild(parent, i);
        if (child is TextBlock textBlock)
        {
          return textBlock;
        }
        else
        {
          var result = FindChildTextBlock(child);
          if (result != null) return result;
        }
      }
      return null;
    }

    #endregion

    #region 窗口相关
    // 移动窗口
    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
      // 只有当用户按下左键时才处理
      if (e.LeftButton == MouseButtonState.Pressed)
      {
        // 调用API使窗口跟随鼠标移动
        DragMove();
      }
    }

    // 窗口最小化
    private void Button_Click_1(object sender, RoutedEventArgs e)
    {
      this.WindowState = WindowState.Minimized;
    }

    //private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    //{
    //  frame1.Height = this.Height - 50;
    //  frame2.Height = this.Height - 50;
    //  frame3.Height = this.Height - 50;
    //}


    // 退出
    private void Button_Click(object sender, RoutedEventArgs e)
    {
      //if (checkBox2.IsChecked == true)
      //  ((App)System.Windows.Application.Current).Exit();
      //else
      //  this.Visibility = Visibility.Hidden; // 或者使用 Collapsed
      this.Close();
    }

    // 设置窗口高度
    //private void Nud22_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
    //{
    //  if (nud22 != null)
    //  {
    //    int height = (int)e.NewValue;
    //    if (height < 500)
    //      height = 500;
    //    this.Height = height;
    //  }
    //}

    // 窗口高度写入配置文件
    //private void Nud22_LostFocus(object sender, RoutedEventArgs e)
    //{
    //  if (nud22 != null)
    //    SetValue("window", "height", nud22.Value.ToString());
    //}

    private void OpenWebPage_Click(object sender, RoutedEventArgs e)
    {
      System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://github.com/hejie13250/KegConfigTools") { UseShellExecute = true });
    }




    #endregion



  }
}
