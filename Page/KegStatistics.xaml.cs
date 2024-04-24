using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using SkiaSharp;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Path = System.IO.Path;

namespace 小科狗配置
{
  /// <summary>
  /// KegStatistics.xaml 的交互逻辑
  /// </summary>
  public partial class KegStatistics : BasePage
  {
    #region 获取GroupBox的Header用于主窗口导航事件
    private void GroupBox_MouseEnter(object sender, MouseEventArgs e)
    {
      if (sender is not GroupBox groupBox) return;
      NameOfSelectedGroupBox = groupBox.Header.ToString();
    }

    #endregion

    #region 消息接口
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint flags, uint timeout, out IntPtr pdwResult);

    const uint ABORTIFHUNG = 0x0002;
    readonly uint flags = (uint)(ABORTIFHUNG);
    readonly uint timeout = 500;
    const int WM_USER = 0x0400;               // 根据Windows API定义
    const uint KWM_GETALLZSTJ = (uint)WM_USER + 214;  //把字数与速度的所有统计数据吐到剪切板 格式见字数统计界面的样子,具体见剪切板
    #endregion


    #region 定义和初始化
    public class 打字统计数据
    {
      public string[] 日期 { get; set; }
      public double[] 字数 { get; set; }
      public double[] 击键 { get; set; }
      public double[] 上屏 { get; set; }
      public double[] 时长 { get; set; }
      public double[] 累计 { get; set; }
      public double[] 速度 { get; set; }
      public double[] 码长 { get; set; }
    }

    打字统计数据 数据统计 = new();
    string[] 日期;
    double[] 字数;
    double[] 击键;
    double[] 上屏;
    double[] 时长;
    double[] 累计;
    double[] 速度;
    double[] 码长;

    ViewModel viewModel = new();
    MatchCollection matches;

    readonly string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

    int ts = 0;

    [Obsolete]
    public KegStatistics()
    {
      InitializeComponent();
      dZcheckBox.Click += CheckBox_Click;
      jJcheckBox.Click += CheckBox_Click;
      sPcheckBox.Click += CheckBox_Click;
      sCcheckBox.Click += CheckBox_Click;
      sDcheckBox.Click += CheckBox_Click;
      mCcheckBox.Click += CheckBox_Click;
      rQcheckBox.Click += CheckBox_Click2;
      comboBox.SelectionChanged += ComboBox_SelectionChanged;

      DataContext = new ViewModel();
      ReadConfig();
      GetClipboardData();
      SetData();
      UpViewModelData(数据统计);
      SetControlData();
    }

    // 读配置项
    private void ReadConfig()
    {
      dZcheckBox.IsChecked = Base.GetValue("dzsjtj", "dz") == "1";
      jJcheckBox.IsChecked = Base.GetValue("dzsjtj", "jj") == "1";
      sPcheckBox.IsChecked = Base.GetValue("dzsjtj", "sp") == "1";
      sCcheckBox.IsChecked = Base.GetValue("dzsjtj", "sc") == "1";
      sDcheckBox.IsChecked = Base.GetValue("dzsjtj", "sd") == "1";
      mCcheckBox.IsChecked = Base.GetValue("dzsjtj", "mc") == "1";
      rQcheckBox.IsChecked = Base.GetValue("dzsjtj", "rq") == "1";
      var isNumberValid = double.TryParse(Base.GetValue("dzsjtj", "xs"), out var xs);
      nud.Value = isNumberValid ? xs : 4.5;

      isNumberValid = int.TryParse(Base.GetValue("dzsjtj", "ts"), out var xxs);
      comboBox.SelectedIndex = isNumberValid ? xxs : 0;
    }
    #endregion

    #region 数据处理

    // 从剪切板获取数据
    [Obsolete]
    private void GetClipboardData()
    {
      string str;
      try
      {
        var hWnd = FindWindow("CKegServer_0", null);
        SendMessageTimeout(hWnd, KWM_GETALLZSTJ, IntPtr.Zero, IntPtr.Zero, flags, timeout, out var pdwResult);
        str = Clipboard.GetText();
      }
      catch
      {
        MessageBox.Show($"操作失败，请重试！");
        return;
      }

      var pattern = @"(\d+).*\t(.*)字.*\t(.*)击.*\t(.*)次.*\t(.*)秒.*\t累计(.*)字";
      matches = Regex.Matches(str, pattern);
    }

    // 数据处理
    private void SetData()
    {
      var count = matches.Count;

      日期 = new string[count];
      字数 = new double[count];
      击键 = new double[count];
      上屏 = new double[count];
      时长 = new double[count];
      累计 = new double[count];
      速度 = new double[count];
      码长 = new double[count];

      if ((bool)rQcheckBox.IsChecked)
      {
        var n = -1;
        foreach (var match in matches.Cast<Match>())
        {
          n++;  // 正序
          UpData(match, n);
        }
      }
      else
      {
        var n = count;
        foreach (var match in matches.Cast<Match>())
        {
          n--;  // 倒序
          UpData(match, n);
        }

      }

      数据统计 = new()
      {
        日期 = 日期,
        字数 = 字数,
        击键 = 击键,
        上屏 = 上屏,
        时长 = 时长,
        累计 = 累计,
        速度 = 速度,
        码长 = 码长,
      };
    }

