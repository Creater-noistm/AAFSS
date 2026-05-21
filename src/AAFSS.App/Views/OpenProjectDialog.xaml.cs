using System.Windows;

namespace AAFSS.App.Views;

public partial class OpenProjectDialog : Window
{
    public string? SelectedFilePath { get; private set; }

    public OpenProjectDialog()
    {
        InitializeComponent();
    }

    private void OnBrowseClick(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "AAFSS 项目文件 (*.aafss)|*.aafss|所有文件 (*.*)|*.*",
            Title = "打开 AAFSS 项目"
        };

        if (dlg.ShowDialog() == true)
        {
            SelectedFilePath = dlg.FileName;
            DialogResult = true;
            Close();
        }
    }

    private void OnOpenClick(object sender, RoutedEventArgs e)
    {
        if (RecentList.SelectedItem != null)
        {
            DialogResult = true;
        }
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
