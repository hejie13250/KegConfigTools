using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using WpfAnimatedGif;
using 小科狗配置.Class;
using 小科狗配置.Contorl;
using Button = System.Windows.Controls.Button;
using Clipboard = System.Windows.Clipboard;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using File = System.IO.File;
using GroupBox = System.Windows.Controls.GroupBox;
using Label = System.Windows.Controls.Label;
using ListView = System.Windows.Controls.ListView;
using ListViewItem = System.Windows.Controls.ListViewItem;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Path = System.IO.Path;
using RadioButton = System.Windows.Controls.RadioButton;
using TextBox = System.Windows.Controls.TextBox;

namespace 小科狗配置.Page
{
  /// <summary>
  /// GlobalSetting.xaml 的交互逻辑
  /// </summary>
  public partial class GlobalSetting
  {
    #region 获取GroupBox的Header用于主窗口导航事件
    private void GroupBox_MouseEnter(object sender, MouseEventArgs e)
    {
      if (sender is not GroupBox groupBox) return;
      NameOfSelectedGroupBox = groupBox.Header.ToString();
    }

    #endregion

    #region 全局设置界面列表项定义

    public sealed class 列表项 : INotifyPropertyChanged
    {
      private bool _enable;
      public bool Enable
      {
        get => _enable;
        set { _enable = value; OnPropertyChanged("Enable"); }
      }
      private string _name;
      public string Name
      {
        get => _name;
        set { _name = value; OnPropertyChanged("Name"); }
      }
      private string _value;
      public string Value
      {
        get => _value;
        set { _value = value; OnPropertyChanged("Value"); }
      }

      private string _cmd;
      public string Cmd
      {
        get => _cmd;
        set { _cmd = value; OnPropertyChanged("CMD"); }
      }


      public event PropertyChangedEventHandler PropertyChanged;

      private void OnPropertyChanged(string propertyName)
      {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

    }

    public class 状态条
    {
      public string 提示文本的位置 { get; set; }
      public bool 提示文本要隐藏吗 { get; set; }
      public string 提示文本中文字体色 { get; set; }
      public string 提示文本英文字体色 { get; set; }
      public int 提示文本字体大小 { get; set; }
      public string 提示文本字体名称 { get; set; }
      public bool 要启用深夜锁屏吗 { get; set; }
      public bool 提示文本要显示中英以及大小写状态吗 { get; set; }
      public bool 快键只在候选窗口显示情况下才起作用吗 { get; set; }
      public bool 打字字数统计等数据是要保存在主程文件夹下吗 { get; set; }
    }

    // 用于 listView 数据绑定
    private ObservableCollection<列表项> 查找列表 { get; set; }
    private ObservableCollection<列表项> 外部工具 { get; set; }
    private ObservableCollection<列表项> 快键命令 { get; set; }
    private ObservableCollection<列表项> 自动关机 { get; set; }
    private ObservableCollection<列表项> 快键   { get; set; }
    private ObservableCollection<列表项> 自启   { get; set; }
    private 状态条                       设置项  { get; set; }


    // 用于存放 全局设置.json
    public struct GlobalSettings
    {
      public 状态条 状态栏和其它设置 { get; set; }
      public ObservableCollection<列表项> 查找列表 { get; set; }
      public ObservableCollection<列表项> 外部工具 { get; set; }
      public ObservableCollection<列表项> 快键命令 { get; set; }
      public ObservableCollection<列表项> 快键 { get; set; }
      public ObservableCollection<列表项> 自启 { get; set; }
      public ObservableCollection<列表项> 自动关机 { get; set; }

    }

    private GlobalSettings _全局设置 = new()
    {
      状态栏和其它设置 = new(),
      查找列表 = new ObservableCollection<列表项>(),
      外部工具 = new ObservableCollection<列表项>(),
      快键命令 = new ObservableCollection<列表项>(),
      快键 = new ObservableCollection<列表项>(),
      自启 = new ObservableCollection<列表项>()
    };




    #endregion

    private readonly string _appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

    private readonly string _globalSettingFilePath; // 全局设置.json
    private readonly string _kegBakPath;            // Keg_bak.txt
    private readonly string _kegPath;               // 小科狗主程序目录
    private readonly string _kegFilePath;           // Keg.txt 文件路径
    private          int    _selectColorLabelNum;   // 用于记录当前选中的
    private          string _zhEn = "中c：";

