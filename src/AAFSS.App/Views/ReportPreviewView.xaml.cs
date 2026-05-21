using AAFSS.App.ViewModels;
using System.Windows.Controls;

namespace AAFSS.App.Views;

public partial class ReportPreviewView : UserControl
{
    private readonly ReportPreviewViewModel _viewModel;

    public ReportPreviewView(ReportPreviewViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ReportPreviewViewModel.ReportHtml) &&
                !string.IsNullOrEmpty(_viewModel.ReportHtml))
            {
                ReportBrowser.NavigateToString(_viewModel.ReportHtml);
            }
        };
    }
}
