using System.Windows;

namespace AAFSS.App.Views;

public partial class BatchProcessingDialog : Window
{
    public BatchProcessingDialog()
    {
        InitializeComponent();
    }

    private async void OnStartClick(object sender, RoutedEventArgs e)
    {
        BatchProgress.Visibility = Visibility.Visible;
        ProgressText.Text = "正在处理...";

        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
