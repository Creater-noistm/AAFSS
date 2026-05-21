using System.Windows.Controls;

namespace AAFSS.App.Views;

public partial class ProjectExplorerView : UserControl
{
    public ProjectExplorerView(AAFSS.App.ViewModels.ProjectExplorerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
