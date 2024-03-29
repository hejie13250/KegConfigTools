﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;
using Color = System.Windows.Media.Color;
using Label = System.Windows.Controls.Label;
using ListBox = System.Windows.Controls.ListBox;
using ComboBox = System.Windows.Controls.ComboBox;
using ColorConverter = System.Windows.Media.ColorConverter;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using MessageBox = System.Windows.MessageBox;
using MenuItem = System.Windows.Forms.MenuItem;
using System.Data.SQLite;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;
using Brush = System.Drawing.Brush;
using Button = System.Windows.Controls.Button;
using Clipboard = System.Windows.Clipboard;
using static 小科狗配置.MainWindow;
using System.Threading;
using System.Windows.Shapes;
using Path = System.IO.Path;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;
using System.Diagnostics;
using System.Text;
using System.Linq;
using System.Xml.Linq;

namespace 小科狗配置
{
  /// <summary>
  /// MainWindow.xaml 的交互逻辑
  /// </summary>
  /// 
  public partial class MainWindow : Window
  {
    #region 定义全局变量和初始化
    public WriteableBitmap Bitmap { get; set; }
    public class ColorScheme
    {
      public string 名称 { get; set; }
      public bool 显示背景图 { get; set; }
      public bool 显示候选窗圆角 { get; set; }
      public bool 显示选中项背景圆角 { get; set; }
      public int 候选窗圆角 { get; set; }
      public int 选中项圆角 { get; set; }
      public int 边框线宽 { get; set; }
      public string 下划线色 { get; set; }
      public string 光标色 { get; set; }
      public string 分隔线色 { get; set; }
      public string 窗口边框色 { get; set; }
      public string 窗背景底色 { get; set; }
      public string 选中背景色 { get; set; }
      public string 选中字体色 { get; set; }
      public string 编码字体色 { get; set; }
      public string 候选字色 { get; set; }
    }
    public class ColorSchemesCollection
    {
      public List<ColorScheme> 配色方案 { get; set; }
    }
    List<ColorScheme> 配色方案 = new();
    ColorScheme colorScheme = new()
    {
      名称 = "默认",
      显示背景图 = false,
      显示候选窗圆角 = true,
      显示选中项背景圆角 = true,
      候选窗圆角 = 15,
      选中项圆角 = 10,
      边框线宽 = 1,
      下划线色 = "#FF0000",
      光标色 = "#004CFF",
      分隔线色 = "#000000",
      窗口边框色 = "#000000",
      窗背景底色 = "#FFFFFF",
      选中背景色 = "#000000",
      选中字体色 = "#333333",
      编码字体色 = "#000000",
      候选字色 = "#000000"
    };
    readonly String filePath = "配色方案.json";
    int select_color_label = 0;

    NotifyIcon notifyIcon;

    readonly string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    readonly string dbPath;

    readonly List<string> configs = new();
    string bgString;
    string currentConfig, modifiedConfig;

    SolidColorBrush bkColor = new ((Color) ColorConverter.ConvertFromString("#00000000"));
    readonly string settingConfigPath = "setting.ini";

    public MainWindow()
    {
      InitializeComponent();
      this.Title += " V" + GetAssemblyVersion();
      this.Height = 425;
#if DEBUG
      dbPath = "Keg.db";
#else
      string relativeFilePath = Path.Combine(appPath, @"..\..\Keg.db");
      dbPath = Path.GetFullPath(relativeFilePath);
#endif
      Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
      Bitmap = new WriteableBitmap(255, 255, 255, 255, PixelFormats.Bgra32, null);
      DataContext = this;
      UpdateBitmap();       // 生成 取色图
      LoadJson();           // 读取 配色方案.json
      LoadHxFile();         // 读取 候选序号.txt
      LoadSettingConfig();  // 读取 setting.ini
      InitIcon();           // 载入托盘图标
      LoadTableNames();     // 载入码表方案名称
  }

    // 读取 setting.ini
    private void LoadSettingConfig(){
      if (!File.Exists(settingConfigPath))
        File.WriteAllText(settingConfigPath, "[window]\nclosed=0");
      var close = GetValue("window", "closed", settingConfigPath);
      checkBox2.IsChecked = close == "1";


    }

    // 读取候选序号
    private void LoadHxFile()
    {
      string file = "候选序号.txt"; string numStr =
@"<1=🥑¹sp><2=🍑²sp><3=🍋³sp><4=🍍⁴sp><5=🍈⁵sp><6=🍐⁶sp><7=🍊⁷sp ><8=⁸sp🍑 ><9=⁹sp🍉><10=¹⁰sp🍊>
<1=¹sp><2=²sp><3=³sp><4=⁴sp><5=⁵sp><6=⁶sp><7=⁷sp ><8=⁸sp ><9=⁹sp><10=¹⁰sp>
<1=①sp><2=②sp><3=③sp><4=④sp><5=⑤sp><6=⑥sp><7=⑦sp><8=⑧sp><9=⑨sp><10=⑩sp>
<1=❶sp><2=❷sp><3=❸sp><4=❹sp><5=❺sp><6=❻sp><7=❼sp><8=❽sp><9=❾sp><10=❿sp>
<1=⓵sp><2=⓶sp><3=⓷sp><4=⓸sp><5=⓹sp><6=⓺sp><7=⓻sp><8=⓼sp><9=⓽sp><10=⓾sp>
<1=㊀sp><2=㊁sp><3=㊂sp><4=㊃sp><5=㊄sp><6=㊅sp><7=㊆sp><8=㊇sp><9=㊈sp><10=㊉sp>
<1=㈠sp><2=㈡sp><3=㈢sp><4=㈣sp><5=㈤sp><6=㈥sp><7=㈦sp><8=㈧sp><9=㈨sp><10=㈩sp>
<1=🀇sp><2=🀈sp><3=🀉sp><4=🀊sp><5=🀋sp><6=🀌sp><7=🀍sp><8=🀎sp><9=🀏sp><10=🀄sp>
<1=Ⅰsp><2=Ⅱsp><3=Ⅲsp><4=Ⅳsp><5=Ⅴsp><6=Ⅵsp><7=Ⅶsp><8=Ⅷsp><9=Ⅸsp><10=Ⅹsp>
<1=Ⓐsp><2=Ⓑsp><3=Ⓒsp><4=Ⓓsp><5=Ⓔsp><6=Ⓕsp><7=Ⓖsp><8=Ⓗsp><9=Ⓘsp><10=Ⓙsp>
<1=ⓐsp><2=ⓑsp><3=ⓒsp><4=ⓓsp><5=ⓔsp><6=ⓕsp><7=ⓖsp><8=ⓗsp><9=ⓘsp><10=ⓙsp>";
      if (!File.Exists(file))
        File.WriteAllText(file, numStr);
      using StreamReader sr = new(file);
      string line;
      while ((line = sr.ReadLine()) != null)
      {
        ComboBoxItem item = new() { Content = line };
        comboBox3.Items.Add(item);
      }
    }

    // 获取版本号
    public string GetAssemblyVersion()
    {
      Assembly assembly = Assembly.GetExecutingAssembly();
      Version version = assembly.GetName().Version;
      return version.ToString().Substring(0,3);
    }
    #endregion

    #region 定义消息接口
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    const int WM_USER = 0x0400; // 根据Windows API定义
    //const uint KWM_RESETPIPLE = (uint)WM_USER + 200;
    const uint KWM_RESET = (uint)WM_USER + 201;
    //const uint KWM_SET0 = (uint)WM_USER + 202;
    const uint KWM_GETSET = (uint)WM_USER + 203;
    //const uint KWM_INSERT = (uint)WM_USER + 204;
    const uint KWM_UPBASE = (uint)WM_USER + 205;
    const uint KWM_SAVEBASE = (uint)WM_USER + 206;
    //const uint KWM_GETDATAPATH = (uint)WM_USER + 207;
    const uint KWM_GETDEF = (uint)WM_USER + 208;
    const uint KWM_SET2ALL = (uint)WM_USER + 209;
    //const uint KWM_GETWRITEPATH = (uint)WM_USER + 210;
    //const uint KWM_UPQJSET = (uint)WM_USER + 211;
    #endregion

    #region 读写配置文件项
    // 读写配置项 API
    [DllImport("kernel32")]// 读配置文件方法的6个参数：所在的分区、   键值、      初始缺省值、         StringBuilder、      参数长度上限 、配置文件路径
    public static extern long GetPrivateProfileString(string section, string key, string defaultValue, StringBuilder retVal, int size, string filePath);
    [DllImport("kernel32")]// 写入配置文件方法的4个参数：所在的分区、    键值、      参数值、      配置文件路径
    private static extern long WritePrivateProfileString(string section, string key, string value, string filePath);

    /// <summary>
    /// 写配置文件
    /// </summary>
    /// <param name="section">配置项</param>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    /// <param name="filePath">路径</param>
    public static void SetValue(string section, string key, string value, string filePath)
    {
      WritePrivateProfileString(section, key, value, filePath);
    }

    /// <summary>
    /// 读配置文件
    /// </summary>
    /// <param name="section">配置项</param>
    /// <param name="key">键</param>
    /// <param name="filePath">路径</param>
    /// <returns>值</returns>
    public static string GetValue(string section, string key, string filePath)
    {
      if (File.Exists(filePath))
      {
        StringBuilder sb = new(255);
        GetPrivateProfileString(section, key, "", sb, 255, filePath);
        return sb.ToString();
      }
      else return string.Empty;
    }
    #endregion

