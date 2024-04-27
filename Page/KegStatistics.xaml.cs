using System;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using SkiaSharp;
using 小科狗配置.Class;

namespace 小科狗配置.Page
{
  /// <summary>
  /// KegStatistics.xaml 的交互逻辑
  /// </summary>
  public partial class KegStatistics
  {
    #region 获取GroupBox的Header用于主窗口导航事件
    private void GroupBox_MouseEnter(object sender, MouseEventArgs e)
    {
      if (sender is not GroupBox groupBox) return;
      NameOfSelectedGroupBox = groupBox.Header.ToString();
    }

    #endregion

    #region 消息接口
    // [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    // static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    // [DllImport("user32.dll", SetLastError = true)]
    // static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, uint flags, uint timeout, out IntPtr pdwResult);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    const uint Abortifhung = 0x0002;
    readonly uint _flags = Abortifhung;
    readonly uint _timeout = 500;
    const int WmUser = 0x0400;               // 根据Windows API定义
    const uint KwmGetallzstj = (uint)WmUser + 214;  //把字数与速度的所有统计数据吐到剪切板 格式见字数统计界面的样子,具体见剪切板
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

    打字统计数据 _数据统计 = new();
    string[] _日期;
    double[] _字数;
    double[] _击键;
    double[] _上屏;
    double[] _时长;
    double[] _累计;
    double[] _速度;
    double[] _码长;

    ViewModel _viewModel = new();
    MatchCollection _matches;

    int _ts;

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
      UpViewModelData(_数据统计);
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
        // var hWnd = FindWindow("CKegServer_0", null);
        // SendMessageTimeout(hWnd, KwmGetallzstj, IntPtr.Zero, IntPtr.Zero, _flags, _timeout, out _);
        PostMessage(Base.hWnd, KwmGetallzstj, IntPtr.Zero, IntPtr.Zero);
        Thread.Sleep(200);
        str = Clipboard.GetText();
      }
      catch
      {
        MessageBox.Show("操作失败，请重试！");
        return;
      }

