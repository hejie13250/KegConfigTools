using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using UserControl = System.Windows.Controls.UserControl;

namespace 小科狗配置
{
  public partial class HotKeyControl : UserControl
  {
    private List<Key> pressedKeys = new List<Key>();
    private string oldHotKey, oldTxt;

    public string HotKey
    {
      get { return (string)GetValue(HotKeyProperty); }
      set { SetValue(HotKeyProperty, value); }
    }


    // 键与缩写的映射表，使用Tuple<string, string>来存储
    private List<Tuple<string, string>> keyMap = new List<Tuple<string, string>>()
    {
        Tuple.Create("PageUp"         , "PU" ),
        Tuple.Create("Next"           , "PD" ),
        Tuple.Create("Home"           , "HOM"),
        Tuple.Create("End"            , "END"),
        Tuple.Create("LeftShift"      , "LSH"),
        Tuple.Create("RightShift"     , "RSH"),
        Tuple.Create("LeftCtrl"       , "LCT"),
        Tuple.Create("RightCtrl"      , "RCT"),
        Tuple.Create("LWin"           , "LWI"),
        Tuple.Create("RWin"           , "RWI"),
        Tuple.Create("Capital"        , "CAP"),
        Tuple.Create("Oem3"           , "`"  ),
        Tuple.Create("D0"             , "0"  ),
        Tuple.Create("D1"             , "1"  ),
        Tuple.Create("D2"             , "2"  ),
        Tuple.Create("D3"             , "3"  ),
        Tuple.Create("D4"             , "4"  ),
        Tuple.Create("D5"             , "5"  ),
        Tuple.Create("D6"             , "6"  ),
        Tuple.Create("D7"             , "7"  ),
        Tuple.Create("D8"             , "8"  ),
        Tuple.Create("D9"             , "9"  ),
        Tuple.Create("OemMinus"       , "-"  ),
        Tuple.Create("OemPlus"        , "="  ),
        Tuple.Create("OemOpenBrackets", "["  ),
        Tuple.Create("Oem6"           , "]"  ),
        Tuple.Create("Oem5"           , "\\" ),
        Tuple.Create("Oem1"           , ";"  ),
        Tuple.Create("OemQuotes"      , "'"  ),
        Tuple.Create("OemComma"       , ","  ),
        Tuple.Create("OemPeriod"      , "."  ),
        Tuple.Create("OemQuestion"    , "?"  ),
        Tuple.Create("Back"           , "BAC"),
        Tuple.Create("Delete"         , "DEL"),
        Tuple.Create("Return"         , "ENT"),
        Tuple.Create("UP"             , "UP" ),
        Tuple.Create("Down"           , "DOW"),
        Tuple.Create("Left"           , "LEF"),
        Tuple.Create("Right"          , "RIG"),
        Tuple.Create("Escape"         , "ESC"),
        Tuple.Create("Scroll"         , "SCR"),
        Tuple.Create("Pause"          , "PAU"),
        Tuple.Create("Insert"         , "INS"),
        Tuple.Create("NumLock"        , "NUM"),
        Tuple.Create("Add"            , "x+" ),
        Tuple.Create("Subtract"       , "x-" ),
        Tuple.Create("Multiply"       , "x*" ),
        Tuple.Create("Divide"         , "x/" ),
        Tuple.Create("NumPad0"        , "x0" ),
        Tuple.Create("NumPad1"        , "x1" ),
        Tuple.Create("NumPad2"        , "x2" ),
        Tuple.Create("NumPad3"        , "x3" ),
        Tuple.Create("NumPad4"        , "x4" ),
        Tuple.Create("NumPad5"        , "x5" ),
        Tuple.Create("NumPad6"        , "x6" ),
        Tuple.Create("NumPad7"        , "x7" ),
        Tuple.Create("NumPad8"        , "x8" ),
        Tuple.Create("NumPad9"        , "x9" ),
        Tuple.Create("Decimal"        , "x." )
    };


    public HotKeyControl()
    {
      InitializeComponent();
    }

    public static readonly DependencyProperty HotKeyProperty = DependencyProperty.Register(
        "HotKey", typeof(string), typeof(HotKeyControl), new PropertyMetadata(default(string), OnHotKeyPropertyChanged));

    // 定义一个事件
    public event RoutedEventHandler HotKeyPressed;

    private static void OnHotKeyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      var control = (HotKeyControl)d;
      control.OnHotKeyPropertyChanged(e.OldValue as string, e.NewValue as string);
    }

    protected virtual void OnHotKeyPropertyChanged(string oldValue, string newValue)
    {
      // 当HotKey属性改变时，更新txtHotKey的文本
      if (!string.IsNullOrEmpty(newValue))
      {
        Convert(newValue);
      }
      else
      {
        // 如果HotKey属性被设置为null或空字符串，可以选择重置txtHotKey的文本
        txtHotKey.Text = "请输入热键";
      }
    }


    private void Convert(string newValue)
    {
      string pattern = @"<1=(.*?)><2=(.*?)><3=(.*?)><4=(.*?)>";
      Match matchs = Regex.Match(newValue, pattern);
      if (matchs.Success)
      {
        newValue = $"{matchs.Groups[1].Value}|{matchs.Groups[2].Value}|{matchs.Groups[3].Value}|{matchs.Groups[4].Value}";
        newValue = newValue.Trim('|', '|').Replace('|', '+');
        if (newValue.Length > 0)
        {
          txtHotKey.Foreground = new SolidColorBrush(Colors.Black);
          txtHotKey.Text = newValue;
        }

      }
    }