    public GlobalSetting()
    {
      InitializeComponent();


      _globalSettingFilePath = $"{_appPath}\\configs\\全局设置.json";
      _kegBakPath = $"{_appPath}\\configs\\Keg_bak.txt";

      if (!Directory.Exists($"{_appPath}\\configs")) Directory.CreateDirectory($"{_appPath}\\configs");

      // 获取小科狗主程序目录
      _kegPath = Base.KegPath;
      _kegFilePath = Base.KegTextPath;

      Loaded += MainWindow_Loaded;
      colorPicker.ColorChanged += ColorPicker_ColorChanged;
    }


    private void ColorPicker_ColorChanged(object sender, ColorChangedEventArgs e)
    {
      if (colorPicker.RgbColor == null) return;
      rgbTextBox.RGBText = colorPicker.RgbText;
      SetColorLableColor(colorPicker.RgbColor);
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
      查找列表 = new ObservableCollection<列表项>();
      外部工具 = new ObservableCollection<列表项>();
      快键命令 = new ObservableCollection<列表项>();
      快键 = new ObservableCollection<列表项>();
      自启 = new ObservableCollection<列表项>();
      自动关机 = new ObservableCollection<列表项>();

      listView3.DataContext = 查找列表;
      listView8.DataContext = 外部工具;
      listView4.DataContext = 快键命令;
      listView5.DataContext = 快键;
      listView6.DataContext = 自启;
      listView7.DataContext = 自动关机;

      DataContext = this;
      LoadKegSkinImages();  // 读取 读取状态条皮肤图片
      ReadKegText();        // 读取 全局设置
    }

    // 显示颜色的 label 鼠标进入事件
    private void Color_label_MouseEnter(object sender, MouseEventArgs e)
    {
      SolidColorBrush color1 = new((Color)ColorConverter.ConvertFromString("#FF000000")!); // 黑色
      SolidColorBrush color2 = new((Color)ColorConverter.ConvertFromString("#FFFF0000")!); // 红色

      color_Label_01.Foreground = color1;
      color_Label_02.Foreground = color1;

      if (sender is not Label label) return;
      switch (label.Name)
      {
        case "color_Label_1":
          _selectColorLabelNum    = 1;
          color_Label_01.Foreground = color2;
          break;
        case "color_Label_2":
          _selectColorLabelNum    = 2;
          color_Label_02.Foreground = color2;
          break;
      }

      var currentColor = ((SolidColorBrush)label.Background).Color;
      // 计算反色
      var invertedColor = Color.FromArgb(255, (byte)~currentColor.R, (byte)~currentColor.G, (byte)~currentColor.B);
      label.BorderThickness = new Thickness(3);
      label.BorderBrush     = new SolidColorBrush(invertedColor);
      var hex = RemoveChars(label.Background.ToString(), 2);
      var rgb = HexToRgb(hex);
      rgbTextBox.RGBText = rgb;
    }

    // 显示颜色的 label 鼠标离开事件
    private void Color_label_MouseLeave(object sender, MouseEventArgs e)
    {
      if (sender is Label label) label.BorderThickness = new Thickness(2);
    }


    private void Button3_Copy_Click(object sender, RoutedEventArgs e)
    {
      var selectedFontName = SelectFontName();
      if (selectedFontName == null) return;
      if (sender is Button { Name: "button3_Copy1" }) textBox_Copy24.Text = selectedFontName;
    }

    private static string SelectFontName()
    {
      using var fontDialog = new FontDialog();

      // 设置初始字体选项（可选）
      // fontDialog.Font = new Font("Arial", 12);
      // 显示字体对话框并获取用户的选择结果
      return fontDialog.ShowDialog() == DialogResult.OK ? fontDialog.Font.Name : null;  // 返回用户选择的字体名称
    }


    // 更新对应标签的背景颜色
    private void SetColorLableColor(SolidColorBrush cColor)
    {
      Label[] colorLabels = { color_Label_1, color_Label_2 };
      // 计算反色
      var currentColor = cColor.Color;
      var invertedColor = Color.FromArgb(255, (byte)~currentColor.R, (byte)~currentColor.G, (byte)~currentColor.B);

      for (var i = 1; i <= colorLabels.Length; i++)
        if (i == _selectColorLabelNum)
        {
          colorLabels[i - 1].BorderBrush = new SolidColorBrush(invertedColor);
          colorLabels[i - 1].Background = cColor;
          toolTipTextBlock.Foreground = cColor;
        }
    }


    private void RGB_ValueChanged(object sender, RoutedPropertyChangedEventArgs<string> e)
    {
      SetColorLableColor(RgbStringToColor(rgbTextBox.RGBText));
    }


