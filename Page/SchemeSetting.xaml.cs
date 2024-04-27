using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Newtonsoft.Json;
using 小科狗配置.Control;
using Button = System.Windows.Controls.Button;
using Clipboard = System.Windows.Clipboard;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using GroupBox = System.Windows.Controls.GroupBox;
using Label = System.Windows.Controls.Label;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Path = System.IO.Path;

namespace 小科狗配置.Page
{
  /// <summary>
  /// SchemeSetting.xaml 的交互逻辑
  /// </summary>
  public partial class SchemeSetting
  {
    #region 获取GroupBox的Header用于主窗口导航事件
    private void GroupBox_MouseEnter(object sender, MouseEventArgs e)
    {
      if (sender is not GroupBox groupBox) return;
      NameOfSelectedGroupBox = groupBox.Header.ToString();
    }

    #endregion


    #region 消息接口定义

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, uint flags, uint timeout, out IntPtr pdwResult);

    private const   uint Abortifhung   = 0x0002;
    private const   uint Flags         = Abortifhung;
    private const   uint Timeout       = 500;
    private   const int  WmUser        = 0x0400;             // 根据Windows API定义
    private   const uint KwmReset      = (uint)WmUser + 201; //重置配置
    private   const uint KwmGetset     = (uint)WmUser + 203; //由剪切板指定方案名,从而获得该方案的配置
    private   const uint KwmUpbase     = (uint)WmUser + 205; //更新内存数据库 
    private   const uint KwmSavebase   = (uint)WmUser + 206; //保存内存数据库
    private   const uint KwmGetdef     = (uint)WmUser + 208; //把默认无方案名的配置模板吐到剪切板
    private   const uint KwmSet2All    = (uint)WmUser + 209; //设置当前码字方案为所有进程的初始方案格式:《所有进程默认初始方案=  》
    private   const uint KwmGetallname = (uint)WmUser + 213; //把所有方案名吐到剪切板,一行一个方案名

    #endregion

    #region 配色方案相关定义

    // 定义配色方案类
    public class ColorScheme
    {
      public string 名称        { get; set; }
      public bool   显示背景图     { get; set; }
      public bool   显示候选窗圆角   { get; set; }
      public bool   显示选中项背景圆角 { get; set; }
      public int    候选窗圆角     { get; set; }
      public int    选中项圆角     { get; set; }
      public int    边框线宽      { get; set; }
      public string 下划线色      { get; set; }
      public string 光标色       { get; set; }
      public string 分隔线色      { get; set; }
      public string 窗口边框色     { get; set; }
      public string 窗背景底色     { get; set; }
      public string 选中背景色     { get; set; }
      public string 选中字体色     { get; set; }
      public string 编码字体色     { get; set; }
      public string 候选字色      { get; set; }
    }
    public class ColorSchemesCollection
    {
      public List<ColorScheme> 配色方案 { get; } = new();
    }

    private List<ColorScheme> _配色方案 = new();

    private ColorScheme _colorScheme = new()
    {
      名称        = "默认",
      显示背景图     = false,
      显示候选窗圆角   = true,
      显示选中项背景圆角 = true,
      候选窗圆角     = 15,
      选中项圆角     = 10,
      边框线宽      = 1,
      下划线色      = "#FF0000",
      光标色       = "#004CFF",
      分隔线色      = "#000000",
      窗口边框色     = "#000000",
      窗背景底色     = "#FFFFFF",
      选中背景色     = "#000000",
      选中字体色     = "#333333",
      编码字体色     = "#000000",
      候选字色      = "#000000"
    };
    #endregion

    #region 全局变量定义
    SolidColorBrush _bkColor = new((Color)ColorConverter.ConvertFromString("#FFFFFFFF")!);  // 候选框无背景色时的值

    private  readonly  string _appPath        = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    private   readonly string _schemeFilePath;  // 配色方案.json"
    private            string _labelName      = "方案名称";

    private int    _selectColorLabelNum;    // 用于记录当前选中的 select_color_label
    private string _bgString;                      // 存放字体色串
    private string _currentConfig, _modifiedConfig; // 存少当前配置和当前修改的配置


    #endregion
    

    #region 初始化

    public SchemeSetting()
    {
      InitializeComponent();

      restor_Default_Button.Visibility = Visibility.Collapsed;
      loading_Templates_Button.Visibility = Visibility.Collapsed;
      set_As_Default_Button.Visibility = Visibility.Collapsed;
      apply_Button.Visibility = Visibility.Collapsed;
      apply_Save_Button.Visibility = Visibility.Collapsed;
      apply_All_Button.Visibility = Visibility.Collapsed;
      comboBox.Visibility = Visibility.Collapsed;

      _schemeFilePath = $"{_appPath}\\configs\\配色方案.json";

      if (!Directory.Exists($"{_appPath}\\configs")) Directory.CreateDirectory($"{_appPath}\\configs");

      Loaded += MainWindow_Loaded;
      colorPicker.ColorChanged += ColorPicker_ColorChanged;
    }

    private void ColorPicker_ColorChanged(object sender, ColorChangedEventArgs e)
    {
      if (colorPicker.RgbColor != null)
      {
        rgbTextBox.RGBText = colorPicker.RgbText;
        SetColorLableColor(colorPicker.RgbColor);
      }
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
      DataContext = this;
      LoadJson();           // 读取 配色方案.jsonString
      LoadHxFile();         // 读取 候选序号.txt
    }




    // 获取版本号
    public string GetAssemblyVersion()
    {
      var assembly = Assembly.GetExecutingAssembly();
      var version = assembly.GetName().Version;
      return version.ToString().Substring(0, 3);
    }
    #endregion



    #region 顶部控件事件
    // 载入码表方案名称
    private void GetList_button_Click(object sender, RoutedEventArgs e)
    {
      LoadTableNames();

      restor_Default_Button.Visibility = Visibility.Visible;
      loading_Templates_Button.Visibility = Visibility.Visible;
      set_As_Default_Button.Visibility = Visibility.Visible;
      apply_Button.Visibility = Visibility.Visible;
      apply_Save_Button.Visibility = Visibility.Visible;
      apply_All_Button.Visibility = Visibility.Visible;
      comboBox.Visibility = Visibility.Visible;
    }

    private void LoadTableNames()
    {
      try
      {
        //把所有方案名吐到剪切板,一行一个方案名
        var hWnd = FindWindow("CKegServer_0", null); //窗口句柄
        SendMessageTimeout(hWnd, KwmGetallname, IntPtr.Zero, IntPtr.Zero, Flags, Timeout, out _);
      }
      catch (Exception ex)
      {
        MessageBox.Show("操作失败，请重试！");
        Console.WriteLine(ex.Message);
      }
      var multiLineString = Clipboard.GetText();

      // 使用StringSplitOptions.RemoveEmptyEntries选项来避免空行被添加
      var lines = multiLineString.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

      // 将每行作为一个项添加到ComboBox中
      comboBox.Items.Clear();
      foreach (var line in lines)
        comboBox.Items.Add(line);
      comboBox.SelectedIndex = 0;
    }
    private string GetConfig(string labelName)
    {
      try
      {
        Clipboard.SetText(labelName);

        var hWnd = FindWindow("CKegServer_0", null); //窗口句柄
        SendMessageTimeout(hWnd, KwmGetset, IntPtr.Zero, IntPtr.Zero, Flags, Timeout, out _);
      }
      catch (Exception ex)
      {
        MessageBox.Show("操作失败，请重试！");
        Console.WriteLine(ex.Message);
      }

      Thread.Sleep(300);
      var result = Clipboard.GetText();
      return result;
    }