    #region 读写db
    // 从 db 读取表名到 ComboBox
    private void LoadTableNames()
    {
      SQLiteConnection connection = new($"Data Source={dbPath};Version=3;");
      connection.Open();
      try
      {
        comboBox.Items.Clear();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
        var reader = command.ExecuteReader();
        while (reader.Read())
        {
          var labelName = reader.GetString(0);
          comboBox.Items.Add(labelName);
          configs.Add(labelName);
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Error loading table names: {ex.Message}");
      }
      connection.Close();
    }

    // 从指定表 labelName 内读取 key 列为"配置"时 value 的值
    private string GetConfig(string labelName)
    {
      using SQLiteConnection connection = new($"Data Source={dbPath};Version=3;");
      string query = $"SELECT value FROM '{labelName}' WHERE key = '配置'";
      connection.Open();
      using SQLiteCommand command = new(query, connection);

      string result;
      using (SQLiteDataReader reader = command.ExecuteReader())
      {
        if (reader.Read()) result = reader["value"].ToString();
        else result = null;
      }
      connection.Close();
      return result;
    }

    // 保存配置到数据库
    // 更新指定表 labelName 内 key 列为 "配置" 时 value 列的值为 value
    private void SaveConfig(string labelName, String value)
    {
      string connectionString = $"Data Source={dbPath};Version=3;";
      using SQLiteConnection connection = new (connectionString);
      string updateQuery = $"UPDATE '{labelName}' SET value = @Value WHERE key = '配置'";
      connection.Open();
      using SQLiteCommand command = new (updateQuery, connection);
      command.Parameters.AddWithValue("@Value", value);
      int rowsAffected = command.ExecuteNonQuery();
      connection.Close();
    }

    #endregion

    #region 顶部控件事件
    private void ComboBox_MouseEnter(object sender, MouseEventArgs e)
    {
      comboBox.Focus();
    }

    // 切换方案
    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      var labelName = comboBox.SelectedValue as string;
      currentConfig = GetConfig(labelName);
      if(!currentConfig.Contains("\n"))
          currentConfig = Regex.Replace(currentConfig, $"《", $"\n《");
      SetControlsValue();

      restor_default_button.IsEnabled = true;
      loading_templates_button.IsEnabled = true;
      set_as_default_button.IsEnabled = true;
      apply_button.IsEnabled = true;
      apply_all_button.IsEnabled = true;

    }

    // 更新配置的值到 UI
    //private void GetSet()
    //{
    //  try{
    //    var labelName = comboBox.SelectedValue as string;
    //    IntPtr hWnd = FindWindow("CKegServer_0", null);
    //    // 将 码表名称 写入剪切板
    //    Clipboard.SetText(labelName);
    //    Thread.Sleep(200);
    //    // 调用 KWM_GETSET 消息接口 -> 读剪切板 码表名称 然后获取配置并写入剪切板
    //    SendMessage(hWnd, KWM_GETSET, IntPtr.Zero, IntPtr.Zero);
    //    Thread.Sleep(200);
    //    // 从剪切板读取配置进行修改
    //    currentConfig = Clipboard.GetText();
    //     Clipboard.Clear();
    //    SetControlsValue();
    //    //GetControlsValue();
    //  }
    //  catch (Exception ex) { MessageBox.Show($"错误信息：{ex.Message}"); }
    //}

    // 重新载入


    private void Restor_default_button_Click(object sender, RoutedEventArgs e)
    {
      if (comboBox.SelectedIndex < 0)
      {
        MessageBox.Show("您没有选择任何方案！");
        return;
      }
      var labelName = comboBox.SelectedValue as string;
      foreach (var config in configs)
      {
        if (config == labelName)
        {
          currentConfig = config;
          break;
        }
      }
      SetControlsValue();
    }

    // 加载默认模板
    private void Loading_templates_button_Click(object sender, RoutedEventArgs e)
    {
      var result = MessageBox.Show(
      $"您确定要从服务端加载默认模板吗？",
      "加载默认模板",
      MessageBoxButton.OKCancel,
      MessageBoxImage.Question);

      if (result == MessageBoxResult.OK)
      {
        var labelName = comboBox.SelectedValue as string;
        IntPtr hWnd = FindWindow("CKegServer_0", null);
        SendMessage(hWnd, KWM_GETDEF, IntPtr.Zero, IntPtr.Zero);
        var str = Clipboard.GetText();
        Clipboard.Clear();
        currentConfig = Regex.Replace(str, "方案：<>配置", $"方案：<{labelName}>配置");
        SetControlsValue();
      }
    }