    // RGB字符串转换成Color
    private static SolidColorBrush RgbStringToColor(string rgbString)
    {
      //候选窗背景色为空时设为对话框背景色
      if (rgbString == "")
      {
        //hxcds_checkBox.IsChecked = true;
        return new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
      }
      // 去掉字符串两边的括号并将逗号分隔的字符串转换为整型数组
      var rgbValues = rgbString.Trim('(', ')').Split(',');
      if (rgbValues.Length != 3)
      {
        throw new ArgumentException("Invalid RGB color format.");
      }

      var r = byte.Parse(rgbValues[0]);
      var g = byte.Parse(rgbValues[1]);
      var b = byte.Parse(rgbValues[2]);

      return new SolidColorBrush(Color.FromRgb(r, g, b));
    }



    #region 全局设置


    // 恢复全局设置
    private void Default_button_Click(object sender, RoutedEventArgs e)
    {
      File.Delete(_globalSettingFilePath);
      查找列表.Clear();
      外部工具.Clear();
      快键命令.Clear();
      快键.Clear();
      自启.Clear();
      自动关机.Clear();
      LoadKegTxt(_kegBakPath);
    }

    // 重新读取全局设置
    private void Apply2_button_Click(object sender, RoutedEventArgs e)
    {
      LoadGlobalSettingJson();
    }
    // 应用全局设置
    private void Apply3_button_Click(object sender, RoutedEventArgs e)
    {
      if(checkBox3_Copy3.IsChecked != null && (bool)checkBox3_Copy3.IsChecked)
      {
        Base.SetValue("dbPath", "SQLiteDB_Path", Base.KegPath + "Keg.db");
        Base.SetValue("dbPath", "LevelDB_Path", Base.KegPath + @"zj\");
      }
      else
      {
        Base.SetValue("dbPath", "SQLiteDB_Path", @"C:\SiKegInput\Keg.db");
        Base.SetValue("dbPath", "LevelDB_Path", @"C:\SiKegInput\zj\");
      }
      SaveGlobalSettingJson();
      SaveKeg();
    }


    // 颜色转换 HexToRgb：(255, 255, 255)
    private static string HexToRgb(string hex)
    {
      // 预期hex字符串格式如 "FF8000" 或 "#FF8000"
      hex = hex.Replace("#", ""); // 移除可能存在的井号
      byte r, g, b;
      if (hex.Length == 6)
      {
        r = Convert.ToByte(hex.Substring(0, 2), 16);
        g = Convert.ToByte(hex.Substring(2, 2), 16);
        b = Convert.ToByte(hex.Substring(4, 2), 16);
      }
      else
      {
        r = Convert.ToByte(hex.Substring(2, 2), 16);
        g = Convert.ToByte(hex.Substring(4, 2), 16);
        b = Convert.ToByte(hex.Substring(6, 2), 16);
      }
      return $"({r}, {g}, {b})";
    }



    // 保存 Keg.txt
    private void SaveKeg()
    {
      var kegText = ""; // 存放 Keg.txt 文件
      kegText += $"《提示文本的位置={取提示文本的位置()}》\n";
      kegText += $"《提示文本要隐藏吗？={要或不要(checkBox3_Copy.IsChecked != null && (bool)checkBox3_Copy.IsChecked)}》\n";
      kegText += $"《提示文本要显示中英以及大小写状态吗？={要或不要(checkBox3_Copy1.IsChecked != null && (bool)checkBox3_Copy1.IsChecked)}》\n";
      kegText += $"《提示文本中文字体色={HexToRgb(color_Label_1.Background.ToString())}》\n";
      kegText += $"《提示文本英文字体色={HexToRgb(color_Label_2.Background.ToString())}》\n";
      kegText += $"《提示文本字体大小={nud23.Value}》\n";
      kegText += $"《提示文本字体名称={textBox_Copy24.Text}》\n";
      kegText += $"《打字字数统计等数据是要保存在主程文件夹下吗？={是或不是(checkBox3_Copy3.IsChecked != null && (bool)checkBox3_Copy3.IsChecked)}》\n";
      kegText += $"《快键只在候选窗口显示情况下才起作用吗？={是或不是(checkBox3_Copy2.IsChecked != null && (bool)checkBox3_Copy2.IsChecked)}》\n";
      kegText += $"《要启用深夜锁屏吗？={要或不要(checkBox3_Copy4.IsChecked != null && (bool)checkBox3_Copy4.IsChecked)}》\n";

      kegText += "\n在线查找\n";
      kegText =  查找列表.Where(item => item.Enable).Aggregate(kegText, (current, item)
        => current + $"《在线查找={item.Name}::{item.Value}》\n");

      kegText += "\n外部工具动态菜单\n";
      kegText =  外部工具.Where(item => item.Enable).Aggregate(kegText, (current, item)
        => current + $"《外部工具={item.Name}:{item.Value}》\n");

      kegText += "\n运行命令行快键\n";
      kegText =  快键命令.Where(item => item.Enable).Aggregate(kegText, (current, item) 
        => current + $"《运行命令行快键={item.Value}<命令行={item.Cmd}>》\n");

      kegText += "\n快键\n";
      kegText =  快键.Where(item => item.Enable).Aggregate(kegText, (current, item) 
        => current + $"《{item.Name}={item.Value}》\n");

      kegText += "\n自启\n";
      kegText =  自启.Where(item => item.Enable).Aggregate(kegText, (current, item) 
        => current + $"《自启={item.Value}》\n");

      kegText += "\n自动关机\n";
      foreach (var item in 自动关机)
        if (item.Enable)
        {
          var str = item.Value.Split(':');
          if (str[1].Length != 2) str[1] = "0" + str[1];
          kegText += $"《自动关机={str[0]}{str[1]}》\n";
        }

      // 写出文件 Keg.txt
      File.WriteAllText(_kegFilePath, kegText);

      try
      {
        Base.SendMessageTimeout(Base.KwmUpqjset);
      }
      catch (Exception ex)
      {
        MessageBox.Show("操作失败，请重试！");
        Console.WriteLine(ex.Message);
      }
    }

    private static bool IsTrueOrFalse(string value)
    {
      return value != "不要" && value != "不是";
    }

    private static string 是或不是(bool b)
    {
      return b ? "是" : "不是";
    }

    private static string 要或不要(bool b)
    {
      return b ? "要" : "不要";
    }


    private void 提示文本的位置(string value)
    {
      toolTipTextBlock = FindName("toolTipTextBlock") as TextBlock;
      switch (value)
      {
        case "0":
          radioButton18.IsChecked = true;
          if (toolTipTextBlock != null) toolTipTextBlock.VerticalAlignment = VerticalAlignment.Top;
          break;
        case "1":
          radioButton19.IsChecked = true;
          if (toolTipTextBlock != null) toolTipTextBlock.VerticalAlignment = VerticalAlignment.Center;
          break;
        case "2":
          radioButton20.IsChecked = true;
          if (toolTipTextBlock != null) toolTipTextBlock.VerticalAlignment = VerticalAlignment.Bottom;
          break;
      }

    }
    private string 取提示文本的位置()
    {
      if (radioButton18.IsChecked == true) return "0";
      return radioButton19.IsChecked == true ? "1" : "2";
    }

    // 读取 全局设置
    private void ReadKegText()
    {
      if (File.Exists(_globalSettingFilePath))
      {
        LoadGlobalSettingJson();
      }
      else
      {
        File.Copy(_kegFilePath, _kegBakPath, true);
        LoadKegTxt(_kegFilePath);
        SaveGlobalSettingJson();
      }
    }

    // 读取文件 Keg.txt
    private void LoadKegTxt(string kegFilePath)
    {
      var    kegText = File.ReadAllText(kegFilePath);

      var pattern =  "《(提示文本.*?)|(打字字数统.*?)|(快键只在候.*?)|(要启用深夜.*?)=(.*)》";
      var    matches = Regex.Matches(kegText, pattern);
      foreach (Match match in matches)
      {
        var value = match.Groups[2].Value;
        switch (match.Groups[1].Value)
        {
          case "提示文本的位置": 提示文本的位置(value); break;
          case "提示文本要隐藏吗？": checkBox3_Copy.IsChecked = IsTrueOrFalse(value); break;
          case "提示文本要显示中英以及大小写状态吗？": checkBox3_Copy1.IsChecked = IsTrueOrFalse(value); break;
          case "提示文本中文字体色": color_Label_1.Background = RgbStringToColor(value); break;
          case "提示文本英文字体色": color_Label_2.Background = RgbStringToColor(value); break;
          case "提示文本字体大小": nud23.Value = int.Parse(value); break;
          case "提示文本字体名称": textBox_Copy24.Text = value; break;
          case "打字字数统计等数据是要保存在主程文件夹下吗？": checkBox3_Copy3.IsChecked = IsTrueOrFalse(value); break;
          case "快键只在候选窗口显示情况下才起作用吗？": checkBox3_Copy2.IsChecked = IsTrueOrFalse(value); break;
          case "要启用深夜锁屏吗？": checkBox3_Copy4.IsChecked = IsTrueOrFalse(value); break;
        }
      }

      pattern = "《在线查找=(.*?)::(.*)》";
      matches = Regex.Matches(kegText, pattern);
      foreach (Match match in matches)
      {
        var item = new 列表项
        {
          Enable = true,
          Name = match.Groups[1].Value,
          Value = match.Groups[2].Value,
          Cmd = "",
        };
        查找列表.Add(item);
      }

      pattern = "《外部工具=(.*?):(.*)》";
      matches = Regex.Matches(kegText, pattern);
      if (matches.Count > 0)
      {
        foreach (Match match in matches)
        {
          var item = new 列表项
          {
            Enable = true,
            Name = match.Groups[1].Value,
            Value = match.Groups[2].Value,
            Cmd = "",
          };
          外部工具.Add(item);
        }
      }


      pattern = "《(运行命令行快键)=(.*)<命令行=(.*)>》";
      matches = Regex.Matches(kegText, pattern);
      if (matches.Count > 0)
      {
        foreach (Match match in matches)
        {
          var item = new 列表项
          {
            Enable = true,
            Name = match.Groups[1].Value,
            Value = match.Groups[2].Value,
            Cmd = match.Groups[3].Value,
          };
          快键命令.Add(item);
        }
      }
      else
      {
        var item = new 列表项
        {
          Enable = false,
          Name = "运行命令行快键=",
          Value = "",
          Cmd = "",
        };
        快键命令.Add(item);
      }

      pattern = "《(?!.*命令行快键)(.*快键)=(.*)》";
      matches = Regex.Matches(kegText, pattern);
      foreach (Match match in matches)
      {
        var item = new 列表项
        {
          Enable = true,
          Name = match.Groups[1].Value,
          Value = match.Groups[2].Value,
          Cmd = "",
        };
        快键.Add(item);
      }

      pattern = "《(自启)=(.*)》";
      matches = Regex.Matches(kegText, pattern);
      if (matches.Count > 0)
      {
        foreach (Match match in matches)
        {
          var item = new 列表项
          {
            Enable = true,
            Name = match.Groups[1].Value,
            Value = match.Groups[2].Value,
            Cmd = "",
          };
          自启.Add(item);
        }
      }
      else
      {
        var item = new 列表项
        {
          Enable = false,
          Name = "自启",
          Value = "",
          Cmd = "",
        };
        自启.Add(item);
      }


      pattern = "《(自动关机)=(.*)》";
      matches = Regex.Matches(kegText, pattern);
      if (matches.Count > 0)
      {
        foreach (Match match in matches)
        {
          var item = new 列表项
          {
            Enable = false,
            Name = match.Groups[1].Value,
            Value = match.Groups[2].Value == "" ? "22:30" : match.Groups[2].Value,
            Cmd = "",
          };
          自动关机.Add(item);
        }
      }
      else
      {
        var item = new 列表项
        {
          Enable = false,
          Name = "自动关机",
          Value = "22:30",
          Cmd = "",
        };
        自动关机.Add(item);
      }
    }

    // 保存文件 全局设置.json
    private void SaveGlobalSettingJson()
    {
      设置项 = new 状态条
      {
        提示文本字体大小              = nud23.Value,
        提示文本字体名称              = textBox_Copy24.Text,
        提示文本的位置               = 取提示文本的位置(),
        提示文本要隐藏吗              = checkBox3_Copy.IsChecked  != null && (bool)checkBox3_Copy.IsChecked,
        提示文本要显示中英以及大小写状态吗     = checkBox3_Copy1.IsChecked != null && (bool)checkBox3_Copy1.IsChecked,
        打字字数统计等数据是要保存在主程文件夹下吗 = checkBox3_Copy3.IsChecked != null && (bool)checkBox3_Copy3.IsChecked,
        快键只在候选窗口显示情况下才起作用吗    = checkBox3_Copy2.IsChecked != null && (bool)checkBox3_Copy2.IsChecked,
        要启用深夜锁屏吗              = checkBox3_Copy4.IsChecked != null && (bool)checkBox3_Copy4.IsChecked,
        提示文本中文字体色             = RemoveChars(color_Label_1.Background.ToString(), 2),
        提示文本英文字体色             = RemoveChars(color_Label_2.Background.ToString(), 2),
      };

      _全局设置 = new()
      {
        状态栏和其它设置 = 设置项,
        查找列表 = 查找列表,
        外部工具 = 外部工具,
        快键命令 = 快键命令,
        快键 = 快键,
        自启 = 自启,
        自动关机 = 自动关机
      };

      var jsonString = JsonConvert.SerializeObject(_全局设置, Formatting.Indented);
      File.WriteAllText(_globalSettingFilePath, jsonString);
    }

    // 读取文件 全局设置.json
    private void LoadGlobalSettingJson()
    {

      _全局设置 = new()
      {
        状态栏和其它设置 = new(),
        查找列表 = new ObservableCollection<列表项>(),
        外部工具 = new ObservableCollection<列表项>(),
        快键命令 = new ObservableCollection<列表项>(),
        快键 = new ObservableCollection<列表项>(),
        自启 = new ObservableCollection<列表项>(),
        自动关机 = new ObservableCollection<列表项>()
      };

      // 读取整个文件内容,将JSON字符串反序列化为对象
      var jsonString = File.ReadAllText(_globalSettingFilePath);
      _全局设置 = JsonConvert.DeserializeObject<GlobalSettings>(jsonString);
      查找列表 = _全局设置.查找列表;
      外部工具 = _全局设置.外部工具;
      快键命令 = _全局设置.快键命令;
      快键 = _全局设置.快键;
      自启 = _全局设置.自启;
      自动关机 = _全局设置.自动关机;
      设置项 = _全局设置.状态栏和其它设置;

      if (设置项 != null)
      {
        提示文本的位置(设置项.提示文本的位置);
        checkBox3_Copy.IsChecked  = 设置项.提示文本要隐藏吗;
        nud23.Value               = 设置项.提示文本字体大小;
        textBox_Copy24.Text       = 设置项.提示文本字体名称;
        checkBox3_Copy4.IsChecked = 设置项.要启用深夜锁屏吗;
        checkBox3_Copy1.IsChecked = 设置项.提示文本要显示中英以及大小写状态吗;
        checkBox3_Copy2.IsChecked = 设置项.快键只在候选窗口显示情况下才起作用吗;
        checkBox3_Copy3.IsChecked = 设置项.打字字数统计等数据是要保存在主程文件夹下吗;
        color_Label_1.Background  = new SolidColorBrush((Color)ColorConverter.ConvertFromString(设置项.提示文本中文字体色)!);
        color_Label_2.Background  = new SolidColorBrush((Color)ColorConverter.ConvertFromString(设置项.提示文本英文字体色)!);
      }

      listView3.ItemsSource = 查找列表; // ListView的数据
      listView8.ItemsSource = 外部工具;
      listView4.ItemsSource = 快键命令;
      listView5.ItemsSource = 快键;
      listView6.ItemsSource = 自启;
      listView7.ItemsSource = 自动关机;

    }


    // Hex格式 ARGB 转 RGB，如 #FFAABBCC -> #AABBCC
    private static string RemoveChars(string str, int n)
    {
      str = str.Replace("#", ""); // 移除可能存在的井号
      return "#" + str.Substring(2, str.Length - n);
    }



    // listView 控件禁止响应滚轮事件
    private void 控件禁止响应滚轮事件(object sender, MouseWheelEventArgs e)
    {
      if (sender is ListView listView)
      {
        e.Handled = true; // 阻止事件继续向下传递到ListView的内部ScrollViewer

        // 创建一个新的MouseWheelEventArgs，将事件向上传递到StackPanel
        var newEventArgs = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
        {
          RoutedEvent = UIElement.MouseWheelEvent,
          Source      = sender
        };
        ((UIElement)listView.Parent).RaiseEvent(newEventArgs);
      }
    }
    
    private static object GetDataItem(object sender)
    {
      // 获取点击按钮的父级容器（即ListViewItem）
      var listViewItem = sender as FrameworkElement;
      while (listViewItem != null && listViewItem is not ListViewItem)
      {
        listViewItem = VisualTreeHelper.GetParent(listViewItem) as FrameworkElement;
      }
      if (listViewItem is not ListViewItem item) return null;

      // 获取绑定到按钮的数据项
      var dataItem = item.DataContext;
      if (dataItem == null) return null;

      return dataItem;
    }

    private void HotKeyControl_HotKeyPressed(object sender, RoutedEventArgs e)
    {
      var hotKeyControl = sender as HotKeyControl;
      var dataItem = GetDataItem(sender);
      if (dataItem is not 列表项 listitem) return;
      if (hotKeyControl != null)
        listitem.Value = hotKeyControl.HotKey;
    }
    private void TextBox1_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (sender is TextBox { DataContext: 列表项 item } textBox) item.Name = textBox.Text;
    }
    private void TextBox2_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (sender is not TextBox textBox) return;
      if (textBox.DataContext is 列表项 item)
        item.Value = textBox.Text;
    }

