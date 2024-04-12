using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace 小科狗配置
{
  /// <summary>
  /// HotKeyControl.xaml 的交互逻辑
  /// </summary>
  /// 
  public partial class HotKeyControl : UserControl
  {
    private List<Key> pressedKeys = new List<Key>();

    public HotKeyControl()
    {
      InitializeComponent();
    }

    //public static DependencyProperty HotKeyProperty = DependencyProperty.Register(
    //    "HotKey", typeof(string), typeof(HotKeyControl), new PropertyMetadata(default(string)));


    public static readonly DependencyProperty HotKeyProperty = DependencyProperty.Register(
        "HotKey", typeof(string), typeof(HotKeyControl), new PropertyMetadata(default(string), OnHotKeyPropertyChanged));

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
        txtHotKey.Text = newValue;
      }
      else
      {
        // 如果HotKey属性被设置为null或空字符串，可以选择重置txtHotKey的文本
        txtHotKey.Text = "请输入热键";
      }
    }

    public string HotKey
    {
      get { return (string)GetValue(HotKeyProperty); }
      set { SetValue(HotKeyProperty, value); }
    }

    // 定义一个事件
    public event RoutedEventHandler HotKeyPressed;

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

    private void TxtHotKey_PreviewKeyUp(object sender, KeyEventArgs e)
    {
      // 移除释放的键
      //pressedKeys.Remove(e.Key);

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
          Interval = TimeSpan.FromMilliseconds(100) // 延迟100毫秒
        };
        timer.Tick += Timer_Tick;
        timer.Start();
      }
      e.Handled = true;
      // 转移焦点
      delBtn.Focus();
    }


    private void Timer_Tick(object sender, EventArgs e)
    {
      // 确保在所有按键被释放后，UpdateHotKeyString方法被调用
      UpdateHotKeyString();
    }
    private void UpdateHotKeyString()
    {
      // 过滤出左右版本的修饰键和非修饰键
      var modifierKeys = new[] { Key.LeftCtrl, Key.RightCtrl, Key.LeftShift, Key.RightShift, Key.LWin, Key.RWin };
      var functionKeys = new[] {
      Key.F1, Key.F2, Key.F3, Key.F4, Key.F5, Key.F6, Key.F7, Key.F8, Key.F9, Key.F10, Key.F11, Key.F12,
      Key.F13, Key.F14, Key.F15, Key.F16, Key.F17, Key.F18, Key.F19, Key.F20, Key.F21, Key.F22, Key.F23, Key.F24,
      };
      var modifiers = pressedKeys.Where(k => modifierKeys.Contains(k)).ToArray();
      var nonModifierKey = pressedKeys.Except(modifierKeys).FirstOrDefault();

      // 如果有非修饰键，则构建热键字符串
      if (nonModifierKey != Key.None)
      {
        // 如果非修饰键是功能键，则可以单独使用
        if (functionKeys.Contains(nonModifierKey))
        {
          HotKey = nonModifierKey.ToString();
        }
        else
        {
          // 否则，字母和数字键必须在修饰键后面
          if (modifiers.Length > 0)
          {
            HotKey = string.Join("+", modifiers.Concat(new[] { nonModifierKey }).Select(k => k.ToString()).ToArray());
          }
          else
          {
            // 如果没有修饰键，则清空热键字符串
            HotKey = string.Empty;
          }
        }
      }
      else
      {
        // 如果没有非修饰键，且修饰键列表不为空，则只显示修饰键
        if (modifiers.Length > 0)
        {
          HotKey = string.Join("+", modifiers.Select(k => k.ToString()).ToArray());
        }
        else
        {
          // 如果没有非修饰键，且修饰键列表为空，则清空热键字符串
          HotKey = string.Empty;
        }
      }

      // 去除因输入法获取到的文本
      HotKey = HotKey.Replace("ImeProcessed", "");
      // 更新txtHotKey的文本
      txtHotKey.Text = HotKey;
      // 当热键更新时，触发事件
      HotKeyPressed?.Invoke(this, new RoutedEventArgs());
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
      txtHotKey.Text = "请输入热键";
      txtHotKey.Focus();
      pressedKeys.Clear();
      HotKey = "";
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


  }


}