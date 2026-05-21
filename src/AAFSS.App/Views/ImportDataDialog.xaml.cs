using System.Windows;

namespace AAFSS.App.Views;

public partial class ImportDataDialog : Window
{
    public string? SelectedFilePath { get; private set; }

    public ImportDataDialog()
    {
        InitializeComponent();
    }

    private void OnBrowseClick(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "数据文件 (*.csv;*.xlsx;*.tdms;*.dat;*.txt)|*.csv;*.xlsx;*.tdms;*.dat;*.txt|所有文件 (*.*)|*.*",
            Title = "选择数据文件"
        };

        if (dlg.ShowDialog() == true)
        {
            FilePathBox.Text = SelectedFilePath = dlg.FileName;
        }
    }

    private async void OnImportClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(SelectedFilePath))
        {
            MessageBox.Show("请先选择要导入的数据文件。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ImportProgress.Visibility = Visibility.Visible;
        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
