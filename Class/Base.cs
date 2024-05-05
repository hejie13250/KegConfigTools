using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;

namespace 小科狗配置.Class
{
  /// <summary>
  /// 读写配置项文件
  /// </summary>
  public static class Base
  {
    private static readonly string AppPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    public static           string KegPath;                        // 小科狗主程序目录
    public static           string SQLiteDB_Path;
    public static           string LevelDB_Path;
    public static           IntPtr hWnd;



    #region 读写配置项
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
    public static void SetValue(string section, string key, string value)
    {
      var settingConfigFile = $"{AppPath}\\configs\\窗口配置.ini";
      WritePrivateProfileString(section, key, value, settingConfigFile);
    }

    /// <summary>
    /// 读配置文件
    /// </summary>
    /// <param name="section">配置项</param>
    /// <param name="key">键</param>
    /// <returns>命令行</returns>
    public static string GetValue(string section, string key)
    {
      var settingConfigFile = $"{AppPath}\\configs\\窗口配置.ini";
      if (File.Exists(settingConfigFile))
      {
        StringBuilder sb = new(255);
        GetPrivateProfileString(section, key, "", sb, 255, settingConfigFile);
        return sb.ToString();
      }
      else return string.Empty;
    }

    #endregion


    #region 消息接口定义

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, uint flags, uint timeout, out IntPtr pdwResult);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);


    private const uint Abortifhung     = 0x0002;
    private const uint Flags           = Abortifhung;
    private const uint Timeout         = 500;
    private const int  WmUser          = 0x0400;             // 根据Windows API定义
    public const uint KwmResetpiple   = (uint)WmUser + 200; //重置双轮流通信命名管道
    public const uint KwmReset        = (uint)WmUser + 201; //重置配置
    public const uint KwmSet0         = (uint)WmUser + 202; //权重全置为0
    public const uint KwmGetset       = (uint)WmUser + 203; //由剪切板指定方案名,从而获得该方案的配置
    public const uint KwmInsert       = (uint)WmUser + 204; //根据剪切板的带方案名(第一行数据)的词条插入词条
    public const uint KwmUpbase       = (uint)WmUser + 205; //更新内存数据库
    public const uint KwmSavebase     = (uint)WmUser + 206; //保存内存数据库
    public const uint KwmGetdatapath  = (uint)WmUser + 207; //从剪切板获得文本码表路径并加载该路径的文本码表到内存数据库
    public const uint KwmGetdef       = (uint)WmUser + 208; //把默认无方案名的配置模板吐到剪切板
    public const uint KwmSet2All      = (uint)WmUser + 209; //设置当前码字方案为所有进程的初始方案格式:《所有进程默认初始方案=  》
    public const uint KwmGetwritepath = (uint)WmUser + 210; //从剪切板获得导出的码字的文件夹路+导出类据 ,格式:path->方案名1#方案名2 所有方案名为ALL
    public const uint KwmUpqjset      = (uint)WmUser + 211; //读取说明文本更新全局设置
    public const uint KwmUppfset      = (uint)WmUser + 212; //从剪切板取皮肤png或gif文件的全路径设置,更新状态之肤 格式:文件全路径放到剪切板
    public const uint KwmGetallname   = (uint)WmUser + 213; //把所有方案名吐到剪切板,一行一个方案名
    public const uint KwmGetallzstj   = (uint)WmUser + 214; //把字数与速度的所有统计数据吐到剪切板 格式见字数统计界面的样子,具体见剪切板
    public const uint KwmGetpath      = (uint)WmUser + 215; //获得小科狗夹子的路径 吐到剪切板
    #endregion



    public static bool PostMessage(uint msg)
    {
      return PostMessage(hWnd, msg, IntPtr.Zero, IntPtr.Zero);
    }

    public static void SendMessageTimeout(uint msg)
    {
      SendMessageTimeout(hWnd, msg, IntPtr.Zero, IntPtr.Zero, Flags, Timeout, out _);
    }

    /// <summary>
    /// 获取小科狗主目录
    /// </summary>
    /// <returns></returns>
    public static void GetKegPath()
    {
      hWnd = FindWindow("CKegServer_0", null); //窗口句柄

      GetProcessPathFormServer();
      var kegText = File.ReadAllText(KegPath + "Keg.txt");
      const string pattern = "《打字字数统.*?=(.*)》";
      var          matches = Regex.Matches(kegText, pattern);
      foreach (Match match in matches)
        switch (match.Groups[1].Value)
        {
          case "是":
            SQLiteDB_Path = KegPath + @"Keg.db";
            LevelDB_Path = KegPath + @"zj\";
            break;
          case "不是":
            SQLiteDB_Path = @"C:\SiKegInput\Keg.db";
            LevelDB_Path = @"C:\SiKegInput\zj\";
            break;
        }

    }


    // private static string GetProcessPath()
    // {
    //   var processes = Process.GetProcessesByName("KegServer");
    //   var str       =  processes.Length > 0 ? processes[0].MainModule!.FileName : string.Empty;
    //   var path =  str.Replace("KegServer.exe", "");
    //   SetValue("window", "keg_path", path);
    //   return path;
    // }

    private static void GetProcessPathFormServer()
    {
      try
      {
        SendMessageTimeout(KwmGetpath);
        Thread.Sleep(200);
        KegPath = Clipboard.GetText();
        SetValue("window", "keg_path", KegPath);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"操作失败，请重试！");
        Console.WriteLine(ex.Message);
        ((App)Application.Current).Exit();
      }
    }







  }
}
