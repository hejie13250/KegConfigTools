using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace 小科狗配置
{
  public class ViewModel //: INotifyPropertyChanged
  {
    public ISeries[] Series { get; set; }
    public Axis[] XAxes { get; set; }
    public Axis[] YAxes { get; set; }
  }
}
