using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using OpenSysKit.UI.ViewModels;

namespace OpenSysKit.UI.Views.Pages;

public sealed partial class StartupPage : Page
{
    public StartupPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is MainViewModel viewModel)
        {
            DataContext = viewModel;
        }
    }
}
