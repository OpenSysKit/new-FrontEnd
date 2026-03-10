using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using OpenSysKit.UI.Models;
using OpenSysKit.UI.ViewModels;

namespace OpenSysKit.UI.Views.Pages;

public sealed partial class ServicesPage : Page
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public ServicesPage()
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

    private void ServicesList_OnRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (FindAncestor<ListViewItem>(e.OriginalSource as DependencyObject) is { } item &&
            item.DataContext is ServiceInfo service)
        {
            ServicesList.SelectedItem = service;
        }
    }

    private async void StartServiceMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.StartServiceCommand.ExecuteAsync(null);
    }

    private async void StopServiceMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.StopServiceCommand.ExecuteAsync(null);
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T match)
            {
                return match;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}
