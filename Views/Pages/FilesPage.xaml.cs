using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using OpenSysKit.UI.Models;
using OpenSysKit.UI.ViewModels;
using Windows.ApplicationModel.DataTransfer;
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

    private void EntriesList_OnRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (ItemsControl.ContainerFromElement(EntriesList, (DependencyObject)e.OriginalSource) is ListViewItem item &&
            item.DataContext is FileEntry entry)
        {
            EntriesList.SelectedItem = entry;
        }
    }

    private async void EntriesList_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        await OpenSelectionAsync();
    }

    private async void OpenMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        await OpenSelectionAsync();
    }

    private void CopyPathMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedFile == null)
        {
            return;
        }

        var data = new DataPackage();
        data.SetText(ViewModel.SelectedFile.Path);
        Clipboard.SetContent(data);
        ViewModel.StatusMessage = "路径已复制";
    }

    private async void KillLockingMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.KillLockingProcessesCommand.ExecuteAsync(null);
    }

    private async void DeleteMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.DeleteFileCommand.ExecuteAsync(null);
    }

    private async Task OpenSelectionAsync()
    {
        if (ViewModel.SelectedFile == null)
        {
            return;
        }

        try
        {
            if (ViewModel.SelectedFile.IsDir)
            {
                await ViewModel.OpenSelectedFileCommand.ExecuteAsync(null);
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = ViewModel.SelectedFile.Path,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ViewModel.StatusMessage = $"打开失败: {ex.Message}";
        }
    }
}
