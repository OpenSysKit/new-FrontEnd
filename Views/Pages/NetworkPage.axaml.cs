using Avalonia.Controls;
using OpenSysKit.UI.ViewModels;

namespace OpenSysKit.UI.Views.Pages;

public partial class NetworkPage : UserControl
{
    private MainViewModel? ViewModel => DataContext as MainViewModel;

    public NetworkPage()
    {
        InitializeComponent();
    }

    private void ProtocolCombo_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ViewModel == null || sender is not ComboBox combo || combo.SelectedItem is not ComboBoxItem item)
        {
            return;
        }

        ViewModel.NetProtocol = item.Tag?.ToString() ?? "all";
    }
}
