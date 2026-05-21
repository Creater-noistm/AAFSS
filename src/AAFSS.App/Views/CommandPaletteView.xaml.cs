using System.Windows.Controls;

namespace AAFSS.App.Views;

public partial class CommandPaletteView : UserControl
{
    public CommandPaletteView(AAFSS.App.ViewModels.CommandPaletteViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += (_, _) => SearchBox.Focus();
    }
}