    private void TextBox3_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (sender is TextBox { DataContext: 列表项 item } textBox) item.Cmd = textBox.Text;
    }

    private void CheckBox_Click(object sender, RoutedEventArgs e)
    {
      var dataItem = GetDataItem(sender);
      if (dataItem is 列表项 listitem)
        listitem.Enable = listitem.Enable;
    }




    // 鼠标进入 ListViewItem 时触发
    private void ListViewItem_MouseEnter(object sender, MouseEventArgs e)
    {
      // 寻找事件源中的 Button 控件
      if (sender is ListViewItem item)
      {
        var delButton = FindChild<Button>(item, "delButton");
        if (delButton != null)
          delButton.Visibility = Visibility.Visible;
      }
    }

    // 鼠标离开 ListViewItem 时触发
    private void ListViewItem_MouseLeave(object sender, MouseEventArgs e)
    {
      // 寻找事件源中的 Button 控件
      if (sender is not ListViewItem item) return;
      var delButton = FindChild<Button>(item, "delButton");
      if (delButton != null)
        delButton.Visibility = Visibility.Hidden;
    }

    // 递归查找子控件的帮助方法
    private static T FindChild<T>(DependencyObject parent, string childName)
       where T : DependencyObject
    {
      // 确认传入的参数有效
      if (parent == null) return null;

      T foundChild = null;

      var childrenCount = VisualTreeHelper.GetChildrenCount(parent);
      for (var i = 0; i < childrenCount; i++)
      {
        var child = VisualTreeHelper.GetChild(parent, i);
        // 确认子控件是正确的类型
        if (child is not T dependencyObject)
        {
          // 如果不是，递归查找
          foundChild = FindChild<T>(child, childName);
          if (foundChild != null) break;
        }
        else if (!string.IsNullOrEmpty(childName))
        {
          // 检查控件的名称是否匹配
          if (dependencyObject is not FrameworkElement frameworkElement || frameworkElement.Name != childName) continue;
          foundChild = dependencyObject;
          break;
        }
        else
        {
          // 名称不重要，返回第一个找到的匹配类型的子控件
          foundChild = dependencyObject;
          break;
        }
      }

      return foundChild;
    }


