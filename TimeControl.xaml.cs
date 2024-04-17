using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace 小科狗配置
{
  /// <summary>
  /// Time.xaml 的交互逻辑
  /// </summary>
  public partial class TimeControl : UserControl
  {
    public static readonly DependencyProperty TimeProperty =
        DependencyProperty.Register("Time", typeof(string), typeof(TimeControl),
            new FrameworkPropertyMetadata("20:20", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTimePropertyChanged));

    public event RoutedPropertyChangedEventHandler<string> TimeChanged;

    private string _time;
    public string Time
    {
      get { return (string)GetValue(TimeProperty); }
      set { SetValue(TimeProperty, value); }
    }

    public TimeControl()
    {
      InitializeComponent();
      nud1.ValueChanged += Nud_ValueChanged;
      nud2.ValueChanged += Nud_ValueChanged;
    }

    private static void OnTimePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      TimeControl timeControl = (TimeControl)d;
      string newValue = (string)e.NewValue;
      timeControl.SetTime(newValue);
    }

    private void SetTime(string strTime)
    {
      if (strTime != null && strTime.Contains(':'))
      {
        string[] parts = strTime.Split(':');
        if (parts.Length == 2 && int.TryParse(parts[0], out int hours) && int.TryParse(parts[1], out int minutes))
        {
          if (nud1.Value != hours) nud1.Value = hours;
          if (nud2.Value != minutes) nud2.Value = minutes;
          _time = strTime;
          RaiseTimeChanged(_time, strTime);
        }
      }
    }

    private void Nud_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
    {
      string newTime = $"{nud1.Value}:{nud2.Value}";
      if (Time != newTime)
      {
        Time = newTime;
      }
    }

    protected virtual void RaiseTimeChanged(string oldValue, string newValue)
    {
      RoutedPropertyChangedEventArgs<string> args = new RoutedPropertyChangedEventArgs<string>(oldValue, newValue);
      TimeChanged?.Invoke(this, args);
    }


    private void UserControl_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
      e.Handled = true;
    }
  }

}