    // 更新数据
    private void UpData(Match match, int n)
    {
      var year = match.Groups[1].Value.Substring(0, 4);
      var month = match.Groups[1].Value.Substring(4, 2);
      var day = match.Groups[1].Value.Substring(6, 2);

      日期[n] = $"{year}-{month}-{day}";
      字数[n] = double.Parse(match.Groups[2].Value);
      击键[n] = double.Parse(match.Groups[3].Value);
      上屏[n] = double.Parse(match.Groups[4].Value);
      时长[n] = double.Parse(match.Groups[5].Value);
      累计[n] = double.Parse(match.Groups[6].Value);
      速度[n] = (double)(字数[n] / (时长[n] / 60 + nud.Value * 上屏[n] * (时长[n] / 60 / 击键[n])));
      码长[n] = Math.Round(击键[n] / 字数[n], 1);
    }

    // 更新数据到图表
    [Obsolete]
    private void UpViewModelData(打字统计数据 全部数据)
    {

      for (var i = 0; i < 时长.Length; i++)
      {
        全部数据.时长[i] = Math.Round(全部数据.时长[i], 2);
        全部数据.速度[i] = Math.Round(全部数据.速度[i], 2);
      }

      // 如果天数为 0 或大于等于数据总数，则使用全部数据，否则就截取指定天数的数据
      打字统计数据 数据片段 = new();
      if (ts == 0 || ts >= matches.Count)
      {
        数据片段 = 全部数据;
      }
      else
      {
        int startIndex;
        var count = matches.Count > 15 ? ts : matches.Count;
        startIndex = matches.Count > 15 ? matches.Count - ts : 0;

        数据片段.日期 = 全部数据.日期.Skip(startIndex).Take(count).ToArray(); ;
        数据片段.字数 = 全部数据.字数.Skip(startIndex).Take(count).ToArray(); ;
        数据片段.击键 = 全部数据.击键.Skip(startIndex).Take(count).ToArray(); ;
        数据片段.上屏 = 全部数据.上屏.Skip(startIndex).Take(count).ToArray(); ;
        数据片段.时长 = 全部数据.时长.Skip(startIndex).Take(count).ToArray(); ;
        数据片段.速度 = 全部数据.速度.Skip(startIndex).Take(count).ToArray(); ;
        数据片段.码长 = 全部数据.码长.Skip(startIndex).Take(count).ToArray(); ;
      }

      viewModel = new()
      {
        Series = new ISeries[]
        {
          new LineSeries<double> // 曲线图
          {
              Values = 数据片段.击键,
              Name = "击键（次）",
              //Fill = null,
              GeometrySize = 0, //圆点尺寸
              //LineSmoothness = 0, // 0为直线，1为圆弧
              //DataPadding = new LvcPoint(-200, 0),
              IsVisible = (bool)dZcheckBox.IsChecked, // 显示/隐藏
          },
          new LineSeries<double>
          {
              Values = 数据片段.字数,
              Name = "字数（个）",
              //Fill = null,
              GeometrySize = 0,
              IsVisible = (bool)jJcheckBox.IsChecked

          },
          new LineSeries<double>
          {
              Values = 数据片段.上屏,
              Name = "上屏（个）",
              //Fill = null,
              GeometrySize = 0,
              IsVisible = (bool)sPcheckBox.IsChecked
          },
          new LineSeries<double>
          {
              Values = 数据片段.时长,
              Name = "用时（秒）",
              //Fill = null,
              GeometrySize = 0,
              IsVisible = (bool)sCcheckBox.IsChecked
          },
          new LineSeries<double>
          {
              Values = 数据片段.速度,
              Name = "速度（字/分）",
              //Fill = null,
              GeometrySize = 0,
              IsVisible = (bool)sDcheckBox.IsChecked
          },
          new LineSeries<double>
          {
              Values = 数据片段.码长,
              Name = "码长（码）",
              Fill = null,
              GeometrySize = 0,
              IsVisible = (bool)mCcheckBox.IsChecked
          },

        },
        XAxes = new Axis[]
        {
          new()
          {
              // 分隔字体颜色和大小
              LabelsPaint = new SolidColorPaint(SKColors.Blue),
              TextSize = 12,
              Labels =  数据片段.日期,
              LabelsRotation = -30,
              // 颜色和线粗
              SeparatorsPaint = new SolidColorPaint(SKColors.LightSlateGray) { StrokeThickness = 1 },

          }
        },
        YAxes = new Axis[]
        {
          new ()
          {
            LabelsPaint = new SolidColorPaint(SKColors.Green),
            TextSize = 20,
            MinLimit = 0, // 设置 Y 轴的最小值为 0
            SeparatorsPaint = new SolidColorPaint(SKColors.LightSlateGray)
              {
                StrokeThickness = 1,
                PathEffect = new DashEffect(new float[] { 3, 8 }) //设为虚线，3和8为实线和留空大小
              }
          }
        },
      };

      liveCharts.Series = viewModel.Series;
      liveCharts.XAxes = viewModel.XAxes;
      liveCharts.YAxes = viewModel.YAxes;
    }

