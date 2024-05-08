using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LiveChartsCore;
using LiveChartsCore.Measure;
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


    #region 定义和初始化
    public class 打字统计数据
    {
      public string[] 日期 { get; set; }
      public double[] 字数 { get; set; }
      public double[] 击键 { get; set; }
      public double[] 上屏 { get; set; }
      public double[] 时长 { get; set; }
      public double[] 速度 { get; set; }
      public double[] 码长 { get; set; }
    }

    private 打字统计数据 _数据统计 = new();
    private string[] _日期;
    private double[] _字数;
    private double[] _击键;
    private double[] _上屏;
    private double[] _时长;
    // private double[] _累计;
    private double[] _速度;
    private double[] _码长;

    private ViewModel       _viewModel = new();
    private MatchCollection _matches;

    private int _ts;

    [Obsolete]
    public KegStatistics()
    {
      InitializeComponent();
      dzCheckBox.Click += CheckBox_Click;
      jjCheckBox.Click += CheckBox_Click;
      spCheckBox.Click += CheckBox_Click;
      scCheckBox.Click += CheckBox_Click;
      sdCheckBox.Click += CheckBox_Click;
      mcCheckBox.Click += CheckBox_Click;
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
      dzCheckBox.IsChecked = Base.GetValue("dzsjtj", "dz") == "1";
      jjCheckBox.IsChecked = Base.GetValue("dzsjtj", "jj") == "1";
      spCheckBox.IsChecked = Base.GetValue("dzsjtj", "sp") == "1";
      scCheckBox.IsChecked = Base.GetValue("dzsjtj", "sc") == "1";
      sdCheckBox.IsChecked = Base.GetValue("dzsjtj", "sd") == "1";
      mcCheckBox.IsChecked = Base.GetValue("dzsjtj", "mc") == "1";
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
      try
      {
        Base.SendMessageTimeout(Base.KwmGetallzstj);
      }
      catch
      {
        MessageBox.Show("操作失败，请重试！");
        return;
      }

      var          str     = Clipboard.GetText();
      const string pattern = @"(\d+).*\t(.*)字.*\t(.*)击.*\t(.*)次.*\t(.*)秒.*\t累计(.*)字";
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
      // _累计 = new double[count];
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

      _数据统计 = new 打字统计数据
      {
        日期 = _日期,
        字数 = _字数,
        击键 = _击键,
        上屏 = _上屏,
        时长 = _时长,
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
      // _累计[n] = double.Parse(match.Groups[6].Value);
      _速度[n] = _字数[n] / (_时长[n] / 60 + nud.Value * _上屏[n] * (_时长[n] / 60 / _击键[n]));
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

      _viewModel = new ViewModel
      {
        Series = new ISeries[]
        {
          new LineSeries<double> // 曲线图
          {
              Values = 数据片段.击键,
              Name   = "击键（次）",
              Stroke = new SolidColorPaint(SKColors.Chartreuse),
              //Fill = null,
              GeometrySize = 0, //圆点尺寸
              //LineSmoothness = 0, // 0为直线，1为圆弧
              //DataPadding = new LvcPoint(-200, 0),
              IsVisible = dzCheckBox.IsChecked != null && (bool)dzCheckBox.IsChecked, // 显示/隐藏
          },
          new LineSeries<double>
          {
              Values = 数据片段.字数,
              Name   = "字数（个）",
              Stroke = new SolidColorPaint(SKColors.Orange),
              //Fill = null,
              GeometrySize = 0,
              IsVisible    = jjCheckBox.IsChecked != null && (bool)jjCheckBox.IsChecked

          },
          new LineSeries<double>
          {
              Values = 数据片段.上屏,
              Name   = "上屏（个）",
              Stroke = new SolidColorPaint(SKColors.Yellow),
              //Fill = null,
              GeometrySize = 0,
              IsVisible    = spCheckBox.IsChecked != null && (bool)spCheckBox.IsChecked
          },
          new LineSeries<double>
          {
            Values = 数据片段.时长,
            Name   = "用时（秒）",
            Stroke = new SolidColorPaint(SKColors.Green),
            //Fill = null,
            GeometrySize = 0,
            IsVisible    = scCheckBox.IsChecked != null && (bool)scCheckBox.IsChecked
          },
          new LineSeries<double>
          {
              ScalesYAt    = 1,
              Values       = 数据片段.速度,
              Name         = "速度（字/分）",
              Stroke       = new SolidColorPaint(SKColors.Red),
              Fill         = null,
              GeometrySize = 0,
              IsVisible    = sdCheckBox.IsChecked != null && (bool)sdCheckBox.IsChecked
          },
          new LineSeries<double>
          {
            ScalesYAt    = 2,
            Values       = 数据片段.码长,
            Name         = "码长（码）",
            Stroke       = new SolidColorPaint(SKColors.Blue),
            Fill         = null,
            GeometrySize = 0,
            IsVisible    = mcCheckBox.IsChecked != null && (bool)mcCheckBox.IsChecked
          }
        },
        XAxes = new Axis[]
        {
          new()
          {
            // 分隔字体颜色和大小
            LabelsPaint    = new SolidColorPaint(SKColors.Black),
            TextSize       = 12,
            Labels         =  数据片段.日期,
            LabelsRotation = -30,
            // 颜色和线粗
            SeparatorsPaint = null,
            //SeparatorsPaint = new SolidColorPaint(SKColors.LightSlateGray) { StrokeThickness = 1 },
          }
        },
        YAxes = new Axis[]
        {
          new ()
          {
            LabelsPaint = new SolidColorPaint(SKColors.Black),
            TextSize    = 12,
            // MinLimit    = 0,    // 设置 Y 轴的最小值为 0
            // MaxLimit    = null, // 设置为 null，以便自动调节最大值
            SeparatorsPaint = null,
            //SeparatorsPaint = new SolidColorPaint(SKColors.LightSlateGray)
            //{
            //  StrokeThickness = 1,
            //  PathEffect      = new DashEffect(new float[] { 3, 8 }) //设为虚线，3和8为实线和留空大小
            //}
          },
          new ()
          {
            //Name = "速度",
            //NameTextSize = 14,
            //NamePaint = new SolidColorPaint(SKColors.Red),
            Position    = AxisPosition.End,
            LabelsPaint = new SolidColorPaint(SKColors.Red),
            TextSize    = 12,
            IsVisible   = sdCheckBox.IsChecked != null && (bool)sdCheckBox.IsChecked,
            SeparatorsPaint = null,
            //SeparatorsPaint = new SolidColorPaint(SKColors.LightSlateGray)
            //{
            //  StrokeThickness = 0,
            //  // PathEffect      = new DashEffect(new float[] { 3, 8 }) //设为虚线，3和8为实线和留空大小
            //}
          },
          new ()
          {
            //Name = "码长",
            //NameTextSize = 14,
            //NamePaint = new SolidColorPaint(SKColors.Blue),
            Position    = AxisPosition.End,
            LabelsPaint = new SolidColorPaint(SKColors.Blue),
            TextSize    = 12,
            IsVisible   = mcCheckBox.IsChecked != null && (bool)mcCheckBox.IsChecked,
            SeparatorsPaint = null,
            //SeparatorsPaint = new SolidColorPaint(SKColors.LightSlateGray)
            //{
            //  StrokeThickness = 1,
            //  // PathEffect      = new DashEffect(new float[] { 3, 8 }) //设为虚线，3和8为实线和留空大小
            //}
          },
        }
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

      zsTextBlock.Text = $"{zs:#,###0}";             //字数
      jjTextBlock.Text = $"{jj:#,###0}";             //击键
      spTextBlock.Text = $"{sp:#,###0}";             //上屏
      scTextBlock.Text = $"{sc / 60:0.00}" + " 分";   //时长
      sdTextBlock.Text = $"{sd:0.00}"      + " 字/分"; //速度
      mcTextBlock.Text = $"{mc:0.00}";               //码长

      ljzsTextBlock.Text = $"{ljzs:#,###0}";               //累计字数
      ljjjTextBlock.Text = $"{ljjj:#,###0}";               //累计击键
      ljspTextBlock.Text = $"{ljsp:#,###0}";               //累计上屏
      ljscTextBlock.Text = $"{ljsc / 3600:0.00}" + " 时";   //累计时长
      ljsdTextBlock.Text = $"{ljsd:0.00}"        + " 字/分"; //累计速度
      ljmcTextBlock.Text = $"{ljmc:0.00}";                 //累计码长

    }

    // 数据选择改变后更新
    [Obsolete]
    private void CheckBox_Click(object sender, RoutedEventArgs e)
    {
      if (dzCheckBox.IsChecked != null && (bool)dzCheckBox.IsChecked) { Base.SetValue("dzsjtj", "dz", "1"); } else Base.SetValue("dzsjtj", "dz", "0");
      if (jjCheckBox.IsChecked != null && (bool)jjCheckBox.IsChecked) { Base.SetValue("dzsjtj", "jj", "1"); } else Base.SetValue("dzsjtj", "jj", "0");
      if (spCheckBox.IsChecked != null && (bool)spCheckBox.IsChecked) { Base.SetValue("dzsjtj", "sp", "1"); } else Base.SetValue("dzsjtj", "sp", "0");
      if (scCheckBox.IsChecked != null && (bool)scCheckBox.IsChecked) { Base.SetValue("dzsjtj", "sc", "1"); } else Base.SetValue("dzsjtj", "sc", "0");
      if (sdCheckBox.IsChecked != null && (bool)sdCheckBox.IsChecked) { Base.SetValue("dzsjtj", "sd", "1"); } else Base.SetValue("dzsjtj", "sd", "0");
      if (mcCheckBox.IsChecked != null && (bool)mcCheckBox.IsChecked) { Base.SetValue("dzsjtj", "mc", "1"); } else Base.SetValue("dzsjtj", "mc", "0");

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
