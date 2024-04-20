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

namespace 小科狗配置
{
  /// <summary>
  /// MainWindow.xaml 的交互逻辑
  /// </summary>
  /// 
  public partial class MainWindow : Window
  {



    #region 初始化
    public MainWindow()
    {
      InitializeComponent();

      this.Width = 920;
      this.Height = 735;
      frame1.Width = 750;
      frame2.Width = 0;
      frame3.Width = 0;
      frame1.Visibility = Visibility.Visible;
      frame2.Visibility = Visibility.Hidden;
      frame3.Visibility = Visibility.Hidden;
      frame1.Height = this.Height - 50;
      frame2.Height = this.Height - 50;
      frame3.Height = this.Height - 50;
      stacPanel1.Visibility = Visibility.Visible;
      stacPanel2.Visibility = Visibility.Collapsed;
      stacPanel3.Visibility = Visibility.Collapsed;

      frame1.Navigated += Frame1_Navigated;
      frame2.Navigated += Frame2_Navigated;
      frame3.Navigated += Frame3_Navigated;
    }
    #endregion


    #region 页面导航

    private void 方案设置页面()
    {
      //this.framePage.Navigate(new Uri("Page/SchemeSetting.xaml", UriKind.Relative));
      frame1.Width = 750;
      frame2.Width = 0;
      frame3.Width = 0;
      frame1.Visibility = Visibility.Visible;
      frame2.Visibility = Visibility.Hidden;
      frame3.Visibility = Visibility.Hidden;
    }
    private void 全局设置页面()
    {
      //this.framePage.Navigate(new Uri("Page/GlobalSetting.xaml", UriKind.Relative));
      frame1.Width = 0;
      frame2.Width = 750;
      frame3.Width = 0;
      frame1.Visibility = Visibility.Hidden;
      frame2.Visibility = Visibility.Visible;
      frame3.Visibility = Visibility.Hidden;
    }
    private void 帮助页面()
    {
      //this.framePage.Navigate(new Uri("Page/Abort.xaml", UriKind.Relative));
      frame1.Width = 0;
      frame2.Width = 0;
      frame3.Width = 750;
      frame1.Visibility = Visibility.Hidden;
      frame2.Visibility = Visibility.Hidden;
      frame3.Visibility = Visibility.Visible; 
    }


    private void RadioButton1_Click(object sender, RoutedEventArgs e)
    {
      RadioButton radioButton = sender as RadioButton;
      StackPanel stackPanel   = radioButton.Content as StackPanel;
      TextBlock textBlock     = stackPanel.Children.OfType<TextBlock>().FirstOrDefault();


      switch (textBlock.Text)
      {
        case "方案设置":
          方案设置页面();
          stacPanel1.Visibility = stacPanel1.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
          ScrollViewerOffset("候选框配色", 1);
          break;
        case "全局设置":
          全局设置页面();
          stacPanel2.Visibility = stacPanel2.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
          ScrollViewerOffset("状态条", 2);
          break;
        case "帮助":
          帮助页面();
          stacPanel3.Visibility = stacPanel3.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
          ScrollViewerOffset("关于", 3);
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
      }
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
        var stackPanel = page?.FindName("stackPanel1") as StackPanel;
        groupBox = FindGroupBox(content, stackPanel);
      }
      else if (n == 2)
      {
        var page = frame2.Content as Page;
        var stackPanel = page?.FindName("stackPanel2") as StackPanel;
        groupBox = FindGroupBox(content, stackPanel);
      }
      else if (n == 3)
      {
        var page = frame3.Content as Page;
        var stackPanel = page?.FindName("stackPanel3") as StackPanel;
        groupBox = FindGroupBox(content, stackPanel);
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
    private GroupBox FindGroupBox(string content, StackPanel stackPanel)
    {
      foreach (var child in stackPanel.Children)
      {
        if (child is GroupBox groupBox && groupBox.Header.ToString() == content)
        {
          return groupBox;
        }
      }
      return null;
    }



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
    private void Frame_NameOfSelectedGroupBoxChanged(object sender, string e)
    {
      foreach (var child in stackPanel.Children)
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
