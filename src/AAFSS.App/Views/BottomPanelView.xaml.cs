using System.Windows.Controls;

namespace AAFSS.App.Views;

public partial class BottomPanelView : UserControl
{
    public BottomPanelView(AAFSS.App.ViewModels.BottomPanelViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