      var pattern = @"(\d+).*\t(.*)字.*\t(.*)击.*\t(.*)次.*\t(.*)秒.*\t累计(.*)字";
      _matches = Regex.Matches(str, pattern);
    }

    // 数据处理
    private void SetData()
    {
      var count = _matches.Count;

      _日期 = new string[count];
      _字数 = new double[count];
      _击键 = new double[count];
      _上屏 = new double[count];
      _时长 = new double[count];
      _累计 = new double[count];
      _速度 = new double[count];
      _码长 = new double[count];

      if (rQcheckBox.IsChecked != null && (bool)rQcheckBox.IsChecked)
      {
        var n = -1;
        foreach (var match in _matches.Cast<Match>())
        {
          n++;  // 正序
          UpData(match, n);
        }
      }
      else
      {
        var n = count;
        foreach (var match in _matches.Cast<Match>())
        {
          n--;  // 倒序
          UpData(match, n);
        }

      }

      _数据统计 = new()
      {
        日期 = _日期,
        字数 = _字数,
        击键 = _击键,
        上屏 = _上屏,
        时长 = _时长,
        累计 = _累计,
        速度 = _速度,
        码长 = _码长,
      };
    }

    // 更新数据
    private void UpData(Match match, int n)
    {
      var year = match.Groups[1].Value.Substring(0, 4);
      var month = match.Groups[1].Value.Substring(4, 2);
      var day = match.Groups[1].Value.Substring(6, 2);

      _日期[n] = $"{year}-{month}-{day}";
      _字数[n] = double.Parse(match.Groups[2].Value);
      _击键[n] = double.Parse(match.Groups[3].Value);
      _上屏[n] = double.Parse(match.Groups[4].Value);
      _时长[n] = double.Parse(match.Groups[5].Value);
      _累计[n] = double.Parse(match.Groups[6].Value);
      _速度[n] = _字数[n] / (_时长[n]   / 60 + nud.Value * _上屏[n] * (_时长[n] / 60 / _击键[n]));
      _码长[n] = Math.Round(_击键[n] / _字数[n], 1);
    }

    // 更新数据到图表
    [Obsolete]
    private void UpViewModelData(打字统计数据 全部数据)
    {

      for (var i = 0; i < _时长.Length; i++)
      {
        全部数据.时长[i] = Math.Round(全部数据.时长[i], 2);
        全部数据.速度[i] = Math.Round(全部数据.速度[i], 2);
      }

      // 如果天数为 0 或大于等于数据总数，则使用全部数据，否则就截取指定天数的数据
      打字统计数据 数据片段 = new();
      if (_ts == 0 || _ts >= _matches.Count)
      {
        数据片段 = 全部数据;
      }
      else
      {
        var count      = _matches.Count > 15 ? _ts : _matches.Count;
        var startIndex = _matches.Count > 15 ? _matches.Count - _ts : 0;

        数据片段.日期 = 全部数据.日期.Skip(startIndex).Take(count).ToArray();
        数据片段.字数 = 全部数据.字数.Skip(startIndex).Take(count).ToArray();
        数据片段.击键 = 全部数据.击键.Skip(startIndex).Take(count).ToArray();
        数据片段.上屏 = 全部数据.上屏.Skip(startIndex).Take(count).ToArray();
        数据片段.时长 = 全部数据.时长.Skip(startIndex).Take(count).ToArray();
        数据片段.速度 = 全部数据.速度.Skip(startIndex).Take(count).ToArray();
        数据片段.码长 = 全部数据.码长.Skip(startIndex).Take(count).ToArray();
      }

      _viewModel = new()
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
              IsVisible = dZcheckBox.IsChecked != null && (bool)dZcheckBox.IsChecked, // 显示/隐藏
          },
          new LineSeries<double>
          {
              Values = 数据片段.字数,
              Name = "字数（个）",
              //Fill = null,
              GeometrySize = 0,
              IsVisible    = jJcheckBox.IsChecked != null && (bool)jJcheckBox.IsChecked

          },
          new LineSeries<double>
          {
              Values = 数据片段.上屏,
              Name = "上屏（个）",
              //Fill = null,
              GeometrySize = 0,
              IsVisible    = sPcheckBox.IsChecked != null && (bool)sPcheckBox.IsChecked
          },
          new LineSeries<double>
          {
              Values = 数据片段.时长,
              Name = "用时（秒）",
              //Fill = null,
              GeometrySize = 0,
              IsVisible    = sCcheckBox.IsChecked != null && (bool)sCcheckBox.IsChecked
          },
          new LineSeries<double>
          {
              Values = 数据片段.速度,
              Name = "速度（字/分）",
              //Fill = null,
              GeometrySize = 0,
              IsVisible    = sDcheckBox.IsChecked != null && (bool)sDcheckBox.IsChecked
          },
          new LineSeries<double>
          {
              Values       = 数据片段.码长,
              Name         = "码长（码）",
              Fill         = null,
              GeometrySize = 0,
              IsVisible    = mCcheckBox.IsChecked != null && (bool)mCcheckBox.IsChecked
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
            TextSize    = 20,
            // MinLimit    = 0,    // 设置 Y 轴的最小值为 0
            // MaxLimit    = null, // 设置为 null，以便自动调节最大值
            SeparatorsPaint = new SolidColorPaint(SKColors.LightSlateGray)
              {
                StrokeThickness = 1,
                PathEffect = new DashEffect(new float[] { 3, 8 }) //设为虚线，3和8为实线和留空大小
              }
          }
        },
      };

      liveCharts.Series = _viewModel.Series;
      liveCharts.XAxes = _viewModel.XAxes;
      liveCharts.YAxes = _viewModel.YAxes;
    }

    // 更新控件上的值
    private void SetControlData()
    {
      var match = _matches[0]; // 今天的数据

      var zs = double.Parse(match.Groups[2].Value);                   //字数
      var jj = double.Parse(match.Groups[3].Value);                   //击键
      var sp = double.Parse(match.Groups[4].Value);                   //上屏
      var sc = double.Parse(match.Groups[5].Value);                   //时长
      var sd = zs / (sc      / 60 + nud.Value * sp * (sc / 60 / jj)); //速度
      var mc = Math.Round(jj / zs, 1);                                //码长

      // 累计的数据
      var ljzs = double.Parse(match.Groups[6].Value); //累计字数
      double ljjj = 0;  //累计击键
      double ljsp = 0;  //累计上屏
      double ljsc = 0;  //累计时长
      for (var i = 0; i < _字数.Length; i++)
      {
        ljjj += _击键[i];
        ljsp += _上屏[i];
        ljsc += _时长[i];
      }
      var ljsd = ljzs / (ljsc    / 60 + nud.Value * ljsp * (ljsc / 60 / ljjj)); //累计速度
      var ljmc = Math.Round(ljjj / ljzs, 1);                                    //累计码长

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
      if (dZcheckBox.IsChecked != null && (bool)dZcheckBox.IsChecked) { Base.SetValue("dzsjtj", "dz", "1"); } else Base.SetValue("dzsjtj", "dz", "0");
      if (jJcheckBox.IsChecked != null && (bool)jJcheckBox.IsChecked) { Base.SetValue("dzsjtj", "jj", "1"); } else Base.SetValue("dzsjtj", "jj", "0");
      if (sPcheckBox.IsChecked != null && (bool)sPcheckBox.IsChecked) { Base.SetValue("dzsjtj", "sp", "1"); } else Base.SetValue("dzsjtj", "sp", "0");
      if (sCcheckBox.IsChecked != null && (bool)sCcheckBox.IsChecked) { Base.SetValue("dzsjtj", "sc", "1"); } else Base.SetValue("dzsjtj", "sc", "0");
      if (sDcheckBox.IsChecked != null && (bool)sDcheckBox.IsChecked) { Base.SetValue("dzsjtj", "sd", "1"); } else Base.SetValue("dzsjtj", "sd", "0");
      if (mCcheckBox.IsChecked != null && (bool)mCcheckBox.IsChecked) { Base.SetValue("dzsjtj", "mc", "1"); } else Base.SetValue("dzsjtj", "mc", "0");

      UpViewModelData(_数据统计);
    }

    [Obsolete]
    private void CheckBox_Click2(object sender, RoutedEventArgs e)
    {
      if (rQcheckBox.IsChecked != null && (bool)rQcheckBox.IsChecked) { Base.SetValue("dzsjtj", "rq", "1"); } else Base.SetValue("dzsjtj", "rq", "0");

      SetData();
      UpViewModelData(_数据统计);
    }

    [Obsolete]
    private void Nud_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      if (_matches == null) return;
      Base.SetValue("dzsjtj", "xs", nud.Value.ToString(CultureInfo.CurrentCulture));
      nud.Value = Math.Round(nud.Value, 2);
      UpViewModelData(_数据统计);
      SetControlData();
    }

    [Obsolete]
    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      Base.SetValue("dzsjtj", "ts", comboBox.SelectedIndex.ToString());

      switch (comboBox.SelectedIndex)
      {
        case 0: _ts = 15; break;
        case 1: _ts = 30; break;
        case 2: _ts = 60; break;
        case 3: _ts = 90; break;
        case 4: _ts = 120; break;
        case 5: _ts = 180; break;
        case 6: _ts = 365; break;
        case 7: _ts = 730; break;
        case 8: _ts = 0; break;
      }
      if (_matches != null)
      {
        UpViewModelData(_数据统计);
      }
    }

    #endregion




  }
}
