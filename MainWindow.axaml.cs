using System;
using System.ComponentModel;
using Avalonia.Controls;
using OpenSysKit.UI.ViewModels;
using SukiUI.Controls;

namespace OpenSysKit.UI;

public partial class MainWindow : SukiWindow
{
    public MainViewModel ViewModel { get; }
    private bool _notifiedTray;

    public MainWindow()
    {
        ViewModel = new MainViewModel();
        DataContext = ViewModel;

        InitializeComponent();

        PropagateDataContext();

        SideMenu.PropertyChanged += OnSideMenuPropertyChanged;
        Closing += OnWindowClosing;

        _ = ViewModel.EnsureConnectedAsync();
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (!App.IsExiting)
        {
            e.Cancel = true;
            Hide();
            if (!_notifiedTray)
            {
                App.ShowTrayBalloon("OpenSysKit", "程序已最小化到系统托盘，单击图标可重新打开。");
                _notifiedTray = true;
            }
            return;
        }
        ViewModel.Dispose();
    }

    private void PropagateDataContext()
    {
        if (SideMenu?.Items == null)
        {
            return;
        }

        foreach (var item in SideMenu.Items)
        {
            if (item is SukiSideMenuItem menuItem && menuItem.PageContent is Control page)
            {
                page.DataContext = ViewModel;
            }
        }
    }

    private void OnSideMenuPropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name != "SelectedItem" || e.NewValue is not SukiSideMenuItem menuItem)
        {
            return;
        }

        if (Enum.TryParse<NavPage>(menuItem.Tag?.ToString(), out var page))
        {
            if (page != ViewModel.CurrentPage)
            {
                _ = ViewModel.NavigateCommand.ExecuteAsync(page);
            }
        }
    }
}
