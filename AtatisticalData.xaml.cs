using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Input;



namespace 小科狗配置
{
  /// <summary>
  /// AtatisticalData.xaml 的交互逻辑
  /// </summary>
  public partial class AtatisticalData : Window, INotifyPropertyChanged
  {

    #region 打字数据定义
    public class 数据项 : INotifyPropertyChanged
    {
      private string _RQ;
      public string RQ  //日期
      {
        get { return _RQ; }
        set { _RQ = value; OnPropertyChanged("RQ"); }
      }
      private string _DZS;
      public string DZS //打字数
      {
        get { return _DZS; }
        set { _DZS = value; OnPropertyChanged("DZS"); }
      }
      private string _JJS;
      public string JJS //击键数
      {
        get { return _JJS; }
        set { _JJS = value; OnPropertyChanged("JJS"); }
      }
      private string _SPS;
      public string SPS //上屏数
      {
        get { return _SPS; }
        set { _SPS = value; OnPropertyChanged("SPS"); }
      }
      private string _SJ;
      public string SJ  //时间
      {
        get { return _SJ; }
        set { _SJ = value; OnPropertyChanged("SJ"); }
      }
      private string _LJ;
      public string LJ  //累计
      {
        get { return _LJ; }
        set { _LJ = value; OnPropertyChanged("LJ"); }
      }
      public 数据项() { }

      public event PropertyChangedEventHandler PropertyChanged;
      public virtual void OnPropertyChanged(string PropertyName)
      {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
      }

    }

    public ObservableCollection<数据项> 打字数据 { get; set; }

    #endregion

    #region 消息接口
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint flags, uint timeout, out IntPtr pdwResult);

    const uint ABORTIFHUNG = 0x0002;
    readonly uint flags = (uint)(ABORTIFHUNG);
    readonly uint timeout = 500;
    const int WM_USER         = 0x0400;               // 根据Windows API定义
    const uint KWM_GETALLZSTJ = (uint)WM_USER + 214;  //把字数与速度的所有统计数据吐到剪切板 格式见字数统计界面的样子,具体见剪切板
    #endregion


    public AtatisticalData()
    {
      InitializeComponent();
      打字数据              = new ObservableCollection<数据项>();
      listView8.DataContext = 打字数据;
    }

    public event PropertyChangedEventHandler PropertyChanged;


    #region 数据统计


    private void Button_Click_1(object sender, RoutedEventArgs e)
    {
      try
      {
        IntPtr hWnd = FindWindow("CKegServer_0", null);
        Thread.Sleep(200);
        SendMessageTimeout(hWnd, KWM_GETALLZSTJ, IntPtr.Zero, IntPtr.Zero, flags, timeout, out IntPtr pdwResult);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"操作失败，请重试！");
        Console.WriteLine(ex.Message);
      }


      string str = Clipboard.GetText();
      string pattern          = @"(\d+).*\t(.*)字.*\t(.*)击.*\t(.*)次.*\t(.*)秒.*\t累计(.*)字";
      MatchCollection matches = Regex.Matches(str, pattern);
      foreach (Match match in matches)
      {
        if (match.Success)
        {

          Console.WriteLine(match.Groups[1].Value);
          Console.WriteLine(match.Groups[2].Value);
          Console.WriteLine(match.Groups[3].Value);
          Console.WriteLine(match.Groups[4].Value);
          Console.WriteLine(match.Groups[5].Value);
          Console.WriteLine(match.Groups[6].Value);
          var item = new 数据项()
          {
            RQ  = match.Groups[1].Value,
            DZS = match.Groups[2].Value,
            JJS = match.Groups[3].Value,
            SPS = match.Groups[4].Value,
            SJ  = match.Groups[5].Value,
            LJ  = match.Groups[6].Value,
          };
          打字数据.Add(item);
        }
        //    //打字数据 = new ObservableCollection<数据项>();
        listView8.DataContext = 打字数据;
      }

    }

    private void Button_Click_3(object sender, RoutedEventArgs e)
    {


    }


    private void Button_Click_2(object sender, RoutedEventArgs e)
    {

      // 创建数据点
      var points = new List<DataPoint> {
                new DataPoint(0, 0),
                new DataPoint(1, 1),
                new DataPoint(2, 4),
                new DataPoint(3, 9),
                new DataPoint(4, 16)
            };

      // 创建一个线性函数系列
      var functionSeries = new FunctionSeries(x => x * x, 0, 4, 100)
      {
        Title     = "x^2",
        Color     = OxyColors.Blue,
        LineStyle = LineStyle.Solid
      };

      // 创建模型并添加系列
      var model = new PlotModel { Title = "Curve Chart Example" };
      model.Series.Add(functionSeries);

      // 将模型设置到PlotView
      plotView.Model = model;

    }
    #endregion












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