    // 列表项删除按钮通过 Tag 来判定
    private void DelButton_Click(object sender, RoutedEventArgs e)
    {
      var button = sender as Button;
      if (button == null) return;

      var dataItem = GetDataItem(sender);
      if (dataItem is not 列表项 listitem) return;
      if (int.TryParse(button.Tag.ToString(), out var listViewNum))
      {
        删除列表项(listViewNum, listitem);
      }
    }

    // listView 删除
    private void 删除列表项(int listViewNum, 列表项 listViewItem)
    {
      var result = MessageBox.Show("您确定要删除吗？", "删除操作", MessageBoxButton.OKCancel, MessageBoxImage.Question);
      if (result != MessageBoxResult.OK) return;
      switch (listViewNum)
      {
        case 3: 查找列表.Remove(listViewItem); break;
        case 4: 快键命令.Remove(listViewItem); break;
        case 5: 快键.Remove(listViewItem); break;
        case 6: 自启.Remove(listViewItem); break;
        case 7: 自动关机.Remove(listViewItem); break;
        case 8: 外部工具.Remove(listViewItem); break;
      }
    }

    // 列表项添加按钮
    private void Add_button_Click(object sender, RoutedEventArgs e)
    {
      if (sender is Button btn)
        AddNewItem(btn);
    }

