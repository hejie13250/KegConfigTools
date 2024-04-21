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

    public class MyDataItem
    {
      public string Key { get; set; }
      public string Value { get; set; }
      public double Weight { get; set; }
      public string FC { get; set; }
    }

    // 设置行头为行号，从1开始计数
    private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
    {
      e.Row.Header = (e.Row.GetIndex() + 1).ToString();
    }



    readonly string dbPath; // Keg.db 
    string labelName;       //方案名称



    public ModiflyTable()
    {
      dbPath = Base.kegPath + "Keg.db";
      InitializeComponent();

      //List<MyDataItem> items = new()
      //  {
      //      new MyDataItem { Key = "Alpha", Value = "100", Weight = 2.5, FC = "X" },
      //      new MyDataItem { Key = "Beta", Value = "200", Weight = 3.6, FC = "Y" },
      //      new MyDataItem { Key = "Gamma", Value = "300", Weight = 4.7, FC = "Z" },
      //      new MyDataItem { Key = "Delta", Value = "400", Weight = 5.8, FC = "W" },
      //      new MyDataItem { Key = "Epsilon", Value = "500", Weight = 6.9, FC = "V" },
      //      new MyDataItem { Key = "Alpha", Value = "100", Weight = 2.5, FC = "X" },
      //      new MyDataItem { Key = "Beta", Value = "200", Weight = 3.6, FC = "Y" },
      //      new MyDataItem { Key = "Gamma", Value = "300", Weight = 4.7, FC = "Z" },
      //      new MyDataItem { Key = "Delta", Value = "400", Weight = 5.8, FC = "W" },
      //      new MyDataItem { Key = "Epsilon", Value = "500", Weight = 6.9, FC = "V" },
      //      new MyDataItem { Key = "Alpha", Value = "100", Weight = 2.5, FC = "X" },
      //      new MyDataItem { Key = "Beta", Value = "200", Weight = 3.6, FC = "Y" },
      //      new MyDataItem { Key = "Gamma", Value = "300", Weight = 4.7, FC = "Z" },
      //      new MyDataItem { Key = "Delta", Value = "400", Weight = 5.8, FC = "W" },
      //      new MyDataItem { Key = "Epsilon", Value = "500", Weight = 6.9, FC = "V" },
      //      new MyDataItem { Key = "Alpha", Value = "100", Weight = 2.5, FC = "X" },
      //  };
      //dataGrid.ItemsSource = items;
    }


    private void GetList_button_Click(object sender, RoutedEventArgs e)
    {
      LoadTableNames();
    }

    private void ComboBox_MouseEnter(object sender, MouseEventArgs e)
    {
      comboBox.Focus();
    }

    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      labelName = comboBox.SelectedValue as string;
      LoadDataGridWithTopRecords(labelName);
    }


    private void Reload_button_Click(object sender, RoutedEventArgs e)
    {

    }

    #region 读写db
    //从 db 读取表名到 ComboBox
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

    // FROM COMPANY LIMIT 3 OFFSET 2;
    private DataTable GetTopRecordsFromTable(string tableName)
    {
      using SQLiteConnection connection = new($"Data Source={dbPath};Version=3;");
      string query = $"SELECT * FROM '{tableName}' LIMIT 10"; // Select top 100 records
      connection.Open();
      using SQLiteDataAdapter adapter = new(query, connection);
      DataTable dataTable = new();
      adapter.Fill(dataTable);
      return dataTable;
    }

    private void PopulateDataGridFromTable(string tableName)
    {
      DataTable dataTable = GetTopRecordsFromTable(tableName);

      dataGrid.Columns.Clear(); // Clear existing columns

      foreach (DataColumn column in dataTable.Columns)
      {
        dataGrid.Columns.Add(new DataGridTextColumn
        {
          Header = column.ColumnName,
          Binding = new Binding(column.ColumnName)
        });
      }

      dataGrid.ItemsSource = dataTable.DefaultView;
    }

    // 调用此方法可将指定表格中的前 100 条记录填充到 DataGrid 中
    private void LoadDataGridWithTopRecords(string tableName)
    {
      PopulateDataGridFromTable(tableName);
    }

    // Example usage:
    private void LoadDataGrid()
    {
      string tableName = "YourTableNameHere"; // Specify your table name
      LoadDataGridWithTopRecords(tableName);
    }























    // 从指定表 labelName 内读取 key 列为"配置"时 value 的值
    private string GetConfig(string labelName)
    {
      using SQLiteConnection connection = new($"Data Source={dbPath};Version=3;");
      string query = $"SELECT value FROM '{labelName}' WHERE key = '配置'";
      connection.Open();
      using SQLiteCommand command = new(query, connection);

      string result;
      using (SQLiteDataReader reader = command.ExecuteReader())
      {
        if (reader.Read()) result = reader["value"].ToString();
        else result = null;
      }
      connection.Close();
      return result;
    }

    // 保存配置到数据库
    // 更新指定表 labelName 内 key 列为 "配置" 时 value 列的值为 value
    //private void SaveConfig(String value)
    //{
    //  string connectionString = $"Data Source={dbPath};Version=3;";
    //  using SQLiteConnection connection = new(connectionString);
    //  string updateQuery = $"UPDATE '{labelName}' SET value = @Value WHERE key = '配置'";
    //  connection.Open();
    //  using SQLiteCommand command = new(updateQuery, connection);
    //  command.Parameters.AddWithValue("@Value", value);
    //  int rowsAffected = command.ExecuteNonQuery();
    //  connection.Close();
    //}

    #endregion

  }
}