    // 保存内存配置到数据库
    private void SaveConfig(string labelName)
    {
      try
      {
        Clipboard.SetText(labelName);
        var hWnd = FindWindow("CKegServer_0", null); //窗口句柄
        SendMessageTimeout(hWnd, KwmSavebase, IntPtr.Zero, IntPtr.Zero, Flags, Timeout, out _);
      }
      catch (Exception ex)
      {
        MessageBox.Show("操作失败，请重试！");
        Console.WriteLine(ex.Message);
      }

    }


    private void ComboBox_MouseEnter(object sender, MouseEventArgs e)
    {
      comboBox.Focus();
    }

    // 切换方案
    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      _labelName = comboBox.SelectedValue as string;


      _currentConfig = GetConfig(_labelName);
      SetControlsValue();
    }

    // 加载默认设置
    private void Restor_default_button_Click(object sender, RoutedEventArgs e)
    {
      _currentConfig = GetConfig(_labelName);
      SetControlsValue();
    }

    // 加载默认模板
    private void Loading_templates_button_Click(object sender, RoutedEventArgs e)
    {
      var result = MessageBox.Show(
      "您确定要从服务端加载默认模板吗？",
      "加载默认模板",
      MessageBoxButton.OKCancel,
      MessageBoxImage.Question);

      if (result != MessageBoxResult.OK)
        return;

      try
      {
        var hWnd = FindWindow("CKegServer_0", null); //窗口句柄
        SendMessageTimeout(hWnd, KwmGetdef, IntPtr.Zero, IntPtr.Zero, Flags, Timeout, out _);
        var str = Clipboard.GetText();
        _currentConfig = Regex.Replace(str, "方案：<>配置", $"方案：<{_labelName}>配置");
        SetControlsValue();

        _modifiedConfig = _currentConfig;
        checkBox_Copy25.IsChecked = true;
        checkBox_Copy26.IsChecked = true;
        checkBox1.IsChecked = false;
        checkBox_Copy12.IsChecked = true;
      }
      catch (Exception ex)
      {
        MessageBox.Show("操作失败，请重试！");
        Console.WriteLine(ex.Message);
      }
    }

    // 设置默认方案
    private void Set_as_default_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        Clipboard.SetText($"《所有进程默认初始方案={_labelName}》");
        var hWnd = FindWindow("CKegServer_0", null); //窗口句柄
        SendMessageTimeout(hWnd, KwmSet2All, IntPtr.Zero, IntPtr.Zero, Flags, Timeout, out _);
      }
      catch (Exception ex)
      {
        MessageBox.Show("操作失败，请重试！");
        Console.WriteLine(ex.Message);
      }
    }

    // 应用修改
    private void Apply_button_Click(object sender, RoutedEventArgs e)
    {
      _modifiedConfig = _currentConfig;
      GetControlsValue(); // 读取所有控件值替换到 modifiedConfig
      // 获取已修改项
      var updataStr = $"方案：<{_labelName}> 配置 \n" + GetDifferences(_modifiedConfig, _currentConfig);

      try
      {

        Clipboard.SetText(updataStr);
        //Thread.Sleep(200);
        var hWnd = FindWindow("CKegServer_0", null); //窗口句柄
        SendMessageTimeout(hWnd, KwmReset, IntPtr.Zero, IntPtr.Zero, Flags, Timeout, out _);
        _currentConfig = _modifiedConfig;
      }
      catch (Exception ex)
      {
        MessageBox.Show("操作失败，请重试！");
        Console.WriteLine(ex.Message);
      }
    }

    // 保存内存数据
    private void Apply_save_button_Click(object sender, RoutedEventArgs e)
    {
      SaveConfig(_labelName);
    }

    // 更新内存数据
    private void Apply_All_button_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        // 更新内存数据库 
        var hWnd = FindWindow("CKegServer_0", null); //窗口句柄
        SendMessageTimeout(hWnd, KwmUpbase, IntPtr.Zero, IntPtr.Zero, Flags, Timeout, out _);
      }
      catch (Exception ex)
      {
        MessageBox.Show("操作失败，请重试！");
        Console.WriteLine(ex.Message);
      }
    }


    // 获取已修改项
    private static string GetDifferences(string modifiedConfig, string currentConfig)
    {
      var pattern = "《.*?》";
      var matches1 = Regex.Matches(modifiedConfig, pattern);
      var matches2 = Regex.Matches(currentConfig, pattern);
      var modifiedLines = matches1.Cast<Match>().Select(m => m.Value).ToArray();
      var currentLines = matches2.Cast<Match>().Select(m => m.Value).ToArray();
      // 找出不同的行
      var differentLines = modifiedLines.Except(currentLines);
      // 将不同的行追加到新的字符串中
      var newConfig = string.Join(Environment.NewLine, differentLines);
      return newConfig;
    }

    #endregion

    #region 读取配置各项值到控件

    // 读取候选序号
    private void LoadHxFile()
    {
      var file = $"{_appPath}\\configs\\候选序号.txt";
      const string numStr = @"<1=🥑¹sp><2=🍑²sp><3=🍋³sp><4=🍍⁴sp><5=🍈⁵sp><6=🍐⁶sp><7=🍊⁷sp ><8=⁸sp🍑 ><9=⁹sp🍉><10=¹⁰sp🍊>
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
      while (sr.ReadLine() is { } line)
      {
        ComboBoxItem item = new() { Content = line };
        comboBox3.Items.Add(item);
      }
    }

    // 读取配置值到控件
    private void SetControlsValue()
    {
      var pattern = "《(.*=?.*)=(.*)》";
      var matches = Regex.Matches(_currentConfig, pattern);
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
      matches = Regex.Matches(_currentConfig, pattern);
      foreach (Match match in matches)
      {
        var value = match.Groups[2].Value;
        switch (match.Groups[1].Value)
        {
          case "背景底色": 背景底色(value); break;
          case "顶功规则": 顶功规则(value); break;
          case "D2D字体样式": D2D字体样式(value); break;
          case "GDI+字体样式": Gdip字体样式(value); break;
          case "候选字体色串": SetLabelColor(value); break;
          case "词语联想检索范围": 词语联想检索范围(value); break;
          case "候选窗口绘制模式": 候选窗口绘制模式(value); break;
          case "编码或候选嵌入模式": 编码或候选嵌入模式(value); break;
          case "词语联想上屏字符串长度": 词语联想上屏字符串长度(value); break;
          case "候选窗口候选排列方向模式": 候选窗口候选排列方向模式(value); break;
          case "大键盘码元": textBox_Copy677.Text = value; break;
          case "小键盘码元": textBox_Copy5.Text = value; break;
          case "键首字根": textBox125.Text = value; break;
          case "字体名称": textBox_Copy145.Text = value; break;
          case "码表标签": textBox_Copy15.Text = value; break;
          case "主码表标识": textBox_Copy22.Text = value; break;
          case "副码表标识": textBox_Copy23.Text = value; break;
          case "候选序号": textBox_Copy67.Text = value; break;
          case "顶功小集码元": textBox_Copy675.Text = value; break;
          case "码表临时快键": textBox_Copy19.Text = value; break;
          case "D2D回退字体集": textBox_Copy10.Text = value; break;
          case "码表引导快键0": textBox_Copy.Text = value; break;
          case "码表引导快键1": textBox_Copy12.Text = value; break;
          case "码表引导快键2": textBox_Copy16.Text = value; break;
          case "候选快键字符串": textBox_Copy66.Text = value; break;
          case "大小键盘万能码元": textBox_Copy6.Text = value; break;
          case "大键盘中文标点串": textBox_Copy68.Text = value; break;
          case "重复上屏码元字符串": textBox_Copy1.Text = value; break;
          case "码表临时快键编码名": textBox_Copy20.Text = value; break;
          case "码表引导快键0编码名0": textBox_Copy9.Text = value; break;
          case "码表引导快键0编码名1": textBox_Copy11.Text = value; break;
          case "码表引导快键1编码名0": textBox_Copy13.Text = value; break;
          case "码表引导快键1编码名1": textBox_Copy14.Text = value; break;
          case "码表引导快键2编码名0": textBox_Copy17.Text = value; break;
          case "码表引导快键2编码名1": textBox_Copy18.Text = value; break;
          case "非编码串首位的大键盘码元": textBox_Copy7.Text = value; break;
          case "非编码串首位的小键盘码元": textBox_Copy8.Text = value; break;
          case "大键盘按下Shift的中文标点串": textBox_Copy69.Text = value; break;
          case "往上翻页大键盘英文符号编码串": textBox_Copy21.Text = value; break;
          case "往下翻页大键盘英文符号编码串": textBox_Copy2.Text = value; break;
          case "往上翻页小键盘英文符号编码串": textBox_Copy3.Text = value; break;
          case "往下翻页小键盘英文符号编码串": textBox_Copy4.Text = value; break;
          case "码表标签显示模式": comboBox1_Copy.SelectedIndex = int.Parse(value); break;
          case "窗口四个角的圆角半径": nud11.Value = int.Parse(value); break;
          case "选中项四个角的圆角半径": nud12.Value = int.Parse(value); break;
          case "候选窗口边框线宽度": nud13.Value = int.Parse(value); break;
          case "最大码长": nud1.Value = int.Parse(value); break;
          case "D2D字体加粗权值": nud14.Value = int.Parse(value); break;
          case "候选个数": nud15.Value = int.Parse(value); break;
          case "1-26候选的横向偏离": nud16.Value = int.Parse(value); break;
          case "候选的高度间距": nud17.Value = int.Parse(value); break;
          case "候选的宽度间距": nud18.Value = int.Parse(value); break;
          case "调频权重最小码长": nud2.Value = int.Parse(value); break;
          case "双检索历史重数": nud3.Value = int.Parse(value); break;
          case "唯一上屏最小码长": nud4.Value = int.Parse(value); break;
          case "GDI字体加粗权值": nud14_Copy.Value = int.Parse(value); break;
          case "光标色": color_Label_2.Background = RgbStringToColor(value); break;
          case "分隔线色": color_Label_3.Background = RgbStringToColor(value); break;
          case "候选选中色": color_Label_6.Background = RgbStringToColor(value); break;
          case "要码长顶屏吗？": checkBox1_Copy111.IsChecked = IsTrueOrFalse(value); break;
          case "要数字顶屏吗？": checkBox1_Copy7.IsChecked = IsTrueOrFalse(value); break;
          case "要标点顶屏吗？": checkBox1_Copy6.IsChecked = IsTrueOrFalse(value); break;
          case "要唯一上屏吗？": checkBox1_Copy5.IsChecked = IsTrueOrFalse(value); break;
          case "嵌入下划线色": color_Label_1.Background = RgbStringToColor(value); break;
          case "候选窗口边框色": color_Label_4.Background = RgbStringToColor(value); break;
          case "候选选中字体色": color_Label_7.Background = RgbStringToColor(value); break;
          case "要显示背景图吗？": checkBox_Copy42.IsChecked = IsTrueOrFalse(value); break;
          case "要启用双检索吗？": checkBox1_Copy3.IsChecked = IsTrueOrFalse(value); break;
          case "关联中文标点吗？": checkBox_Copy31.IsChecked = IsTrueOrFalse(value); break;
          case "无候选要清屏吗？": checkBox_Copy20.IsChecked = IsTrueOrFalse(value); break;
          case "要使用嵌入模式吗？": checkBox_Copy44.IsChecked = IsTrueOrFalse(value); break;
          case "要开启词语联想吗？": checkBox_Copy4.IsChecked = IsTrueOrFalse(value); break;
          case "是键首字根码表吗？": checkBox1_Copy55.IsChecked = IsTrueOrFalse(value); break;
          case "要显示键首字根吗？": checkBox_Copy34.IsChecked = IsTrueOrFalse(value); break;
          case "超过码长要清屏吗？": checkBox_Copy19.IsChecked = IsTrueOrFalse(value); break;
          case "要逐码提示检索吗？": checkBox_Copy.IsChecked = IsTrueOrFalse(value); break;
          case "要显示逐码提示吗？": checkBox.IsChecked = IsTrueOrFalse(value); break;
          case "要显示反查提示吗？": checkBox1.IsChecked = IsTrueOrFalse(value); break;
          case "要启用单字模式吗？": checkBox1_Copy.IsChecked = IsTrueOrFalse(value); break;
          case "GDI字体要倾斜吗？": checkBox_Copy314.IsChecked = IsTrueOrFalse(value); break;
          case "要启用右Ctrl键吗？": checkBox_Copy16.IsChecked = IsTrueOrFalse(value); break;
          case "要启用左Ctrl键吗？": checkBox_Copy15.IsChecked = IsTrueOrFalse(value); break;
          case "要启用左Shift键吗？": checkBox_Copy13.IsChecked = IsTrueOrFalse(value); break;
          case "要启用右Shift键吗？": checkBox_Copy14.IsChecked = IsTrueOrFalse(value); break;
          case "GDI+字体要下划线吗？": checkBox19.IsChecked = IsTrueOrFalse(value); break;
          case "GDI+字体要删除线吗？": checkBox20.IsChecked = IsTrueOrFalse(value); break;
          case "窗口四个角要圆角吗？": hxc_CheckBox.IsChecked = IsTrueOrFalse(value); break;
          case "码表标签要左对齐吗？": checkBox_Copy39.IsChecked = IsTrueOrFalse(value); break;
          case "过渡态按1要上屏1吗？": checkBox_Copy30.IsChecked = IsTrueOrFalse(value); break;
          case "Shift键上屏编码串吗？": checkBox_Copy23.IsChecked = IsTrueOrFalse(value); break;
          case "Enter键上屏编码串吗？": checkBox_Copy26.IsChecked = IsTrueOrFalse(value); break;
          case "要启用Ctrl+Space键吗？": checkBox_Copy17.IsChecked = IsTrueOrFalse(value); break;
          case "要开启Ctrl键清联想吗？": checkBox_Copy10.IsChecked = IsTrueOrFalse(value); break;
          case "选中项四个角要圆角吗？": hxcbj_CheckBox.IsChecked = IsTrueOrFalse(value); break;
          case "要启用ESC键自动造词吗？": checkBox_Copy3.IsChecked = IsTrueOrFalse(value); break;
          case "词语联想只是匹配首位吗？": checkBox_Copy6.IsChecked = IsTrueOrFalse(value); break;
          case "高度宽度要完全自动调整吗？": checkBox_Copy40.IsChecked = IsTrueOrFalse(value); break;
          case "中英切换要显示提示窗口吗？": checkBox_Copy11.IsChecked = IsTrueOrFalse(value); break;
          case "双检索时编码要完全匹配吗？": checkBox1_Copy4.IsChecked = IsTrueOrFalse(value); break;
          case "词语联想要显示词语全部吗？": checkBox_Copy5.IsChecked = IsTrueOrFalse(value); break;
          case "上屏后候选窗口要立即消失吗？": checkBox_Copy18.IsChecked = IsTrueOrFalse(value); break;
          case "要启用最大码长无候选清屏吗？": checkBox_Copy21.IsChecked = IsTrueOrFalse(value); break;
          case "无候选敲空格要上屏编码串吗？": checkBox_Copy22.IsChecked = IsTrueOrFalse(value); break;
          case "词语联想时标点顶屏要起作用吗？": checkBox_Copy7.IsChecked = IsTrueOrFalse(value); break;
          case "候选词条要按码长短优先排序吗？": checkBox_Copy2.IsChecked = IsTrueOrFalse(value); break;
          case "要启用上屏自动增加调频权重吗？": checkBox1_Copy1.IsChecked = IsTrueOrFalse(value); break;
          case "Space键要上屏临时英文编码串吗？": checkBox_Copy25.IsChecked = IsTrueOrFalse(value); break;
          case "Enter键上屏并使首个字母大写吗？": checkBox_Copy27.IsChecked = IsTrueOrFalse(value); break;
          case "候选词条要按调频权重检索排序吗？": checkBox1_Copy2.IsChecked = IsTrueOrFalse(value); break;
          case "竖向候选窗口选中背景色要等宽吗？": checkBox_Copy41.IsChecked = IsTrueOrFalse(value); break;
          case "候选窗口候选从上到下排列要锁定吗？": checkBox_Copy45.IsChecked = IsTrueOrFalse(value); break;
          case "无临时快键时,也要显示主码表标识吗？": checkBox_Copy32.IsChecked = IsTrueOrFalse(value); break;
          case "从中文切换到英文时,要上屏编码串吗？": checkBox_Copy12.IsChecked = IsTrueOrFalse(value); break;
          case "Shift键+字母键要进入临时英文长句态吗？": checkBox_Copy24.IsChecked = IsTrueOrFalse(value); break;
          case "要启用上屏自动增加调频权重直接到顶吗？": checkBox_Copy1.IsChecked = IsTrueOrFalse(value); break;
          case "Backspace键一次性删除前次上屏的内容吗？": checkBox_Copy28.IsChecked = IsTrueOrFalse(value); break;
          case "标点或数字顶屏时,若是引导键,要继续引导吗？": checkBox1_Copy8.IsChecked = IsTrueOrFalse(value); break;
          case "前次上屏的是数字再上屏句号*要转成点号*吗？": checkBox_Copy29.IsChecked = IsTrueOrFalse(value); break;
          case "候选窗口候选排列方向模式>1时要隐藏编码串行吗？"
                                                             :
            checkBox_Copy38.IsChecked = IsTrueOrFalse(value); break;
          case "候选窗口候选从上到下排列锁定的情况下要使编码区离光标最近吗？"
                                                             :
            checkBox_Copy46.IsChecked = IsTrueOrFalse(value); break;
        }
      }
    }

    private void 背景底色(string value)
    {
      if (value == "")
      {
        hxcds_CheckBox.IsChecked = true;
        color_Label_5.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
        _bkColor = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
      }
      else
      {
        hxcds_CheckBox.IsChecked = false;
        color_Label_5.Background = RgbStringToColor(value);
        _bkColor = RgbStringToColor(value);
      }
    }

    private bool IsTrueOrFalse(string value)
    {
      if (value == "不要" || value == "不是") return false;
      return true;
    }

    private void 顶功规则(string value)
    {
      switch (value)
      {
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

    private void Gdip字体样式(string value)
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
    private SolidColorBrush RgbStringToColor(string rgbString)
    {
      //候选窗背景色为空时设为对话框背景色
      if (rgbString == "")
      {
        hxcds_CheckBox.IsChecked = true;
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

    private void SetLabelColor(string str)
    {
      var pattern = "<(.*?)=(.*?)>";
      var matches2 = Regex.Matches(str, pattern);

      foreach (Match match in matches2)
      {
        var value = match.Groups[2].Value;
        switch (match.Groups[1].Value)
        {
          case "0": //编码字体色
            color_Label_8.Background = RgbStringToColor(value);
            break;
          case "1":
            color_Label_9.Background = RgbStringToColor(value);
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
      try
      {
        _modifiedConfig = Regex.Replace(_modifiedConfig, $"《{key}=.*?》", $"《{key}={value}》");
      }
      catch (Exception e)
      {
        Console.WriteLine(e.Message);
        Console.WriteLine($@"《{key}={value}》");
      }
    }
    // 读取控件属性值
    private void GetControlsValue()
    {
      ReplaceConfig("顶功规则",             取顶功规则());
      ReplaceConfig("背景底色",             取背景底色());
      ReplaceConfig("候选字体色串",           _bgString);
      ReplaceConfig("键首字根",             textBox125.Text);
      ReplaceConfig("D2D字体样式",          取d2D字体样式());
      ReplaceConfig("码表标签",             textBox_Copy15.Text);
      ReplaceConfig("候选序号",             textBox_Copy67.Text);
      ReplaceConfig("小键盘码元",            textBox_Copy5.Text);
      ReplaceConfig("字体名称",             textBox_Copy145.Text);
      ReplaceConfig("最大码长",             nud1.Value.ToString());
      ReplaceConfig("主码表标识",            textBox_Copy22.Text);
      ReplaceConfig("副码表标识",            textBox_Copy23.Text);
      ReplaceConfig("候选个数",             nud15.Value.ToString());
      ReplaceConfig("大键盘码元",            textBox_Copy677.Text);
      ReplaceConfig("码表引导快键0",          textBox_Copy.Text);
      ReplaceConfig("码表临时快键",           textBox_Copy19.Text);
      ReplaceConfig("码表引导快键1",          textBox_Copy12.Text);
      ReplaceConfig("D2D回退字体集",         textBox_Copy10.Text);
      ReplaceConfig("顶功小集码元",           textBox_Copy675.Text);
      ReplaceConfig("码表引导快键2",          textBox_Copy16.Text);
      ReplaceConfig("候选快键字符串",          textBox_Copy66.Text);
      ReplaceConfig("大小键盘万能码元",         textBox_Copy6.Text);
      ReplaceConfig("大键盘中文标点串",         textBox_Copy68.Text);
      ReplaceConfig("双检索历史重数",          nud3.Value.ToString());
      ReplaceConfig("候选窗口绘制模式",         取候选窗口绘制模式());
      ReplaceConfig("重复上屏码元字符串",        textBox_Copy1.Text);
      ReplaceConfig("词语联想检索范围",         取词语联想检索范围());
      ReplaceConfig("候选的高度间距",          nud17.Value.ToString());
      ReplaceConfig("候选的宽度间距",          nud18.Value.ToString());
      ReplaceConfig("D2D字体加粗权值",        nud14.Value.ToString());
      ReplaceConfig("调频权重最小码长",         nud2.Value.ToString());
      ReplaceConfig("唯一上屏最小码长",         nud4.Value.ToString());
      ReplaceConfig("码表临时快键编码名",        textBox_Copy20.Text);
      ReplaceConfig("码表引导快键0编码名0",      textBox_Copy9.Text);
      ReplaceConfig("码表引导快键0编码名1",      textBox_Copy11.Text);
      ReplaceConfig("码表引导快键1编码名0",      textBox_Copy13.Text);
      ReplaceConfig("码表引导快键1编码名1",      textBox_Copy14.Text);
      ReplaceConfig("码表引导快键2编码名0",      textBox_Copy17.Text);
      ReplaceConfig("码表引导快键2编码名1",      textBox_Copy18.Text);
      ReplaceConfig("1-26候选的横向偏离",      nud16.Value.ToString());
      ReplaceConfig("编码或候选嵌入模式",        取编码或候选嵌入模式());
      ReplaceConfig("候选窗口边框线宽度",        nud13.Value.ToString());
      ReplaceConfig("GDI字体加粗权值",        nud14_Copy.Value.ToString());
      ReplaceConfig("非编码串首位的大键盘码元",     textBox_Copy7.Text);
      ReplaceConfig("非编码串首位的小键盘码元",     textBox_Copy8.Text);
      ReplaceConfig("窗口四个角的圆角半径",       nud11.Value.ToString());
      ReplaceConfig("选中项四个角的圆角半径",      nud12.Value.ToString());
      ReplaceConfig("大键盘按下Shift的中文标点串", textBox_Copy69.Text);
      ReplaceConfig("往下翻页大键盘英文符号编码串",   textBox_Copy2.Text);
      ReplaceConfig("往上翻页小键盘英文符号编码串",   textBox_Copy3.Text);
      ReplaceConfig("往下翻页小键盘英文符号编码串",   textBox_Copy4.Text);
      ReplaceConfig("往上翻页大键盘英文符号编码串",   textBox_Copy21.Text);
      ReplaceConfig("词语联想上屏字符串长度",      取词语联想上屏字符串长度());
      ReplaceConfig("光标色",              HexToRgb(color_Label_2.Background.ToString()));
      ReplaceConfig("候选窗口候选排列方向模式",     取候选窗口候选排列方向模式());
      ReplaceConfig("要显示逐码提示吗？",        要或不要(checkBox.IsChecked != null && (bool)checkBox.IsChecked));
      ReplaceConfig("分隔线色",             HexToRgb(color_Label_3.Background.ToString()));

      ReplaceConfig("要显示反查提示吗？", 要或不要(checkBox1.IsChecked != null && (bool)checkBox1.IsChecked));
      ReplaceConfig("要数字顶屏吗？", 要或不要(checkBox1_Copy7.IsChecked != null && (bool)checkBox1_Copy7.IsChecked));
      ReplaceConfig("要标点顶屏吗？", 要或不要(checkBox1_Copy6.IsChecked != null && (bool)checkBox1_Copy6.IsChecked));
      ReplaceConfig("要唯一上屏吗？", 要或不要(checkBox1_Copy5.IsChecked != null && (bool)checkBox1_Copy5.IsChecked));
      ReplaceConfig("码表标签显示模式", comboBox1_Copy.SelectedIndex.ToString());
      ReplaceConfig("候选选中色", HexToRgb(color_Label_6.Background.ToString()));
      ReplaceConfig("要逐码提示检索吗？", 要或不要(checkBox_Copy.IsChecked != null && (bool)checkBox_Copy.IsChecked));
      ReplaceConfig("要显示背景图吗？", 要或不要(checkBox_Copy42.IsChecked != null && (bool)checkBox_Copy42.IsChecked));
      ReplaceConfig("无候选要清屏吗？", 要或不要(checkBox_Copy20.IsChecked != null && (bool)checkBox_Copy20.IsChecked));
      ReplaceConfig("要启用双检索吗？", 要或不要(checkBox1_Copy3.IsChecked != null && (bool)checkBox1_Copy3.IsChecked));
      ReplaceConfig("要码长顶屏吗？", 要或不要(checkBox1_Copy111.IsChecked != null && (bool)checkBox1_Copy111.IsChecked));
      ReplaceConfig("嵌入下划线色", HexToRgb(color_Label_1.Background.ToString()));
      ReplaceConfig("关联中文标点吗？", 要或不要(checkBox_Copy31 != null && (bool)checkBox_Copy31.IsChecked));
      ReplaceConfig("要启用单字模式吗？", 要或不要(checkBox1_Copy != null && (bool)checkBox1_Copy.IsChecked));
      ReplaceConfig("窗口四个角要圆角吗？", 要或不要(hxc_CheckBox != null && (bool)hxc_CheckBox.IsChecked));
      ReplaceConfig("要开启词语联想吗？", 要或不要(checkBox_Copy4 != null && (bool)checkBox_Copy4.IsChecked));
      ReplaceConfig("要启用左Ctrl键吗？", 要或不要(checkBox_Copy15 != null && (bool)checkBox_Copy15.IsChecked));
      ReplaceConfig("要启用右Ctrl键吗？", 要或不要(checkBox_Copy16 != null && (bool)checkBox_Copy16.IsChecked));
      ReplaceConfig("要显示键首字根吗？", 要或不要(checkBox_Copy34 != null && (bool)checkBox_Copy34.IsChecked));
      ReplaceConfig("候选窗口边框色", HexToRgb(color_Label_4.Background.ToString()));
      ReplaceConfig("候选选中字体色", HexToRgb(color_Label_7.Background.ToString()));
      ReplaceConfig("超过码长要清屏吗？", 要或不要(checkBox_Copy19 != null && (bool)checkBox_Copy19.IsChecked));
      ReplaceConfig("GDI字体要倾斜吗？", 要或不要(checkBox_Copy314 != null && (bool)checkBox_Copy314.IsChecked));
      ReplaceConfig("要使用嵌入模式吗？", 要或不要(checkBox_Copy44 != null && (bool)checkBox_Copy44.IsChecked));
      ReplaceConfig("要启用左Shift键吗？", 要或不要(checkBox_Copy13 != null && (bool)checkBox_Copy13.IsChecked));
      ReplaceConfig("要启用右Shift键吗？", 要或不要(checkBox_Copy14 != null && (bool)checkBox_Copy14.IsChecked));
      ReplaceConfig("是键首字根码表吗？", 是或不是(checkBox1_Copy55 != null && (bool)checkBox1_Copy55.IsChecked));
      ReplaceConfig("码表标签要左对齐吗？", 要或不要(checkBox_Copy39 != null && (bool)checkBox_Copy39.IsChecked));
      ReplaceConfig("过渡态按1要上屏1吗？", 要或不要(checkBox_Copy30 != null && (bool)checkBox_Copy30.IsChecked));
      ReplaceConfig("Shift键上屏编码串吗？", 要或不要(checkBox_Copy23 != null && (bool)checkBox_Copy23.IsChecked));
      ReplaceConfig("Enter键上屏编码串吗？", 要或不要(checkBox_Copy26 != null && (bool)checkBox_Copy26.IsChecked));
      ReplaceConfig("选中项四个角要圆角吗？", 要或不要(hxcbj_CheckBox.IsChecked != null && (bool)hxcbj_CheckBox.IsChecked));
      ReplaceConfig("要启用ESC键自动造词吗？", 要或不要(checkBox_Copy3 != null && (bool)checkBox_Copy3.IsChecked));
      ReplaceConfig("要开启Ctrl键清联想吗？", 要或不要(checkBox_Copy10 != null && (bool)checkBox_Copy10.IsChecked));
      ReplaceConfig("词语联想只是匹配首位吗？", 是或不是(checkBox_Copy6 != null && (bool)checkBox_Copy6.IsChecked));
      ReplaceConfig("词语联想要显示词语全部吗？", 要或不要(checkBox_Copy5 != null && (bool)checkBox_Copy5.IsChecked));
      ReplaceConfig("中英切换要显示提示窗口吗？", 要或不要(checkBox_Copy11 != null && (bool)checkBox_Copy11.IsChecked));
      ReplaceConfig("高度宽度要完全自动调整吗？", 要或不要(checkBox_Copy40 != null && (bool)checkBox_Copy40.IsChecked));
      ReplaceConfig("双检索时编码要完全匹配吗？", 要或不要(checkBox1_Copy4 != null && (bool)checkBox1_Copy4.IsChecked));
      ReplaceConfig("要启用最大码长无候选清屏吗？", 要或不要(checkBox_Copy21 != null && (bool)checkBox_Copy21.IsChecked));
      ReplaceConfig("上屏后候选窗口要立即消失吗？", 要或不要(checkBox_Copy18 != null && (bool)checkBox_Copy18.IsChecked));
      ReplaceConfig("无候选敲空格要上屏编码串吗？", 要或不要(checkBox_Copy22 != null && (bool)checkBox_Copy22.IsChecked));
      ReplaceConfig("候选词条要按码长短优先排序吗？", 要或不要(checkBox_Copy2.IsChecked != null && (bool)checkBox_Copy2.IsChecked));
      ReplaceConfig("词语联想时标点顶屏要起作用吗？", 要或不要(checkBox_Copy7.IsChecked != null && (bool)checkBox_Copy7.IsChecked));
      ReplaceConfig("要启用上屏自动增加调频权重吗？", 要或不要(checkBox1_Copy1.IsChecked != null && (bool)checkBox1_Copy1.IsChecked));
      ReplaceConfig("Space键要上屏临时英文编码串吗？", 要或不要(checkBox_Copy25.IsChecked != null && (bool)checkBox_Copy25.IsChecked));
      ReplaceConfig("Enter键上屏并使首个字母大写吗？", 要或不要(checkBox_Copy27.IsChecked != null && (bool)checkBox_Copy27.IsChecked));
      ReplaceConfig("竖向候选窗口选中背景色要等宽吗？", 要或不要(checkBox_Copy41.IsChecked != null && (bool)checkBox_Copy41.IsChecked));
      ReplaceConfig("候选词条要按调频权重检索排序吗？", 要或不要(checkBox1_Copy2.IsChecked != null && (bool)checkBox1_Copy2.IsChecked));
      ReplaceConfig("候选窗口候选从上到下排列要锁定吗？", 要或不要(checkBox_Copy45.IsChecked != null && (bool)checkBox_Copy45.IsChecked));
      ReplaceConfig("从中文切换到英文时,要上屏编码串吗？", 要或不要(checkBox_Copy12.IsChecked != null && (bool)checkBox_Copy12.IsChecked));
      ReplaceConfig("要启用上屏自动增加调频权重直接到顶吗？", 要或不要(checkBox_Copy1.IsChecked != null && (bool)checkBox_Copy1.IsChecked));
      ReplaceConfig("Backspace键一次性删除前次上屏的内容吗？", 要或不要(checkBox_Copy28.IsChecked != null && (bool)checkBox_Copy28.IsChecked));
      ReplaceConfig("无临时快键时,也要显示主码表标识吗？", 要或不要(checkBox_Copy32.IsChecked != null && (bool)checkBox_Copy32.IsChecked));
      ReplaceConfig("标点或数字顶屏时,若是引导键,要继续引导吗？", 要或不要(checkBox1_Copy8.IsChecked != null && (bool)checkBox1_Copy8.IsChecked));
      ReplaceConfig("候选窗口候选排列方向模式>1时要隐藏编码串行吗？", 要或不要(checkBox_Copy38.IsChecked != null && (bool)checkBox_Copy38.IsChecked));
      ReplaceConfig("候选窗口候选从上到下排列锁定的情况下要使编码区离光标最近吗？", 要或不要(checkBox_Copy46.IsChecked != null && (bool)checkBox_Copy46.IsChecked));

      _modifiedConfig = Regex.Replace(_modifiedConfig, @"《要启用Ctrl\+Space键吗？=.*?》", $"《要启用Ctrl+Space键吗？={要或不要(checkBox_Copy17.IsChecked != null && (bool)checkBox_Copy17.IsChecked)}》");
      _modifiedConfig = Regex.Replace(_modifiedConfig, @"《GDI\+字体样式=.*?》", $"《GDI+字体样式={取gdIp字体样式()}》");
      _modifiedConfig = Regex.Replace(_modifiedConfig, @"GDI\+字体要下划线吗？=.*?》", $"GDI+字体要下划线吗？={要或不要(checkBox19.IsChecked != null && (bool)checkBox19.IsChecked)}》");
      _modifiedConfig = Regex.Replace(_modifiedConfig, @"GDI\+字体要删除线吗？=.*?》", $"GDI+字体要删除线吗？={要或不要(checkBox20.IsChecked != null && (bool)checkBox20.IsChecked)}》");
      _modifiedConfig = Regex.Replace(_modifiedConfig, @"《上屏词条精准匹配key=1\*的值进行词语联想吗？=.*?》", $"《上屏词条精准匹配key=1*的值进行词语联想吗？={要或不要(checkBox_Copy8.IsChecked != null && (bool)checkBox_Copy8.IsChecked)}》");
      _modifiedConfig = Regex.Replace(_modifiedConfig, @"《精准匹配key=1\*的值时要词语模糊联想吗？=.*?》", $"《精准匹配key=1*的值时要词语模糊联想吗？={要或不要(checkBox_Copy9.IsChecked != null && (bool)checkBox_Copy9.IsChecked)}》");
      _modifiedConfig = Regex.Replace(_modifiedConfig, @"《Shift键\+字母键要进入临时英文长句态吗？=.*?》", $"《Shift键+字母键要进入临时英文长句态吗？={要或不要(checkBox_Copy24.IsChecked != null && (bool)checkBox_Copy24.IsChecked)}》");
      _modifiedConfig = Regex.Replace(_modifiedConfig, @"《前次上屏的是数字再上屏句号\*要转成点号\*吗？=.*?》", $"《前次上屏的是数字再上屏句号*要转成点号*吗？={要或不要(checkBox_Copy29.IsChecked != null && (bool)checkBox_Copy29.IsChecked)}》");
    }
    private string 取编码或候选嵌入模式()
    {
      var selected = comboBox1.SelectedIndex.ToString();
      if (checkBox_Copy33.IsChecked == true)
        selected = "1" + selected;
      return selected;
    }
    private string 取背景底色()
    {
      if (hxcds_CheckBox.IsChecked == true)
        return "";
      return HexToRgb(color_Label_5.Background.ToString());
    }
    private string 取候选窗口绘制模式()
    {
      if (radioButton10.IsChecked == true) return "0";
      if (radioButton11.IsChecked == true) return "1";
      return "2";
    }
    private string 取d2D字体样式()
    {
      //if (radioButton6.IsChecked == true) return "0";
      if (radioButton7.IsChecked == true) return "1";
      return "0";
    }
    private string 取gdIp字体样式()
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
      return "3";
    }
    private string 取顶功规则()
    {
      if (radioButton454.IsChecked == true) return "1";
      if (radioButton455.IsChecked == true) return "2";
      return "3";
    }
    private string 是或不是(bool b)
    {
      if (b) return "是";
      return "不是";
    }
    private string 要或不要(bool b)
    {
      if (b) return "要";
      return "不要";
    }



    #endregion



    #region 配色相关
    // 更新对应标签的背景颜色
    private void SetColorLableColor(SolidColorBrush cColor)
    {
      Label[] colorLabels = { color_Label_1, color_Label_2, color_Label_3, color_Label_4, color_Label_5, color_Label_6, color_Label_7, color_Label_8, color_Label_9 };
      // 计算反色
      var currentColor = cColor.Color;
      var invertedColor = Color.FromArgb(255, (byte)~currentColor.R, (byte)~currentColor.G, (byte)~currentColor.B);
      for (var i = 1; i <= colorLabels.Length; i++)
        if (i == _selectColorLabelNum)
        {
          colorLabels[i - 1].BorderBrush = new SolidColorBrush(invertedColor);
          colorLabels[i - 1].Background = cColor;
        }
    }

    private void RGB_ValueChanged(object sender, RoutedPropertyChangedEventArgs<string> e)
    {
      SetColorLableColor(RgbStringToColor(rgbTextBox.RGBText));
    }


    // 读取 Json 文件
    void LoadJson()
    {
      if (File.Exists(_schemeFilePath))
      {
        // 读取整个文件内容,将JSON字符串反序列化为对象
        var jsonString = File.ReadAllText(_schemeFilePath);
        var colorSchemesJson = JsonConvert.DeserializeObject<ColorSchemesCollection>(jsonString);
        _配色方案 = colorSchemesJson.配色方案;

        foreach (var scheme in _配色方案)
        {
          colorSchemeListBox.Items.Add(scheme.名称);
        }
      }
      else
      {
        _配色方案.Add(_colorScheme);
        var jsonString = JsonConvert.SerializeObject(new { 配色方案 = _配色方案 }, Formatting.Indented);
        File.WriteAllText(_schemeFilePath, jsonString);

        colorSchemeListBox.Items.Add("默认");
      }
    }

    // Hex格式 ARGB 转 RGB，如 #FFAABBCC -> #AABBCC
    private static string RemoveChars(string str, int n)
    {
      str = str.Replace("#", ""); // 移除可能存在的井号
      return "#" + str.Substring(2, str.Length - n);
    }


    // 更新所有候选字色（改为同一个颜色）
    private void HXZ_TextBoxText()
    {
      var rgb1 = HexToRgb(color_Label_8.Background.ToString());
      var rgb2 = HexToRgb(color_Label_9.Background.ToString());

      _bgString = $"<0={rgb1}>";
      for (var i = 1; i <= 26; i++)
        _bgString += $"<{i}={rgb2}>";
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

    // 显示颜色的 label 鼠标进入事件
    private void color_label_MouseEnter(object sender, MouseEventArgs e)
    {
      SolidColorBrush color1 = new((Color)ColorConverter.ConvertFromString("#FF000000")!); // 黑色
      SolidColorBrush color2 = new((Color)ColorConverter.ConvertFromString("#FFFF0000")!); // 红色
      color_Label_001.Foreground = color1;
      color_Label_002.Foreground = color1;
      color_Label_003.Foreground = color1;
      color_Label_004.Foreground = color1;
      color_Label_005.Foreground = color1;
      color_Label_006.Foreground = color1;
      color_Label_007.Foreground = color1;
      color_Label_008.Foreground = color1;
      color_Label_009.Foreground = color1;

      if (sender is not Label lb) return;
      switch (lb.Name)
      {
        case "color_Label_1":
          _selectColorLabelNum       = 1;
          color_Label_001.Foreground = color2;
          break;
        case "color_Label_2":
          _selectColorLabelNum       = 2;
          color_Label_002.Foreground = color2;
          break;
        case "color_Label_3":
          _selectColorLabelNum       = 3;
          color_Label_003.Foreground = color2;
          break;
        case "color_Label_4":
          _selectColorLabelNum       = 4;
          color_Label_004.Foreground = color2;
          break;
        case "color_Label_5":
          _selectColorLabelNum       = 5;
          color_Label_005.Foreground = color2;
          break;
        case "color_Label_6":
          _selectColorLabelNum       = 6;
          color_Label_006.Foreground = color2;
          break;
        case "color_Label_7":
          _selectColorLabelNum       = 7;
          color_Label_007.Foreground = color2;
          break;
        case "color_Label_8":
          _selectColorLabelNum       = 8;
          color_Label_008.Foreground = color2;
          break;
        case "color_Label_9":
          _selectColorLabelNum       = 9;
          color_Label_009.Foreground = color2;
          break;
      }

      var currentColor = ((SolidColorBrush)lb.Background).Color;
      // 计算反色
      var invertedColor = Color.FromArgb(255, (byte)~currentColor.R, (byte)~currentColor.G, (byte)~currentColor.B);
      lb.BorderThickness = new Thickness(3);
      lb.BorderBrush     = new SolidColorBrush(invertedColor);
      var hex = RemoveChars(lb.Background.ToString(), 2);
      var rgb = HexToRgb(hex);
      rgbTextBox.RGBText = rgb;
    }

    // 显示颜色的 label 鼠标离开事件
    private void color_label_MouseLeave(object sender, MouseEventArgs e)
    {
      if (sender is Label) label.BorderThickness = new Thickness(2);
    }

    // 候选框圆角、选中项背景圆角 和 候选框边框调节
    private void Nud11_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
    {
      if (hxk_Border == null) return;
      hxk_Border.CornerRadius    = hxc_CheckBox.IsChecked   == true ? new CornerRadius(nud11.Value) : new CornerRadius(0);
      hxz_Border.CornerRadius    = hxcbj_CheckBox.IsChecked == true ? new CornerRadius(nud12.Value) : new CornerRadius(0);
      hxk_Border.BorderThickness = new Thickness(nud13.Value);
    }

    // 配色列表双击事件
    private void ColorSchemeListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (e.ChangedButton == MouseButton.Left && colorSchemeListBox.SelectedItem != null)
      {
        var schemeColor = _配色方案[colorSchemeListBox.SelectedIndex];
        checkBox_Copy42.IsChecked = schemeColor.显示背景图;
        hxc_CheckBox.IsChecked    = schemeColor.显示候选窗圆角;
        hxcbj_CheckBox.IsChecked  = schemeColor.显示选中项背景圆角;
        nud11.Value               = schemeColor.候选窗圆角;
        nud12.Value               = schemeColor.选中项圆角;
        nud13.Value               = schemeColor.边框线宽;
        color_Label_1.Background  = new SolidColorBrush((Color)ColorConverter.ConvertFromString(schemeColor.下划线色)!);
        color_Label_2.Background  = new SolidColorBrush((Color)ColorConverter.ConvertFromString(schemeColor.光标色)!);
        color_Label_3.Background  = new SolidColorBrush((Color)ColorConverter.ConvertFromString(schemeColor.分隔线色)!);
        color_Label_4.Background  = new SolidColorBrush((Color)ColorConverter.ConvertFromString(schemeColor.窗口边框色)!);

        if (schemeColor.窗背景底色 == "")
        {
          color_Label_5.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFFFF")!);
          _bkColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFFFF")!);
          hxcds_CheckBox.IsChecked = true;
        }
        else
        {
          color_Label_5.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(schemeColor.窗背景底色)!);
          _bkColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(schemeColor.窗背景底色)!);
          hxcds_CheckBox.IsChecked = false;
        }
        color_Label_6.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(schemeColor.选中背景色)!);
        color_Label_7.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(schemeColor.选中字体色)!);
        color_Label_8.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(schemeColor.编码字体色)!);
        color_Label_9.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(schemeColor.候选字色)!);
      }
    }

    // 配色列表选中项改变事件
    private void ColorSchemeListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (colorSchemeListBox.SelectedItem != null)
      {
        if (saveButton.Content.ToString() == "保存配色")
          color_Scheme_Name_TextBox.Text = "";
        if (saveButton.Content.ToString() == "修改配色")
          color_Scheme_Name_TextBox.Text = colorSchemeListBox.SelectedItem.ToString();
      }
    }

    // 新建配色方案
    private void MenuItem_Click_1(object sender, RoutedEventArgs e)
    {
      saveButton.Content = "保存配色";
      saveButton.Visibility = Visibility.Visible;
      color_Scheme_Name_TextBox.Visibility = Visibility.Visible;
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
      color_Scheme_Name_TextBox.Visibility = Visibility.Visible;
      color_Scheme_Name_TextBox.Text += colorSchemeListBox.SelectedItem.ToString();
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
        _配色方案.RemoveAt(colorSchemeListBox.SelectedIndex);
        var jsonString = JsonConvert.SerializeObject(new { 配色方案 = _配色方案 }, Formatting.Indented);
        File.WriteAllText(_schemeFilePath, jsonString);

        colorSchemeListBox.Items.Remove(name);
        colorSchemeListBox.Items.Refresh();
      }

    }

    // 添加配色
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {

      var name = color_Scheme_Name_TextBox.Text.Trim();
      _colorScheme = new ColorScheme
      {
        名称           = name,
        候选窗圆角     = nud11.Value,
        选中项圆角     = nud12.Value,
        边框线宽       = nud13.Value,
        显示背景图     = checkBox_Copy42.IsChecked != null && (bool)checkBox_Copy42.IsChecked,
        显示候选窗圆角     = hxc_CheckBox.IsChecked    != null && (bool)hxc_CheckBox.IsChecked,
        显示选中项背景圆角 = hxcbj_CheckBox.IsChecked  != null && (bool)hxcbj_CheckBox.IsChecked,
        窗背景底色 = hxcds_CheckBox.IsChecked == true ? "" :
          RemoveChars(color_Label_5.Background.ToString(), 2),
        下划线色   = RemoveChars(color_Label_1.Background.ToString(), 2),
        光标色     = RemoveChars(color_Label_2.Background.ToString(), 2),
        分隔线色   = RemoveChars(color_Label_3.Background.ToString(), 2),
        窗口边框色 = RemoveChars(color_Label_4.Background.ToString(), 2),
        选中背景色 = RemoveChars(color_Label_6.Background.ToString(), 2),
        选中字体色 = RemoveChars(color_Label_7.Background.ToString(), 2),
        编码字体色 = RemoveChars(color_Label_8.Background.ToString(), 2),
        候选字色   = RemoveChars(color_Label_9.Background.ToString(), 2),
      };

      if (saveButton.Content.ToString() == "保存配色")
      {
        foreach (var item in colorSchemeListBox.Items)
        {
          if (item.ToString() == name)
          {
            MessageBox.Show("存在同名配色！");
            return;
          }
          if (color_Scheme_Name_TextBox.Text.Length == 0)
          {
            MessageBox.Show("请输入新的配色名称！");
            color_Scheme_Name_TextBox.Focus();
            return;
          }
        }
        _配色方案.Insert(0, _colorScheme);
        colorSchemeListBox.Items.Insert(0, name);
      }
      if (saveButton.Content.ToString() == "修改配色" && colorSchemeListBox.SelectedItem != null)
      {
        var n = colorSchemeListBox.SelectedIndex;
        _配色方案[n] = _colorScheme;
        colorSchemeListBox.Items.Clear();

        foreach (var scheme in _配色方案)
          colorSchemeListBox.Items.Add(scheme.名称);

        colorSchemeListBox.SelectedIndex = n;
      }
      var jsonString = JsonConvert.SerializeObject(new { 配色方案 = _配色方案 }, Formatting.Indented);
      File.WriteAllText(_schemeFilePath, jsonString);
    }

    private void Button3_Copy_Click(object sender, RoutedEventArgs e)
    {
      var selectedFontName = SelectFontName();
      if (selectedFontName == null) return;
      if (sender is Button { Name: "button3_Copy" }) textBox_Copy145.Text = selectedFontName;
    }

    private static string SelectFontName()
    {
      using var fontDialog = new FontDialog();

      // 设置初始字体选项（可选）
      // fontDialog.Font = new Font("Arial", 12);
      // 显示字体对话框并获取用户的选择结果
      return fontDialog.ShowDialog() == DialogResult.OK ? fontDialog.Font.Name : // 返回用户选择的字体名称
        null;
    }

    private void TextBox_Copy22_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (textBox_Copy22.Text == "")
      {
        textBox_Copy22.Text = @"⁠⁣"; // 有个隐藏符号
      }
    }

    private void TextBox_Copy23_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (textBox_Copy23.Text == "")
      {
        textBox_Copy23.Text = @"⁠⁣"; // 有个隐藏符号
      }
    }

    private void ComboBox3_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      textBox_Copy67.Text = ((ComboBoxItem)comboBox3.SelectedItem).Content.ToString();
    }

    private void Hxc_checkBox_Checked(object sender, RoutedEventArgs e)
    {
      if (hxk_Border != null)
        hxk_Border.CornerRadius = new CornerRadius(nud11.Value);
    }

    private void Hxc_checkBox_Unchecked(object sender, RoutedEventArgs e)
    {
      if (hxk_Border != null)
        hxk_Border.CornerRadius = new CornerRadius(0);
    }

    private void Hxcbj_checkBox_Checked(object sender, RoutedEventArgs e)
    {
      if (hxz_Border != null)
        hxz_Border.CornerRadius = new CornerRadius(nud12.Value);
    }
    private void Hxcbj_checkBox_Unchecked(object sender, RoutedEventArgs e)
    {
      if (hxz_Border != null)
        hxz_Border.CornerRadius = new CornerRadius(0);
    }

    private void Hxcds_checkBox_Checked(object sender, RoutedEventArgs e)
    {
      color_Label_5.Visibility = Visibility.Hidden;
      _bkColor = (SolidColorBrush)color_Label_5.Background;
      color_Label_005.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF000000")!);  // 黑色
      _selectColorLabelNum = 0;
    }

    private void Hxcds_checkBox_Unchecked(object sender, RoutedEventArgs e)
    {
      color_Label_5.Visibility = Visibility.Visible;
      color_Label_5.Background = _bkColor;
    }

    #endregion


  }
}