    // listView 添加
    private void AddNewItem(Button btn)
    {
      // 根据按钮创建新地列表项
      列表项 item = new()
      {
        Enable = false,
        Name = "",
        Value = "",
        Cmd = ""
      };

      switch(btn.Content){
        case "添加在线查找": 查找列表.Insert(0, item); FocusAndSelect(listView3); break;
        case "添加快捷命令": item.Name = "运行命令行快键";
                             快键命令.Insert(0, item); FocusAndSelect(listView4); break;
        case "添加快捷键"  : item.Name = (comboBox4.SelectedItem as ComboBoxItem)?.Content?.ToString(); 
                             快键.Insert(0, item); FocusAndSelect(listView5); break;
        case "添加自启应用": item.Name = "自启";
                             自启.Insert(0, item); FocusAndSelect(listView6); break;
        case "添加定时关机": item.Name = "自动关机"; item.Value = "22:30";
                             自动关机.Insert(0, item); FocusAndSelect(listView7); break;
        case "添加外部工具": 外部工具.Insert(0, item); FocusAndSelect(listView8); break;
      }
    }

    private void FocusAndSelect(ListView listView)
    {
      // 设置焦点并选择第一项
      listView.Focus();
      listView.SelectedIndex = 0;
    }



    private void CheckBox3_Copy1_CheckedChanged(object sender, RoutedEventArgs e)
    {
      _zhEn = checkBox3_Copy1.IsChecked == true ? "中c：" : "";
      toolTipTextBlock.Text = $"{_zhEn}方案名称";
    }

