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
using System.Data.SQLite;
using System.Data;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using static 小科狗配置.GlobalSetting;
using static 小科狗配置.ModiflyTable;

namespace 小科狗配置
{
  /// <summary>
  /// ModiflyTable.xaml 的交互逻辑
  /// </summary>
  public partial class ModiflyTable : BasePage
  {

    #region 获取GroupBox的Header用于主窗口导航事件
    private void GroupBox_MouseEnter(object sender, MouseEventArgs e)
    {
      if (sender is not GroupBox groupBox) return;
      NameOfSelectedGroupBox = groupBox.Header.ToString();
    }

    #endregion

    public class ListViewDataItem : INotifyPropertyChanged
    {

      private string _key;
      private string _value;
      private double _weight;
      private string _fc;
      private bool _isDel;
      private bool _isMod;
      private bool _isAdd;

      public string Key
      {
        get => _key;
        set => SetProperty(ref _key, value);
      }

      public string Value
      {
        get => _value;
        set => SetProperty(ref _value, value);
      }

      public double Weight
      {
        get => _weight;
        set => SetProperty(ref _weight, value);
      }

      public string FC
      {
        get => _fc;
        set => SetProperty(ref _fc, value);
      }

      public bool IsDel
      {
        get => _isDel;
        set => SetProperty(ref _isDel, value);
      }
      public bool IsMod
      {
        get => _isMod;
        set => SetProperty(ref _isMod, value);
      }
      public bool IsAdd
      {
        get => _isAdd;
        set => SetProperty(ref _isAdd, value);
      }
      public event PropertyChangedEventHandler PropertyChanged;

      protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
      {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
      {
        if (EqualityComparer<T>.Default.Equals(storage, value)) return false;
        storage = value;
        OnPropertyChanged(propertyName);
        return true;
      }
    }

    public ObservableCollection<ListViewDataItem> listViewDataItems { get; set; }
    // 设置行头为行号，从1开始计数


    readonly string dbPath; // Keg.db 
    string tableName;       // 表名称
    int pageCount;          // 页码

    public ModiflyTable()
    {
      dbPath = Base.kegPath + "Keg.db";
      InitializeComponent();
      LoadTableNames();
      //listViewDataItems = new ObservableCollection<ListViewDataItem>();
    }

    #region 读写db
    /// <summary>
    /// 从 db 读取表名到 ComboBox
    /// </summary>
    private void LoadTableNames()
    {
      SQLiteConnection connection = new ($"Data Source={dbPath}");
      connection.Open();
      try
      {
        comboBox.Items.Clear();
        using var command = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'", connection);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
          var labelName = reader.GetString(0);
          comboBox.Items.Add(labelName);
          comboBox.SelectedIndex = 0;
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Error loading table names: {ex.Message}");
      }
      finally
      {
        connection.Close();
      }
    }

    /// <summary>
    /// 获取指定表内词条总数
    /// </summary>
    /// <param name="tableName">表名称</param>
    /// <returns></returns>
    private int GetTableRowCount(string tableName)
    {
      using SQLiteConnection connection = new($"Data Source={dbPath};Version=3;");
      string query = $"SELECT COUNT(*) FROM '{tableName}'";
      connection.Open();
      using SQLiteCommand command = new SQLiteCommand(query, connection);
      int rowCount = Convert.ToInt32(command.ExecuteScalar());
      return rowCount;
    }

    /// <summary>
    /// 从指定表内获取指定条数的数据
    /// </summary>
    /// <param name="offset"></param>
    /// <param name="limit"></param>
    /// <returns></returns>
    private DataTable GetTopRecordsFromTable(int offset, int limit)
    {
      using SQLiteConnection connection = new($"Data Source={dbPath};Version=3;");
      string query = $"SELECT * FROM '{tableName}' LIMIT {limit} OFFSET {offset}";
      connection.Open();
      using SQLiteDataAdapter adapter = new(query, connection);
      DataTable dataTable = new();
      adapter.Fill(dataTable);
      return dataTable;
    }





    #endregion

    /// <summary>
    /// 将数据填充到 ListView
    /// </summary>
    /// <param name="tableName"></param>
    private void PopulateListView(DataTable dataTable)
    {
      listViewDataItems = new ObservableCollection<ListViewDataItem>();

      foreach (DataRow row in dataTable.Rows)
      {
        listViewDataItems.Add(new ListViewDataItem
        {
          Key = row["Key"].ToString(),
          Value = row["Value"].ToString(),
          Weight = Convert.ToDouble(row["Weight"]),
          FC = row["FC"].ToString()
        });
      }

      listView.ItemsSource = listViewDataItems;
    }


    private void GetList_button_Click(object sender, RoutedEventArgs e)
    {

      //listViewDataItems.Clear();
      //LoadTableNames();
    }

    private void ComboBox_MouseEnter(object sender, MouseEventArgs e)
    {
      comboBox.Focus();
    }

    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      tableName = comboBox.SelectedValue as string;
      int count = GetTableRowCount(tableName);
      pageCount = (int)(Math.Ceiling((decimal)(count / 5000)));
      textBox1.Text = $"词条总数：{count}   共计 {pageCount} 页";

      DataTable dataTable = GetTopRecordsFromTable(0, 5000);

      PopulateListView(dataTable);
    }


    private void Reload_button_Click(object sender, RoutedEventArgs e)
    {

    }

    private void Del_button_Click(object sender, RoutedEventArgs e)
    {

      foreach (ListViewDataItem item in listView.SelectedItems)
      {
        item.IsDel = true;
      }
    }

    private void Add_button_Click(object sender, RoutedEventArgs e)
    {
      ListViewDataItem newItem = new()
      {
        Key = "新键",
        Value = "新值",
        Weight = 1450,
        FC = "90EE90",
        IsAdd = true
      };

      listViewDataItems.Add(newItem);
    }

    private void Mod_button_Click(object sender, RoutedEventArgs e)
    {
      foreach (ListViewDataItem item in listView.SelectedItems)
      {
        item.IsMod = true;
      }
    }
  }
}
