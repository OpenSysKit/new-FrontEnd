using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using OpenSysKit.UI.Models;
using OpenSysKit.UI.ViewModels;

namespace OpenSysKit.UI.Views.Pages;

public sealed partial class ProcessesPage : Page
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public ProcessesPage()
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

    private void ProcessesList_OnRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (ItemsControl.ContainerFromElement(ProcessesList, (DependencyObject)e.OriginalSource) is ListViewItem item &&
            item.DataContext is ProcessInfo process)
        {
            ProcessesList.SelectedItem = process;
        }
    }

    private async void TaskKillMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.TaskKillProcessCommand.ExecuteAsync(null);
    }

    private async void KernelKillMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.KillProcessCommand.ExecuteAsync(null);
    }

    private async void FreezeMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.FreezeProcessCommand.ExecuteAsync(null);
    }

    private async void UnfreezeMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.UnfreezeProcessCommand.ExecuteAsync(null);
    }

    private async void ViewModulesMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ViewModulesCommand.ExecuteAsync(null);
    }

    private async void ViewThreadsMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ViewThreadsCommand.ExecuteAsync(null);
    }
}
