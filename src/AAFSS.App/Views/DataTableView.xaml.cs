using System.Windows.Controls;

namespace AAFSS.App.Views;

public partial class DataTableView : UserControl
{
    public DataTableView(AAFSS.App.ViewModels.DataTableViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
