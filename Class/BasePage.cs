using System;
using System.ComponentModel;

namespace 小科狗配置.Class
{
  public class BasePage : System.Windows.Controls.Page, INotifyPropertyChanged
  {
    public event EventHandler<string> NameOfSelectedGroupBoxChanged;
    private string _nameOfSelectedGroupBox;

    public string NameOfSelectedGroupBox
    {
      get => _nameOfSelectedGroupBox;
      set
      {
        if (_nameOfSelectedGroupBox == value) return;
        _nameOfSelectedGroupBox = value;
        OnPropertyChanged(nameof(NameOfSelectedGroupBox));
        NameOfSelectedGroupBoxChanged?.Invoke(this, _nameOfSelectedGroupBox);
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
 }