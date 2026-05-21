using System.Windows.Controls;

namespace AAFSS.App.Views;

public partial class StatusBarView : UserControl
{
    public StatusBarView(AAFSS.App.ViewModels.StatusBarViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
