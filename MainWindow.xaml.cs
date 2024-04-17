using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
namespace 小科狗配置
{
  /// <summary>
  /// MainWindow.xaml 的交互逻辑
  /// </summary>
  /// 
  public partial class MainWindow : Window
  {
    #region 配色方案相关定义
    public WriteableBitmap Bitmap     { get; set; } // 定义取色器背景图
    public WriteableBitmap Hue_bitmap { get; set; } // 定义取色器色相滑块背景图

    // 定义配色方案类
    public class ColorScheme
    {
      public string 名称               { get; set; }
      public bool   显示背景图         { get; set; }
      public bool   显示候选窗圆角     { get; set; }
      public bool   显示选中项背景圆角 { get; set; }
      public int    候选窗圆角         { get; set; }
      public int    选中项圆角         { get; set; }
      public int    边框线宽           { get; set; }
      public string 下划线色           { get; set; }
      public string 光标色             { get; set; }
      public string 分隔线色           { get; set; }
      public string 窗口边框色         { get; set; }
      public string 窗背景底色         { get; set; }
      public string 选中背景色         { get; set; }
      public string 选中字体色         { get; set; }
      public string 编码字体色         { get; set; }
      public string 候选字色           { get; set; }
    }
    public class ColorSchemesCollection
    {
      public List<ColorScheme> 配色方案 { get; set; }
    }
    List<ColorScheme> 配色方案  = new();
    ColorScheme colorScheme     = new()
    {
      名称               = "默认",
      显示背景图         = false,
      显示候选窗圆角     = true,
      显示选中项背景圆角 = true,
      候选窗圆角         = 15,
      选中项圆角         = 10,
      边框线宽           = 1,
      下划线色           = "#FF0000",
      光标色             = "#004CFF",
      分隔线色           = "#000000",
      窗口边框色         = "#000000",
      窗背景底色         = "#FFFFFF",
      选中背景色         = "#000000",
      选中字体色         = "#333333",
      编码字体色         = "#000000",
      候选字色           = "#000000"
    };
    #endregion

    #region 全局变量定义
    SolidColorBrush bkColor               = new ((Color) ColorConverter.ConvertFromString("#FFFFFFFF"));  // 候选框无背景色时的值
    readonly string appPath               = Environment.CurrentDirectory;
    readonly string settingConfigFile     = "窗口配置.ini";
    readonly string schemeFilePath        = "配色方案.json";
    readonly string globalSettingFilePath = "全局设置.json";
    string labelName                      = "方案名称";
    string zh_en                          = "中c：";
    int select_color_label_num            = 0;       // 用于记录当前选中的 select_color_label
    readonly string kegPath;                         // 小科狗主程序目录
    string bgString;                                 // 存放字体色串
    string currentConfig, modifiedConfig;            // 存少当前配置和当前修改的配置
    readonly string dbPath, kegFilePath;             // Keg.db 和 Keg.txt 文件路径
    //NotifyIcon notifyIcon;                           // 托盘图标
    #endregion

    #region 全局设置界面列表项定义

    public class 列表项 : INotifyPropertyChanged
    {
      private bool _Enable;
      public bool  Enable
      {
        get { return _Enable; }
        set { _Enable = value; OnPropertyChanged("Enable"); }
      }
      private string  _Name;
      public string   Name
      {
        get { return _Name; }
        set { _Name = value; OnPropertyChanged("Name"); }
      }
      private string  _Value;
      public string   Value
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
      public 列表项(){ }


      public event PropertyChangedEventHandler PropertyChanged;
      public virtual void OnPropertyChanged(string PropertyName)
      {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
      }

    }

    public class 状态条
    {
      public string 提示文本的位置       { get; set; }
      public bool   提示文本要隐藏吗     { get; set; }
      public string 提示文本中文字体色   { get; set; }
      public string 提示文本英文字体色   { get; set; }
      public int    提示文本字体大小     { get; set; }
      public string 提示文本字体名称     { get; set; }
      public bool   要启用深夜锁屏吗     { get; set; }
      public bool   提示文本要显示中英以及大小写状态吗         { get; set; }
      public bool   快键只在候选窗口显示情况下才起作用吗       { get; set; }
      public bool   打字字数统计等数据是要保存在主程文件夹下吗 { get; set; }
    }

    // 用于 listView 数据绑定
    public ObservableCollection<列表项> 查找列表 { get; set; }
    public ObservableCollection<列表项> 外部工具 { get; set; }
    public ObservableCollection<列表项> 快键命令 { get; set; }
    public ObservableCollection<列表项> 自动关机 { get; set; }
    public ObservableCollection<列表项> 快键     { get; set; }
    public ObservableCollection<列表项> 自启     { get; set; }
    public 状态条 设置项                         { get; set; }


    // 用于存放 全局设置.json
    public struct GlobalSettings
    {
      public 状态条 状态栏和其它设置               { get; set; }
      public ObservableCollection<列表项> 查找列表 { get; set; }
      public ObservableCollection<列表项> 外部工具 { get; set; }
      public ObservableCollection<列表项> 快键命令 { get; set; }
      public ObservableCollection<列表项> 快键     { get; set; }
      public ObservableCollection<列表项> 自启     { get; set; }
      public ObservableCollection<列表项> 自动关机 { get; set; }

    }

    GlobalSettings 全局设置 = new()
    {
      状态栏和其它设置 = new(),
      查找列表         = new ObservableCollection<列表项>(),
      外部工具         = new ObservableCollection<列表项>(),
      快键命令         = new ObservableCollection<列表项>(),
      快键             = new ObservableCollection<列表项>(),
      自启             = new ObservableCollection<列表项>()
    };




    #endregion

    #region 初始化

    public MainWindow()
    {
      InitializeComponent();

      settingConfigFile = appPath + "\\窗口配置.ini";

      // 获取小科狗主程序目录
      kegPath = GetValue("window", "keg_path");
      if (!File.Exists(kegPath + "\\KegServer.exe"))
      {
        kegPath = GetKegPath();
        if (File.Exists(kegPath + "\\KegServer.exe")) SetValue("window", "keg_path", kegPath);
        else Close();
      }
      kegFilePath = kegPath + "\\Keg.txt";
      dbPath = kegPath + "\\Keg.db";

      toolTipTextBlock.Text = $"{zh_en}{labelName}";

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

      Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
      // 读取 setting.ini
      LoadSettingConfig();


      Bitmap      = new WriteableBitmap(170, 170, 170, 170, PixelFormats.Bgra32, null);
      DataContext = this;
      LoadImages();
      UpdateBitmap();       // 生成 取色图
      //InitIcon();           // 载入托盘图标
      //LoadTableNames();     // 载入码表方案名称
      LoadJson();           // 读取 配色方案.jsonString
      LoadHxFile();         // 读取 候选序号.txt
      ReadKegText();        // 读取 全局设置
    }

    // 获取版本号
    public string GetAssemblyVersion()
    {
      Assembly assembly = Assembly.GetExecutingAssembly();
      Version version   = assembly.GetName().Version;
      return version.ToString().Substring(0,3);
    }
    #endregion

    #region 消息接口定义
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint flags, uint timeout, out IntPtr pdwResult);

    //const uint NORMAL = 0x0000;
    //const uint BLOCK = 0x0001;
    const uint ABORTIFHUNG = 0x0002;
    //const uint NOTIMEOUTIFNOTHUNG = 0x0008;
    //uint flags = (uint)(ABORTIFHUNG | BLOCK);
    readonly uint flags   = (uint)(ABORTIFHUNG);
    readonly uint timeout = 500;

    const int WM_USER           = 0x0400;               // 根据Windows API定义
    //const uint KWM_RESETPIPLE   = (uint)WM_USER + 200;  //重置双轮流通信命名管道
    const uint KWM_RESET        = (uint)WM_USER + 201;  //重置配置
    //const uint KWM_SET0         = (uint)WM_USER + 202;  //权重全置为0
    const uint KWM_GETSET       = (uint)WM_USER + 203;  //由剪切板指定方案名,从而获得该方案的配置
    //const uint KWM_INSERT       = (uint)WM_USER + 204;  //根据剪切板的带方案名(第一行数据)的词条插入词条
    const uint KWM_UPBASE       = (uint)WM_USER + 205;  //更新内存数据库 
    const uint KWM_SAVEBASE     = (uint)WM_USER + 206;  //保存内存数据库
    //const uint KWM_GETDATAPATH  = (uint)WM_USER + 207;  //从剪切板获得文本码表路径并加载该路径的文本码表到内存数据库
    const uint KWM_GETDEF       = (uint)WM_USER + 208;  //把默认无方案名的配置模板吐到剪切板
    const uint KWM_SET2ALL      = (uint)WM_USER + 209;  //设置当前码字方案为所有进程的初始方案格式:《所有进程默认初始方案=  》
    //const uint KWM_GETWRITEPATH = (uint)WM_USER + 210;  //从剪切板获得导出的码字的文件夹路+导出类据 ,格式:path->方案名1#方案名2 所有方案名为ALL
    const uint KWM_UPQJSET      = (uint)WM_USER + 211;  //读取说明文本更新全局设置
    const uint KWM_UPPFSET      = (uint)WM_USER + 212;  //从剪切板取皮肤png或gif文件的全路径设置,更新状态之肤 格式:文件全路径放到剪切板
    const uint KWM_GETALLNAME   = (uint)WM_USER + 213;  //把所有方案名吐到剪切板,一行一个方案名
    //const uint KWM_GETALLZSTJ   = (uint)WM_USER + 214;  //把字数与速度的所有统计数据吐到剪切板 格式见字数统计界面的样子,具体见剪切板
    #endregion

    #region 读写配置文件项
    // 读写配置项 API
    [DllImport("kernel32", CharSet = CharSet.Unicode)]// 读配置文件方法的6个参数：所在的分区、   键值、      初始缺省值、         StringBuilder、      参数长度上限 、配置文件路径
    private static extern long GetPrivateProfileString(string section, string key, string defaultValue, StringBuilder retVal, int size, string filePath);
    [DllImport("kernel32", CharSet = CharSet.Unicode)]// 写入配置文件方法的4个参数：所在的分区、    键值、      参数值、      配置文件路径
    private static extern long WritePrivateProfileString(string section, string key, string value, string filePath);

