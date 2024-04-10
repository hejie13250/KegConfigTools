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

namespace 小科狗配置
{
  /// <summary>
  /// HotKeyControl.xaml 的交互逻辑
  /// </summary>
  public partial class HotKeyControl : UserControl
  {
    private List<Key> pressedKeys = new();

    public HotKeyControl()
    {
      InitializeComponent();
    }

    public static DependencyProperty HotKeyProperty = DependencyProperty.Register(
        "HotKey", typeof(string), typeof(HotKeyControl), new PropertyMetadata(default(string)));

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
      pressedKeys.Remove(e.Key);

      // 构建热键字符串
      UpdateHotKeyString();
      e.Handled = true;
    }

    private void TxtHotKey_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      // 阻止输入法的字符输入
      e.Handled = true;
      HotKey = string.Empty;
    }

    private void UpdateHotKeyString()
    {
      // 过滤出修饰键和非修饰键 目前不支持 Key.LeftAlt, Key.RightAlt
      var modifierKeys = new[] { Key.LeftCtrl, Key.RightCtrl, Key.LeftShift, Key.RightShift, Key.LWin, Key.RWin };
      var modifiers = pressedKeys.Where(k => modifierKeys.Contains(k)).ToArray();
      var nonModifierKey = pressedKeys.Except(modifierKeys).FirstOrDefault();

      // 如果有非修饰键，则构建热键字符串
      if (nonModifierKey != Key.None)
      {
        HotKey = string.Join("+", modifiers.Concat(new[] { nonModifierKey }).Select(k => k.ToString()).ToArray());
      }
      else
      {
        HotKey = string.Empty;
      }
      txtHotKey.Text = HotKey;
      // 当热键更新时，触发事件
      HotKeyPressed?.Invoke(this, new RoutedEventArgs());
    }

    private void TxtHotKey_TextChanged(object sender, TextChangedEventArgs e)
    {
      txtHotKey.Text = txtHotKey.Text.Replace("ImeProcessed", "");
    }
  }
}