    private void Button_Click(object sender, RoutedEventArgs e)
    {
      pressedKeys.Clear();
      txtHotKey.Focus();
      HotKey = "<1=><2=><3=><4=>";
      txtHotKey.Text = "请输入热键";
      HotKeyPressed?.Invoke(this, new RoutedEventArgs());
    }

    private void TxtHotKey_GotFocus(object sender, RoutedEventArgs e)
    {
      txtHotKey.Foreground = new SolidColorBrush(Colors.Red);
    }

    private void TxtHotKey_LostFocus(object sender, RoutedEventArgs e)
    {
      txtHotKey.Foreground = new SolidColorBrush(Colors.Gray);
      pressedKeys.Clear();
    }

    private void TxtHotKey_MouseEnter(object sender, MouseEventArgs e)
    {
      if (txtHotKey.Text != "点击输入热键")
        delBtn.Visibility = Visibility.Visible;
    }

    private void TxtHotKey_MouseLeave(object sender, MouseEventArgs e)
    {
      delBtn.Visibility = Visibility.Hidden;
    }


    // 按下按键
    private void TxtHotKey_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      // 添加按下的键到列表中，最多四个键
      if (!pressedKeys.Contains(e.Key) && pressedKeys.Count < 4)
      {
        pressedKeys.Add(e.Key);
      }

      // 构建热键字符串
      UpdateHotKeyString();
      e.Handled = true;
    }

    // 松开按键
    private void TxtHotKey_PreviewKeyUp(object sender, KeyEventArgs e)
    {
      // 如果有按键按下，则更新热键字符串
      if (pressedKeys.Count > 0)
      {
        UpdateHotKeyString();
      }
      else
      {
        // 如果没有按键按下，启动一个定时器来更新HotKey
        DispatcherTimer timer = new DispatcherTimer()
        {
          Interval = TimeSpan.FromMilliseconds(200) // 延迟100毫秒
        };
        timer.Tick += Timer_Tick;
        timer.Start();
      }
      e.Handled = true;
      // 转移焦点
      focusTextBox.Focus();
      txtHotKey.Foreground = new SolidColorBrush(Colors.Black);
    }

    // 确保在所有按键被释放后，UpdateHotKeyString方法被调用
    private void Timer_Tick(object sender, EventArgs e)
    {
      UpdateHotKeyString();
    }

    private void UpdateHotKeyString()
    {
      string hoKey;
      var modifierKeys = new[] { Key.LeftCtrl, Key.RightCtrl, Key.LeftShift, Key.RightShift, Key.LWin, Key.RWin };
      var functionKeys = Enum.GetValues(typeof(Key)).OfType<Key>()
          .Where(k => k >= Key.F1 && k <= Key.F24).ToArray();

      var modifiers = pressedKeys.Where(k => modifierKeys.Contains(k)).ToArray();
      var nonModifierKey = pressedKeys.Except(modifierKeys).FirstOrDefault();

      if (nonModifierKey == Key.None)
      {
        hoKey = modifiers.Any() ? string.Join("#", modifiers.Select(k => k.ToString())) : string.Empty;
      }
      else
      {
        hoKey = modifiers.Any() ? string.Join("#", modifiers.Select(k => k.ToString()).Concat(new[] { nonModifierKey.ToString() })) : nonModifierKey.ToString();
      }
      // 去掉因输入法和Alt 输入的字符串
      hoKey = hoKey.Replace("ImeProcessed", "").Replace("System", "");

      if (hoKey == "")
      {
        HotKey = "<1=><2=><3=><4=>";
        txtHotKey.Text = "请输入热键";
        txtHotKey.Foreground = new SolidColorBrush(Colors.Gray);
      }
      else
      {
        // 转换按键字符串名称
        foreach (var key in keyMap)
          hoKey = hoKey.Replace(key.Item1, key.Item2);

        txtHotKey.Text = hoKey.Trim('#', '#').Replace('#', '+');

        string pattern = "";
        switch (pressedKeys.Count)
        {
          case 1: pattern = "(.*)"; break;
          case 2: pattern = "(.*)#(.*)"; break;
          case 3: pattern = "(.*)#(.*)#(.*)"; break;
          case 4: pattern = "(.*)#(.*)#(.*)#(.*)"; break;
        }
        Match matchs = Regex.Match(hoKey, pattern);
        if (matchs.Success)
          hoKey = $"{matchs.Groups[1].Value}+{matchs.Groups[2].Value}+{matchs.Groups[3].Value}+{matchs.Groups[4].Value}";
        switch (pressedKeys.Count)
        {
          case 1: hoKey = $"<1={matchs.Groups[1].Value}><2=><3=><4=>"; break;
          case 2: hoKey = $"<1={matchs.Groups[1].Value}><2={matchs.Groups[2].Value}><3=><4=>"; break;
          case 3: hoKey = $"<1={matchs.Groups[1].Value}><2={matchs.Groups[2].Value}><3={matchs.Groups[3].Value}><4=>"; break;
          case 4: hoKey = $"<1={matchs.Groups[1].Value}><2={matchs.Groups[2].Value}><3={matchs.Groups[3].Value}><4={matchs.Groups[4].Value}>"; break;
        }

        //hoKey = hoKey.Trim('#', '#').Replace('#', '+');
        HotKey = hoKey;
        //txtHotKey.Text = hoKey;
      }
      HotKeyPressed?.Invoke(this, new RoutedEventArgs());
    }


  }
}