    /// <summary>
    /// 写配置文件
    /// </summary>
    /// <param name="section">配置项</param>
    /// <param name="key">键</param>
    /// <param name="value">命令行</param>
    /// <param name="filePath">路径</param>
    public  void SetValue(string section, string key, string value)
    {
      WritePrivateProfileString(section, key, value, settingConfigFile);
    }

    /// <summary>
    /// 读配置文件
    /// </summary>
    /// <param name="section">配置项</param>
    /// <param name="key">键</param>
    /// <param name="filePath">路径</param>
    /// <returns>命令行</returns>
    public string GetValue(string section, string key)
    {
      if (File.Exists(settingConfigFile))
      {
        StringBuilder sb = new(255);
        GetPrivateProfileString(section, key, "", sb, 255, settingConfigFile);
        return sb.ToString();
      }
      else return string.Empty;
    }

    #endregion

    #region 读写db
    // 从 db 读取表名到 ComboBox
    //private void LoadTableNames()
    //{
    //  SQLiteConnection connection = new($"Data Source={dbPath};Version=3;");
    //  connection.Open();
    //  try
    //  {
    //    comboBox.Items.Clear();
    //    using var command = connection.CreateCommand();
    //    command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
    //    var reader = command.ExecuteReader();
    //    while (reader.Read())
    //    {
    //      var labelName = reader.GetString(0);
    //      comboBox.Items.Add(labelName);
    //      configs.Add(labelName);
    //    }
    //  }
    //  catch (Exception ex)
    //  {
    //    MessageBox.Show($"Error loading table names: {ex.Message}");
    //  }
    //  connection.Close();
    //}

    // 从指定表 labelName 内读取 key 列为"配置"时 value 的值
    //private string GetConfig(string labelName)
    //{
    //  using SQLiteConnection connection = new($"Data Source={dbPath};Version=3;");
    //  string query = $"SELECT value FROM '{labelName}' WHERE key = '配置'";
    //  connection.Open();
    //  using SQLiteCommand command = new(query, connection);

    //  string result;
    //  using (SQLiteDataReader reader = command.ExecuteReader())
    //  {
    //    if (reader.Read()) result = reader["value"].ToString();
    //    else result = null;
    //  }
    //  connection.Close();
    //  return result;
    //}

    // 保存配置到数据库
    // 更新指定表 labelName 内 key 列为 "配置" 时 value 列的值为 value
    //private void SaveConfig(String value)
    //{
    //  string connectionString = $"Data Source={dbPath};Version=3;";
    //  using SQLiteConnection connection = new(connectionString);
    //  string updateQuery = $"UPDATE '{labelName}' SET value = @Value WHERE key = '配置'";
    //  connection.Open();
    //  using SQLiteCommand command = new(updateQuery, connection);
    //  command.Parameters.AddWithValue("@Value", value);
    //  int rowsAffected = command.ExecuteNonQuery();
    //  connection.Close();
    //}

    #endregion

    #region 顶部控件事件
    // 载入码表方案名称
    private void GetList_button_Click(object sender, RoutedEventArgs e)
    {
      LoadTableNames();
    }
    private void LoadTableNames()
    {
      try
      {
        //把所有方案名吐到剪切板,一行一个方案名
        IntPtr hWnd = FindWindow("CKegServer_0", null);
        SendMessageTimeout(hWnd, KWM_GETALLNAME, IntPtr.Zero, IntPtr.Zero, flags, timeout, out IntPtr pdwResult);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"操作失败，请重试！");
        Console.WriteLine(ex.Message);
      }
      var multiLineString = Clipboard.GetText();

      // 使用StringSplitOptions.RemoveEmptyEntries选项来避免空行被添加
      string[] lines = multiLineString.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

