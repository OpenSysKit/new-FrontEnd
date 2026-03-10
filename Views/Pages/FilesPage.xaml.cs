using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace OpenSysKit.UI.Views.Pages;

public sealed partial class FilesPage : ViewModelPage
{
    public FilesPage()
    {
        InitializeComponent();
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
