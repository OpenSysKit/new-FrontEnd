using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using OpenSysKit.UI.ViewModels;

namespace OpenSysKit.UI.Views.Pages;

public partial class FilesPage : UserControl
{
    private MainViewModel? ViewModel => DataContext as MainViewModel;

    public FilesPage()
    {
        InitializeComponent();
    }

    private async void PathBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && ViewModel != null)
        {
            await ViewModel.NavigateToCommand.ExecuteAsync(ViewModel.CurrentPath);
        }
    }

    private async void FilesGrid_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        await OpenSelectionAsync();
    }

    private async void OpenMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        await OpenSelectionAsync();
    }

    private void CopyPathMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel?.SelectedFile == null)
        {
            return;
        }

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard != null)
        {
            _ = clipboard.SetTextAsync(ViewModel.SelectedFile.Path);
            ViewModel.StatusMessage = "路径已复制";
        }
    }

    private async Task OpenSelectionAsync()
    {
        if (ViewModel?.SelectedFile == null)
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
            if (ViewModel != null)
            {
                ViewModel.StatusMessage = $"打开失败: {ex.Message}";
            }
        }
    }
}