      // 将每行作为一个项添加到ComboBox中
      foreach (string line in lines)
        comboBox.Items.Add(line);
      comboBox.SelectedIndex = 0;
      getList_button.Visibility = Visibility.Collapsed;
    }
    private string GetConfig(string labelName)
    {
      try
      {
        Clipboard.SetText(labelName);

        IntPtr hWnd = FindWindow("CKegServer_0", null);
        SendMessageTimeout(hWnd, KWM_GETSET, IntPtr.Zero, IntPtr.Zero, flags, timeout, out IntPtr pdwResult);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"操作失败，请重试！");
        Console.WriteLine(ex.Message);
      }

      Thread.Sleep(300);
      string result = Clipboard.GetText();
      return result;
    }

    // 保存内存配置到数据库
    private void SaveConfig(string labelName)
    {
      try
      {
        Clipboard.SetText(labelName);
        IntPtr hWnd = FindWindow("CKegServer_0", null);
        SendMessageTimeout(hWnd, KWM_SAVEBASE, IntPtr.Zero, IntPtr.Zero, flags, timeout, out IntPtr pdwResult);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"操作失败，请重试！");
        Console.WriteLine(ex.Message);
      }

    }

    // 删除Keg.db内所有方案配置
    //private void Res_button_Click(object sender, RoutedEventArgs e)
    //{
    //  var result = MessageBox.Show(
    //  $"如果你的方案配置出了问题，确定后将删除 Keg.db 内所有方案的配置！",
    //  "清除操作",
    //  MessageBoxButton.OKCancel,
    //  MessageBoxImage.Question);

    //  if (result == MessageBoxResult.OK)
    //  {
    //    // 连接到SQLite数据库
    //    using var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;");
    //    conn.Open();

    //    // 创建命令对象
    //    var cmd = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table';", conn);

    //    // 执行命令，获取数据表名
    //    var tables = cmd.ExecuteReader();
    //    while (tables.Read())
    //    {
    //      string tableName = tables.GetString(0);

    //      // 更新每个表中的配置值
    //      var updateCmd = new SQLiteCommand($"UPDATE {tableName} SET value='' WHERE key='配置';", conn);
    //      updateCmd.ExecuteNonQuery();
    //    }

    //    // 提交事务
    //    conn.Close();
    //  }
    //}

    private void ComboBox_MouseEnter(object sender, MouseEventArgs e)
    {
      comboBox.Focus();
    }

    // 切换方案
    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      labelName             = comboBox.SelectedValue as string;
      toolTipTextBlock.Text = $"{zh_en}{labelName}";

      currentConfig = GetConfig(labelName);
      SetControlsValue();
      
      restor_default_button.IsEnabled     = true;
      loading_templates_button.IsEnabled  = true;
      set_as_default_button.IsEnabled     = true;
      apply_button.IsEnabled              = true;
      apply_save_button.IsEnabled         = true;
      apply_all_button.IsEnabled          = true;
      //res_button.IsEnabled                = true;
      getList_button.IsEnabled            = true;
    }

    // 加载默认设置
    private void Restor_default_button_Click(object sender, RoutedEventArgs e)
    {
      currentConfig = GetConfig(labelName);
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

      if (result != MessageBoxResult.OK)
        return;

      try
      {
        IntPtr hWnd   = FindWindow("CKegServer_0", null);
        SendMessageTimeout(hWnd, KWM_GETDEF, IntPtr.Zero, IntPtr.Zero, flags, timeout, out IntPtr pdwResult);
        var str       = Clipboard.GetText();
        currentConfig = Regex.Replace(str, "方案：<>配置", $"方案：<{labelName}>配置");
        SetControlsValue();

        modifiedConfig            = currentConfig;
        checkBox_Copy25.IsChecked = true;
        checkBox_Copy26.IsChecked = true;
        checkBox1.IsChecked       = false;
        checkBox_Copy12.IsChecked = true;
      }
      catch (Exception ex)
      {
        MessageBox.Show($"操作失败，请重试！");
        Console.WriteLine(ex.Message);
      }
    }

    // 设置默认方案
    private void Set_as_default_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        Clipboard.SetText($"《所有进程默认初始方案={labelName}》");
        IntPtr hWnd = FindWindow("CKegServer_0", null);
        SendMessageTimeout(hWnd, KWM_SET2ALL, IntPtr.Zero, IntPtr.Zero, flags, timeout, out IntPtr pdwResult);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"操作失败，请重试！");
        Console.WriteLine(ex.Message);
      }
    }

    // 应用修改
    private void Apply_button_Click(object sender, RoutedEventArgs e)
    {
      modifiedConfig = currentConfig;
      GetControlsValue(); // 读取所有控件值替换到 modifiedConfig
      // 获取已修改项
      string updataStr = $"方案：<{labelName}> 配置 \n" + GetDifferences(modifiedConfig, currentConfig);

      try
      {
        IntPtr hWnd = FindWindow("CKegServer_0", null);
        Clipboard.SetText(updataStr);
        Thread.Sleep(200);
        SendMessageTimeout(hWnd, KWM_RESET, IntPtr.Zero, IntPtr.Zero, flags, timeout, out IntPtr pdwResult);
        currentConfig = modifiedConfig;
      }
      catch (Exception ex)
      {
        MessageBox.Show($"操作失败，请重试！");
        Console.WriteLine(ex.Message);
      }
    }

    // 保存内存数据
    private void Apply_save_button_Click(object sender, RoutedEventArgs e)
    {
      SaveConfig(labelName);
    }

    // 更新内存数据
    private void Apply_All_button_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        IntPtr hWnd = FindWindow("CKegServer_0", null);
        // 更新内存数据库 
        SendMessageTimeout(hWnd, KWM_UPBASE, IntPtr.Zero, IntPtr.Zero, flags, timeout, out IntPtr pdwResult); 
      }
      catch (Exception ex)
      {
        MessageBox.Show($"操作失败，请重试！");
        Console.WriteLine(ex.Message);
      }
    }

    // 关闭窗口后直接退出
    //private void CheckBox2_Click(object sender, RoutedEventArgs e)
    //{
    //  if (checkBox2.IsChecked == true)
    //    SetValue("window", "closed", "1");
    //  else
    //    SetValue("window", "closed", "0");
    //}

    // 窗口置顶
    //private void CheckBox3_Click(object sender, RoutedEventArgs e)
    //{
    //  this.Topmost = (bool)checkBox2.IsChecked;
    //  var topmost = checkBox2.IsChecked == true ? "1" : "0";
    //  SetValue("window", "topmost", topmost);
    //}

    // 获取已修改项
    public static string GetDifferences(string modifiedConfig, string currentConfig)
    {
      string pattern            = "《.*?》";
      MatchCollection matches1  = Regex.Matches(modifiedConfig, pattern);
      MatchCollection matches2  = Regex.Matches(currentConfig, pattern);
      string[] modifiedLines    = matches1.Cast<Match>().Select(m => m.Value).ToArray();
      string[] currentLines     = matches2.Cast<Match>().Select(m => m.Value).ToArray();
      // 找出不同的行
      var differentLines        = modifiedLines.Except(currentLines);
      // 将不同的行追加到新的字符串中
      string newConfig          = string.Join(Environment.NewLine, differentLines);
      return newConfig;
    }

    #endregion

    #region 读取配置各项值到控件
    // 显示对话框，并获取用户选择的文件夹路径
    private string GetKegPath(){
      FolderBrowserDialog folderBrowserDialog = new()
      {
        Description = "选择小科狗主程序目录"
      };

      if (folderBrowserDialog.ShowDialog() == FormsDialogResult.OK)
        return folderBrowserDialog.SelectedPath;
      return null;
    }

    // 读取 setting.ini
    private void LoadSettingConfig()
    {
      //checkBox2.IsChecked = GetValue("window", "closed") == "1";
      bool isNumberValid  = int.TryParse(GetValue("window", "height"), out int height);
      nud22.Value         = isNumberValid ? height : 600;
      this.Width          = 900;
      Grid1.Height        = this.Height - 50;
      Grid2.Height        = this.Height - 50;
      Grid3.Height        = this.Height - 50;
      Grid2.Width         = 750;
      Grid2.Width         = 0;
      Grid3.Width         = 0;
      Grid1.Visibility    = Visibility.Visible;
      Grid2.Visibility    = Visibility.Hidden;
      Grid3.Visibility    = Visibility.Hidden;
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
          case "背景底色"                     : 背景底色                     (value); break;
          case "顶功规则"                     : 顶功规则                     (value); break;
          case "D2D字体样式"                  : D2D字体样式                  (value); break;
          case "GDI+字体样式"                 : GDIp字体样式                 (value); break;
          case "候选字体色串"                 : SetLabelColor                (value); break;
          case "词语联想检索范围"             : 词语联想检索范围             (value); break;
          case "候选窗口绘制模式"             : 候选窗口绘制模式             (value); break;
          case "编码或候选嵌入模式"           : 编码或候选嵌入模式           (value); break;
          case "词语联想上屏字符串长度"       : 词语联想上屏字符串长度       (value); break;
          case "候选窗口候选排列方向模式"     : 候选窗口候选排列方向模式     (value); break;
          case "大键盘码元"                   : textBox_Copy677.Text         = value; break;
          case "小键盘码元"                   : textBox_Copy5.Text           = value; break;
          case "键首字根"                     : textBox125.Text              = value; break;
          case "字体名称"                     : textBox_Copy145.Text         = value; break;
          case "码表标签"                     : textBox_Copy15.Text          = value; break;
          case "主码表标识"                   : textBox_Copy22.Text          = value; break;
          case "副码表标识"                   : textBox_Copy23.Text          = value; break;
          case "候选序号"                     : textBox_Copy67.Text          = value; break;
          case "顶功小集码元"                 : textBox_Copy675.Text         = value; break;
          case "码表临时快键"                 : textBox_Copy19.Text          = value; break;
          case "D2D回退字体集"                : textBox_Copy10.Text          = value; break;
          case "码表引导快键0"                : textBox_Copy.Text            = value; break;
          case "码表引导快键1"                : textBox_Copy12.Text          = value; break;
          case "码表引导快键2"                : textBox_Copy16.Text          = value; break;
          case "候选快键字符串"               : textBox_Copy66.Text          = value; break;
          case "大小键盘万能码元"             : textBox_Copy6.Text           = value; break;
          case "大键盘中文标点串"             : textBox_Copy68.Text          = value; break;
          case "重复上屏码元字符串"           : textBox_Copy1.Text           = value; break;
          case "码表临时快键编码名"           : textBox_Copy20.Text          = value; break;
          case "码表引导快键0编码名0"         : textBox_Copy9.Text           = value; break;
          case "码表引导快键0编码名1"         : textBox_Copy11.Text          = value; break;
          case "码表引导快键1编码名0"         : textBox_Copy13.Text          = value; break;
          case "码表引导快键1编码名1"         : textBox_Copy14.Text          = value; break;
          case "码表引导快键2编码名0"         : textBox_Copy17.Text          = value; break;
          case "码表引导快键2编码名1"         : textBox_Copy18.Text          = value; break;
          case "非编码串首位的大键盘码元"     : textBox_Copy7.Text           = value; break;
          case "非编码串首位的小键盘码元"     : textBox_Copy8.Text           = value; break;
          case "大键盘按下Shift的中文标点串"  : textBox_Copy69.Text          = value; break;
          case "往上翻页大键盘英文符号编码串" : textBox_Copy21.Text          = value; break;
          case "往下翻页大键盘英文符号编码串" : textBox_Copy2.Text           = value; break;
          case "往上翻页小键盘英文符号编码串" : textBox_Copy3.Text           = value; break;
          case "往下翻页小键盘英文符号编码串" : textBox_Copy4.Text           = value; break;
          case "码表标签显示模式"             : comboBox1_Copy.SelectedIndex = int.Parse(value); break;
          case "窗口四个角的圆角半径"         : nud11.Value                  = int.Parse(value); break;
          case "选中项四个角的圆角半径"       : nud12.Value                  = int.Parse(value); break;
          case "候选窗口边框线宽度"           : nud13.Value                  = int.Parse(value); break;
          case "最大码长"                     : nud1.Value                   = int.Parse(value); break;
          case "D2D字体加粗权值"              : nud14.Value                  = int.Parse(value); break;
          case "候选个数"                     : nud15.Value                  = int.Parse(value); break;
          case "1-26候选的横向偏离"           : nud16.Value                  = int.Parse(value); break;
          case "候选的高度间距"               : nud17.Value                  = int.Parse(value); break;
          case "候选的宽度间距"               : nud18.Value                  = int.Parse(value); break;
          case "调频权重最小码长"             : nud2.Value                   = int.Parse(value); break;
          case "双检索历史重数"               : nud3.Value                   = int.Parse(value); break;
          case "唯一上屏最小码长"             : nud4.Value                   = int.Parse(value); break;
          case "GDI字体加粗权值"              : nud14_Copy.Value             = int.Parse(value); break;
          case "光标色"                                      : color_label_2.Background    = RGBStringToColor(value); break;
          case "分隔线色"                                    : color_label_3.Background    = RGBStringToColor(value); break;
          case "候选选中色"                                  : color_label_6.Background    = RGBStringToColor(value); break;
          case "要码长顶屏吗？"                              : checkBox1_Copy111.IsChecked = IsTrueOrFalse   (value); break;
          case "要数字顶屏吗？"                              : checkBox1_Copy7.IsChecked   = IsTrueOrFalse   (value); break;
          case "要标点顶屏吗？"                              : checkBox1_Copy6.IsChecked   = IsTrueOrFalse   (value); break;
          case "要唯一上屏吗？"                              : checkBox1_Copy5.IsChecked   = IsTrueOrFalse   (value); break;
          case "嵌入下划线色"                                : color_label_1.Background    = RGBStringToColor(value); break;
          case "候选窗口边框色"                              : color_label_4.Background    = RGBStringToColor(value); break;
          case "候选选中字体色"                              : color_label_7.Background    = RGBStringToColor(value); break;
          case "要显示背景图吗？"                            : checkBox_Copy42.IsChecked   = IsTrueOrFalse   (value); break;
          case "要启用双检索吗？"                            : checkBox1_Copy3.IsChecked   = IsTrueOrFalse   (value); break;
          case "关联中文标点吗？"                            : checkBox_Copy31.IsChecked   = IsTrueOrFalse   (value); break;
          case "无候选要清屏吗？"                            : checkBox_Copy20.IsChecked   = IsTrueOrFalse   (value); break;
          case "要使用嵌入模式吗？"                          : checkBox_Copy44.IsChecked   = IsTrueOrFalse   (value); break;
          case "要开启词语联想吗？"                          : checkBox_Copy4.IsChecked    = IsTrueOrFalse   (value); break;
          case "是键首字根码表吗？"                          : checkBox1_Copy55.IsChecked  = IsTrueOrFalse   (value); break;
          case "要显示键首字根吗？"                          : checkBox_Copy34.IsChecked   = IsTrueOrFalse   (value); break;
          case "超过码长要清屏吗？"                          : checkBox_Copy19.IsChecked   = IsTrueOrFalse   (value); break;
          case "要逐码提示检索吗？"                          : checkBox_Copy.IsChecked     = IsTrueOrFalse   (value); break;
          case "要显示逐码提示吗？"                          : checkBox.IsChecked          = IsTrueOrFalse   (value); break;
          case "要显示反查提示吗？"                          : checkBox1.IsChecked         = IsTrueOrFalse   (value); break;
          case "要启用单字模式吗？"                          : checkBox1_Copy.IsChecked    = IsTrueOrFalse   (value); break;
          case "GDI字体要倾斜吗？"                           : checkBox_Copy314.IsChecked  = IsTrueOrFalse   (value); break;
          case "要启用右Ctrl键吗？"                          : checkBox_Copy16.IsChecked   = IsTrueOrFalse   (value); break;
          case "要启用左Ctrl键吗？"                          : checkBox_Copy15.IsChecked   = IsTrueOrFalse   (value); break;
          case "要启用左Shift键吗？"                         : checkBox_Copy13.IsChecked   = IsTrueOrFalse   (value); break;
          case "要启用右Shift键吗？"                         : checkBox_Copy14.IsChecked   = IsTrueOrFalse   (value); break;
          case "GDI+字体要下划线吗？"                        : checkBox19.IsChecked        = IsTrueOrFalse   (value); break;
          case "GDI+字体要删除线吗？"                        : checkBox20.IsChecked        = IsTrueOrFalse   (value); break;
          case "窗口四个角要圆角吗？"                        : hxc_checkBox.IsChecked      = IsTrueOrFalse   (value); break;
          case "码表标签要左对齐吗？"                        : checkBox_Copy39.IsChecked   = IsTrueOrFalse   (value); break;
          case "过渡态按1要上屏1吗？"                        : checkBox_Copy30.IsChecked   = IsTrueOrFalse   (value); break;
          case "Shift键上屏编码串吗？"                       : checkBox_Copy23.IsChecked   = IsTrueOrFalse   (value); break;
          case "Enter键上屏编码串吗？"                       : checkBox_Copy26.IsChecked   = IsTrueOrFalse   (value); break;
          case "要启用Ctrl+Space键吗？"                      : checkBox_Copy17.IsChecked   = IsTrueOrFalse   (value); break;
          case "要开启Ctrl键清联想吗？"                      : checkBox_Copy10.IsChecked   = IsTrueOrFalse   (value); break;
          case "选中项四个角要圆角吗？"                      : hxcbj_checkBox.IsChecked    = IsTrueOrFalse   (value); break;
          case "要启用ESC键自动造词吗？"                     : checkBox_Copy3.IsChecked    = IsTrueOrFalse   (value); break;
          case "词语联想只是匹配首位吗？"                    : checkBox_Copy6.IsChecked    = IsTrueOrFalse   (value); break;
          case "高度宽度要完全自动调整吗？"                  : checkBox_Copy40.IsChecked   = IsTrueOrFalse   (value); break;
          case "中英切换要显示提示窗口吗？"                  : checkBox_Copy11.IsChecked   = IsTrueOrFalse   (value); break;
          case "双检索时编码要完全匹配吗？"                  : checkBox1_Copy4.IsChecked   = IsTrueOrFalse   (value); break;
          case "词语联想要显示词语全部吗？"                  : checkBox_Copy5.IsChecked    = IsTrueOrFalse   (value); break;
          case "上屏后候选窗口要立即消失吗？"                : checkBox_Copy18.IsChecked   = IsTrueOrFalse   (value); break;
          case "要启用最大码长无候选清屏吗？"                : checkBox_Copy21.IsChecked   = IsTrueOrFalse   (value); break;
          case "无候选敲空格要上屏编码串吗？"                : checkBox_Copy22.IsChecked   = IsTrueOrFalse   (value); break;
          case "词语联想时标点顶屏要起作用吗？"              : checkBox_Copy7.IsChecked    = IsTrueOrFalse   (value); break;
          case "候选词条要按码长短优先排序吗？"              : checkBox_Copy2.IsChecked    = IsTrueOrFalse   (value); break;
          case "要启用上屏自动增加调频权重吗？"              : checkBox1_Copy1.IsChecked   = IsTrueOrFalse   (value); break;
          case "Space键要上屏临时英文编码串吗？"             : checkBox_Copy25.IsChecked   = IsTrueOrFalse   (value); break;
          case "Enter键上屏并使首个字母大写吗？"             : checkBox_Copy27.IsChecked   = IsTrueOrFalse   (value); break;
          case "候选词条要按调频权重检索排序吗？"            : checkBox1_Copy2.IsChecked   = IsTrueOrFalse   (value); break;
          case "竖向候选窗口选中背景色要等宽吗？"            : checkBox_Copy41.IsChecked   = IsTrueOrFalse   (value); break;
          case "候选窗口候选从上到下排列要锁定吗？"          : checkBox_Copy45.IsChecked   = IsTrueOrFalse   (value); break;
          case "无临时快键时,也要显示主码表标识吗？"         : checkBox_Copy32.IsChecked   = IsTrueOrFalse   (value); break;
          case "从中文切换到英文时,要上屏编码串吗？"         : checkBox_Copy12.IsChecked   = IsTrueOrFalse   (value); break;
          case "Shift键+字母键要进入临时英文长句态吗？"      : checkBox_Copy24.IsChecked   = IsTrueOrFalse   (value); break;
          case "要启用上屏自动增加调频权重直接到顶吗？"      : checkBox_Copy1.IsChecked    = IsTrueOrFalse   (value); break;
          case "Backspace键一次性删除前次上屏的内容吗？"     : checkBox_Copy28.IsChecked   = IsTrueOrFalse   (value); break;
          case "标点或数字顶屏时,若是引导键,要继续引导吗？"  : checkBox1_Copy8.IsChecked   = IsTrueOrFalse   (value); break;
          case "前次上屏的是数字再上屏句号*要转成点号*吗？"  : checkBox_Copy29.IsChecked   = IsTrueOrFalse   (value); break;
          case "候选窗口候选排列方向模式>1时要隐藏编码串行吗？"      
                                                             : checkBox_Copy38.IsChecked   = IsTrueOrFalse   (value); break;
          case "候选窗口候选从上到下排列锁定的情况下要使编码区离光标最近吗？"
                                                             : checkBox_Copy46.IsChecked   = IsTrueOrFalse   (value); break;
        }
      }
    }

    private void 背景底色(string value)
    {
      if (value == "")
      {
        hxcds_checkBox.IsChecked = true;
        color_label_5.Background = new SolidColorBrush(Color.FromArgb(255, 255,255, 255));
        bkColor                  = new SolidColorBrush(Color.FromArgb(255, 255,255, 255));
      }
      else
      {
        hxcds_checkBox.IsChecked = false;
        color_label_5.Background = RGBStringToColor(value);
        bkColor                  = RGBStringToColor(value);
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
      comboBox1.SelectedIndex     = value switch
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
            break;
        case "1":
            color_label_9.Background = RGBStringToColor(value);
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
          radioButton.IsChecked  = true; break;
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
    try
    {
      modifiedConfig = Regex.Replace(modifiedConfig, $"《{key}=.*?》", $"《{key}={value}》");
    }
    catch (Exception e)
    {
      Console.WriteLine(e.Message);
      Console.WriteLine($"《{key}={value}》");
    }
    }
    // 读取控件属性值
    private void GetControlsValue()
    {
      ReplaceConfig("顶功规则", 取顶功规则());
      ReplaceConfig("背景底色", 取背景底色());
      ReplaceConfig("候选字体色串", bgString);
      ReplaceConfig("键首字根", textBox125.Text);
      ReplaceConfig("D2D字体样式", 取D2D字体样式());
      ReplaceConfig("码表标签", textBox_Copy15.Text);
      ReplaceConfig("候选序号", textBox_Copy67.Text);
      ReplaceConfig("小键盘码元", textBox_Copy5.Text);
      ReplaceConfig("字体名称", textBox_Copy145.Text);
      ReplaceConfig("最大码长", nud1.Value.ToString());
      ReplaceConfig("主码表标识", textBox_Copy22.Text);
      ReplaceConfig("副码表标识", textBox_Copy23.Text);
      ReplaceConfig("候选个数", nud15.Value.ToString());
      ReplaceConfig("大键盘码元", textBox_Copy677.Text);
      ReplaceConfig("码表引导快键0", textBox_Copy.Text);
      ReplaceConfig("码表临时快键", textBox_Copy19.Text);
      ReplaceConfig("码表引导快键1", textBox_Copy12.Text);
      ReplaceConfig("D2D回退字体集", textBox_Copy10.Text);
      ReplaceConfig("顶功小集码元", textBox_Copy675.Text);
      ReplaceConfig("码表引导快键2", textBox_Copy16.Text);
      ReplaceConfig("候选快键字符串", textBox_Copy66.Text);
      ReplaceConfig("大小键盘万能码元", textBox_Copy6.Text);
      ReplaceConfig("大键盘中文标点串", textBox_Copy68.Text);
      ReplaceConfig("双检索历史重数", nud3.Value.ToString());
      ReplaceConfig("候选窗口绘制模式", 取候选窗口绘制模式());
      ReplaceConfig("重复上屏码元字符串", textBox_Copy1.Text);
      ReplaceConfig("词语联想检索范围", 取词语联想检索范围());
      ReplaceConfig("候选的高度间距", nud17.Value.ToString());
      ReplaceConfig("候选的宽度间距", nud18.Value.ToString());
      ReplaceConfig("D2D字体加粗权值", nud14.Value.ToString());
      ReplaceConfig("调频权重最小码长", nud2.Value.ToString());
      ReplaceConfig("唯一上屏最小码长", nud4.Value.ToString());
      ReplaceConfig("码表临时快键编码名", textBox_Copy20.Text);
      ReplaceConfig("码表引导快键0编码名0", textBox_Copy9.Text);
      ReplaceConfig("码表引导快键0编码名1", textBox_Copy11.Text);
      ReplaceConfig("码表引导快键1编码名0", textBox_Copy13.Text);
      ReplaceConfig("码表引导快键1编码名1", textBox_Copy14.Text);
      ReplaceConfig("码表引导快键2编码名0", textBox_Copy17.Text);
      ReplaceConfig("码表引导快键2编码名1", textBox_Copy18.Text);
      ReplaceConfig("1-26候选的横向偏离", nud16.Value.ToString());
      ReplaceConfig("编码或候选嵌入模式", 取编码或候选嵌入模式());
      ReplaceConfig("候选窗口边框线宽度", nud13.Value.ToString());
      ReplaceConfig("GDI字体加粗权值", nud14_Copy.Value.ToString());
      ReplaceConfig("非编码串首位的大键盘码元", textBox_Copy7.Text);
      ReplaceConfig("非编码串首位的小键盘码元", textBox_Copy8.Text);
      ReplaceConfig("窗口四个角的圆角半径", nud11.Value.ToString());
      ReplaceConfig("选中项四个角的圆角半径", nud12.Value.ToString());
      ReplaceConfig("大键盘按下Shift的中文标点串", textBox_Copy69.Text);
      ReplaceConfig("往下翻页大键盘英文符号编码串", textBox_Copy2.Text);
      ReplaceConfig("往上翻页小键盘英文符号编码串", textBox_Copy3.Text);
      ReplaceConfig("往下翻页小键盘英文符号编码串", textBox_Copy4.Text);
      ReplaceConfig("往上翻页大键盘英文符号编码串", textBox_Copy21.Text);
      ReplaceConfig("词语联想上屏字符串长度", 取词语联想上屏字符串长度());
      ReplaceConfig("光标色", HexToRgb(color_label_2.Background.ToString()));
      ReplaceConfig("候选窗口候选排列方向模式", 取候选窗口候选排列方向模式());
      ReplaceConfig("要显示逐码提示吗？", 要或不要((bool)checkBox.IsChecked));
      ReplaceConfig("分隔线色", HexToRgb(color_label_3.Background.ToString()));
      ReplaceConfig("要显示反查提示吗？", 要或不要((bool)checkBox1.IsChecked));
      ReplaceConfig("要数字顶屏吗？", 要或不要((bool)checkBox1_Copy7.IsChecked));
      ReplaceConfig("要标点顶屏吗？", 要或不要((bool)checkBox1_Copy6.IsChecked));
      ReplaceConfig("要唯一上屏吗？", 要或不要((bool)checkBox1_Copy5.IsChecked));
      ReplaceConfig("码表标签显示模式", comboBox1_Copy.SelectedIndex.ToString());
      ReplaceConfig("候选选中色", HexToRgb(color_label_6.Background.ToString()));
      ReplaceConfig("要逐码提示检索吗？", 要或不要((bool)checkBox_Copy.IsChecked));
      ReplaceConfig("要显示背景图吗？", 要或不要((bool)checkBox_Copy42.IsChecked));
      ReplaceConfig("无候选要清屏吗？", 要或不要((bool)checkBox_Copy20.IsChecked));
      ReplaceConfig("要启用双检索吗？", 要或不要((bool)checkBox1_Copy3.IsChecked));
      ReplaceConfig("要码长顶屏吗？", 要或不要((bool)checkBox1_Copy111.IsChecked));
      ReplaceConfig("嵌入下划线色", HexToRgb(color_label_1.Background.ToString()));
      ReplaceConfig("关联中文标点吗？", 要或不要((bool)checkBox_Copy31.IsChecked));
      ReplaceConfig("要启用单字模式吗？", 要或不要((bool)checkBox1_Copy.IsChecked));
      ReplaceConfig("窗口四个角要圆角吗？", 要或不要((bool)hxc_checkBox.IsChecked));
      ReplaceConfig("要开启词语联想吗？", 要或不要((bool)checkBox_Copy4.IsChecked));
      ReplaceConfig("要启用左Ctrl键吗？", 要或不要((bool)checkBox_Copy15.IsChecked));
      ReplaceConfig("要启用右Ctrl键吗？", 要或不要((bool)checkBox_Copy16.IsChecked));
      ReplaceConfig("要显示键首字根吗？", 要或不要((bool)checkBox_Copy34.IsChecked));
      ReplaceConfig("候选窗口边框色", HexToRgb(color_label_4.Background.ToString()));
      ReplaceConfig("候选选中字体色", HexToRgb(color_label_7.Background.ToString()));
      ReplaceConfig("超过码长要清屏吗？", 要或不要((bool)checkBox_Copy19.IsChecked));
      ReplaceConfig("GDI字体要倾斜吗？", 要或不要((bool)checkBox_Copy314.IsChecked));
      ReplaceConfig("要使用嵌入模式吗？", 要或不要((bool)checkBox_Copy44.IsChecked));
      ReplaceConfig("要启用左Shift键吗？", 要或不要((bool)checkBox_Copy13.IsChecked));
      ReplaceConfig("要启用右Shift键吗？", 要或不要((bool)checkBox_Copy14.IsChecked));
      ReplaceConfig("是键首字根码表吗？", 是或不是((bool)checkBox1_Copy55.IsChecked));
      ReplaceConfig("码表标签要左对齐吗？", 要或不要((bool)checkBox_Copy39.IsChecked));
      ReplaceConfig("过渡态按1要上屏1吗？", 要或不要((bool)checkBox_Copy30.IsChecked));
      ReplaceConfig("Shift键上屏编码串吗？", 要或不要((bool)checkBox_Copy23.IsChecked));
      ReplaceConfig("Enter键上屏编码串吗？", 要或不要((bool)checkBox_Copy26.IsChecked));
      ReplaceConfig("选中项四个角要圆角吗？", 要或不要((bool)hxcbj_checkBox.IsChecked));
      ReplaceConfig("要启用ESC键自动造词吗？", 要或不要((bool)checkBox_Copy3.IsChecked));
      ReplaceConfig("要开启Ctrl键清联想吗？", 要或不要((bool)checkBox_Copy10.IsChecked));
      ReplaceConfig("词语联想只是匹配首位吗？", 是或不是((bool)checkBox_Copy6.IsChecked));
      ReplaceConfig("词语联想要显示词语全部吗？", 要或不要((bool)checkBox_Copy5.IsChecked));
      ReplaceConfig("中英切换要显示提示窗口吗？", 要或不要((bool)checkBox_Copy11.IsChecked));
      ReplaceConfig("高度宽度要完全自动调整吗？", 要或不要((bool)checkBox_Copy40.IsChecked));
      ReplaceConfig("双检索时编码要完全匹配吗？", 要或不要((bool)checkBox1_Copy4.IsChecked));
      ReplaceConfig("要启用最大码长无候选清屏吗？", 要或不要((bool)checkBox_Copy21.IsChecked));
      ReplaceConfig("上屏后候选窗口要立即消失吗？", 要或不要((bool)checkBox_Copy18.IsChecked));
      ReplaceConfig("无候选敲空格要上屏编码串吗？", 要或不要((bool)checkBox_Copy22.IsChecked));
      ReplaceConfig("候选词条要按码长短优先排序吗？", 要或不要((bool)checkBox_Copy2.IsChecked));
      ReplaceConfig("词语联想时标点顶屏要起作用吗？", 要或不要((bool)checkBox_Copy7.IsChecked));
      ReplaceConfig("要启用上屏自动增加调频权重吗？", 要或不要((bool)checkBox1_Copy1.IsChecked));
      ReplaceConfig("Space键要上屏临时英文编码串吗？", 要或不要((bool)checkBox_Copy25.IsChecked));
      ReplaceConfig("Enter键上屏并使首个字母大写吗？", 要或不要((bool)checkBox_Copy27.IsChecked));
      ReplaceConfig("竖向候选窗口选中背景色要等宽吗？", 要或不要((bool)checkBox_Copy41.IsChecked));
      ReplaceConfig("候选词条要按调频权重检索排序吗？", 要或不要((bool)checkBox1_Copy2.IsChecked));
      ReplaceConfig("候选窗口候选从上到下排列要锁定吗？", 要或不要((bool)checkBox_Copy45.IsChecked));
      ReplaceConfig("从中文切换到英文时,要上屏编码串吗？", 要或不要((bool)checkBox_Copy12.IsChecked));
      ReplaceConfig("要启用上屏自动增加调频权重直接到顶吗？", 要或不要((bool)checkBox_Copy1.IsChecked));
      ReplaceConfig("Backspace键一次性删除前次上屏的内容吗？", 要或不要((bool)checkBox_Copy28.IsChecked));
      ReplaceConfig("无临时快键时,也要显示主码表标识吗？", 要或不要((bool)checkBox_Copy32.IsChecked));
      ReplaceConfig("标点或数字顶屏时,若是引导键,要继续引导吗？", 要或不要((bool)checkBox1_Copy8.IsChecked));
      ReplaceConfig("候选窗口候选排列方向模式>1时要隐藏编码串行吗？", 要或不要((bool)checkBox_Copy38.IsChecked));
      ReplaceConfig("候选窗口候选从上到下排列锁定的情况下要使编码区离光标最近吗？", 要或不要((bool)checkBox_Copy46.IsChecked));

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

    #region 页面切换


    private void 方案设置页面()
    {
      Grid1.Width      = 750;
      Grid2.Width      = 0;
      Grid3.Width      = 0;
      Grid1.Visibility = Visibility.Visible;
      Grid2.Visibility = Visibility.Hidden;
      Grid3.Visibility = Visibility.Hidden;
    }
    private void 全局设置页面()
    {
      Grid1.Width      = 0;
      Grid2.Width      = 750;
      Grid3.Width      = 0;
      Grid1.Visibility = Visibility.Hidden;
      Grid2.Visibility = Visibility.Visible;
      Grid3.Visibility = Visibility.Hidden;
    }
    private void 帮助页面()
    {
      Grid1.Width      = 0;
      Grid2.Width      = 0;
      Grid3.Width      = 750;
      Grid1.Visibility = Visibility.Hidden;
      Grid2.Visibility = Visibility.Hidden;
      Grid3.Visibility = Visibility.Visible;
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
        case "码表设置":
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
      if (n == 1)
      {
        GroupBox groupBox = FindGroupBox(content, stackPanel1);
        groupBox.BringIntoView();
      }
      if (n == 2)
      {
        GroupBox groupBox = FindGroupBox(content, stackPanel2);
        groupBox.BringIntoView();
      }
      if (n == 3)
      {
        GroupBox groupBox = FindGroupBox(content, stackPanel3);
        groupBox.BringIntoView();
      }
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


    private void GroupBox_MouseEnter(object sender, MouseEventArgs e)
    {
      if (sender is not GroupBox groupBox) return;

      string textFromSource = groupBox.Header.ToString();

      foreach (var child in stackPanel.Children)
      {
        if (child is RadioButton radioButton)
        {
          var textBlock = FindChildTextBlock(radioButton);
          if (textBlock != null && textBlock.Text.Equals(textFromSource, StringComparison.OrdinalIgnoreCase))
          {
            radioButton.IsChecked = true;
          }
          else
          {
            radioButton.IsChecked = false;
          }
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

    #region 配色相关

    // 画布 canvas 点击取色
    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      Canvas canvas;
      Thumb thumb;

      if (Grid1.Visibility == Visibility.Visible)
      {
        canvas = canvas1;
        thumb  = thumb1;
      }
      else
      {
        canvas = canvas2;
        thumb  = thumb2;
      }
      var canvasPosition  = e.GetPosition(canvas);
      double newLeft      = canvasPosition.X - thumb.ActualWidth / 2;
      double newTop       = canvasPosition.Y - thumb.ActualHeight / 2;

      double canvasRight  = canvas.ActualWidth - thumb.ActualWidth;
      double canvasBottom = canvas.ActualHeight - thumb.ActualHeight;

      newLeft = Math.Max(0, Math.Min(newLeft, canvasRight));
      newTop = Math.Max(0, Math.Min(newTop, canvasBottom));

      Canvas.SetLeft(thumb, newLeft);
      Canvas.SetTop(thumb, newTop);
      GetAreaColor();
    }

    // 画布 canvas 的 thumb 移动取色
    private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
      Canvas canvas;
      Thumb thumb;

      if (Grid1.Visibility == Visibility.Visible)
      {
        canvas = canvas1;
        thumb = thumb1;
      }
      else
      {
        canvas = canvas2;
        thumb = thumb2;
      }
      double newLeft = Canvas.GetLeft(thumb) + e.HorizontalChange;
      double newTop  = Canvas.GetTop(thumb) + e.VerticalChange;

      // 计算画布边界
      double canvasRight  = canvas.ActualWidth - thumb.ActualWidth + 5;
      double canvasBottom = canvas.ActualHeight - thumb.ActualHeight + 5;

      // 限制 Thumb 在画布内部移动
      newLeft = Math.Max(-6, Math.Min(newLeft, canvasRight));
      newTop  = Math.Max(-6, Math.Min(newTop, canvasBottom));

      Canvas.SetLeft(thumb, newLeft);
      Canvas.SetTop(thumb, newTop);
      GetAreaColor();
    }

    // 获取当前选中颜色
    void GetAreaColor()
    {
      Canvas canvas;
      Thumb thumb;
      //Label color_label;
      RGBTextBox rgbTextBox;

      if (Grid1.Visibility == Visibility.Visible)
      {
        canvas      = canvas1;
        thumb       = thumb1;
        //color_label = color_label_10;
        rgbTextBox  = rgb1;
      }
      else
      {
        canvas      = canvas2;
        thumb       = thumb2;
        //color_label = color_label_11;
        rgbTextBox  = rgb2;
      }

      Point? thumbPosition = thumb.TranslatePoint(new Point(thumb.ActualWidth / 2, thumb.ActualHeight / 2), canvas);

      if (thumbPosition.HasValue && thumbPosition.Value.X >= 0 && thumbPosition.Value.X < Bitmap.PixelWidth && thumbPosition.Value.Y >= 0 && thumbPosition.Value.Y < Bitmap.PixelHeight)
      {
        int xCoordinate = (int)thumbPosition.Value.X;
        int yCoordinate = (int)thumbPosition.Value.Y;

        int stride = Bitmap.PixelWidth * (Bitmap.Format.BitsPerPixel / 8);
        byte[] pixels = new byte[Bitmap.PixelHeight * stride];
        Bitmap.CopyPixels(new Int32Rect(0, 0, Bitmap.PixelWidth, Bitmap.PixelHeight), pixels, stride, 0);

        int pixelIndex = (yCoordinate * stride) + (xCoordinate * (Bitmap.Format.BitsPerPixel / 8));
        Color color = Color.FromArgb(pixels[pixelIndex + 3], pixels[pixelIndex + 2], pixels[pixelIndex + 1], pixels[pixelIndex]);

        var c_color = new SolidColorBrush(color);
        rgbTextBox.RGBText = $"({color.R}, {color.G}, {color.B})";
        // 更新Thumb的BorderBrush，取反色
        var f_color = new SolidColorBrush(Color.FromRgb((byte)(255 - color.R), (byte)(255 - color.G), (byte)(255 - color.B)));
        thumb.BorderBrush = f_color; 
        // 更新对应标签的背景颜色
        SetColorLableColor(c_color);
      }

    }

    // 更新对应标签的背景颜色
    private void SetColorLableColor(SolidColorBrush c_color)
    {
      Label[] colorLabels = { color_label_1, color_label_2, color_label_3, color_label_4, color_label_5, color_label_6, color_label_7, color_label_8, color_label_9, color_label_10, color_label_11 };

      for (int i = 1; i <= colorLabels.Length; i++)
        if (i == select_color_label_num){
          colorLabels[i - 1].Background = c_color;
          var currentColor = ((SolidColorBrush)colorLabels[i - 1].Background).Color;
          // 计算反色
          var invertedColor = Color.FromArgb(255, (byte)~currentColor.R, (byte)~currentColor.G, (byte)~currentColor.B);
          colorLabels[i - 1].BorderBrush = new SolidColorBrush(invertedColor);
        }

      if (select_color_label_num == 10 || select_color_label_num == 11)
        toolTipTextBlock.Foreground = c_color;
    }

    private void RGB_ValueChanged(object sender, RoutedPropertyChangedEventArgs<string> e)
    {
      SetColorLableColor(RGBStringToColor(rgb1.RGBText));
    }


    // 读取 Json 文件
    void LoadJson()
    {
      if (File.Exists(schemeFilePath))
      {
        // 读取整个文件内容,将JSON字符串反序列化为对象
        string jsonString = File.ReadAllText(schemeFilePath);
        ColorSchemesCollection colorSchemesJson = JsonConvert.DeserializeObject<ColorSchemesCollection>(jsonString);
        配色方案 = colorSchemesJson.配色方案;

        foreach (var scheme in 配色方案)
        {
          colorSchemeListBox.Items.Add(scheme.名称);
        }
      }
      else
      {
        配色方案.Add(colorScheme);
        string jsonString = JsonConvert.SerializeObject(new { 配色方案 }, Formatting.Indented);
        File.WriteAllText(schemeFilePath, jsonString);

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
      int width  = 170;
      int height = 170;
      double hue = hue_slider.Value / 360; // Hue 值现在来自滑动条
      Bitmap.Lock();
      IntPtr backBuffer = Bitmap.BackBuffer;
      int stride        = Bitmap.BackBufferStride;

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

      Bitmap.AddDirtyRect(new Int32Rect(0, 0, 170,170));
      Bitmap.Unlock();
    }


    // Hue_slider 值改变事件
    private void Hue_slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      UpdateBitmap();
      GetAreaColor();
    }

    // Hue_slider 滚轮事件
    private void Hue_slider_MouseWheel(object sender, MouseWheelEventArgs e)
    {
      int step = 5;
      if (Keyboard.Modifiers == ModifierKeys.Control) step *= -10;

      if (e.Delta > 0 && hue_slider.Value + step <= hue_slider.Maximum)
        hue_slider.Value += step;
      else if (e.Delta < 0 && hue_slider.Value - step >= hue_slider.Minimum)
        hue_slider.Value -= step;


      // 阻止滚轮事件继续向上冒泡
      e.Handled = true;
    }

    private void Hue_slider_MouseUp(object sender, MouseButtonEventArgs e)
    {
      var slider = sender as Slider;
      if (slider != null)
      {
        var point = e.GetPosition(slider);
        // 计算点击位置相对于Slider的比例
        double newValue = (point.Y / slider.ActualHeight) * slider.Maximum;
        slider.Value = slider.Maximum - newValue;  // 反转值，因为Slider垂直方向是从上到下
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
        r = g = b = (byte)(v * 255);
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
        throw new FormatException("RGB 取值：0-255");
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

    // 显示颜色的 label 鼠标进入事件
    private void Color_label_MouseEnter(object sender, MouseEventArgs e)
    {

      SolidColorBrush color1 = new((Color)ColorConverter.ConvertFromString("#FF000000"));  // 黑色
      SolidColorBrush color2 = new((Color)ColorConverter.ConvertFromString("#FFFF0000"));  // 红色
      color_label_001.Foreground = color1;
      color_label_002.Foreground = color1;
      color_label_003.Foreground = color1;
      color_label_004.Foreground = color1;
      color_label_005.Foreground = color1;
      color_label_006.Foreground = color1;
      color_label_007.Foreground = color1;
      color_label_008.Foreground = color1;
      color_label_009.Foreground = color1;
      color_label_010.Foreground = color1;
      color_label_011.Foreground = color1;

      Label label = sender as Label;
      switch (label.Name)
      {
        case "color_label_1": select_color_label_num  = 1 ; color_label_001.Foreground = color2; break;
        case "color_label_2": select_color_label_num  = 2 ; color_label_002.Foreground = color2; break;
        case "color_label_3": select_color_label_num  = 3 ; color_label_003.Foreground = color2; break;
        case "color_label_4": select_color_label_num  = 4 ; color_label_004.Foreground = color2; break;
        case "color_label_5": select_color_label_num  = 5 ; color_label_005.Foreground = color2; break;
        case "color_label_6": select_color_label_num  = 6 ; color_label_006.Foreground = color2; break;
        case "color_label_7": select_color_label_num  = 7 ; color_label_007.Foreground = color2; break;
        case "color_label_8": select_color_label_num  = 8 ; color_label_008.Foreground = color2; break;
        case "color_label_9": select_color_label_num  = 9 ; color_label_009.Foreground = color2; break;
        case "color_label_10": select_color_label_num = 10; color_label_010.Foreground = color2; break;
        case "color_label_11": select_color_label_num = 11; color_label_011.Foreground = color2; break;
      }
      var currentColor = ((SolidColorBrush)label.Background).Color;
      // 计算反色
      var invertedColor = Color.FromArgb(255, (byte)~currentColor.R, (byte)~currentColor.G, (byte)~currentColor.B);
      label.BorderThickness = new Thickness(3);
      label.BorderBrush = new SolidColorBrush(invertedColor);
      var hex = RemoveChars(label.Background.ToString(), 2);
      var rgb = HexToRgb(hex);
      rgb1.RGBText = rgb;
    }

    // 显示颜色的 label 鼠标离开事件
    private void Color_label_MouseLeave(object sender, MouseEventArgs e)
    {
      Label label = sender as Label;
      label.BorderThickness = new Thickness(2);
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
        hxk_border.CornerRadius = new CornerRadius(nud11.Value);
      else
        hxk_border.CornerRadius = new CornerRadius(0);
    }



    // 配色列表双击事件
    private void ColorSchemeListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (e.ChangedButton == MouseButton.Left && colorSchemeListBox.SelectedItem != null)
      {
        var colorScheme           = 配色方案[colorSchemeListBox.SelectedIndex];
        checkBox_Copy42.IsChecked = colorScheme.显示背景图;
        hxc_checkBox.IsChecked    = colorScheme.显示候选窗圆角;
        hxcbj_checkBox.IsChecked  = colorScheme.显示选中项背景圆角;
        nud11.Value               = colorScheme.候选窗圆角;
        nud12.Value               = colorScheme.选中项圆角;
        nud13.Value               = colorScheme.边框线宽;
        color_label_1.Background  = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorScheme.下划线色));
        color_label_2.Background  = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorScheme.光标色));
        color_label_3.Background  = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorScheme.分隔线色));
        color_label_4.Background  = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorScheme.窗口边框色));

        if (colorScheme.窗背景底色 == "")
        {
          color_label_5.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFFFF"));
          bkColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFFFF"));
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
          color_scheme_name_textBox.Text  =  "";
        if (saveButton.Content.ToString() == "修改配色")
          color_scheme_name_textBox.Text  =  colorSchemeListBox.SelectedItem.ToString();
      }
    }

    // 新建配色方案
    private void MenuItem_Click_1(object sender, RoutedEventArgs e)
    {
      saveButton.Content                   = "保存配色";
      saveButton.Visibility                = Visibility.Visible;
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
      saveButton.Content                   = "修改配色";
      saveButton.Visibility                = Visibility.Visible;
      color_scheme_name_textBox.Visibility = Visibility.Visible;
      color_scheme_name_textBox.Text      += colorSchemeListBox.SelectedItem.ToString();
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
        string jsonString = JsonConvert.SerializeObject(new { 配色方案 }, Formatting.Indented);
        File.WriteAllText(schemeFilePath, jsonString);

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
        名称               = name,
        候选窗圆角         = nud11.Value,
        选中项圆角         = nud12.Value,
        边框线宽           = nud13.Value,
        显示背景图         = (bool)checkBox_Copy42.IsChecked,
        显示候选窗圆角     = (bool)hxc_checkBox.IsChecked,
        显示选中项背景圆角 = (bool)hxcbj_checkBox.IsChecked,
        窗背景底色         = hxcds_checkBox.IsChecked ==true ? "" : 
                             RemoveChars(color_label_5.Background.ToString(), 2),
        下划线色           = RemoveChars(color_label_1.Background.ToString(), 2),
        光标色             = RemoveChars(color_label_2.Background.ToString(), 2),
        分隔线色           = RemoveChars(color_label_3.Background.ToString(), 2),
        窗口边框色         = RemoveChars(color_label_4.Background.ToString(), 2),
        选中背景色         = RemoveChars(color_label_6.Background.ToString(), 2),
        选中字体色         = RemoveChars(color_label_7.Background.ToString(), 2),
        编码字体色         = RemoveChars(color_label_8.Background.ToString(), 2),
        候选字色           = RemoveChars(color_label_9.Background.ToString(), 2),
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
        配色方案.Insert(0, colorScheme);
        colorSchemeListBox.Items.Insert(0, name);
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
      string jsonString = JsonConvert.SerializeObject(new { 配色方案 }, Formatting.Indented);
      File.WriteAllText(schemeFilePath, jsonString);
    }

    private void Button3_Copy_Click(object sender, RoutedEventArgs e)
    {
      var selectedFontName = SelectFontName();
      if (selectedFontName != null) {
        Button btn = sender as Button;
        switch (btn.Name)
        {
          case "button3_Copy": textBox_Copy145.Text = selectedFontName.ToString(); break;
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
      color_label_5.Visibility = Visibility.Hidden;
      bkColor = (SolidColorBrush)color_label_5.Background;
      color_label_005.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF000000"));  // 黑色
      select_color_label_num = 0;
    }

    private void Hxcds_checkBox_Unchecked(object sender, RoutedEventArgs e)
    {
      color_label_5.Visibility = Visibility.Visible;
      color_label_5.Background = bkColor;
    }

    private void Hxcds_checkBox_Click(object sender, RoutedEventArgs e)
    {
      if (hxcds_checkBox.IsChecked == false)
      {
        color_label_5.Visibility = Visibility.Visible;
        //color_label_5.Background = new SolidColorBrush(Color.FromArgb(255, 255,255, 255));
        color_label_5.Background = bkColor;

      }
      else
      {
        color_label_5.Visibility = Visibility.Hidden;
        color_label_5.Background = new SolidColorBrush(Color.FromArgb(255, 255,255, 255));
      }
    }
    #endregion

    #region 托盘图标

    // 加载托盘图标
    //private void InitIcon(){
    //  // 阻止默认的关闭行为
    //  //this.Closing += MainWindow_Closing;

    //  notifyIcon = new NotifyIcon
    //  {
    //    Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath),
    //    Tag = this,
    //    Visible = true
    //};

    //  MenuItem menuItem1 = new("显示配置窗口"); menuItem1.Click += MenuItem1_Click;
    //  MenuItem menuItem5 = new("显示/隐藏壮态条"); menuItem5.Click += MenuItem5_Click;
    //  MenuItem menuItem4 = new("重启服务端"); menuItem4.Click += MenuItem4_Click;
    //  MenuItem menuItem2 = new("关于"); menuItem2.Click += MenuItem2_Click;
    //  MenuItem menuItem3 = new("退出"); menuItem3.Click += MenuItem3_Click;



    //  MenuItem[] menuItems = new MenuItem[] {
    //    menuItem1,  //显示配置窗口
    //    //menuItem5,  //显示/隐藏壮态条
    //    //menuItem4,  //重启服务端
    //    //menuItem2,  //关于
    //    menuItem3   //退出
    //  };
    //  notifyIcon.ContextMenu = new System.Windows.Forms.ContextMenu(menuItems);
    //  // 添加托盘图标双击事件处理
    //  notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
    //}

    // 发送Ctrl+F1组合键
    //private void MenuItem5_Click(object sender, EventArgs e)
    //{
    //  SendKeys.SendWait("^(F1)");
    //}

    // 查找并关闭所有名为“KegServer”的进程
    //private void MenuItem4_Click(object sender, EventArgs e)
    //{
    //  foreach (var process in Process.GetProcessesByName("KegServer"))
    //  {
    //    process.Kill();
    //    process.WaitForExit(); // 等待进程关闭
    //  }
    //}

    // 退出
    //private void MenuItem3_Click(object sender, EventArgs e)
    //{
    //  notifyIcon.Dispose();
    //  //this.Close();
    //  ((App)System.Windows.Application.Current).Exit();
    //}

    // 显示主窗口
    //private void MenuItem1_Click(object sender, System.EventArgs e) {
    //  this.Visibility= Visibility.Visible;
    //}

    // 关于
    //private void MenuItem2_Click(object sender, System.EventArgs e)
    //{
    //  MessageBox.Show("本工具用于小科狗码表方案配置", "说明");
    //}

    // 托盘图标双击事件
    //private void NotifyIcon_DoubleClick(object sender, EventArgs e)
    //{
    //  this.Visibility = Visibility.Visible;
    //  this.WindowState = WindowState.Normal;
    //}

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

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      Grid1.Height = this.Height - 50;
      Grid2.Height = this.Height - 50;
      Grid3.Height = this.Height - 50;

    }
    // 确定
    private void Button_Click(object sender, RoutedEventArgs e)
    {
      //if (checkBox2.IsChecked == true)
      //  ((App)System.Windows.Application.Current).Exit();
      //else
      //  this.Visibility = Visibility.Hidden; // 或者使用 Collapsed
      this.Close();
    }

    // 设置窗口高度
    private void Nud22_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
    {
      if (nud22 != null)
      {
        int height = (int)e.NewValue;
        if (height < 500)
          height = 500;
        this.Height = height;
      }
    }

    // 窗口高度写入配置文件
    private void Nud22_LostFocus(object sender, RoutedEventArgs e)
    {
      if (nud22 != null)
        SetValue("window", "height", nud22.Value.ToString());
    }

    private void OpenWebPage_Click(object sender, RoutedEventArgs e)
    {
      System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://github.com/hejie13250/KegConfigTools") { UseShellExecute = true });
    }

    #endregion

    #region 全局设置

    // 数据统计
    //private void Run_button_Click(object sender, RoutedEventArgs e)
    //{
    //  string path = appPath + "\\小科狗统计.exe";
    //  if (File.Exists(path)) Process.Start(path);
    //  else MessageBox.Show($"请将 “小科狗统计.exe” 移到本应用所在目录内！");
    //}

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
      LoadKegTxt("Keg_bak.txt");
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

    // 保存 Keg.txt
    private void SaveKeg(){
      string kegText = ""; // 存放 Keg.txt 文件
      kegText += $"《提示文本的位置={取提示文本的位置()}》\n";
      kegText += $"《提示文本要隐藏吗？={要或不要((bool)checkBox3_Copy.IsChecked)}》\n";
      kegText += $"《提示文本要显示中英以及大小写状态吗？={要或不要((bool)checkBox3_Copy1.IsChecked)}》\n";
      kegText += $"《提示文本中文字体色={HexToRgb(color_label_10.Background.ToString())}》\n";
      kegText += $"《提示文本英文字体色={HexToRgb(color_label_11.Background.ToString())}》\n";
      kegText += $"《提示文本字体大小={nud23.Value}》\n";
      kegText += $"《提示文本字体名称={textBox_Copy24.Text}》\n";
      kegText += $"《打字字数统计等数据是要保存在主程文件夹下吗？={是或不是((bool)checkBox3_Copy3.IsChecked) }》\n";
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
          kegText += $"《自启={item.Value }》\n";

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
        IntPtr hWnd = FindWindow("CKegServer_0", null);
        SendMessageTimeout(hWnd, KWM_UPQJSET, IntPtr.Zero, IntPtr.Zero, flags, timeout, out IntPtr pdwResult);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"操作失败，请重试！");
        Console.WriteLine(ex.Message);
      }
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
        File.Copy(kegFilePath, "Keg_bak.txt", true);
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
          case "提示文本中文字体色": color_label_10.Background = RGBStringToColor(value); break;
          case "提示文本英文字体色": color_label_11.Background = RGBStringToColor(value); break;
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
        };
        自启.Add(item);
      }


      pattern = "《(自动关机)=(.*)》";
      matches = Regex.Matches(kegText, pattern);
      if (matches.Count > 0){
        foreach (Match match in matches)
        {
          var item = new 列表项()
          {
            Enable = false,
            Name = match.Groups[1].Value,
            Value = match.Groups[2].Value == "" ? "22:30" : match.Groups[2].Value,
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
        };
        自动关机.Add(item);
      }
    }

    // 保存文件 全局设置.json
    private void SaveGlobalSettingJson()
    {
      设置项 = new 状态条
      {
        提示文本字体大小                           = nud23.Value,
        提示文本字体名称                           = textBox_Copy24.Text,
        提示文本的位置                             = 取提示文本的位置(),
        提示文本要隐藏吗                           = (bool)checkBox3_Copy.IsChecked,
        提示文本要显示中英以及大小写状态吗         = (bool)checkBox3_Copy1.IsChecked,
        打字字数统计等数据是要保存在主程文件夹下吗 = (bool)checkBox3_Copy3.IsChecked,
        快键只在候选窗口显示情况下才起作用吗       = (bool)checkBox3_Copy2.IsChecked,
        要启用深夜锁屏吗                           = (bool)checkBox3_Copy4.IsChecked,
        提示文本中文字体色                         = RemoveChars(color_label_10.Background.ToString(), 2),
        提示文本英文字体色                         = RemoveChars(color_label_11.Background.ToString(), 2),
    };

      全局设置 = new()
      {
        状态栏和其它设置 = 设置项,
        查找列表         = 查找列表,
        外部工具         = 外部工具,
        快键命令         = 快键命令,
        快键             = 快键,
        自启             = 自启,
        自动关机         = 自动关机
      };

      string jsonString = JsonConvert.SerializeObject(全局设置, Formatting.Indented);
      File.WriteAllText(globalSettingFilePath, jsonString);
    }

    // 读取文件 全局设置.json
    private void LoadGlobalSettingJson(){

      全局设置 = new()
      {
        状态栏和其它设置 = new(),
        查找列表 = new ObservableCollection<列表项>(),
        外部工具 = new ObservableCollection<列表项>(),
        快键命令 = new ObservableCollection<列表项>(),
        快键     = new ObservableCollection<列表项>(),
        自启     = new ObservableCollection<列表项>(),
        自动关机 = new ObservableCollection<列表项>()
      };

      // 读取整个文件内容,将JSON字符串反序列化为对象
      string jsonString = File.ReadAllText(globalSettingFilePath);
      全局设置 = JsonConvert.DeserializeObject<GlobalSettings>(jsonString);
      查找列表 = 全局设置.查找列表;
      外部工具 = 全局设置.外部工具;
      快键命令 = 全局设置.快键命令;
      快键     = 全局设置.快键;
      自启     = 全局设置.自启;
      自动关机 = 全局设置.自动关机;
      设置项   = 全局设置.状态栏和其它设置;

      提示文本的位置(设置项.提示文本的位置);
      checkBox3_Copy.IsChecked  = 设置项.提示文本要隐藏吗;
      nud23.Value               = 设置项.提示文本字体大小;
      textBox_Copy24.Text       = 设置项.提示文本字体名称;
      checkBox3_Copy4.IsChecked = 设置项.要启用深夜锁屏吗;
      checkBox3_Copy1.IsChecked = 设置项.提示文本要显示中英以及大小写状态吗;
      checkBox3_Copy2.IsChecked = 设置项.快键只在候选窗口显示情况下才起作用吗;
      checkBox3_Copy3.IsChecked = 设置项.打字字数统计等数据是要保存在主程文件夹下吗;
      color_label_10.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(设置项.提示文本中文字体色));
      color_label_11.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(设置项.提示文本英文字体色));

      listView3.ItemsSource = 查找列表; // ListView的数据
      listView8.ItemsSource = 外部工具;
      listView4.ItemsSource = 快键命令;
      listView5.ItemsSource = 快键;
      listView6.ItemsSource = 自启;
      listView7.ItemsSource = 自动关机;

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
      {
        if (listView.SelectedItem is 列表项 selectedItem)
        {
          selectedItem.Enable = selectedItem.Enable == true;
        }
      }
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
      {
        listitem.Value = hotKeyControl.HotKey;
      }
    }
    private void TextBox1_TextChanged(object sender, TextChangedEventArgs e)
    {
      // sender是触发事件的TextBox控件
      TextBox textBox = sender as TextBox;

      // 确保textBox不是null
      if (textBox != null)
      {
        // 获取TextBox的DataContext，它应该是一个列表项实例

        // 确保item不是null
        if (textBox.DataContext is 列表项 item)
        {
          // 更新列表项的Name属性
          item.Name = textBox.Text;
        }
      }
    }
    private void TextBox2_TextChanged(object sender, TextChangedEventArgs e)
    {
      // sender是触发事件的TextBox控件
      TextBox textBox = sender as TextBox;

      // 确保textBox不是null
      if (textBox != null)
      {
        // 获取TextBox的DataContext，它应该是一个列表项实例

        // 确保item不是null
        if (textBox.DataContext is 列表项 item)
        {
          item.Value = textBox.Text;
        }
      }
    }

    private void TextBox3_TextChanged(object sender, TextChangedEventArgs e)
    {
      // sender是触发事件的TextBox控件
      TextBox textBox = sender as TextBox;

      // 确保textBox不是null
      if (textBox != null)
      {
        // 获取TextBox的DataContext，它应该是一个列表项实例

        // 确保item不是null
        if (textBox.DataContext is 列表项 item)
        {
          item.CMD = textBox.Text;
        }
      }
    }

    private void CheckBox_Click(object sender, RoutedEventArgs e)
    {
      var dataItem = GetDataItem(sender, e);

      // 检查dataItem是否是列表项的实例
      if (dataItem is 列表项 listitem)
      {
        listitem.Enable = listitem.Enable == true;
      }
    }
    private void DelButton3_Click(object sender, RoutedEventArgs e)
    {
      var dataItem = GetDataItem(sender, e);

      // 检查dataItem是否是列表项的实例
      if (dataItem is 列表项 listitem)
      {
        删除列表项(3, listitem);
      }
    }
    private void DelButton4_Click(object sender, RoutedEventArgs e)
    {
      var dataItem = GetDataItem(sender, e);

      // 检查dataItem是否是列表项的实例
      if (dataItem is 列表项 listitem)
      {
        删除列表项(4, listitem);
      }
    }

    private void DelButton5_Click(object sender, RoutedEventArgs e)
    {
      var dataItem = GetDataItem(sender, e);

      // 检查dataItem是否是列表项的实例
      if (dataItem is 列表项 listitem)
      {
        删除列表项(5, listitem);
      }
    }
    private void DelButton6_Click(object sender, RoutedEventArgs e)
    {
      var dataItem = GetDataItem(sender, e);

      // 检查dataItem是否是列表项的实例
      if (dataItem is 列表项 listitem)
      {
        删除列表项(6, listitem);
      }
    }
    private void DelButton7_Click(object sender, RoutedEventArgs e)
    {
      var dataItem = GetDataItem(sender, e);

      // 检查dataItem是否是列表项的实例
      if (dataItem is 列表项 listitem)
      {
        删除列表项(7, listitem);
      }
    }
    private void DelButton8_Click(object sender, RoutedEventArgs e)
    {
      var dataItem = GetDataItem(sender, e);

      // 检查dataItem是否是列表项的实例
      if (dataItem is 列表项 listitem)
      {
        删除列表项(8, listitem);
      }
    }
    // 鼠标进入 ListViewItem 时触发
    private void ListViewItem_MouseEnter(object sender, MouseEventArgs e)
    {
      // 寻找事件源中的 Button 控件
      if (sender is ListViewItem item)
      {
        Button delButton = FindChild<Button>(item, "delButton");
        if (delButton != null)
        {
          delButton.Visibility = Visibility.Visible;  // 显示按钮
        }
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
        {
          delButton.Visibility = Visibility.Hidden;  // 隐藏按钮
        }
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
        ScrollViewerOffset("在线查找", 2);
        var item = new 列表项()
        {
          Enable = false,
          Name = "",
          Value = ""
        };
        查找列表.Insert(0, item);
        listView3.Focus();
        listView3.SelectedIndex = 0;
      }

      if (btn == add_button8)
      {
        ScrollViewerOffset("外部工具", 2);
        var item = new 列表项()
        {
          Enable = false,
          Name = "",
          Value = ""
        };
        外部工具.Insert(0, item);
        listView8.Focus();
        listView8.SelectedIndex = 0;
      }

      // 快捷命令 列表
      if (btn == add_button4)
      {
        ScrollViewerOffset("快捷命令", 2);
        var item = new 列表项()
        {
          Enable = false,
          Name = "运行命令行快键",
          Value = "",
        };
        快键命令.Insert(0, item);
        listView4.Focus();
        listView4.SelectedIndex = 0;
      }

      // 快捷键 列表
      if (btn == add_button5)
      {
        ScrollViewerOffset("快捷键", 2);
        var item = new 列表项()
        {
          Enable = false,
          Name = (comboBox4.SelectedItem as ComboBoxItem)?.Content?.ToString(),
          Value = "",
        };
        快键.Insert(0, item);
        listView5.Focus();
        listView5.SelectedIndex = 0;
      }

      // 自启应用 列表
      if (btn == add_button6)
      {
        ScrollViewerOffset("自启动应用", 2);
        var item = new 列表项()
        {
          Enable = false,
          Name = "自启",
          Value = "",
        };
        自启.Insert(0, item);
        listView6.Focus();
        listView6.SelectedIndex = 0;
      }

      // 定时关机 列表
      if (btn == add_button7)
      {
        ScrollViewerOffset("定时关机", 2);
        var item = new 列表项()
        {
          Enable = false,
          Name = "自动关机",
          Value = $"22:30"
        };
        自动关机.Insert(0, item);
        listView7.Focus();
        listView7.SelectedIndex = 0;
      }
    }


    private void CheckBox3_Copy1_CheckedChanged(object sender, RoutedEventArgs e)
    {
      zh_en = checkBox3_Copy1.IsChecked ==true ? "中c：" : "";
      toolTipTextBlock.Text = $"{zh_en}{labelName}";
    }

    private void CheckBox3_Copy_CheckedChanged(object sender, RoutedEventArgs e)
    {
      toolTipTextBlock.Visibility = checkBox3_Copy.IsChecked == true ? Visibility.Hidden : Visibility.Visible;
    }

    private void LoadImages()
    {
      // 获取应用的相对路径并转换为绝对路径
      string directoryPath = kegPath + "\\skin\\";
      
      // 设置皮肤图片路径
      string skin = $"{directoryPath}Keg.png";
      string skinBackup = $"{directoryPath}默认.png";
      //Console.WriteLine(skin);
      // 将皮肤图片设置为图像源
      try  { image.Source = new BitmapImage(new Uri(GetValue("skin", "path"))); }
      catch{ image.Source = new BitmapImage(new Uri(skin)); }

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
      string skin = kegPath + "\\skin\\" + selectedItem + ".png";
      image.Source = new BitmapImage(new Uri(skin));
    }



    private void SaveButton1_Click(object sender, RoutedEventArgs e)
    {
      if (skinListBox.SelectedIndex > 0)
      {
        string selectedItem = (string)skinListBox.SelectedItem;
        string selectedSkin = kegPath + "\\skin\\" + selectedItem + ".png";

        try
        {
          IntPtr hWnd = FindWindow("CKegServer_0", null);
          Clipboard.SetText(selectedSkin);
          Thread.Sleep(200);
          // KWM_UPPFSET 更新皮肤文件路径
          SendMessageTimeout(hWnd, KWM_UPPFSET, IntPtr.Zero, IntPtr.Zero, flags, timeout, out IntPtr pdwResult);

          SetValue("skin", "path", selectedSkin);
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
