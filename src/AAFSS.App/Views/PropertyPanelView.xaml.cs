using System.Windows.Controls;

namespace AAFSS.App.Views;

public partial class PropertyPanelView : UserControl
{
    public PropertyPanelView(AAFSS.App.ViewModels.PropertyPanelViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
