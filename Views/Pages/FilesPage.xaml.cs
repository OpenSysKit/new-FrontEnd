using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using OpenSysKit.UI.ViewModels;
using Windows.System;

namespace OpenSysKit.UI.Views.Pages;

public sealed partial class FilesPage : Page
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public FilesPage()
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

    private async void PathBox_OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            await ViewModel.NavigateToCommand.ExecuteAsync(ViewModel.CurrentPath);
        }
    }

    private async void EntriesList_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        await ViewModel.OpenSelectedFileCommand.ExecuteAsync(null);
    }
}
