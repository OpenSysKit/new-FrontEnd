using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenSysKit.UI.ViewModels;
using OpenSysKit.UI.Views.Pages;

namespace OpenSysKit.UI;

public sealed partial class MainWindow : Window
{
    private bool _didAutoConnect;

    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        InitializeComponent();
        Title = "OpenSysKit";

        ViewModel = new MainViewModel(DispatcherQueue);
        RootGrid.DataContext = ViewModel;

        Activated += OnActivated;
        Closed += OnClosed;

        if (ShellNav.MenuItems.OfType<NavigationViewItem>().FirstOrDefault() is { } firstItem)
        {
            ShellNav.SelectedItem = firstItem;
            ContentFrame.Navigate(typeof(ProcessesPage), ViewModel);
        }
    }

    private async void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        if (_didAutoConnect)
        {
            return;
        }

        _didAutoConnect = true;
        await ViewModel.EnsureConnectedAsync();
    }

    private async void ShellNav_OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item)
        {
            return;
        }

        if (!Enum.TryParse(item.Tag?.ToString(), out NavPage page))
        {
            return;
        }

        await ViewModel.NavigateCommand.ExecuteAsync(page);
        ContentFrame.Navigate(ResolvePage(page), ViewModel);
    }

    private static Type ResolvePage(NavPage page) => page switch
    {
        NavPage.Processes => typeof(ProcessesPage),
        NavPage.Network => typeof(NetworkPage),
        NavPage.Services => typeof(ServicesPage),
        NavPage.Files => typeof(FilesPage),
        NavPage.KernelModules => typeof(KernelModulesPage),
        NavPage.Handles => typeof(HandlesPage),
        NavPage.Startup => typeof(StartupPage),
        NavPage.Audit => typeof(AuditPage),
        _ => typeof(ProcessesPage)
    };

    private void OnClosed(object sender, WindowEventArgs args)
    {
        ViewModel.Dispose();
    }
}
