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

namespace 小科狗配置
{
  /// <summary>
  /// MainWindow.xaml 的交互逻辑
  /// </summary>
  /// 
  public partial class MainWindow : Window
  {
    static readonly string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    StackPanel radioButtonStackPanel;

    #region 初始化
    public MainWindow()
    {
      if (!Directory.Exists($"{appPath}\\configs")) Directory.CreateDirectory($"{appPath}\\configs");
      InitializeComponent();

      title.Text = $"{title.Text} v {GetAssemblyVersion()}";

      this.Width = 930;
      this.Height = 800;
      frame1.Height = this.Height - 50;
      frame2.Height = this.Height - 50;
      frame3.Height = this.Height - 50;
      frame4.Height = this.Height - 50;

      方案设置页面();

      frame1.Navigated += Frame1_Navigated;
      frame2.Navigated += Frame2_Navigated;
      frame3.Navigated += Frame3_Navigated;
      frame4.Navigated += Frame4_Navigated;

      //this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
    }

    // 获取版本号
    public string GetAssemblyVersion()
    {
      Assembly assembly = Assembly.GetExecutingAssembly();
      Version version = assembly.GetName().Version;
      return version.ToString().Substring(0, 5);
    }

    void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
      var hWnd = new WindowInteropHelper(this).Handle;
      GlassEffect.MARGINS margins = new()
      {
        cxLeftWidth = -1,
        cxRightWidth = -1,
        cyTopHeight = -1,
        cyBottomHeight = -1
      };

      GlassEffect.DwmExtendFrameIntoClientArea(hWnd, ref margins);

      GlassEffect.DWM_BLURBEHIND bb = new()
      {
        dwFlags = 0x1,
        fEnable = true,
        hRgnBlur = IntPtr.Zero,
        fTransitionOnMaximized = true,
      };

      GlassEffect.DwmEnableBlurBehindWindow(hWnd, ref bb);
    }
    #endregion





    #region 点击 RadioButton 跳转到指定的 GroupBox
    private void RadioButton1_Click(object sender, RoutedEventArgs e)
    {
      RadioButton radioButton = sender as RadioButton;
      StackPanel stackPanel   = radioButton.Content as StackPanel;
      TextBlock textBlock     = stackPanel.Children.OfType<TextBlock>().FirstOrDefault();

      switch (textBlock.Text)
      {
        case "方案设置":
          方案设置页面();
          ScrollViewerOffset("候选框配色", 1);
          break;
        case "全局设置":
          全局设置页面(); 
          ScrollViewerOffset("状态条", 2);
          break;
        case "帮助":
          帮助页面(); 
          ScrollViewerOffset("关于", 3);
          break;
        case "打字统计":
          打字统计();
          ScrollViewerOffset("曲线图", 4);
          break;
      }
    }

    private void RadioButton2_Click(object sender, RoutedEventArgs e)
    {
      RadioButton radioButton = sender as RadioButton;
      StackPanel stackPanel = radioButton.Content as StackPanel;
      TextBlock textBlock = stackPanel.Children.OfType<TextBlock>().FirstOrDefault();

      switch (textBlock.Text)
      {
        case "候选框配色":
        case "候选框样式":
        case "字体和渲染":
        case "码元设置":
        case "标点设置":
        case "动作设置":
        case "顶功设置":
        case "上屏设置":
        case "中英切换":
        case "翻页按键":
        case "词语联想":
        case "码表调频与检索":
        case "重复上屏":
        case "自动造词":
        case "临时码表检索":
        case "引导码表检索":
          方案设置页面();
          ScrollViewerOffset(textBlock.Text, 1);
          break;
        case "状态条":
        case "在线查找":
        case "外部工具":
        case "快捷命令":
        case "快捷键":
        case "自启动应用":
        case "定时关机":
        case "其它选项":
          全局设置页面();
          ScrollViewerOffset(textBlock.Text, 2);
          break;
        case "关于":
        case "全局设置说明":
          帮助页面();
          ScrollViewerOffset(textBlock.Text, 3);
          break;
        case "曲线图":
        case "今日和累计数据":
          打字统计();
          ScrollViewerOffset(textBlock.Text, 4);
          break;
      }
    }

    private void 方案设置页面()
    {
      SetPageVisibility(Visibility.Visible, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, radioButtonStackPanel1);
    }

    private void 全局设置页面()
    {
      SetPageVisibility(Visibility.Collapsed, Visibility.Visible, Visibility.Collapsed, Visibility.Collapsed, radioButtonStackPanel2);
    }

    private void 帮助页面()
    {
      SetPageVisibility(Visibility.Collapsed, Visibility.Collapsed, Visibility.Visible, Visibility.Collapsed, radioButtonStackPanel3);
    }

    private void 打字统计()
    {
      SetPageVisibility(Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, Visibility.Visible, radioButtonStackPanel4);
    }

    private void SetPageVisibility(Visibility frame1Visibility, Visibility frame2Visibility, Visibility frame3Visibility, Visibility frame4Visibility, StackPanel activePanel)
    {
      frame1.Width = frame1Visibility == Visibility.Visible ? 770 : 0;
      frame2.Width = frame2Visibility == Visibility.Visible ? 770 : 0;
      frame3.Width = frame3Visibility == Visibility.Visible ? 770 : 0;
      frame4.Width = frame4Visibility == Visibility.Visible ? 770 : 0;

      frame1.Visibility = frame1Visibility;
      frame2.Visibility = frame2Visibility;
      frame3.Visibility = frame3Visibility;
      frame4.Visibility = frame4Visibility;

      radioButtonStackPanel1.Visibility = Visibility.Collapsed;
      radioButtonStackPanel2.Visibility = Visibility.Collapsed;
      radioButtonStackPanel3.Visibility = Visibility.Collapsed;
      radioButtonStackPanel4.Visibility = Visibility.Collapsed;

      activePanel.Visibility = Visibility.Visible;
      radioButtonStackPanel = activePanel;
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



    private void Frame_NameOfSelectedGroupBoxChanged(object sender, string e)
    {
      foreach (var child in radioButtonStackPanel.Children)
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