    // 更新控件上的值
    private void SetControlData()
    {
      var match = matches[0]; // 今天的数据

      var zs = double.Parse(match.Groups[2].Value); //字数
      var jj = double.Parse(match.Groups[3].Value); //击键
      var sp = double.Parse(match.Groups[4].Value); //上屏
      var sc = double.Parse(match.Groups[5].Value); //时长
      var sd = (double)(zs / (sc / 60 + nud.Value * sp * (sc / 60 / jj)));  //速度
      var mc = Math.Round(jj / zs, 1);  //码长

      // 累计的数据
      var ljzs = double.Parse(match.Groups[6].Value); //累计字数
      double ljjj = 0;  //累计击键
      double ljsp = 0;  //累计上屏
      double ljsc = 0;  //累计时长
      for (var i = 0; i < 字数.Length; i++)
      {
        ljjj += 击键[i];
        ljsp += 上屏[i];
        ljsc += 时长[i];
      }
      var ljsd = (double)(ljzs / (ljsc / 60 + nud.Value * ljsp * (ljsc / 60 / ljjj)));  //累计速度
      var ljmc = Math.Round(ljjj / ljzs, 1);  //累计码长

      zsTextBlock.Text = String.Format("{0:#,###0}", zs); //字数
      jjTextBlock.Text = String.Format("{0:#,###0}", jj); //击键
      spTextBlock.Text = String.Format("{0:#,###0}", sp); //上屏
      scTextBlock.Text = $"{sc / 60:0.00}" + " 分"; //时长
      sdTextBlock.Text = $"{sd:0.00}" + " 字/分";  //速度
      mcTextBlock.Text = $"{mc:0.00}";  //码长

      ljzsTextBlock.Text = String.Format("{0:#,###0}", ljzs); //累计字数
      ljjjTextBlock.Text = String.Format("{0:#,###0}", ljjj); //累计击键
      ljspTextBlock.Text = String.Format("{0:#,###0}", ljsp); //累计上屏
      ljscTextBlock.Text = $"{ljsc / 3600:0.00}" + " 时"; //累计时长
      ljsdTextBlock.Text = $"{ljsd:0.00}" + " 字/分"; //累计速度
      ljmcTextBlock.Text = $"{ljmc:0.00}"; //累计码长

    }

    // 数据选择改变后更新
    [Obsolete]
    private void CheckBox_Click(object sender, RoutedEventArgs e)
    {
      if ((bool)dZcheckBox.IsChecked) { Base.SetValue("dzsjtj", "dz", "1"); } else Base.SetValue("dzsjtj", "dz", "0");
      if ((bool)jJcheckBox.IsChecked) { Base.SetValue("dzsjtj", "jj", "1"); } else Base.SetValue("dzsjtj", "jj", "0");
      if ((bool)sPcheckBox.IsChecked) { Base.SetValue("dzsjtj", "sp", "1"); } else Base.SetValue("dzsjtj", "sp", "0");
      if ((bool)sCcheckBox.IsChecked) { Base.SetValue("dzsjtj", "sc", "1"); } else Base.SetValue("dzsjtj", "sc", "0");
      if ((bool)sDcheckBox.IsChecked) { Base.SetValue("dzsjtj", "sd", "1"); } else Base.SetValue("dzsjtj", "sd", "0");
      if ((bool)mCcheckBox.IsChecked) { Base.SetValue("dzsjtj", "mc", "1"); } else Base.SetValue("dzsjtj", "mc", "0");

      UpViewModelData(数据统计);
    }

    [Obsolete]
    private void CheckBox_Click2(object sender, RoutedEventArgs e)
    {
      if ((bool)rQcheckBox.IsChecked) { Base.SetValue("dzsjtj", "rq", "1"); } else Base.SetValue("dzsjtj", "rq", "0");

      SetData();
      UpViewModelData(数据统计);
    }

    [Obsolete]
    private void Nud_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      if (matches != null)
      {
        Base.SetValue("dzsjtj", "xs", nud.Value.ToString());
        nud.Value = Math.Round(nud.Value, 2);
        UpViewModelData(数据统计);
        SetControlData();
      }
    }

    [Obsolete]
    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      Base.SetValue("dzsjtj", "ts", comboBox.SelectedIndex.ToString());

      switch (comboBox.SelectedIndex)
      {
        case 0: ts = 15; break;
        case 1: ts = 30; break;
        case 2: ts = 60; break;
        case 3: ts = 90; break;
        case 4: ts = 120; break;
        case 5: ts = 180; break;
        case 6: ts = 365; break;
        case 7: ts = 730; break;
        case 8: ts = 0; break;
      }
      if (matches != null)
      {
        UpViewModelData(数据统计);
      }
    }

    #endregion




  }
}