    private void CheckBox3_Copy_CheckedChanged(object sender, RoutedEventArgs e)
    {
      toolTipTextBlock.Visibility = checkBox3_Copy.IsChecked == true ? Visibility.Hidden : Visibility.Visible;
    }

    private void LoadKegSkinImages()
    {
      // 获取应用的相对路径并转换为绝对路径
      var directoryPath = _kegPath + "skin\\";

      // 设置皮肤图片路径
      var skin = $"{directoryPath}Keg.png";
      var skinBackup = $"{directoryPath}默认.png";

      // 将皮肤图片设置为图像源
      try
      {
        SetImage(Base.GetValue("skin", "path")); 
      }
      catch
      {
        SetImage(skin);
      }

      // 如果备份皮肤文件不存在，则复制当前皮肤文件作为备份
      if (!File.Exists(skinBackup))
        File.Copy(skin, skinBackup);


      // 定义支持的图片扩展名数组
      var imageExtensions = new[] { ".png", ".gif" };

      // 获取目录下所有非“Keg”且具有指定扩展名的图片文件
      var allFiles = Directory.GetFiles(directoryPath)
                           .Where(f => imageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant())
                                       && Path.GetFileNameWithoutExtension(f) != "Keg");

      // 查找默认皮肤文件，如果存在则将其从原始文件列表中移除
      var enumerable  = allFiles as string[] ?? allFiles.ToArray();
      var defaultFile = enumerable.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == "默认");

