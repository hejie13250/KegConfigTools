using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace 小科狗配置
{
  public class BasePage : Page, INotifyPropertyChanged
  {
    public event EventHandler<string> NameOfSelectedGroupBoxChanged;
    private string _NameOfSelectedGroupBox;

    public string NameOfSelectedGroupBox
    {
      get { return _NameOfSelectedGroupBox; }
      set
      {
        if (_NameOfSelectedGroupBox != value)
        {
          _NameOfSelectedGroupBox = value;
          OnPropertyChanged(nameof(NameOfSelectedGroupBox));
          NameOfSelectedGroupBoxChanged?.Invoke(this, _NameOfSelectedGroupBox);
        }
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
 }