    // 设置默认方案
    private void Set_as_default_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        var labelName = comboBox.SelectedValue as string;
        Clipboard.SetText($"《所有进程默认初始方案={labelName}》");
        Thread.Sleep(200);
        IntPtr hWnd = FindWindow("CKegServer_0", null);
        SendMessage(hWnd, KWM_SET2ALL, IntPtr.Zero, IntPtr.Zero);
      }
      catch (Exception ex) { MessageBox.Show($"错误信息：{ex.Message}"); }
    }

    // 应用修改
    private void Apply_button_Click(object sender, RoutedEventArgs e)
    {
      var labelName = comboBox.SelectedValue as string;
      modifiedConfig = currentConfig;
      GetControlsValue(); // 读取所有控件值替换到 modifiedConfig
      // 获取已修改项
      string updataStr = $"方案：<{labelName}> 配置 \n" + GetDifferences(modifiedConfig, currentConfig);
      if (updataStr == $"方案：<{labelName}> 配置 \n") {
        MessageBox.Show("没修改任何项");
        return;
      }
      SaveConfig(labelName, modifiedConfig);
      try
      {
        IntPtr hWnd = FindWindow("CKegServer_0", null);
        Clipboard.SetText(updataStr);
        Thread.Sleep(200);
        SendMessage(hWnd, KWM_RESET, IntPtr.Zero, IntPtr.Zero);
        currentConfig = modifiedConfig;
      }
      catch (Exception ex)
      {
        MessageBox.Show($"错误信息：{ex.Message}");
      }
    }

    // 应用所有
    private void Apply_All_button_Click(object sender, RoutedEventArgs e)
    {
      var labelName = comboBox.SelectedValue as string;
      modifiedConfig = currentConfig;
      GetControlsValue(); 
      if (modifiedConfig == currentConfig)
      {
        MessageBox.Show("没修改任何项");
        return;
      }
      SaveConfig(labelName, modifiedConfig);

      try
      {
        IntPtr hWnd = FindWindow("CKegServer_0", null);
        SendMessage(hWnd, KWM_UPBASE, IntPtr.Zero, IntPtr.Zero);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"错误信息：{ex.Message}");
      }
    }

    // 关闭窗口后直接退出
    private void CheckBox2_Click(object sender, RoutedEventArgs e)
    {
      if (checkBox2.IsChecked == true)
        SetValue("window", "closed", "1", settingConfigPath);
      else
        SetValue("window", "closed", "0", settingConfigPath);
    }

    // 获取已修改项
    public static string GetDifferences(string modifiedConfig, string currentConfig)
    {
      // 将字符串按行分割成数组
      var modifiedLines = modifiedConfig.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
      var currentLines = currentConfig.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

      // 找出不同的行
      var differentLines = modifiedLines.Except(currentLines);

      // 将不同的行追加到新的字符串中
      string newConfig = string.Join(Environment.NewLine, differentLines);

      return newConfig;
    }

    #endregion

    #region 读取配置各项值到控件
    // 读取配置值到控件
    private void SetControlsValue()
    {
      string pattern = "《(.*=?.*)=(.*)》";
      MatchCollection matches = Regex.Matches(currentConfig, pattern);
      foreach (Match match in matches)
      {
        var value = match.Groups[2].Value;
        switch (match.Groups[1].Value)
        {
          case "上屏词条精准匹配key=1*的值进行词语联想吗？": checkBox_Copy8.IsChecked = IsTrueOrFalse(value); break;
          case "精准匹配key=1*的值时要词语模糊联想吗？": checkBox_Copy9.IsChecked = IsTrueOrFalse(value); break;
        }
      }

      pattern = "《(.*?)=(.*)》";
      matches = Regex.Matches(currentConfig, pattern);
      foreach (Match match in matches)
      {
        var value = match.Groups[2].Value;
        switch (match.Groups[1].Value)
        {
          case "大键盘码元": textBox_Copy677.Text = value; break;
          case "小键盘码元": textBox_Copy5.Text = value; break;
          case "大小键盘万能码元": textBox_Copy6.Text = value; break;
          case "非编码串首位的大键盘码元": textBox_Copy7.Text = value; break;
          case "非编码串首位的小键盘码元": textBox_Copy8.Text = value; break;
          case "最大码长": nud1.Value = int.Parse(value); break;
          case "是键首字根码表吗？": checkBox1_Copy55.IsChecked = IsTrueOrFalse(value); break;
          case "键首字根": textBox125.Text = value; break;
          case "顶功小集码元": textBox_Copy675.Text = value; break;
          case "顶功规则": 顶功规则(value); break;
          case "重复上屏码元字符串": textBox_Copy1.Text = value; break;
          case "中英切换要显示提示窗口吗？": checkBox_Copy11.IsChecked = IsTrueOrFalse(value); break;
          case "从中文切换到英文时,要上屏编码串吗？": checkBox_Copy12.IsChecked = IsTrueOrFalse(value); break;
          case "要启用左Shift键吗？": checkBox_Copy13.IsChecked = IsTrueOrFalse(value); break;
          case "要启用右Shift键吗？": checkBox_Copy14.IsChecked = IsTrueOrFalse(value); break;
          case "要启用左Ctrl键吗？": checkBox_Copy15.IsChecked = IsTrueOrFalse(value); break;
          case "要启用右Ctrl键吗？": checkBox_Copy16.IsChecked = IsTrueOrFalse(value); break;
          case "要启用Ctrl+Space键吗？": checkBox_Copy17.IsChecked = IsTrueOrFalse(value); break;
          case "字体名称": textBox_Copy145.Text = value; break;
          case "D2D回退字体集": textBox_Copy10.Text = value; break;
          case "候选窗口绘制模式": 候选窗口绘制模式(value); break;
          case "GDI字体加粗权值": nud14_Copy.Value = int.Parse(value); break;
          case "GDI字体要倾斜吗？": checkBox_Copy314.IsChecked = IsTrueOrFalse(value); break;
          case "GDI+字体样式": GDIp字体样式(value); break;
          case "GDI+字体要下划线吗？": checkBox19.IsChecked = IsTrueOrFalse(value); break;
          case "GDI+字体要删除线吗？": checkBox20.IsChecked = IsTrueOrFalse(value); break;
          case "D2D字体加粗权值": nud14.Value = int.Parse(value); break;
          case "D2D字体样式": D2D字体样式(value); break;
          case "候选个数": nud15.Value = int.Parse(value); break;
          case "1-26候选的横向偏离": nud16.Value = int.Parse(value); break;
          case "候选的高度间距": nud17.Value = int.Parse(value); break;
          case "候选的宽度间距": nud18.Value = int.Parse(value); break;
          case "候选窗口候选排列方向模式": 候选窗口候选排列方向模式(value); break;
          case "候选窗口候选从上到下排列要锁定吗？": checkBox_Copy45.IsChecked = IsTrueOrFalse(value); break;
          case "候选窗口候选从上到下排列锁定的情况下要使编码区离光标最近吗？": checkBox_Copy46.IsChecked = IsTrueOrFalse(value); break;
          case "候选窗口候选排列方向模式>1时要隐藏编码串行吗？": checkBox_Copy38.IsChecked = IsTrueOrFalse(value); break;
          case "要使用嵌入模式吗？": checkBox_Copy44.IsChecked = IsTrueOrFalse(value); break;
          case "编码或候选嵌入模式": 编码或候选嵌入模式(value); break;
          case "码表标签": textBox_Copy15.Text = value; break;
          case "码表标签显示模式": comboBox1_Copy.SelectedIndex = int.Parse(value); break;
          case "码表标签要左对齐吗？": checkBox_Copy39.IsChecked = IsTrueOrFalse(value); break;
          case "竖向候选窗口选中背景色要等宽吗？": checkBox_Copy41.IsChecked = IsTrueOrFalse(value); break;
          case "要显示背景图吗？": checkBox_Copy42.IsChecked = IsTrueOrFalse(value); break;
          case "高度宽度要完全自动调整吗？": checkBox_Copy40.IsChecked = IsTrueOrFalse(value); break;
          case "窗口四个角要圆角吗？": hxc_checkBox.IsChecked = IsTrueOrFalse(value); break;
          case "选中项四个角要圆角吗？": hxcbj_checkBox.IsChecked = IsTrueOrFalse(value); break;
          case "窗口四个角的圆角半径": nud11.Value = int.Parse(value); break;
          case "选中项四个角的圆角半径": nud12.Value = int.Parse(value); break;
          case "候选窗口边框线宽度": nud13.Value = int.Parse(value); break;
          case "嵌入下划线色": color_label_1.Background = RGBStringToColor(value); break;
          case "光标色": color_label_2.Background = RGBStringToColor(value); break;
          case "分隔线色": color_label_3.Background = RGBStringToColor(value); break;
          case "候选窗口边框色": color_label_4.Background = RGBStringToColor(value); break;
          case "候选选中色": color_label_6.Background = RGBStringToColor(value); break;
          case "候选选中字体色": color_label_7.Background = RGBStringToColor(value); break;
          case "背景底色": 背景底色(value); break;
          case "候选字体色串": SetLabelColor(value); break;
          case "关联中文标点吗？": checkBox_Copy31.IsChecked = IsTrueOrFalse(value); break;
          case "大键盘中文标点串": textBox_Copy68.Text = value; break;
          case "大键盘按下Shift的中文标点串": textBox_Copy69.Text = value; break;
          case "要开启词语联想吗？": checkBox_Copy4.IsChecked = IsTrueOrFalse(value); break;
          case "词语联想上屏字符串长度": 词语联想上屏字符串长度(value); break;
          case "词语联想检索范围": 词语联想检索范围(value); break;
          case "词语联想要显示词语全部吗？": checkBox_Copy5.IsChecked = IsTrueOrFalse(value); break;
          case "词语联想只是匹配首位吗？": checkBox_Copy6.IsChecked = IsTrueOrFalse(value); break;
          case "词语联想时标点顶屏要起作用吗？": checkBox_Copy7.IsChecked = IsTrueOrFalse(value); break;
          case "要开启Ctrl键清联想吗？": checkBox_Copy10.IsChecked = IsTrueOrFalse(value); break;
          case "要显示键首字根吗？": checkBox_Copy34.IsChecked = IsTrueOrFalse(value); break;
          case "上屏后候选窗口要立即消失吗？": checkBox_Copy18.IsChecked = IsTrueOrFalse(value); break;
          case "超过码长要清屏吗？": checkBox_Copy19.IsChecked = IsTrueOrFalse(value); break;
          case "无候选要清屏吗？": checkBox_Copy20.IsChecked = IsTrueOrFalse(value); break;
          case "要启用最大码长无候选清屏吗？": checkBox_Copy21.IsChecked = IsTrueOrFalse(value); break;
          case "无候选敲空格要上屏编码串吗？": checkBox_Copy22.IsChecked = IsTrueOrFalse(value); break;
          case "Shift键上屏编码串吗？": checkBox_Copy23.IsChecked = IsTrueOrFalse(value); break;
          case "Shift键+字母键要进入临时英文长句态吗？": checkBox_Copy24.IsChecked = IsTrueOrFalse(value); break;
          case "Space键要上屏临时英文编码串吗？": checkBox_Copy25.IsChecked = IsTrueOrFalse(value); break;
          case "Enter键上屏编码串吗？": checkBox_Copy26.IsChecked = IsTrueOrFalse(value); break;
          case "Enter键上屏并使首个字母大写吗？": checkBox_Copy27.IsChecked = IsTrueOrFalse(value); break;
          case "Backspace键一次性删除前次上屏的内容吗？": checkBox_Copy28.IsChecked = IsTrueOrFalse(value); break;
          case "前次上屏的是数字再上屏句号*要转成点号*吗？": checkBox_Copy29.IsChecked = IsTrueOrFalse(value); break;
          case "过渡态按1要上屏1吗？": checkBox_Copy30.IsChecked = IsTrueOrFalse(value); break;
          case "要启用ESC键自动造词吗？": checkBox_Copy3.IsChecked = IsTrueOrFalse(value); break;
          case "要逐码提示检索吗？": checkBox_Copy.IsChecked = IsTrueOrFalse(value); break;
          case "要显示逐码提示吗？": checkBox.IsChecked = IsTrueOrFalse(value); break;
          case "要显示反查提示吗？": checkBox1.IsChecked = IsTrueOrFalse(value); break;
          case "要启用单字模式吗？": checkBox1_Copy.IsChecked = IsTrueOrFalse(value); break;
          case "要启用上屏自动增加调频权重吗？": checkBox1_Copy1.IsChecked = IsTrueOrFalse(value); break;
          case "调频权重最小码长": nud2.Value = int.Parse(value); break;
          case "要启用上屏自动增加调频权重直接到顶吗？": checkBox_Copy1.IsChecked = IsTrueOrFalse(value); break;
          case "候选词条要按码长短优先排序吗？": checkBox_Copy2.IsChecked = IsTrueOrFalse(value); break;
          case "候选词条要按调频权重检索排序吗？": checkBox1_Copy2.IsChecked = IsTrueOrFalse(value); break;
          case "双检索历史重数": nud3.Value = int.Parse(value); break;
          case "要启用双检索吗？": checkBox1_Copy3.IsChecked = IsTrueOrFalse(value); break;
          case "双检索时编码要完全匹配吗？": checkBox1_Copy4.IsChecked = IsTrueOrFalse(value); break;
          case "候选快键字符串": textBox_Copy66.Text = value; break;
          case "候选序号": textBox_Copy67.Text = value; break;
          case "要码长顶屏吗？": checkBox1_Copy111.IsChecked = IsTrueOrFalse(value); break;
          case "要数字顶屏吗？": checkBox1_Copy7.IsChecked = IsTrueOrFalse(value); break;
          case "要标点顶屏吗？": checkBox1_Copy6.IsChecked = IsTrueOrFalse(value); break;
          case "标点或数字顶屏时,若是引导键,要继续引导吗？": checkBox1_Copy8.IsChecked = IsTrueOrFalse(value); break;
          case "要唯一上屏吗？": checkBox1_Copy5.IsChecked = IsTrueOrFalse(value); break;
          case "唯一上屏最小码长": nud4.Value = int.Parse(value); break;
          case "往上翻页大键盘英文符号编码串": textBox_Copy21.Text = value; break;
          case "往下翻页大键盘英文符号编码串": textBox_Copy2.Text = value; break;
          case "往上翻页小键盘英文符号编码串": textBox_Copy3.Text = value; break;
          case "往下翻页小键盘英文符号编码串": textBox_Copy4.Text = value; break;
          case "码表临时快键": textBox_Copy19.Text = value; break;
          case "码表临时快键编码名": textBox_Copy20.Text = value; break;
          case "无临时快键时,也要显示主码表标识吗？": checkBox_Copy32.IsChecked = IsTrueOrFalse(value); break;
          case "主码表标识": textBox_Copy22.Text = value; break;
          case "副码表标识": textBox_Copy23.Text = value; break;
          case "码表引导快键0": textBox_Copy.Text = value; break;
          case "码表引导快键0编码名0": textBox_Copy9.Text = value; break;
          case "码表引导快键0编码名1": textBox_Copy11.Text = value; break;
          case "码表引导快键1": textBox_Copy12.Text = value; break;
          case "码表引导快键1编码名0": textBox_Copy13.Text = value; break;
          case "码表引导快键1编码名1": textBox_Copy14.Text = value; break;
          case "码表引导快键2": textBox_Copy16.Text = value; break;
          case "码表引导快键2编码名0": textBox_Copy17.Text = value; break;
          case "码表引导快键2编码名1": textBox_Copy18.Text = value; break;
        }
      }
    }

    private void 背景底色(string value)
    {
      if (value == "")
      {
        hxcds_checkBox.IsChecked = true;
        color_label_5.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        bkColor = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        //hxk_border.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
      }
      else
      {
        hxcds_checkBox.IsChecked = false;
        color_label_5.Background = RGBStringToColor(value);
        bkColor = RGBStringToColor(value);
        //hxk_border.Background = RGBStringToColor(value);
      }
    }

    private bool IsTrueOrFalse(string value)
    {
      if (value == "不要" || value == "不是") return false;
      else return true;
    }

    private void 顶功规则(string value)
    {
      switch (value) {
        case "1":
          radioButton454.IsChecked = true; break;
        case "2":
          radioButton455.IsChecked = true; break;
        case "3":
          radioButton456.IsChecked = true; break;
      }
    }

    private void 候选窗口绘制模式(string value)
    {
      switch (value)
      {
        case "2":
          radioButton9.IsChecked = true; break;
        case "0":
          radioButton10.IsChecked = true; break;
        case "1":
          radioButton11.IsChecked = true; break;
      }
    }

    private void D2D字体样式(string value)
    {
      switch (value)
      {
        case "0":
          radioButton6.IsChecked = true; break;
        case "1":
          radioButton7.IsChecked = true; break;
      }
    }
    private void GDIp字体样式(string value)
    {
      switch (value)
      {
        case "0":
          radioButton14.IsChecked = true; break;
        case "1":
          radioButton15.IsChecked = true; break;
        case "2":
          radioButton16.IsChecked = true; break;
        case "3":
          radioButton17.IsChecked = true; break;
      }
    }
    private void 候选窗口候选排列方向模式(string value)
    {
      switch (value)
      {
        case "1":
          radioButton8.IsChecked = true; break;
        case "2":
          radioButton12.IsChecked = true; break;
        case "3":
          radioButton13.IsChecked = true; break;
      }
    }
    private void 编码或候选嵌入模式(string value)
    {
      if (value.Length > 1)
        checkBox_Copy33.IsChecked = true;
      comboBox1.SelectedIndex = value switch
      {
        "0" or "10" => 0,
        "1" or "11" => 1,
        "2" or "12" => 2,
        "3" or "13" => 3,
        _ => 4,
      };
    }

    // RGB字符串转换成Color
    private SolidColorBrush RGBStringToColor(string rgbString)
    {
      //候选窗背景色为空时设为对话框背景色
      if (rgbString == ""){
        hxcds_checkBox.IsChecked= true;
        return new SolidColorBrush(Color.FromArgb(0,0, 0, 0));
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

    private void SetLabelColor(string str)
    {
      string pattern = "<(.*?)=(.*?)>";
      MatchCollection matches2 = Regex.Matches(str, pattern);

      foreach (Match match in matches2)
      {
        var value = match.Groups[2].Value;
        switch (match.Groups[1].Value) {
        case "0": //编码字体色
            color_label_8.Background = RGBStringToColor(value);
            //hxz_label_0.Foreground = RGBStringToColor(value);
            break;
        case "1":
            color_label_9.Background = RGBStringToColor(value);
            //hxz_label_1.Foreground = RGBStringToColor(value);
            //hxz_label_2.Foreground = RGBStringToColor(value);
            //hxz_label_4.Foreground = RGBStringToColor(value);
            //hxz_label_5.Foreground = RGBStringToColor(value);
            //hxz_label_6.Foreground = RGBStringToColor(value);
            break;
        }
      }
      HXZ_TextBoxText();
    }

    private void 词语联想上屏字符串长度(string value)
    {
      switch (value)
      {
        case "1":
          radioButton.IsChecked = true; break;
        case "2":
          radioButton1.IsChecked = true; break;
        case "3":
          radioButton2.IsChecked = true; break;
      }
    }
    private void 词语联想检索范围(string value)
    {
      switch (value)
      {
        case "1":
          radioButton3.IsChecked = true; break;
        case "2":
          radioButton4.IsChecked = true; break;
        case "3":
          radioButton5.IsChecked = true; break;
      }
    }

    #endregion

    #region 读取控件属性值
    // 正则替换 modifiedConfig
    private void ReplaceConfig(string key, string value)
    {
      modifiedConfig = Regex.Replace(modifiedConfig, $"《{key}=.*?》", $"《{key}={value}》");
    }
    // 读取控件属性值
    private void GetControlsValue()
    {
      ReplaceConfig("大键盘码元", textBox_Copy677.Text);
      ReplaceConfig("小键盘码元", textBox_Copy5.Text);
      ReplaceConfig("大小键盘万能码元", textBox_Copy6.Text);
      ReplaceConfig("非编码串首位的大键盘码元", textBox_Copy7.Text);
      ReplaceConfig("非编码串首位的小键盘码元", textBox_Copy8.Text);
      ReplaceConfig("最大码长", nud1.Value.ToString());
      ReplaceConfig("是键首字根码表吗？", 是或不是((bool)checkBox1_Copy55.IsChecked));
      ReplaceConfig("键首字根", textBox125.Text);
      ReplaceConfig("顶功小集码元", textBox_Copy675.Text);
      ReplaceConfig("顶功规则", 取顶功规则());
      ReplaceConfig("重复上屏码元字符串", textBox_Copy1.Text);
      ReplaceConfig("中英切换要显示提示窗口吗？", 要或不要((bool)checkBox_Copy11.IsChecked));
      ReplaceConfig("从中文切换到英文时,要上屏编码串吗？", 要或不要((bool)checkBox_Copy12.IsChecked));
      ReplaceConfig("要启用左Shift键吗？", 要或不要((bool)checkBox_Copy13.IsChecked));
      ReplaceConfig("要启用右Shift键吗？", 要或不要((bool)checkBox_Copy14.IsChecked));
      ReplaceConfig("要启用左Ctrl键吗？", 要或不要((bool)checkBox_Copy15.IsChecked));
      ReplaceConfig("要启用右Ctrl键吗？", 要或不要((bool)checkBox_Copy16.IsChecked));
      ReplaceConfig("字体名称", textBox_Copy145.Text);
      ReplaceConfig("D2D回退字体集", textBox_Copy10.Text);
      ReplaceConfig("候选窗口绘制模式", 取候选窗口绘制模式());
      ReplaceConfig("GDI字体加粗权值", nud14_Copy.Value.ToString());
      ReplaceConfig("GDI字体要倾斜吗？", 要或不要((bool)checkBox_Copy314.IsChecked));
      ReplaceConfig("D2D字体加粗权值", nud14.Value.ToString());
      ReplaceConfig("D2D字体样式", 取D2D字体样式());
      ReplaceConfig("候选个数", nud15.Value.ToString());
      ReplaceConfig("1-26候选的横向偏离", nud16.Value.ToString());
      ReplaceConfig("候选的高度间距", nud17.Value.ToString());
      ReplaceConfig("候选的宽度间距", nud18.Value.ToString());
      ReplaceConfig("候选窗口候选排列方向模式", 取候选窗口候选排列方向模式());
      ReplaceConfig("候选窗口候选从上到下排列要锁定吗？", 要或不要((bool)checkBox_Copy45.IsChecked));
      ReplaceConfig("候选窗口候选从上到下排列锁定的情况下要使编码区离光标最近吗？", 要或不要((bool)checkBox_Copy46.IsChecked));
      ReplaceConfig("候选窗口候选排列方向模式>1时要隐藏编码串行吗？", 要或不要((bool)checkBox_Copy38.IsChecked));
      ReplaceConfig("要使用嵌入模式吗？", 要或不要((bool)checkBox_Copy44.IsChecked));
      ReplaceConfig("编码或候选嵌入模式", 取编码或候选嵌入模式());
      ReplaceConfig("码表标签", textBox_Copy15.Text);
      ReplaceConfig("码表标签显示模式", comboBox1_Copy.SelectedIndex.ToString());
      ReplaceConfig("码表标签要左对齐吗？", 要或不要((bool)checkBox_Copy39.IsChecked));
      ReplaceConfig("竖向候选窗口选中背景色要等宽吗？", 要或不要((bool)checkBox_Copy41.IsChecked));
      ReplaceConfig("要显示背景图吗？", 要或不要((bool)checkBox_Copy42.IsChecked));
      ReplaceConfig("高度宽度要完全自动调整吗？", 要或不要((bool)checkBox_Copy40.IsChecked));
      ReplaceConfig("窗口四个角要圆角吗？", 要或不要((bool)hxc_checkBox.IsChecked));
      ReplaceConfig("选中项四个角要圆角吗？", 要或不要((bool)hxcbj_checkBox.IsChecked));
      ReplaceConfig("窗口四个角的圆角半径", nud11.Value.ToString());
      ReplaceConfig("选中项四个角的圆角半径", nud12.Value.ToString());
      ReplaceConfig("候选窗口边框线宽度", nud13.Value.ToString());
      ReplaceConfig("嵌入下划线色", HexToRgb(color_label_1.Background.ToString()));
      ReplaceConfig("光标色", HexToRgb(color_label_2.Background.ToString()));
      ReplaceConfig("分隔线色", HexToRgb(color_label_3.Background.ToString()));
      ReplaceConfig("候选窗口边框色", HexToRgb(color_label_4.Background.ToString()));
      ReplaceConfig("候选选中色", HexToRgb(color_label_6.Background.ToString()));
      ReplaceConfig("候选选中字体色", HexToRgb(color_label_7.Background.ToString()));
      ReplaceConfig("背景底色", 取背景底色());
      ReplaceConfig("候选字体色串", bgString);
      ReplaceConfig("关联中文标点吗？", 要或不要((bool)checkBox_Copy31.IsChecked));
      ReplaceConfig("大键盘中文标点串", textBox_Copy68.Text);
      ReplaceConfig("大键盘按下Shift的中文标点串", textBox_Copy69.Text);
      ReplaceConfig("要开启词语联想吗？", 要或不要((bool)checkBox_Copy4.IsChecked));
      ReplaceConfig("词语联想上屏字符串长度", 取词语联想上屏字符串长度());
      ReplaceConfig("词语联想检索范围", 取词语联想检索范围());
      ReplaceConfig("词语联想要显示词语全部吗？", 要或不要((bool)checkBox_Copy5.IsChecked));
      ReplaceConfig("词语联想只是匹配首位吗？", 是或不是((bool)checkBox_Copy6.IsChecked));
      ReplaceConfig("词语联想时标点顶屏要起作用吗？", 要或不要((bool)checkBox_Copy7.IsChecked));
      ReplaceConfig("要开启Ctrl键清联想吗？", 要或不要((bool)checkBox_Copy10.IsChecked));
      ReplaceConfig("要显示键首字根吗？", 要或不要((bool)checkBox_Copy34.IsChecked));
      ReplaceConfig("上屏后候选窗口要立即消失吗？", 要或不要((bool)checkBox_Copy18.IsChecked));
      ReplaceConfig("超过码长要清屏吗？", 要或不要((bool)checkBox_Copy19.IsChecked));
      ReplaceConfig("无候选要清屏吗？", 要或不要((bool)checkBox_Copy20.IsChecked));
      ReplaceConfig("要启用最大码长无候选清屏吗？", 要或不要((bool)checkBox_Copy21.IsChecked));
      ReplaceConfig("无候选敲空格要上屏编码串吗？", 要或不要((bool)checkBox_Copy22.IsChecked));
      ReplaceConfig("Shift键上屏编码串吗？", 要或不要((bool)checkBox_Copy23.IsChecked));
      ReplaceConfig("Space键要上屏临时英文编码串吗？", 要或不要((bool)checkBox_Copy25.IsChecked));
      ReplaceConfig("Enter键上屏编码串吗？", 要或不要((bool)checkBox_Copy26.IsChecked));
      ReplaceConfig("Enter键上屏并使首个字母大写吗？", 要或不要((bool)checkBox_Copy27.IsChecked));
      ReplaceConfig("Backspace键一次性删除前次上屏的内容吗？", 要或不要((bool)checkBox_Copy28.IsChecked));
      ReplaceConfig("过渡态按1要上屏1吗？", 要或不要((bool)checkBox_Copy30.IsChecked));
      ReplaceConfig("要启用ESC键自动造词吗？", 要或不要((bool)checkBox_Copy3.IsChecked));
      ReplaceConfig("要逐码提示检索吗？", 要或不要((bool)checkBox_Copy.IsChecked));
      ReplaceConfig("要显示逐码提示吗？", 要或不要((bool)checkBox.IsChecked));
      ReplaceConfig("要显示反查提示吗？", 要或不要((bool)checkBox1.IsChecked));
      ReplaceConfig("要启用单字模式吗？", 要或不要((bool)checkBox1_Copy.IsChecked));
      ReplaceConfig("要启用上屏自动增加调频权重吗？", 要或不要((bool)checkBox1_Copy1.IsChecked));
      ReplaceConfig("调频权重最小码长", nud2.Value.ToString());
      ReplaceConfig("要启用上屏自动增加调频权重直接到顶吗？", 要或不要((bool)checkBox_Copy1.IsChecked));
      ReplaceConfig("候选词条要按码长短优先排序吗？", 要或不要((bool)checkBox_Copy2.IsChecked));
      ReplaceConfig("候选词条要按调频权重检索排序吗？", 要或不要((bool)checkBox1_Copy2.IsChecked));
      ReplaceConfig("双检索历史重数", nud3.Value.ToString());
      ReplaceConfig("要启用双检索吗？", 要或不要((bool)checkBox1_Copy3.IsChecked));
      ReplaceConfig("双检索时编码要完全匹配吗？", 要或不要((bool)checkBox1_Copy4.IsChecked));
      ReplaceConfig("候选快键字符串", textBox_Copy66.Text);
      ReplaceConfig("候选序号", textBox_Copy67.Text);
      ReplaceConfig("要码长顶屏吗？", 要或不要((bool)checkBox1_Copy111.IsChecked));
      ReplaceConfig("要数字顶屏吗？", 要或不要((bool)checkBox1_Copy7.IsChecked));
      ReplaceConfig("要标点顶屏吗？", 要或不要((bool)checkBox1_Copy6.IsChecked));
      ReplaceConfig("标点或数字顶屏时,若是引导键,要继续引导吗？", 要或不要((bool)checkBox1_Copy8.IsChecked));
      ReplaceConfig("要唯一上屏吗？", 要或不要((bool)checkBox1_Copy5.IsChecked));
      ReplaceConfig("唯一上屏最小码长", nud4.Value.ToString());
      ReplaceConfig("往上翻页大键盘英文符号编码串", textBox_Copy21.Text);
      ReplaceConfig("往下翻页大键盘英文符号编码串", textBox_Copy2.Text);
      ReplaceConfig("往上翻页小键盘英文符号编码串", textBox_Copy3.Text);
      ReplaceConfig("往下翻页小键盘英文符号编码串", textBox_Copy4.Text);
      ReplaceConfig("码表临时快键", textBox_Copy19.Text);
      ReplaceConfig("码表临时快键编码名", textBox_Copy20.Text);
      ReplaceConfig("无临时快键时,也要显示主码表标识吗？", 要或不要((bool)checkBox_Copy32.IsChecked));
      ReplaceConfig("主码表标识", textBox_Copy22.Text);
      ReplaceConfig("副码表标识", textBox_Copy23.Text);
      ReplaceConfig("码表引导快键0", textBox_Copy.Text);
      ReplaceConfig("码表引导快键0编码名0", textBox_Copy9.Text);
      ReplaceConfig("码表引导快键0编码名1", textBox_Copy11.Text);
      ReplaceConfig("码表引导快键1", textBox_Copy12.Text);
      ReplaceConfig("码表引导快键1编码名0", textBox_Copy13.Text);
      ReplaceConfig("码表引导快键1编码名1", textBox_Copy14.Text);
      ReplaceConfig("码表引导快键2", textBox_Copy16.Text);
      ReplaceConfig("码表引导快键2编码名0", textBox_Copy17.Text);
      ReplaceConfig("码表引导快键2编码名1", textBox_Copy18.Text);

      modifiedConfig = Regex.Replace(modifiedConfig, @"《要启用Ctrl\+Space键吗？=.*?》", $"《要启用Ctrl+Space键吗？={要或不要((bool)checkBox_Copy17.IsChecked)}》");
      modifiedConfig = Regex.Replace(modifiedConfig, @"《GDI\+字体样式=.*?》", $"《GDI+字体样式={取GDIp字体样式()}》");
      modifiedConfig = Regex.Replace(modifiedConfig, @"GDI\+字体要下划线吗？=.*?》", $"GDI+字体要下划线吗？={要或不要((bool)checkBox19.IsChecked)}》");
      modifiedConfig = Regex.Replace(modifiedConfig, @"GDI\+字体要删除线吗？=.*?》", $"GDI+字体要删除线吗？={要或不要((bool)checkBox20.IsChecked)}》");
      modifiedConfig = Regex.Replace(modifiedConfig, @"《上屏词条精准匹配key=1\*的值进行词语联想吗？=.*?》", $"《上屏词条精准匹配key=1*的值进行词语联想吗？={要或不要((bool)checkBox_Copy8.IsChecked)}》");
      modifiedConfig = Regex.Replace(modifiedConfig, @"《精准匹配key=1\*的值时要词语模糊联想吗？=.*?》", $"《精准匹配key=1*的值时要词语模糊联想吗？={要或不要((bool)checkBox_Copy9.IsChecked)}》");
      modifiedConfig = Regex.Replace(modifiedConfig, @"《Shift键\+字母键要进入临时英文长句态吗？=.*?》", $"《Shift键+字母键要进入临时英文长句态吗？={要或不要((bool)checkBox_Copy24.IsChecked)}》");
      modifiedConfig = Regex.Replace(modifiedConfig, @"《前次上屏的是数字再上屏句号\*要转成点号\*吗？=.*?》", $"《前次上屏的是数字再上屏句号*要转成点号*吗？={要或不要((bool)checkBox_Copy29.IsChecked)}》");
    }
    private string 取编码或候选嵌入模式(){
      string selected = comboBox1.SelectedIndex.ToString();
      if (checkBox_Copy33.IsChecked == true)
        selected = "1" + selected;
      return selected;
    }
    private string 取背景底色(){
      if (hxcds_checkBox.IsChecked == true)
        return "";
      else return HexToRgb(color_label_5.Background.ToString());
    }
    private string 取候选窗口绘制模式()
    {
      if (radioButton10.IsChecked == true) return "0";
      if (radioButton11.IsChecked == true) return "1";
      //if (radioButton9.IsChecked == true) return "2";
      return "2";
    }
    private string 取D2D字体样式()
    {
      //if (radioButton6.IsChecked == true) return "0";
      if (radioButton7.IsChecked == true) return "1";
      return "0";
    }
    private string 取GDIp字体样式()
    {
      if (radioButton14.IsChecked == true) return "0";
      if (radioButton15.IsChecked == true) return "1";
      if (radioButton16.IsChecked == true) return "2";
      return "3";
    }
    private string 取候选窗口候选排列方向模式()
    {
      if (radioButton8.IsChecked == true) return "1";
      if (radioButton12.IsChecked == true) return "2";
      //if (radioButton13.IsChecked == true) return "3";
      return "3";
    }
    private string 取词语联想上屏字符串长度()
    {
      if (radioButton.IsChecked == true) return "1";
      if (radioButton1.IsChecked == true) return "2";
      //if (radioButton1.IsChecked == true) return "3";
      return "3";
    }
    private string 取词语联想检索范围()
    {
      if (radioButton3.IsChecked == true) return "1";
      if (radioButton4.IsChecked == true) return "2";
      else return "3";
    }
    private string 取顶功规则()
    {
      if (radioButton454.IsChecked == true) return "1";
      if (radioButton455.IsChecked == true) return "2";
      return "3";
    }
    private string 是或不是(bool b){
      if (b == true) return "是";
      else return "不是";
    }
    private string 要或不要(bool b)
    {
      if (b == true) return "要";
      else return "不要";
    }



    #endregion

    #region 配色相关
    // 读取 Json 文件
    void LoadJson()
    {
      if (File.Exists(filePath))
      {
        // 读取整个文件内容,将JSON字符串反序列化为对象
        string json = File.ReadAllText(filePath);
        ColorSchemesCollection colorSchemesJson = JsonConvert.DeserializeObject<ColorSchemesCollection>(json);
        配色方案 = colorSchemesJson.配色方案;

        foreach (var scheme in 配色方案)
        {
          colorSchemeListBox.Items.Add(scheme.名称);
        }
      }
      else
      {
        配色方案.Add(colorScheme);
        string json = JsonConvert.SerializeObject(new { 配色方案 }, Formatting.Indented);
        File.WriteAllText(filePath, json);

        colorSchemeListBox.Items.Add("默认");
      }
    }

    // Hex格式 ARGB 转 RGB，如 #FFAABBCC -> #AABBCC
    public static string RemoveChars(string str, int n)
    {
      str = str.Replace("#", ""); // 移除可能存在的井号
      return "#" + str.Substring(2, str.Length - n);
    }

    // 取色器更新取色位图
    private void UpdateBitmap()
    {
      int width = 255;
      int height = 255;
      double hue = hue_slider.Value / 360; // Hue 值现在来自滑动条
      Bitmap.Lock();
      IntPtr backBuffer = Bitmap.BackBuffer;
      int stride = Bitmap.BackBufferStride;

      for (int y = 0; y < height; y++)
      {
        for (int x = 0; x < width; x++)
        {
          double normalizedX = (double)x / (width - 1);
          double normalizedY = (double)y / (height - 1);

          // 传递给HSVToRGB函数的Hue值现在是0-360度的范围
          HSVToRGB(hue, normalizedX, 1 - normalizedY, out byte r, out byte g, out byte b);

          int pixelOffset = y * stride + x * 4;
          Marshal.WriteByte(backBuffer, pixelOffset + 0, b);
          Marshal.WriteByte(backBuffer, pixelOffset + 1, g);
          Marshal.WriteByte(backBuffer, pixelOffset + 2, r);
          Marshal.WriteByte(backBuffer, pixelOffset + 3, 255); // Alpha 通道设为最大值255（不透明）
        }
      }

      Bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
      Bitmap.Unlock();
    }

    // 取色器 移动取色
    private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
      var thumb = (Thumb)sender;
      double newLeft = Canvas.GetLeft(thumb) + e.HorizontalChange;
      double newTop = Canvas.GetTop(thumb) + e.VerticalChange;
      double canvasRight = canvas.ActualWidth - thumb.ActualWidth + 5;
      double canvasBottom = canvas.ActualHeight - thumb.ActualHeight + 5;

      if (newLeft < -6)
        newLeft = -6;
      else if (newLeft > canvasRight)
        newLeft = canvasRight;

      if (newTop < -6)
        newTop = -6;
      else if (newTop > canvasBottom)
        newTop = canvasBottom;

      Canvas.SetLeft(thumb, newLeft);
      Canvas.SetTop(thumb, newTop);

      GetAreaColor();
    }

    // 取色器 点击取色
    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      var canvasPosition = e.GetPosition(canvas);
      double newLeft = canvasPosition.X - thumb.ActualWidth / 2;
      double newTop = canvasPosition.Y - thumb.ActualHeight / 2;

      double canvasRight = canvas.ActualWidth - thumb.ActualWidth;
      double canvasBottom = canvas.ActualHeight - thumb.ActualHeight;

      if (newLeft < 0)
        newLeft = 0;
      else if (newLeft > canvasRight)
        newLeft = canvasRight;

      if (newTop < 0)
        newTop = 0;
      else if (newTop > canvasBottom)
        newTop = canvasBottom;

      Canvas.SetLeft(thumb, newLeft);
      Canvas.SetTop(thumb, newTop);
      var thumbPosition = e.GetPosition(canvas);
      GetAreaColor(thumbPosition);
    }

    // Hue_slider 值改变事件
    private void Hue_slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      color_textBox_0.Text = (hue_slider.Value / 360).ToString();
      UpdateBitmap();
      GetAreaColor();
    }

    // Hue_slider 滚轮事件
    private void Hue_slider_MouseWheel(object sender, MouseWheelEventArgs e)
    {
      int step = -5;
      if (Keyboard.Modifiers == ModifierKeys.Control) step *= -10;

      if (e.Delta > 0 && hue_slider.Value + step <= hue_slider.Maximum)
      {
        hue_slider.Value += step;
      }
      else if (e.Delta < 0 && hue_slider.Value - step >= hue_slider.Minimum)
      {
        hue_slider.Value -= step;
      }

      // 阻止滚轮事件继续向上冒泡
      e.Handled = true;
    }

    // 设置控件背景色
    void GetAreaColor(Point? thumbPosition = null)
    {
      thumbPosition = thumbPosition == null ? _ = thumb.TranslatePoint(new Point(thumb.ActualWidth / 2, thumb.ActualHeight / 2), canvas) : thumbPosition;
      int xCoordinate = (int)thumbPosition?.X;
      int yCoordinate = (int)thumbPosition?.Y;

      if (xCoordinate >= 0 && xCoordinate < Bitmap.PixelWidth && yCoordinate >= 0 && yCoordinate < Bitmap.PixelHeight)
      {
        int stride = Bitmap.PixelWidth * (Bitmap.Format.BitsPerPixel / 8);
        byte[] pixels = new byte[Bitmap.PixelHeight * stride];
        Bitmap.CopyPixels(new Int32Rect(0, 0, Bitmap.PixelWidth, Bitmap.PixelHeight), pixels, stride, 0);
        int pixelIndex = (yCoordinate * stride) + (xCoordinate * (Bitmap.Format.BitsPerPixel / 8));
        Color color = Color.FromArgb(pixels[pixelIndex + 3], pixels[pixelIndex + 2], pixels[pixelIndex + 1], pixels[pixelIndex]);
        var c_color = new SolidColorBrush(color);
        color_textBox_0.Text = $"({color.R}, {color.G}, {color.B})"; // 格式化 color 为字符串
        color_label_10.Background = c_color;

        Color invertedColor = Color.FromRgb((byte)(255 - color.R), (byte)(255 - color.G), (byte)(255 - color.B)); // 计算反色
        thumb.BorderBrush = new SolidColorBrush(invertedColor);

        // 依select_color_label的值更新指定控件相关属性
        switch (select_color_label)
        {
          case 1: //嵌入下划线色
            color_label_1.Background = c_color; break;
          case 2: //光标色
            color_label_2.Background = c_color; break;
          case 3: //分隔线色
            color_label_3.Background = c_color; break;
          case 4: //候选窗口边框色
            color_label_4.Background = c_color; break;
          case 5: //候选窗背景底色
            color_label_5.Background = c_color; break;
          case 6: //候选选中背景色
            color_label_6.Background = c_color; break;
          case 7: //候选选中字体色
            color_label_7.Background = c_color; break;
          case 8: //编码字体色
            color_label_8.Background = c_color; HXZ_TextBoxText(); break;
          case 9: //候选字体色
            color_label_9.Background = c_color; HXZ_TextBoxText(); break;
          case 10:
            color_label_10.Background = c_color; break;
        }
      }
    }

    // 更新所有候选字色（改为同一个颜色）
    private void HXZ_TextBoxText()
    {
      string rgb1 = HexToRgb(color_label_8.Background.ToString());
      string rgb2 = HexToRgb(color_label_9.Background.ToString());

      bgString = $"<0={rgb1}>";
      for (int i = 1; i <= 26; i++)
        bgString += $"<{i}={rgb2}>";
    }

    // 颜色转换 HSVToRGB
    private static void HSVToRGB(double h, double s, double v, out byte r, out byte g, out byte b)
    {
      if (s == 0)
      {
        r = g = b = (byte)(v * 255);
      }
      else
      {
        double hue = h * 6.0;
        int i = (int)Math.Floor(hue);
        double f = hue - i;
        double p = v * (1.0 - s);
        double q = v * (1.0 - (s * f));
        double t = v * (1.0 - (s * (1.0 - f)));

        switch (i)
        {
          case 0:
            r = (byte)(v * 255);
            g = (byte)(t * 255);
            b = (byte)(p * 255);
            break;
          case 1:
            r = (byte)(q * 255);
            g = (byte)(v * 255);
            b = (byte)(p * 255);
            break;
          case 2:
            r = (byte)(p * 255);
            g = (byte)(v * 255);
            b = (byte)(t * 255);
            break;
          case 3:
            r = (byte)(p * 255);
            g = (byte)(q * 255);
            b = (byte)(v * 255);
            break;
          case 4:
            r = (byte)(t * 255);
            g = (byte)(p * 255);
            b = (byte)(v * 255);
            break;
          default:
            r = (byte)(v * 255);
            g = (byte)(p * 255);
            b = (byte)(q * 255);
            break;
        }
      }
    }

    // 颜色转换 RgbToHex
    public static string RgbToHex(string rgb)
    {
      // 预期rgb字符串格式如 "255, 128, 0"
      string[] rgbValues = rgb.Trim().Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

      if (!byte.TryParse(rgbValues[0], out byte r) || !byte.TryParse(rgbValues[1], out byte g) || !byte.TryParse(rgbValues[2], out byte b))
      {
        throw new FormatException("RGB 取值：0-255");
      }

      // 将字节转换为十六进制字符串，并去掉前导零
      return $"{r:X2}{g:X2}{b:X2}";
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
      }else{
        r = Convert.ToByte(hex.Substring(2, 2), 16);
        g = Convert.ToByte(hex.Substring(4, 2), 16);
        b = Convert.ToByte(hex.Substring(6, 2), 16);
      }
      return $"({r}, {g}, {b})";
    }

    // 显示颜色的 label 鼠标放开事件
    private void Color_label_MouseUp(object sender, MouseButtonEventArgs e)
    {
      Label label = sender as Label;
      switch (label.Name)
      {
        case "color_label_1":
          color_label_content.Content = "嵌入下划线色";
          select_color_label = 1;
          break;
        case "color_label_2":
          select_color_label = 2;
          color_label_content.Content = "光标色";
          break;
        case "color_label_3":
          color_label_content.Content = "分隔线色";
          select_color_label = 3;
          break;
        case "color_label_4":
          select_color_label = 4;
          color_label_content.Content = "候选窗口边框色";
          break;
        case "color_label_5":
          select_color_label = 5;
          color_label_content.Content = "候选窗背景底色";
          break;
        case "color_label_6":
          select_color_label = 6;
          color_label_content.Content = "候选选中背景色";
          break;
        case "color_label_7":
          select_color_label = 7;
          color_label_content.Content = "候选选中字体色";
          break;
        case "color_label_8":
          select_color_label = 8;
          color_label_content.Content = "编码字体色";
          break;
        case "color_label_9":
          select_color_label = 9;
          color_label_content.Content = "候选字体色";
          break;
        case "color_label_10":
          select_color_label = 0;
          color_label_content.Content = "";
          break;
      }
    }

    // 显示颜色的 label 鼠标进入事件
    private void Color_label_MouseEnter(object sender, MouseEventArgs e)
    {
      Label label = sender as Label;
      label.BorderThickness = new Thickness(3);
      color_label_10.Background = label.Background;
      var hex = RemoveChars(label.Background.ToString(), 2);
      var rgb = HexToRgb(hex);
      color_textBox_0.Text = rgb;
    }

    // 显示颜色的 label 鼠标离开事件
    private void Color_label_MouseLeave(object sender, MouseEventArgs e)
    {
      Label label = sender as Label;
      label.BorderThickness = new Thickness(1);
    }

    // 候选框圆角、选中项背景圆角 和 候选框边框调节
    private void Nud11_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
    {
      if (hxk_border != null)
      {
        if (hxc_checkBox.IsChecked == true)
          hxk_border.CornerRadius = new CornerRadius(nud11.Value);
        else
          hxk_border.CornerRadius = new CornerRadius(0);
        if (hxcbj_checkBox.IsChecked == true)
          hxz_border.CornerRadius = new CornerRadius(nud12.Value);
        else
          hxz_border.CornerRadius = new CornerRadius(0);
        hxk_border.BorderThickness = new Thickness(nud13.Value);
      }
    }

    // 候选框圆角 复选框
    private void Hxc_checkBox_Click(object sender, RoutedEventArgs e)
    {
      if (nud11.IsEnabled == true)
      {
        hxk_border.CornerRadius = new CornerRadius(nud11.Value);
      }
      else
      {
        hxk_border.CornerRadius = new CornerRadius(0);
      }
    }

    private void Hxcds_checkBox_Click(object sender, RoutedEventArgs e)
    {
      if (hxcds_checkBox.IsChecked == false)
      {
        color_label_5.Background = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
        //hxk_border.Background = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
      }
      else
      {
        color_label_5.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        //hxk_border.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
      }
    }

    // 配色列表双击事件
    private void ColorSchemeListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (e.ChangedButton == MouseButton.Left && colorSchemeListBox.SelectedItem != null)
      {
        var colorScheme = 配色方案[colorSchemeListBox.SelectedIndex];
        checkBox_Copy42.IsChecked = colorScheme.显示背景图;
        hxc_checkBox.IsChecked = colorScheme.显示候选窗圆角;
        hxcbj_checkBox.IsChecked = colorScheme.显示选中项背景圆角;
        nud11.Value = colorScheme.候选窗圆角;
        nud12.Value = colorScheme.选中项圆角;
        nud13.Value = colorScheme.边框线宽;
        color_label_1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorScheme.下划线色));
        color_label_2.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorScheme.光标色));
        color_label_3.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorScheme.分隔线色));
        color_label_4.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorScheme.窗口边框色));

        if (colorScheme.窗背景底色 == "")
        {
          color_label_5.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00000000"));
          bkColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00000000"));
          hxcds_checkBox.IsChecked = true;
        }
        else
        {
          color_label_5.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorScheme.窗背景底色));
          bkColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorScheme.窗背景底色));
          hxcds_checkBox.IsChecked = false;
        }
        color_label_6.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorScheme.选中背景色));
        color_label_7.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorScheme.选中字体色));
        color_label_8.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorScheme.编码字体色));
        color_label_9.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorScheme.候选字色));
      }
    }

    // 配色列表选中项改变事件
    private void ColorSchemeListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (colorSchemeListBox.SelectedItem != null)
      {
        if (saveButton.Content.ToString() == "保存配色")
        {
          color_scheme_name_textBox.Text = "";
        }
        if (saveButton.Content.ToString() == "修改配色")
        {
          color_scheme_name_textBox.Text = colorSchemeListBox.SelectedItem.ToString();
        }
      }
    }

    // 新建配色方案
    private void MenuItem_Click_1(object sender, RoutedEventArgs e)
    {
      saveButton.Content = "保存配色";
      saveButton.Visibility = Visibility.Visible;
      color_scheme_name_textBox.Visibility = Visibility.Visible;
    }

    // 修改配色方案
    private void MenuItem_Click_2(object sender, RoutedEventArgs e)
    {
      if (colorSchemeListBox.SelectedItem == null)
      {
        MessageBox.Show("您没有选中任何配色！",
        "修改操作",
        MessageBoxButton.OK,
        MessageBoxImage.Question);
        return;
      }
      saveButton.Content = "修改配色";
      saveButton.Visibility = Visibility.Visible;
      color_scheme_name_textBox.Visibility = Visibility.Visible;
      color_scheme_name_textBox.Text += colorSchemeListBox.SelectedItem.ToString();
    }

    // 删除选中配色方案
    private void MenuItem_Click_3(object sender, RoutedEventArgs e)
    {
      if (colorSchemeListBox.SelectedItem == null)
      {
        MessageBox.Show("您没有选中任何配色！",
        "删除操作",
        MessageBoxButton.OK,
        MessageBoxImage.Question);
        return;
      }
      var name = colorSchemeListBox.SelectedItem.ToString();
      var result = MessageBox.Show(
      $"您确定要删除 {name} 吗？",
      "删除操作",
      MessageBoxButton.OKCancel,
      MessageBoxImage.Question);

      if (result == MessageBoxResult.OK)
      {
        配色方案.RemoveAt(colorSchemeListBox.SelectedIndex);
        string json = JsonConvert.SerializeObject(new { 配色方案 }, Formatting.Indented);
        File.WriteAllText(filePath, json);

        colorSchemeListBox.Items.Remove(name);
        colorSchemeListBox.Items.Refresh();
      }

    }

    // 添加配色
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {

      var name = color_scheme_name_textBox.Text.Trim();
      colorScheme = new ColorScheme
      {
        名称 = name,
        显示背景图 = (bool)checkBox_Copy42.IsChecked,
        显示候选窗圆角 = (bool)hxc_checkBox.IsChecked,
        显示选中项背景圆角 = (bool)hxcbj_checkBox.IsChecked,
        候选窗圆角 = nud11.Value,
        选中项圆角 = nud12.Value,
        边框线宽 = nud13.Value,
        下划线色 = RemoveChars(color_label_1.Background.ToString(), 2),
        光标色 = RemoveChars(color_label_2.Background.ToString(), 2),
        分隔线色 = RemoveChars(color_label_3.Background.ToString(), 2),
        窗口边框色 = RemoveChars(color_label_4.Background.ToString(), 2),
        窗背景底色 = hxcds_checkBox.IsChecked ==true ? "" : RemoveChars(color_label_5.Background.ToString(), 2),
        选中背景色 = RemoveChars(color_label_6.Background.ToString(), 2),
        选中字体色 = RemoveChars(color_label_7.Background.ToString(), 2),
        编码字体色 = RemoveChars(color_label_8.Background.ToString(), 2),
        候选字色 = RemoveChars(color_label_9.Background.ToString(), 2),
      };

      if (saveButton.Content.ToString() == "保存配色")
      {
        foreach (var item in colorSchemeListBox.Items)
        {
          if (item.ToString() == name){
            MessageBox.Show("存在同名配色！");
            return;
          }
          if (color_scheme_name_textBox.Text.Length == 0)
          {
            MessageBox.Show("请输入新的配色名称！");
            color_scheme_name_textBox.Focus();
            return;
          }
        }
        配色方案.Add(colorScheme);
        colorSchemeListBox.Items.Add(name);
      }
      if (saveButton.Content.ToString() == "修改配色" && colorSchemeListBox.SelectedItem != null)
      {
        var n = colorSchemeListBox.SelectedIndex;
        配色方案[n] = colorScheme;
        colorSchemeListBox.Items.Clear();

        foreach (var scheme in 配色方案)
          colorSchemeListBox.Items.Add(scheme.名称);

        colorSchemeListBox.SelectedIndex = n;
      }
      string json = JsonConvert.SerializeObject(new { 配色方案 }, Formatting.Indented);
      File.WriteAllText(filePath, json);
    }

    private void Button3_Copy_Click(object sender, RoutedEventArgs e)
    {
      var selectedFontName = SelectFontName();
      if (selectedFontName != null) {
        textBox_Copy145.Text = selectedFontName.ToString();
      }
    }

    public static string SelectFontName()
    {
      using var fontDialog = new FontDialog();
      // 设置初始字体选项（可选）
      // fontDialog.Font = new Font("Arial", 12);

      // 显示字体对话框并获取用户的选择结果
      if (fontDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
      {
        return fontDialog.Font.Name; // 返回用户选择的字体名称
      }

      // 如果用户取消了对话框，则可能返回null或默认值
      return null;
    }

    private void TextBox_Copy22_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (textBox_Copy22.Text == "") {
        textBox_Copy22.Text = "⁠⁣"; // 有个隐藏符号
      }

    }

    private void TextBox_Copy23_TextChanged(object sender, TextChangedEventArgs e)
    {
      if(textBox_Copy23.Text == "") {
        textBox_Copy23.Text = "⁠⁣"; // 有个隐藏符号
      }
    }

    private void ComboBox3_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      textBox_Copy67.Text = ((ComboBoxItem)comboBox3.SelectedItem).Content.ToString();
    }

    private void Hxc_checkBox_Checked(object sender, RoutedEventArgs e)
    {
    if(hxk_border != null)
      hxk_border.CornerRadius = new CornerRadius(nud11.Value);
    }

    private void Hxc_checkBox_Unchecked(object sender, RoutedEventArgs e)
    {
      if (hxk_border != null)
        hxk_border.CornerRadius = new CornerRadius(0);
    }

    private void Hxcbj_checkBox_Checked(object sender, RoutedEventArgs e)
    {
    if(hxz_border != null)
        hxz_border.CornerRadius = new CornerRadius(nud12.Value);
    }
    private void Hxcbj_checkBox_Unchecked(object sender, RoutedEventArgs e)
    {
    if(hxz_border != null)
        hxz_border.CornerRadius = new CornerRadius(0);
    }

    private void Hxcds_checkBox_Checked(object sender, RoutedEventArgs e)
    {
      //color_label_5.Background = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
      bkColor = (SolidColorBrush)color_label_5.Background;
      color_label_5.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
    }

    private void Hxcds_checkBox_Unchecked(object sender, RoutedEventArgs e)
    {
      color_label_5.Background = bkColor;
    }

    #endregion

    #region 托盘图标和窗口

    // 加载托盘图标
    private void InitIcon(){
      // 阻止默认的关闭行为
      //this.Closing += MainWindow_Closing;

      notifyIcon = new NotifyIcon
      {
        Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath),
        Tag = this,
        Visible = true
    };

      MenuItem menuItem1 = new("显示配置窗口"); menuItem1.Click += MenuItem1_Click;
      MenuItem menuItem5 = new("显示/隐藏壮态条"); menuItem5.Click += MenuItem5_Click;
      MenuItem menuItem4 = new("重启服务端"); menuItem4.Click += MenuItem4_Click;
      MenuItem menuItem2 = new("关于"); menuItem2.Click += MenuItem2_Click;
      MenuItem menuItem3 = new("退出"); menuItem3.Click += MenuItem3_Click;



      MenuItem[] menuItems = new MenuItem[] {
        menuItem1,  //显示配置窗口
        //menuItem5,  //显示/隐藏壮态条
        //menuItem4,  //重启服务端
        menuItem2,  //关于
        menuItem3   //退出
      };
      notifyIcon.ContextMenu = new System.Windows.Forms.ContextMenu(menuItems);
      // 添加托盘图标双击事件处理
      notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
    }



    // 发送Ctrl+F1组合键
    private void MenuItem5_Click(object sender, EventArgs e)
    {
      SendKeys.SendWait("^(F1)");
    }

    // 查找并关闭所有名为“KegServer”的进程
    private void MenuItem4_Click(object sender, EventArgs e)
    {
      foreach (var process in Process.GetProcessesByName("KegServer"))
      {
        process.Kill();
        process.WaitForExit(); // 等待进程关闭
      }
    }

    // 退出
    private void MenuItem3_Click(object sender, EventArgs e)
    {
      notifyIcon.Dispose();
      //this.Close();
      ((App)System.Windows.Application.Current).Exit();
    }

    // 显示主窗口
    private void MenuItem1_Click(object sender, System.EventArgs e) {
      this.Visibility= Visibility.Visible;
    }

    // 关于
    private void MenuItem2_Click(object sender, System.EventArgs e)
    {
      MessageBox.Show("本工具用于小科狗码表方案配置", "说明");
    }

    // 托盘图标双击事件
    private void NotifyIcon_DoubleClick(object sender, EventArgs e)
    {
      this.Visibility = Visibility.Visible;
    }

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

    private void button_Click(object sender, RoutedEventArgs e)
    {
      if (checkBox2.IsChecked == true)
        ((App)System.Windows.Application.Current).Exit();
      else
        this.Visibility = Visibility.Hidden; // 或者使用 Collapsed
    }

    #endregion



  }
}
