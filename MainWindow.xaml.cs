using System;
using System.Drawing;
using System.Linq;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenSysKit.UI.ViewModels;
using OpenSysKit.UI.Views.Pages;
using WinRT.Interop;
using Forms = System.Windows.Forms;

namespace OpenSysKit.UI;

public sealed partial class MainWindow : Window
{
    private readonly AppWindow _appWindow;
    private readonly Forms.NotifyIcon _notifyIcon;
    private bool _isExiting;
    private bool _didAutoConnect;

    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        InitializeComponent();
        Title = "OpenSysKit";

        ViewModel = new MainViewModel(DispatcherQueue);
        RootGrid.DataContext = ViewModel;

        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);
        _appWindow.Closing += AppWindow_OnClosing;
        _notifyIcon = CreateNotifyIcon();

        Activated += OnActivated;
        Closed += OnClosed;

        if (ShellNav.MenuItems.OfType<NavigationViewItem>().FirstOrDefault() is { } firstItem)
        {
            ShellNav.SelectedItem = firstItem;
            ContentFrame.Navigate(typeof(ProcessesPage), ViewModel);
        }
    }

    private Forms.NotifyIcon CreateNotifyIcon()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("显示主窗口", null, (_, _) => ShowFromTray());
        menu.Items.Add("退出", null, (_, _) => ExitFromTray());

        var notifyIcon = new Forms.NotifyIcon
        {
            Text = "OpenSysKit",
            Visible = true,
            Icon = SystemIcons.Application,
            ContextMenuStrip = menu
        };
        notifyIcon.DoubleClick += (_, _) => ShowFromTray();
        return notifyIcon;
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

    private async void ExportButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ExportReportCommand.ExecuteAsync(null);
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

    private void AppWindow_OnClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_isExiting)
        {
            return;
        }

        args.Cancel = true;
        _appWindow.Hide();
    }

    private void ShowFromTray()
    {
        _appWindow.Show();
        Activate();
    }

    private void ExitFromTray()
    {
        _isExiting = true;
        _notifyIcon.Visible = false;
        Close();
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        ViewModel.Dispose();
    }
}
