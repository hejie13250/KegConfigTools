using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Button = System.Windows.Controls.Button;
using Clipboard = System.Windows.Clipboard;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using ListView = System.Windows.Controls.ListView;
using ListViewItem = System.Windows.Controls.ListViewItem;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Page = System.Windows.Controls.Page;
using Path = System.IO.Path;
using RadioButton = System.Windows.Controls.RadioButton;
using TextBox = System.Windows.Controls.TextBox;
using static 小科狗配置.MainWindow;
using System.Windows.Controls.Primitives;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Windows.Forms;
using Label = System.Windows.Controls.Label;
using GroupBox = System.Windows.Controls.GroupBox;

namespace 小科狗配置
{
  /// <summary>
  /// GlobalSetting.xaml 的交互逻辑
  /// </summary>
  public partial class GlobalSetting : Page, INotifyPropertyChanged
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

    #region 消息接口定义
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint flags, uint timeout, out IntPtr pdwResult);

    const uint ABORTIFHUNG = 0x0002;
    readonly uint flags = (uint)(ABORTIFHUNG);
    readonly uint timeout = 500;

    const int WM_USER = 0x0400;                   // 根据Windows API定义
    const uint KWM_UPQJSET = (uint)WM_USER + 211; //读取说明文本更新全局设置
    const uint KWM_UPPFSET = (uint)WM_USER + 212; //从剪切板取皮肤png或gif文件的全路径设置,更新状态之肤 格式:文件全路径放到剪切板

    #endregion

    readonly string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

    readonly string globalSettingFilePath = "全局设置.json";
    readonly string kegBakPath = "Keg_bak.txt";
    string zh_en = "中c：";
    readonly string kegPath;                        // 小科狗主程序目录
    readonly string kegFilePath;                    // Keg.txt 文件路径
    int select_color_label_num = 0;                 // 用于记录当前选中的 


    #region 全局设置界面列表项定义

    public class 列表项 : INotifyPropertyChanged
    {
      private bool _Enable;
      public bool Enable
      {
        get { return _Enable; }
        set { _Enable = value; OnPropertyChanged("Enable"); }
      }
      private string _Name;
      public string Name
      {
        get { return _Name; }
        set { _Name = value; OnPropertyChanged("Name"); }
      }
      private string _Value;
      public string Value
      {
        get { return _Value; }
        set { _Value = value; OnPropertyChanged("Value"); }
      }

      private string _Cmd;
      public string CMD
      {
        get { return _Cmd; }
        set { _Cmd = value; OnPropertyChanged("CMD"); }
      }
      public 列表项() { }


      public event PropertyChangedEventHandler PropertyChanged;
      public virtual void OnPropertyChanged(string PropertyName)
      {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
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
    public ObservableCollection<列表项> 查找列表 { get; set; }
    public ObservableCollection<列表项> 外部工具 { get; set; }
    public ObservableCollection<列表项> 快键命令 { get; set; }
    public ObservableCollection<列表项> 自动关机 { get; set; }
    public ObservableCollection<列表项> 快键 { get; set; }
    public ObservableCollection<列表项> 自启 { get; set; }
    public 状态条 设置项 { get; set; }


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

    GlobalSettings 全局设置 = new()
    {
      状态栏和其它设置 = new(),
      查找列表 = new ObservableCollection<列表项>(),
      外部工具 = new ObservableCollection<列表项>(),
      快键命令 = new ObservableCollection<列表项>(),
      快键 = new ObservableCollection<列表项>(),
      自启 = new ObservableCollection<列表项>()
    };




    #endregion



    public GlobalSetting()
    {
      InitializeComponent();


      globalSettingFilePath = $"{appPath}\\configs\\全局设置.json";
      kegBakPath = $"{appPath}\\configs\\Keg_bak.txt";

      if (!Directory.Exists($"{appPath}\\configs")) Directory.CreateDirectory($"{appPath}\\configs");

      // 获取小科狗主程序目录
      kegPath = Base.GetValue("window", "keg_path");
      if (!File.Exists(kegPath + "KegServer.exe"))
      {
        kegPath = Base.GetKegPath();
        if (File.Exists(kegPath + "KegServer.exe")) Base.SetValue("window", "keg_path", kegPath);
        else ((App)System.Windows.Application.Current).Exit();
      }
      kegFilePath = kegPath + "Keg.txt";

      Loaded += MainWindow_Loaded;
      colorPicker.ColorChanged += ColorPicker_ColorChanged;
    }


    private void ColorPicker_ColorChanged(object sender, ColorChangedEventArgs e)
    {
      if (colorPicker.RGBcolor != null)
      {
        rgbTextBox.RGBText = colorPicker.RGBText;
        SetColorLableColor(colorPicker.RGBcolor);
      }
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

      SolidColorBrush color1 = new((Color)ColorConverter.ConvertFromString("#FF000000"));  // 黑色
      SolidColorBrush color2 = new((Color)ColorConverter.ConvertFromString("#FFFF0000"));  // 红色

      color_label_01.Foreground = color1;
      color_label_02.Foreground = color1;

      Label label = sender as Label;
      switch (label.Name)
      {
        case "color_label_1": select_color_label_num = 1; color_label_01.Foreground = color2; break;
        case "color_label_2": select_color_label_num = 2; color_label_02.Foreground = color2; break;
      }
      var currentColor = ((SolidColorBrush)label.Background).Color;
      // 计算反色
      var invertedColor = Color.FromArgb(255, (byte)~currentColor.R, (byte)~currentColor.G, (byte)~currentColor.B);
      label.BorderThickness = new Thickness(3);
      label.BorderBrush = new SolidColorBrush(invertedColor);
      var hex = RemoveChars(label.Background.ToString(), 2);
      var rgb = HexToRgb(hex);
      rgbTextBox.RGBText = rgb;
    }

    // 显示颜色的 label 鼠标离开事件
    private void Color_label_MouseLeave(object sender, MouseEventArgs e)
    {
      Label label = sender as Label;
      label.BorderThickness = new Thickness(2);
    }


    private void Button3_Copy_Click(object sender, RoutedEventArgs e)
    {
      var selectedFontName = SelectFontName();
      if (selectedFontName != null)
      {
        Button btn = sender as Button;
        switch (btn.Name)
        {
          //case "button3_Copy": textBox_Copy145.Text = selectedFontName.ToString(); break;
          case "button3_Copy1": textBox_Copy24.Text = selectedFontName.ToString(); break;
        }
      }
    }

    public static string SelectFontName()
    {
      using var fontDialog = new FontDialog();
      // 设置初始字体选项（可选）
      // fontDialog.Font = new Font("Arial", 12);

      // 显示字体对话框并获取用户的选择结果
      if (fontDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        return fontDialog.Font.Name; // 返回用户选择的字体名称

      return null;
    }


    // 更新对应标签的背景颜色
    private void SetColorLableColor(SolidColorBrush c_color)
    {
      Label[] colorLabels = { color_label_1, color_label_2 };
      // 计算反色
      var currentColor = c_color.Color;
      var invertedColor = Color.FromArgb(255, (byte)~currentColor.R, (byte)~currentColor.G, (byte)~currentColor.B);

      for (int i = 1; i <= colorLabels.Length; i++)
        if (i == select_color_label_num)
        {
          colorLabels[i - 1].BorderBrush = new SolidColorBrush(invertedColor);
          colorLabels[i - 1].Background = c_color;
          toolTipTextBlock.Foreground = c_color;
        }
    }


    private void RGB_ValueChanged(object sender, RoutedPropertyChangedEventArgs<string> e)
    {
      SetColorLableColor(RGBStringToColor(rgbTextBox.RGBText));
    }


    // RGB字符串转换成Color
    private SolidColorBrush RGBStringToColor(string rgbString)
    {
      //候选窗背景色为空时设为对话框背景色
      if (rgbString == "")
      {
        //hxcds_checkBox.IsChecked = true;
        return new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
      }
      // 去掉字符串两边的括号并将逗号分隔的字符串转换为整型数组
      string[] rgbValues = rgbString.Trim('(', ')').Split(',');
      if (rgbValues.Length != 3)
      {
        throw new ArgumentException("Invalid RGB color format.");
      }

      byte r = byte.Parse(rgbValues[0]);
      byte g = byte.Parse(rgbValues[1]);
      byte b = byte.Parse(rgbValues[2]);

      return new SolidColorBrush(Color.FromRgb(r, g, b));
    }



    #region 全局设置


    // 恢复全局设置
    private void Default_button_Click(object sender, RoutedEventArgs e)
    {
      File.Delete(globalSettingFilePath);
      查找列表.Clear();
      外部工具.Clear();
      快键命令.Clear();
      快键.Clear();
      自启.Clear();
      自动关机.Clear();
      LoadKegTxt(kegBakPath);
    }

    // 重新读取全局设置
    private void Apply2_button_Click(object sender, RoutedEventArgs e)
    {
      LoadGlobalSettingJson();
    }
    // 应用全局设置
    private void Apply3_button_Click(object sender, RoutedEventArgs e)
    {
      SaveGlobalSettingJson();
      SaveKeg();
    }


    // 颜色转换 HexToRgb：(255, 255, 255)
    public static string HexToRgb(string hex)
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
      string kegText = ""; // 存放 Keg.txt 文件
      kegText += $"《提示文本的位置={取提示文本的位置()}》\n";
      kegText += $"《提示文本要隐藏吗？={要或不要((bool)checkBox3_Copy.IsChecked)}》\n";
      kegText += $"《提示文本要显示中英以及大小写状态吗？={要或不要((bool)checkBox3_Copy1.IsChecked)}》\n";
      kegText += $"《提示文本中文字体色={HexToRgb(color_label_1.Background.ToString())}》\n";
      kegText += $"《提示文本英文字体色={HexToRgb(color_label_2.Background.ToString())}》\n";
      kegText += $"《提示文本字体大小={nud23.Value}》\n";
      kegText += $"《提示文本字体名称={textBox_Copy24.Text}》\n";
      kegText += $"《打字字数统计等数据是要保存在主程文件夹下吗？={是或不是((bool)checkBox3_Copy3.IsChecked)}》\n";
      kegText += $"《快键只在候选窗口显示情况下才起作用吗？={是或不是((bool)checkBox3_Copy2.IsChecked)}》\n";
      kegText += $"《要启用深夜锁屏吗？={要或不要((bool)checkBox3_Copy4.IsChecked)}》\n";

      kegText += $"\n在线查找\n";
      foreach (var item in 查找列表)
        if (item.Enable)
          kegText += $"《在线查找={item.Name}::{item.Value}》\n";

      kegText += $"\n外部工具动态菜单\n";
      foreach (var item in 外部工具)
        if (item.Enable)
          kegText += $"《外部工具={item.Name}:{item.Value}》\n";

      kegText += $"\n运行命令行快键\n";
      foreach (var item in 快键命令)
        if (item.Enable)
          kegText += $"《运行命令行快键={item.Value}<命令行={item.CMD}>》\n";

      kegText += $"\n快键\n";
      foreach (var item in 快键)
        if (item.Enable)
          kegText += $"《{item.Name}={item.Value}》\n";

      kegText += $"\n自启\n";
      foreach (var item in 自启)
        if (item.Enable)
          kegText += $"《自启={item.Value}》\n";

      kegText += $"\n自动关机\n";
      foreach (var item in 自动关机)
        if (item.Enable)
        {
          string[] str = item.Value.Split(':');
          if (str[1].Length != 2) str[1] = "0" + str[1];
          kegText += $"《自动关机={str[0]}{str[1]}》\n";
        }

      // 写出文件 Keg.txt
      File.WriteAllText(kegFilePath, kegText);

      try
      {
        IntPtr hWnd = FindWindow("CKegServer_0", null); //窗口句柄
        SendMessageTimeout(hWnd, KWM_UPQJSET, IntPtr.Zero, IntPtr.Zero, flags, timeout, out IntPtr pdwResult);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"操作失败，请重试！");
        Console.WriteLine(ex.Message);
      }
    }

    private bool IsTrueOrFalse(string value)
    {
      if (value == "不要" || value == "不是") return false;
      else return true;
    }

    private string 是或不是(bool b)
    {
      if (b == true) return "是";
      else return "不是";
    }

    private string 要或不要(bool b)
    {
      if (b == true) return "要";
      else return "不要";
    }


    private void 提示文本的位置(string value)
    {
      toolTipTextBlock = FindName("toolTipTextBlock") as TextBlock;
      switch (value)
      {
        case "0":
          radioButton18.IsChecked = true;
          toolTipTextBlock.VerticalAlignment = System.Windows.VerticalAlignment.Top;
          break;
        case "1":
          radioButton19.IsChecked = true;
          toolTipTextBlock.VerticalAlignment = System.Windows.VerticalAlignment.Center;
          break;
        case "2":
          radioButton20.IsChecked = true;
          toolTipTextBlock.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
          break;
      }

    }
    private string 取提示文本的位置()
    {
      if (radioButton18.IsChecked == true) return "0";
      if (radioButton19.IsChecked == true) return "1";
      return "2";
    }

    // 读取 全局设置
    private void ReadKegText()
    {
      if (File.Exists(globalSettingFilePath))
      {
        LoadGlobalSettingJson();
      }
      else
      {
        File.Copy(kegFilePath, kegBakPath, true);
        LoadKegTxt(kegFilePath);
        SaveGlobalSettingJson();
      }
    }

    // 读取文件 Keg.txt
    private void LoadKegTxt(string kegFilePath)
    {
      string kegText = File.ReadAllText(kegFilePath);
      MatchCollection matches;
      string pattern;

      // 设当状态栏控件值
      pattern = "《(提示文本.*?)|(打字字数统.*?)|(快键只在候.*?)|(要启用深夜.*?)=(.*)》";
      matches = Regex.Matches(kegText, pattern);
      foreach (Match match in matches)
      {
        var value = match.Groups[2].Value;
        switch (match.Groups[1].Value)
        {
          case "提示文本的位置": 提示文本的位置(value); break;
          case "提示文本要隐藏吗？": checkBox3_Copy.IsChecked = IsTrueOrFalse(value); break;
          case "提示文本要显示中英以及大小写状态吗？": checkBox3_Copy1.IsChecked = IsTrueOrFalse(value); break;
          case "提示文本中文字体色": color_label_1.Background = RGBStringToColor(value); break;
          case "提示文本英文字体色": color_label_2.Background = RGBStringToColor(value); break;
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
        var item = new 列表项()
        {
          Enable = true,
          Name = match.Groups[1].Value,
          Value = match.Groups[2].Value,
          CMD = "",
        };
        查找列表.Add(item);
      }

      pattern = "《外部工具=(.*?):(.*)》";
      matches = Regex.Matches(kegText, pattern);
      if (matches.Count > 0)
      {
        foreach (Match match in matches)
        {
          var item = new 列表项()
          {
            Enable = true,
            Name = match.Groups[1].Value,
            Value = match.Groups[2].Value,
            CMD = "",
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
          var item = new 列表项()
          {
            Enable = true,
            Name = match.Groups[1].Value,
            Value = match.Groups[2].Value,
            CMD = match.Groups[3].Value,
          };
          快键命令.Add(item);
        }
      }
      else
      {
        var item = new 列表项()
        {
          Enable = false,
          Name = "运行命令行快键=",
          Value = "",
          CMD = "",
        };
        快键命令.Add(item);
      }

      pattern = "《(?!.*命令行快键)(.*快键)=(.*)》";
      matches = Regex.Matches(kegText, pattern);
      foreach (Match match in matches)
      {
        var item = new 列表项()
        {
          Enable = true,
          Name = match.Groups[1].Value,
          Value = match.Groups[2].Value,
          CMD = "",
        };
        快键.Add(item);
      }

      pattern = "《(自启)=(.*)》";
      matches = Regex.Matches(kegText, pattern);
      if (matches.Count > 0)
      {
        foreach (Match match in matches)
        {
          var item = new 列表项()
          {
            Enable = true,
            Name = match.Groups[1].Value,
            Value = match.Groups[2].Value,
            CMD = "",
          };
          自启.Add(item);
        }
      }
      else
      {
        var item = new 列表项()
        {
          Enable = false,
          Name = "自启",
          Value = "当前文件夹的tools\\QInputV2.exe",
          CMD = "",
        };
        自启.Add(item);
      }


      pattern = "《(自动关机)=(.*)》";
      matches = Regex.Matches(kegText, pattern);
      if (matches.Count > 0)
      {
        foreach (Match match in matches)
        {
          var item = new 列表项()
          {
            Enable = false,
            Name = match.Groups[1].Value,
            Value = match.Groups[2].Value == "" ? "22:30" : match.Groups[2].Value,
            CMD = "",
          };
          自动关机.Add(item);
        }
      }
      else
      {
        var item = new 列表项()
        {
          Enable = false,
          Name = "自动关机",
          Value = "22:30",
          CMD = "",
        };
        自动关机.Add(item);
      }
    }

    // 保存文件 全局设置.json
    private void SaveGlobalSettingJson()
    {
      设置项 = new 状态条
      {
        提示文本字体大小 = nud23.Value,
        提示文本字体名称 = textBox_Copy24.Text,
        提示文本的位置 = 取提示文本的位置(),
        提示文本要隐藏吗 = (bool)checkBox3_Copy.IsChecked,
        提示文本要显示中英以及大小写状态吗 = (bool)checkBox3_Copy1.IsChecked,
        打字字数统计等数据是要保存在主程文件夹下吗 = (bool)checkBox3_Copy3.IsChecked,
        快键只在候选窗口显示情况下才起作用吗 = (bool)checkBox3_Copy2.IsChecked,
        要启用深夜锁屏吗 = (bool)checkBox3_Copy4.IsChecked,
        提示文本中文字体色 = RemoveChars(color_label_1.Background.ToString(), 2),
        提示文本英文字体色 = RemoveChars(color_label_2.Background.ToString(), 2),
      };

      全局设置 = new()
      {
        状态栏和其它设置 = 设置项,
        查找列表 = 查找列表,
        外部工具 = 外部工具,
        快键命令 = 快键命令,
        快键 = 快键,
        自启 = 自启,
        自动关机 = 自动关机
      };

      string jsonString = JsonConvert.SerializeObject(全局设置, Formatting.Indented);
      File.WriteAllText(globalSettingFilePath, jsonString);
    }

    // 读取文件 全局设置.json
    private void LoadGlobalSettingJson()
    {

      全局设置 = new()
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
      string jsonString = File.ReadAllText(globalSettingFilePath);
      全局设置 = JsonConvert.DeserializeObject<GlobalSettings>(jsonString);
      查找列表 = 全局设置.查找列表;
      外部工具 = 全局设置.外部工具;
      快键命令 = 全局设置.快键命令;
      快键 = 全局设置.快键;
      自启 = 全局设置.自启;
      自动关机 = 全局设置.自动关机;
      设置项 = 全局设置.状态栏和其它设置;

      提示文本的位置(设置项.提示文本的位置);
      checkBox3_Copy.IsChecked = 设置项.提示文本要隐藏吗;
      nud23.Value = 设置项.提示文本字体大小;
      textBox_Copy24.Text = 设置项.提示文本字体名称;
      checkBox3_Copy4.IsChecked = 设置项.要启用深夜锁屏吗;
      checkBox3_Copy1.IsChecked = 设置项.提示文本要显示中英以及大小写状态吗;
      checkBox3_Copy2.IsChecked = 设置项.快键只在候选窗口显示情况下才起作用吗;
      checkBox3_Copy3.IsChecked = 设置项.打字字数统计等数据是要保存在主程文件夹下吗;
      color_label_1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(设置项.提示文本中文字体色));
      color_label_2.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(设置项.提示文本英文字体色));

      listView3.ItemsSource = 查找列表; // ListView的数据
      listView8.ItemsSource = 外部工具;
      listView4.ItemsSource = 快键命令;
      listView5.ItemsSource = 快键;
      listView6.ItemsSource = 自启;
      listView7.ItemsSource = 自动关机;

    }


    // Hex格式 ARGB 转 RGB，如 #FFAABBCC -> #AABBCC
    public static string RemoveChars(string str, int n)
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
          Source = sender
        };
        ((UIElement)listView.Parent).RaiseEvent(newEventArgs);
      }
    }


    // 列表双击 启用/禁用选中项
    private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (sender is ListView listView)
        if (listView.SelectedItem is 列表项 selectedItem)
          selectedItem.Enable = selectedItem.Enable == true;
    }

    private object GetDataItem(object sender, RoutedEventArgs e)
    {
      // 获取点击按钮的父级容器（即ListViewItem）
      var listViewItem = sender as FrameworkElement;
      while (listViewItem != null && listViewItem is not ListViewItem)
      {
        listViewItem = VisualTreeHelper.GetParent(listViewItem) as FrameworkElement;
      }
      if (listViewItem == null || listViewItem is not ListViewItem item) return null;

      // 获取绑定到按钮的数据项
      var dataItem = item.DataContext;
      if (dataItem == null) return null;

      return dataItem;
    }

    private void HotKeyControl_HotKeyPressed(object sender, RoutedEventArgs e)
    {
      HotKeyControl hotKeyControl = sender as HotKeyControl;
      var dataItem = GetDataItem(sender, e);
      // 检查dataItem是否是列表项的实例
      if (dataItem is 列表项 listitem)
        listitem.Value = hotKeyControl.HotKey;
    }
    private void TextBox1_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (sender is TextBox textBox)
      {
        // 获取TextBox的DataContext，它应该是一个列表项实例
        // 确保item不是null
        if (textBox.DataContext is 列表项 item)
          item.Name = textBox.Text;
      }
    }
    private void TextBox2_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (sender is TextBox textBox)
        if (textBox.DataContext is 列表项 item)
          item.Value = textBox.Text;
    }

    private void TextBox3_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (sender is TextBox textBox)
        if (textBox.DataContext is 列表项 item)
          item.CMD = textBox.Text;
    }

    private void CheckBox_Click(object sender, RoutedEventArgs e)
    {
      var dataItem = GetDataItem(sender, e);
      // 检查dataItem是否是列表项的实例
      if (dataItem is 列表项 listitem)
        listitem.Enable = listitem.Enable == true;
    }
    private void DelButton3_Click(object sender, RoutedEventArgs e)
    {
      var dataItem = GetDataItem(sender, e);

      // 检查dataItem是否是列表项的实例
      if (dataItem is 列表项 listitem)
        删除列表项(3, listitem);
    }
    private void DelButton4_Click(object sender, RoutedEventArgs e)
    {
      var dataItem = GetDataItem(sender, e);

      // 检查dataItem是否是列表项的实例
      if (dataItem is 列表项 listitem)
        删除列表项(4, listitem);
    }

    private void DelButton5_Click(object sender, RoutedEventArgs e)
    {
      var dataItem = GetDataItem(sender, e);

      // 检查dataItem是否是列表项的实例
      if (dataItem is 列表项 listitem)
        删除列表项(5, listitem);
    }
    private void DelButton6_Click(object sender, RoutedEventArgs e)
    {
      var dataItem = GetDataItem(sender, e);

      // 检查dataItem是否是列表项的实例
      if (dataItem is 列表项 listitem)
        删除列表项(6, listitem);
    }
    private void DelButton7_Click(object sender, RoutedEventArgs e)
    {
      var dataItem = GetDataItem(sender, e);

      // 检查dataItem是否是列表项的实例
      if (dataItem is 列表项 listitem)
        删除列表项(7, listitem);
    }
    private void DelButton8_Click(object sender, RoutedEventArgs e)
    {
      var dataItem = GetDataItem(sender, e);

      // 检查dataItem是否是列表项的实例
      if (dataItem is 列表项 listitem)
        删除列表项(8, listitem);
    }
    // 鼠标进入 ListViewItem 时触发
    private void ListViewItem_MouseEnter(object sender, MouseEventArgs e)
    {
      // 寻找事件源中的 Button 控件
      if (sender is ListViewItem item)
      {
        Button delButton = FindChild<Button>(item, "delButton");
        if (delButton != null)
          delButton.Visibility = Visibility.Visible;
      }
    }

    // 鼠标离开 ListViewItem 时触发
    private void ListViewItem_MouseLeave(object sender, MouseEventArgs e)
    {
      // 寻找事件源中的 Button 控件
      if (sender is ListViewItem item)
      {
        Button delButton = FindChild<Button>(item, "delButton");
        if (delButton != null)
          delButton.Visibility = Visibility.Hidden;
      }
    }

    // 递归查找子控件的帮助方法
    public static T FindChild<T>(DependencyObject parent, string childName)
       where T : DependencyObject
    {
      // 确认传入的参数有效
      if (parent == null) return null;

      T foundChild = null;

      int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
      for (int i = 0; i < childrenCount; i++)
      {
        var child = VisualTreeHelper.GetChild(parent, i);
        // 确认子控件是正确的类型
        if (child is not T)
        {
          // 如果不是，递归查找
          foundChild = FindChild<T>(child, childName);
          if (foundChild != null) break;
        }
        else if (!string.IsNullOrEmpty(childName))
        {
          // 检查控件的名称是否匹配
          if (child is FrameworkElement frameworkElement && frameworkElement.Name == childName)
          {
            foundChild = (T)child;
            break;
          }
        }
        else
        {
          // 名称不重要，返回第一个找到的匹配类型的子控件
          foundChild = (T)child;
          break;
        }
      }

      return foundChild;
    }

    // listView 删除
    private void 删除列表项(int listViewNum, 列表项 listViewItem)
    {
      var result = MessageBox.Show("您确定要删除吗？", "删除操作", MessageBoxButton.OKCancel, MessageBoxImage.Question);
      if (result == MessageBoxResult.OK)
        switch (listViewNum)
        {
          case 3: 查找列表.Remove(listViewItem); break;
          case 8: 外部工具.Remove(listViewItem); break;
          case 4: 快键命令.Remove(listViewItem); break;
          case 5: 快键.Remove(listViewItem); break;
          case 6: 自启.Remove(listViewItem); break;
          case 7: 自动关机.Remove(listViewItem); break;
        }
    }

    // listView 添加
    private void Add_button_Click(object sender, RoutedEventArgs e)
    {
      var btn = sender as Button;
      // 在线查找 列表
      if (btn == add_button3)
      {
        //ScrollViewerOffset("在线查找", 2);
        var item = new 列表项()
        {
          Enable = false,
          Name = "",
          Value = "",
          CMD = ""
        };
        查找列表.Insert(0, item);
        listView3.Focus();
        listView3.SelectedIndex = 0;
      }

      if (btn == add_button8)
      {
        //ScrollViewerOffset("外部工具", 2);
        var item = new 列表项()
        {
          Enable = false,
          Name = "",
          Value = "",
          CMD = ""
        };
        外部工具.Insert(0, item);
        listView8.Focus();
        listView8.SelectedIndex = 0;
      }

      // 快捷命令 列表
      if (btn == add_button4)
      {
        //ScrollViewerOffset("快捷命令", 2);
        var item = new 列表项()
        {
          Enable = false,
          Name = "运行命令行快键",
          Value = "",
          CMD = ""
        };
        快键命令.Insert(0, item);
        listView4.Focus();
        listView4.SelectedIndex = 0;
      }

      // 快捷键 列表
      if (btn == add_button5)
      {
        //ScrollViewerOffset("快捷键", 2);
        var item = new 列表项()
        {
          Enable = false,
          Name = (comboBox4.SelectedItem as ComboBoxItem)?.Content?.ToString(),
          Value = "",
          CMD = ""
        };
        快键.Insert(0, item);
        listView5.Focus();
        listView5.SelectedIndex = 0;
      }

      // 自启应用 列表
      if (btn == add_button6)
      {
        //ScrollViewerOffset("自启动应用", 2);
        var item = new 列表项()
        {
          Enable = false,
          Name = "自启",
          Value = "",
          CMD = ""
        };
        自启.Insert(0, item);
        listView6.Focus();
        listView6.SelectedIndex = 0;
      }

      // 定时关机 列表
      if (btn == add_button7)
      {
        //ScrollViewerOffset("定时关机", 2);
        var item = new 列表项()
        {
          Enable = false,
          Name = "自动关机",
          Value = $"22:30",
          CMD = ""
        };
        自动关机.Insert(0, item);
        listView7.Focus();
        listView7.SelectedIndex = 0;
      }
    }


    private void CheckBox3_Copy1_CheckedChanged(object sender, RoutedEventArgs e)
    {
      zh_en = checkBox3_Copy1.IsChecked == true ? "中c：" : "";
      toolTipTextBlock.Text = $"{zh_en}方案名称";
    }

    private void CheckBox3_Copy_CheckedChanged(object sender, RoutedEventArgs e)
    {
      toolTipTextBlock.Visibility = checkBox3_Copy.IsChecked == true ? Visibility.Hidden : Visibility.Visible;
    }

    private void LoadKegSkinImages()
    {
      // 获取应用的相对路径并转换为绝对路径
      string directoryPath = kegPath + "skin\\";

      // 设置皮肤图片路径
      string skin = $"{directoryPath}Keg.png";
      string skinBackup = $"{directoryPath}默认.png";
      //Console.WriteLine(skin);
      // 将皮肤图片设置为图像源
      try { image.Source = new BitmapImage(new Uri(Base.GetValue("skin", "path"))); }
      catch { image.Source = new BitmapImage(new Uri(skin)); }

      // 如果备份皮肤文件不存在，则复制当前皮肤文件作为备份
      if (!File.Exists(skinBackup))
      {
        File.Copy(skin, skinBackup);
      }

      // 定义支持的图片扩展名数组
      var imageExtensions = new[] { ".png", ".gif" };

      // 获取目录下所有非“Keg”且具有指定扩展名的图片文件
      var allFiles = Directory.GetFiles(directoryPath)
                           .Where(f => imageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant())
                                       && Path.GetFileNameWithoutExtension(f) != "Keg");

      // 查找默认皮肤文件，如果存在则将其从原始文件列表中移除
      var defaultFile = allFiles.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == "默认");

      List<string> files;
      if (defaultFile != null)
      {
        // 移除默认皮肤文件后，将其添加到新列表的首位
        files = allFiles.Except(new[] { defaultFile }).ToList();
        files.Insert(0, defaultFile);
      }
      else
      {
        // 若默认皮肤文件不存在，则直接使用所有符合条件的文件列表
        files = allFiles.ToList();
      }

      // 遍历处理后的图片文件集合
      foreach (var file in files)
      {
        // 从文件路径中提取图片名称
        var imageName = Path.GetFileNameWithoutExtension(file);

        // 将图片名称添加到皮肤列表框中
        skinListBox.Items.Add(imageName);
      }
    }

    private void SkinListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      string selectedItem = (string)skinListBox.SelectedItem;
      string skin = kegPath + "skin\\" + selectedItem + ".png";
      image.Source = new BitmapImage(new Uri(skin));
    }

    private void SaveButton1_Click(object sender, RoutedEventArgs e)
    {
      if (skinListBox.SelectedIndex > 0)
      {
        string selectedItem = (string)skinListBox.SelectedItem;
        string selectedSkin = kegPath + "skin\\" + selectedItem + ".png";

        try
        {

          Clipboard.SetText(selectedSkin);
          //Thread.Sleep(200);
          // KWM_UPPFSET 更新皮肤文件路径
          IntPtr hWnd = FindWindow("CKegServer_0", null); //窗口句柄
          SendMessageTimeout(hWnd, KWM_UPPFSET, IntPtr.Zero, IntPtr.Zero, flags, timeout, out IntPtr pdwResult);

          Base.SetValue("skin", "path", selectedSkin);
        }
        catch (Exception ex)
        {
          MessageBox.Show($"操作失败，请重试！");
          Console.WriteLine(ex.Message);
        }
      }
    }

    // 提示文本位置
    private void RadioButton_Click(object sender, RoutedEventArgs e)
    {
      RadioButton radioButton = (RadioButton)sender;

      if ((bool)radioButton.IsChecked)
      {
        switch (radioButton.Content.ToString())
        {
          case "顶部":
            toolTipTextBlock.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            break;
          case "中间":
            toolTipTextBlock.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            break;
          case "底部":
            toolTipTextBlock.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
            break;
        }
      }
    }

    #endregion





  }
}
