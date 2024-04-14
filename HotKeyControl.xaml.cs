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
  /// <summary>
  /// HotKeyControl.xaml 的交互逻辑
  /// </summary>



  //  public partial class HotKeyControl : UserControl
  //  {
  //    private readonly List<Key> _pressedKeys = new();

  //    public HotKeyControl()
  //    {
  //      InitializeComponent();
  //      InitializeHotKey();
  //    }

  //    #region Dependency Property

  //    public static readonly DependencyProperty HotKeyProperty = DependencyProperty.Register(
  //        "HotKey", typeof(string), typeof(HotKeyControl), new PropertyMetadata("<1=><2=><3=><4=>", OnHotKeyPropertyChanged));

  //    public string HotKey
  //    {
  //      get => (string)GetValue(HotKeyProperty);
  //      set => SetValue(HotKeyProperty, value);
  //    }

  //    private static void OnHotKeyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
  //    {
  //      //var control = (HotKeyControl)d;
  //      //control.OnHotKeyPropertyChanged(e.NewValue as string);
  //      var control = (HotKeyControl)d;
  //      if (control != null)
  //      {
  //        control.OnHotKeyPropertyChanged(e.NewValue as string);
  //      }
  //    }

  //    protected virtual void OnHotKeyPropertyChanged(string newValue)
  //    {
  //      //txtHotKey.Text = string.IsNullOrEmpty(newValue) ? "请输入热键" : newValue;
  //      if (txtHotKey != null) // 确保txtHotKey已经初始化
  //      {
  //        txtHotKey.Text = string.IsNullOrEmpty(newValue) ? "请输入热键" : newValue;
  //      }
  //    }


  //    #endregion

  //    #region Event Handlers

  //    private void TxtHotKey_PreviewKeyDown(object sender, KeyEventArgs e)
  //    {
  //      if (!_pressedKeys.Contains(e.Key) && _pressedKeys.Count < 4)
  //        _pressedKeys.Add(e.Key);

  //      UpdateHotKeyString();
  //      e.Handled = true;
  //    }
  //    private void TxtHotKey_PreviewKeyUp(object sender, KeyEventArgs e)
  //    {
  //      if (_pressedKeys.Contains(e.Key))
  //        _pressedKeys.Remove(e.Key);

  //      if (_pressedKeys.Count == 0)
  //      {
  //        //让TxtHotKey失去焦点
  //        //Keyboard.ClearFocus();
  //        focusTextBox.Focus();
  //        txtHotKey.Foreground = new SolidColorBrush(Colors.Gray);
  //      }
  //    }

  //    private void Timer_Tick(object sender, EventArgs e)
  //    {
  //      UpdateHotKeyString();
  //    }

  //    private void Button_Click(object sender, RoutedEventArgs e)
  //    {
  //      ResetHotKey();
  //      HotKeyPressed?.Invoke(this, new RoutedEventArgs());
  //    }

  //    private void TxtHotKey_GotFocus(object sender, RoutedEventArgs e)
  //    {
  //      txtHotKey.Foreground = new SolidColorBrush(Colors.Red);
  //    }

  //    private void TxtHotKey_LostFocus(object sender, RoutedEventArgs e)
  //    {
  //      txtHotKey.Foreground = new SolidColorBrush(Colors.Gray);
  //      _pressedKeys.Clear();
  //    }

  //    private void TxtHotKey_MouseEnter(object sender, MouseEventArgs e)
  //    {
  //      delBtn.Visibility = Visibility.Visible;
  //    }

  //    private void TxtHotKey_MouseLeave(object sender, MouseEventArgs e)
  //    {
  //      delBtn.Visibility = Visibility.Hidden;
  //    }

  //    #endregion

  //    #region Private Methods

  //    private void InitializeHotKey()
  //    {
  //      //HotKey = string.Empty;
  //      //DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
  //      //timer.Tick += Timer_Tick;
  //      HotKey = string.Empty;
  //      DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
  //      timer.Tick += Timer_Tick;
  //      //timer.Start(); // 确保启动计时器

  //    }


  //    private void UpdateHotKeyString()
  //    {
  //      var modifierKeys = new[] { Key.LeftCtrl, Key.RightCtrl, Key.LeftShift, Key.RightShift, Key.LWin, Key.RWin };
  //      var functionKeys = Enum.GetValues(typeof(Key)).OfType<Key>()
  //          .Where(k => k >= Key.F1 && k <= Key.F24).ToArray();

  //      var modifiers = _pressedKeys.Where(k => modifierKeys.Contains(k)).ToArray();
  //      var nonModifierKey = _pressedKeys.Except(modifierKeys).FirstOrDefault();

  //      if (nonModifierKey == Key.None)
  //      {
  //        HotKey = modifiers.Any() ? string.Join("+", modifiers.Select(k => k.ToString())) : string.Empty;
  //      }
  //      else
  //      {
  //        HotKey = modifiers.Any() ? string.Join("+", modifiers.Select(k => k.ToString()).Concat(new[] { nonModifierKey.ToString() })) : nonModifierKey.ToString();
  //      }

  //      HotKey = HotKey.Replace("ImeProcessed", "");
  //      txtHotKey.Text = HotKey;
  //      HotKeyPressed?.Invoke(this, new RoutedEventArgs());
  //    }

  //    private void ResetHotKey()
  //    {
  //      txtHotKey.Focus();
  //      _pressedKeys.Clear();
  //      HotKey = "<1=><2=><3=><4=>";
  //      txtHotKey.Text = "请输入热键";
  //    }

  //    #endregion

  //    public event RoutedEventHandler HotKeyPressed;
  //  }
  //}


















  public partial class HotKeyControl : UserControl
  {
    private List<Key> pressedKeys = new();
    private string oldHotKey;

    public string HotKey
    {
      get { return (string)GetValue(HotKeyProperty); }
      set { SetValue(HotKeyProperty, value); }
    }
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
        if(newValue.Length > 0) 
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
        DispatcherTimer timer = new()
        {
          Interval = TimeSpan.FromMilliseconds(100) // 延迟100毫秒
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
        HotKey = modifiers.Any() ? string.Join("+", modifiers.Select(k => k.ToString())) : string.Empty;
      }
      else
      {
        HotKey = modifiers.Any() ? string.Join("+", modifiers.Select(k => k.ToString()).Concat(new[] { nonModifierKey.ToString() })) : nonModifierKey.ToString();
      }

      HotKey = HotKey.Replace("ImeProcessed", "");
      txtHotKey.Text = HotKey;
      HotKeyPressed?.Invoke(this, new RoutedEventArgs());
    }






  }
}


