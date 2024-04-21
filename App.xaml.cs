using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using SkiaSharp;
using System.Threading;
using System.Windows;

namespace 小科狗配置
{
  /// <summary>
  /// App.xaml 的交互逻辑
  /// </summary>
  public partial class App : Application
  {
    private static Mutex mutex = null;

    protected override void OnStartup(StartupEventArgs e)
    {
      const string appName = "小科狗配置";
      bool createdNew;

      mutex = new Mutex(true, appName, out createdNew);

      if (!createdNew)
      {
        // 应用程序的另一个实例已经在运行
        //MessageBox.Show("应用程序已在运行。");
        Current.Shutdown(); // 关闭当前实例
        return;
      }

      base.OnStartup(e);


      LiveCharts.Configure(config =>
      config
          // you can override the theme 
          //.AddDarkTheme()

          // In case you need a non-Latin based font, you must register a typeface for SkiaSharp
          .HasGlobalSKTypeface(SKFontManager.Default.MatchCharacter('汉')) // <- Chinese 
                                                                          //.HasGlobalSKTypeface(SKFontManager.Default.MatchCharacter('あ')) // <- Japanese 
                                                                          //.HasGlobalSKTypeface(SKFontManager.Default.MatchCharacter('헬')) // <- Korean 
                                                                          //.HasGlobalSKTypeface(SKFontManager.Default.MatchCharacter('Ж'))  // <- Russian 
                                                                          //.HasGlobalSKTypeface(SKFontManager.Default.MatchCharacter('أ'))  // <- Arabic 
                                                                          //.UseRightToLeftSettings() // Enables right to left tooltips 

          // finally register your own mappers
          // you can learn more about mappers at:
          // https://livecharts.dev/docs/wpf/2.0.0-rc2/Overview.Mappers

          // here we use the index as X, and the population as Y 
          //.HasMap<City>((city, index) => new(index, city.Population))
          // .HasMap<Foo>( .... ) 
          // .HasMap<Bar>( .... ) 
          );




    }



    public new void Exit()
    {
      this.MainWindow.Close();
      Application.Current.Shutdown();
    }
  }

}