      List<string> files;
      if (defaultFile != null)
      {
        // 移除默认皮肤文件后，将其添加到新列表的首位
        files = enumerable.Except(new[] { defaultFile }).ToList();
        files.Insert(0, defaultFile);
      }
      else
      {
        // 若默认皮肤文件不存在，则直接使用所有符合条件的文件列表
        files = enumerable.ToList();
      }

      // 遍历处理后的图片文件集合
      foreach (var file in files)
      {
        // 从文件路径中提取图片名称
        var imageName = Path.GetFileName(file);

        // 将图片名称添加到皮肤列表框中
        skinListBox.Items.Add(imageName);
      }
    }

    private void SkinListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      var selectedItem = (string)skinListBox.SelectedItem;
      var selectedImagePath = _kegPath + "skin\\" + selectedItem;

      SetImage(selectedImagePath);
    }


    private void SetImage(string imagePath)
    {
      if (string.IsNullOrEmpty(imagePath)) return;
      BitmapImage image = new();
      image.BeginInit();
      image.UriSource = new Uri(imagePath);
      image.EndInit();

      // 检查文件扩展名以确定是否为GIF
      if (Path.GetExtension(imagePath).Equals(".gif", StringComparison.OrdinalIgnoreCase))
      {
        ImageBehavior.SetAnimatedSource(displayImage, image);
      }
      else
      {
        // 清除动画状态
        ImageBehavior.SetAnimatedSource(displayImage, null);
        displayImage.Source = image;
      }
    }

    private void SaveButton1_Click(object sender, RoutedEventArgs e)
    {
      if (skinListBox.SelectedIndex < 0) return;
      var selectedItem = (string)skinListBox.SelectedItem;
      var selectedSkin = _kegPath + "skin\\" + selectedItem;

      try
      {
        Clipboard.SetText(selectedSkin);
        // 更新皮肤文件路径
        Base.SendMessageTimeout(Base.KwmUppfset);
        Base.SetValue("skin", "path", selectedSkin);
      }
      catch (Exception ex)
      {
        MessageBox.Show("操作失败，请重试！");
        Console.WriteLine(ex.Message);
      }
    }

    // 提示文本位置
    private void RadioButton_Click(object sender, RoutedEventArgs e)
    {
      var radioButton = (RadioButton)sender;

      if (radioButton.IsChecked != null && !(bool)radioButton.IsChecked) return;
      toolTipTextBlock.VerticalAlignment = radioButton.Content.ToString() switch
      {
        "顶部" => VerticalAlignment.Top,
        "中间" => VerticalAlignment.Center,
        "底部" => VerticalAlignment.Bottom,
        _    => toolTipTextBlock.VerticalAlignment
      };
    }





    #endregion


  }
}
