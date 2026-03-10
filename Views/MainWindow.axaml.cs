using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using OpenSysKit.UI.ViewModels;
using System;

namespace OpenSysKit.UI.Views;

public partial class MainWindow : Window
{
    private MainViewModel _vm;
    private Panel _pageHost = new();
    private TextBlock _statusDot = new();
    private TextBlock _statusText = new();

    static readonly SolidColorBrush BrBase = Br("#0D1117");
    static readonly SolidColorBrush BrSurface = Br("#141B22");
    static readonly SolidColorBrush BrPanel = Br("#1A2332");
    static readonly SolidColorBrush BrCard = Br("#1F2B3A");
    static readonly SolidColorBrush BrHover = Br("#243042");
    static readonly SolidColorBrush BrSelected = Br("#1A3554");
    static readonly SolidColorBrush BrBorderSub = Br("#2A3F55");
    static readonly SolidColorBrush BrText = Br("#D8E6F3");
    static readonly SolidColorBrush BrTextSec = Br("#7A9BB5");
    static readonly SolidColorBrush BrTextMuted = Br("#4A6580");
    static readonly SolidColorBrush BrAccent = Br("#4FACDE");
    static readonly SolidColorBrush BrAccentDim = Br("#2A6FA0");
    static readonly SolidColorBrush BrOk = Br("#3DD68C");
    static readonly SolidColorBrush BrWarn = Br("#F5A623");
    static readonly SolidColorBrush BrError = Br("#E05C5C");
    static readonly FontFamily FtUi = new("Segoe UI, system-ui, sans-serif");

    static SolidColorBrush Br(string hex) => new(Color.Parse(hex));

    static TextBlock T(string text, double size = 12, FontWeight w = FontWeight.Normal, SolidColorBrush? fg = null)
        => new() { Text = text, FontSize = size, FontWeight = w, Foreground = fg ?? BrText, FontFamily = FtUi, VerticalAlignment = VerticalAlignment.Center };

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainViewModel();
        DataContext = _vm;
        _vm.PropertyChanged += OnVmChanged;

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(32) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(26) });

        var titleBar = BuildTitleBar();
        Grid.SetRow(titleBar, 0);
        var main = BuildMain();
        Grid.SetRow(main, 1);
        var statusBar = BuildStatusBar();
        Grid.SetRow(statusBar, 2);

        root.Children.Add(titleBar);
        root.Children.Add(main);
        root.Children.Add(statusBar);
        Content = root;

        this.PointerPressed += (_, e) => { if (e.GetCurrentPoint(this).Position.Y < 32) BeginMoveDrag(e); };
        RefreshPageVisibility();
    }

    Control BuildTitleBar()
    {
        var grid = new Grid { Background = BrBase };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var dotIcon = T("⚙", 11, FontWeight.Bold, Br("#FFFFFF"));
        dotIcon.HorizontalAlignment = HorizontalAlignment.Center;
        var logoDot = new Border { Width = 18, Height = 18, CornerRadius = new CornerRadius(3), Background = BrAccent, Margin = new Thickness(14, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center, Child = dotIcon };
        var logo = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        logo.Children.Add(logoDot);
        logo.Children.Add(T("OpenSysKit", 12, FontWeight.SemiBold, BrTextSec));
        logo.Children.Add(T(" · 系统工具包", 12, fg: BrTextMuted));
        Grid.SetColumn(logo, 0);

        var wc = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        wc.Children.Add(WinBtn("—", () => WindowState = WindowState.Minimized));
        wc.Children.Add(WinBtn("□", () => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized));
        wc.Children.Add(WinBtn("✕", Close, true));
        Grid.SetColumn(wc, 2);

        grid.Children.Add(logo);
        grid.Children.Add(wc);
        return grid;
    }

    Button WinBtn(string symbol, Action onClick, bool isClose = false)
    {
        var lbl = T(symbol, 11, fg: BrTextSec);
        lbl.HorizontalAlignment = HorizontalAlignment.Center;
        var btn = new Button { Width = 46, Height = 32, Background = Brushes.Transparent, BorderThickness = new Thickness(0), Padding = new Thickness(0), Cursor = new Cursor(StandardCursorType.Arrow), Content = lbl };
        btn.Click += (_, _) => onClick();
        btn.PointerEntered += (_, _) => btn.Background = isClose ? BrError : BrHover;
        btn.PointerExited += (_, _) => btn.Background = Brushes.Transparent;
        return btn;
    }

    Control BuildMain()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var sidebar = BuildSidebar();
        Grid.SetColumn(sidebar, 0);
        var content = BuildContent();
        Grid.SetColumn(content, 1);
        grid.Children.Add(sidebar);
        grid.Children.Add(content);
        return grid;
    }

    Control BuildSidebar()
    {
        var root = new Border { Background = BrSurface, BorderBrush = BrBorderSub, BorderThickness = new Thickness(0, 0, 1, 0) };
        var dock = new DockPanel();

        var connCard = BuildConnCard();
        DockPanel.SetDock(connCard, Dock.Top);
        dock.Children.Add(connCard);

        var healthPanel = new Border { Margin = new Thickness(12, 0, 12, 8) };
        var healthSp = new StackPanel { Spacing = 2 };
        healthPanel.Child = healthSp;
        _vm.HealthComponents.CollectionChanged += (_, _) => Dispatcher.UIThread.Post(() =>
        {
            healthSp.Children.Clear();
            foreach (var c in _vm.HealthComponents)
            {
                var row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                SolidColorBrush sc = c.Status switch { "ok" => BrOk, "degraded" => BrWarn, "error" => BrError, _ => BrTextMuted };
                var nl = T(c.Name, 10, fg: BrTextMuted);
                var sl = T(c.Status, 10, FontWeight.Medium, sc);
                Grid.SetColumn(nl, 0); Grid.SetColumn(sl, 1);
                row.Children.Add(nl); row.Children.Add(sl);
                healthSp.Children.Add(row);
            }
        });
        DockPanel.SetDock(healthPanel, Dock.Top);
        dock.Children.Add(healthPanel);

        var sep = new Border { Height = 1, Background = BrBorderSub, Margin = new Thickness(12, 4) };
        DockPanel.SetDock(sep, Dock.Top);
        dock.Children.Add(sep);

        var navLbl = T("NAVIGATION", 9.5, FontWeight.SemiBold, BrTextMuted);
        navLbl.Margin = new Thickness(16, 10, 0, 4);
        navLbl.CharacterSpacing = 100;
        DockPanel.SetDock(navLbl, Dock.Top);
        dock.Children.Add(navLbl);

        var navSp = new StackPanel { Margin = new Thickness(8, 0) };
        navSp.Children.Add(NavBtn("⚡", "进程管理", NavPage.Processes));
        navSp.Children.Add(NavBtn("🌐", "网络连接", NavPage.Network));
        navSp.Children.Add(NavBtn("🔧", "系统服务", NavPage.Services));
        navSp.Children.Add(NavBtn("📂", "文件浏览", NavPage.Files));
        navSp.Children.Add(NavBtn("🔩", "内核模块", NavPage.KernelModules));
        navSp.Children.Add(NavBtn("🔗", "句柄信息", NavPage.Handles));
        navSp.Children.Add(NavBtn("🚀", "启动项", NavPage.Startup));
        navSp.Children.Add(NavBtn("📋", "操作日志", NavPage.Audit));
        var navSv = new ScrollViewer { Content = navSp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        DockPanel.SetDock(navSv, Dock.Top);
        dock.Children.Add(navSv);

        var bottom = new Border { Background = BrSurface, BorderBrush = BrBorderSub, BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(8, 8) };
        var exportBtn = new Button { Background = Brushes.Transparent, BorderThickness = new Thickness(0), Padding = new Thickness(12, 7), HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left, Cursor = new Cursor(StandardCursorType.Hand), Content = T("📤  导出报告", 12, fg: BrTextSec) };
        exportBtn.Bind(IsEnabledProperty, new Binding("IsConnected") { Source = _vm });
        exportBtn.Click += async (_, _) => await _vm.ExportReportCommand.ExecuteAsync(null);
        exportBtn.PointerEntered += (_, _) => exportBtn.Background = BrHover;
        exportBtn.PointerExited += (_, _) => exportBtn.Background = Brushes.Transparent;
        bottom.Child = exportBtn;
        DockPanel.SetDock(bottom, Dock.Bottom);
        dock.Children.Add(bottom);

        dock.Children.Add(new Border());
        root.Child = dock;
        return root;
    }

    Border BuildConnCard()
    {
        var card = new Border { Margin = new Thickness(12, 12, 12, 8), Background = BrPanel, CornerRadius = new CornerRadius(6), BorderBrush = BrBorderSub, BorderThickness = new Thickness(1), Padding = new Thickness(12, 10) };
        var sp = new StackPanel { Spacing = 4 };

        var hdr = new Grid();
        hdr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        hdr.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var cap = T("后端连接", 9.5, FontWeight.SemiBold, BrTextMuted);
        cap.CharacterSpacing = 80;
        var dot = new Ellipse { Width = 7, Height = 7, VerticalAlignment = VerticalAlignment.Center, Fill = BrTextMuted };
        Grid.SetColumn(cap, 0); Grid.SetColumn(dot, 1);
        hdr.Children.Add(cap); hdr.Children.Add(dot);

        _statusText = T("未连接", 11, fg: BrTextSec);
        _statusText.TextTrimming = TextTrimming.CharacterEllipsis;

        var connBtn = new Button { Background = BrAccentDim, BorderBrush = BrAccent, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4), Padding = new Thickness(0, 6), HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 6, 0, 0), Cursor = new Cursor(StandardCursorType.Hand), Content = T("连接", 12, fg: BrText) };
        connBtn.Bind(IsVisibleProperty, new Binding("!IsConnected") { Source = _vm });
        connBtn.Click += async (_, _) => await _vm.ConnectCommand.ExecuteAsync(null);
        connBtn.PointerEntered += (_, _) => connBtn.Background = BrAccent;
        connBtn.PointerExited += (_, _) => connBtn.Background = BrAccentDim;

        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(MainViewModel.StatusMessage) or nameof(MainViewModel.IsConnected))
            {
                _statusText.Text = _vm.StatusMessage;
                dot.Fill = _vm.IsConnected ? BrOk : BrTextMuted;
            }
        };

        sp.Children.Add(hdr); sp.Children.Add(_statusText); sp.Children.Add(connBtn);
        card.Child = sp;
        return card;
    }

    readonly System.Collections.Generic.Dictionary<NavPage, Border> _navItems = [];

    Border NavBtn(string icon, string label, NavPage page)
    {
        var border = new Border { CornerRadius = new CornerRadius(5), Margin = new Thickness(0, 1), Padding = new Thickness(10, 7), Cursor = new Cursor(StandardCursorType.Hand) };
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var iconT = T(icon, 14, fg: BrTextSec);
        var labelT = T(label, 13, fg: BrTextSec);
        labelT.Margin = new Thickness(9, 0, 0, 0);
        Grid.SetColumn(iconT, 0); Grid.SetColumn(labelT, 1);
        row.Children.Add(iconT); row.Children.Add(labelT);
        border.Child = row;
        border.PointerPressed += async (_, _) => { await _vm.NavigateCommand.ExecuteAsync(page); RefreshPageVisibility(); };
        border.PointerEntered += (_, _) => { if (_vm.CurrentPage != page) border.Background = BrHover; };
        border.PointerExited += (_, _) => { if (_vm.CurrentPage != page) border.Background = Brushes.Transparent; };
        _navItems[page] = border;
        return border;
    }

    void UpdateNavHighlight()
    {
        foreach (var (page, border) in _navItems)
        {
            bool active = page == _vm.CurrentPage;
            border.Background = active ? BrSelected : Brushes.Transparent;
            if (border.Child is Grid g)
                foreach (var child in g.Children)
                    if (child is TextBlock tb) { tb.Foreground = active ? BrAccent : BrTextSec; tb.FontWeight = active ? FontWeight.SemiBold : FontWeight.Normal; }
        }
    }

    Grid BuildContent()
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var toolbar = BuildContentToolbar();
        Grid.SetRow(toolbar, 0);

        _pageHost = new Panel();
        _pageHost.Children.Add(new ProcessesPage { DataContext = _vm });
        _pageHost.Children.Add(new NetworkPage { DataContext = _vm });
        _pageHost.Children.Add(new ServicesPage { DataContext = _vm });
        _pageHost.Children.Add(new FilesPage { DataContext = _vm });
        _pageHost.Children.Add(new KernelModulesPage { DataContext = _vm });
        _pageHost.Children.Add(new HandlesPage { DataContext = _vm });
        _pageHost.Children.Add(new StartupPage { DataContext = _vm });
        _pageHost.Children.Add(new AuditPage { DataContext = _vm });
        Grid.SetRow(_pageHost, 1);

        grid.Children.Add(toolbar);
        grid.Children.Add(_pageHost);
        return grid;
    }

    Border BuildContentToolbar()
    {
        var border = new Border { Background = BrSurface, BorderBrush = BrBorderSub, BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(16, 8) };
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var search = new TextBox { Width = 280, Padding = new Thickness(8, 5), FontSize = 12, FontFamily = FtUi, Background = BrCard, Foreground = BrText, BorderBrush = BrBorderSub, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4), Watermark = "搜索..." };
        search.Bind(TextBox.TextProperty, new Binding("SearchText") { Source = _vm, Mode = BindingMode.TwoWay });
        var searchWrap = new Panel();
        searchWrap.HorizontalAlignment = HorizontalAlignment.Left;
        searchWrap.Children.Add(search);
        Grid.SetColumn(searchWrap, 0);

        var refreshBtn = new Button { Background = Brushes.Transparent, BorderThickness = new Thickness(0), Padding = new Thickness(10, 5), Margin = new Thickness(0, 0, 8, 0), Cursor = new Cursor(StandardCursorType.Hand), Content = T("↻  刷新", 12, fg: BrTextSec) };
        refreshBtn.Bind(IsEnabledProperty, new Binding("IsConnected") { Source = _vm });
        refreshBtn.Click += async (_, _) => await _vm.RefreshCommand.ExecuteAsync(null);
        refreshBtn.PointerEntered += (_, _) => refreshBtn.Background = BrHover;
        refreshBtn.PointerExited += (_, _) => refreshBtn.Background = Brushes.Transparent;
        Grid.SetColumn(refreshBtn, 1);

        var progress = new ProgressBar { IsIndeterminate = true, Width = 80, Height = 2, Foreground = BrAccent, Background = BrCard, VerticalAlignment = VerticalAlignment.Center };
        progress.Bind(IsVisibleProperty, new Binding("IsLoading") { Source = _vm });
        Grid.SetColumn(progress, 2);

        row.Children.Add(searchWrap);
        row.Children.Add(refreshBtn);
        row.Children.Add(progress);
        border.Child = row;
        return border;
    }

    Border BuildStatusBar()
    {
        var border = new Border { Background = Br("#0F161D"), BorderBrush = BrBorderSub, BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(16, 0) };
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _statusDot = T("●  ", 10, fg: BrTextMuted);
        _statusDot.VerticalAlignment = VerticalAlignment.Center;
        var statusTxt = T("", 11, fg: BrTextMuted);
        statusTxt.VerticalAlignment = VerticalAlignment.Center;
        statusTxt.Bind(TextBlock.TextProperty, new Binding("StatusMessage") { Source = _vm });
        var healthTxt = T("", 11, fg: BrTextMuted);
        healthTxt.VerticalAlignment = VerticalAlignment.Center;
        healthTxt.Margin = new Thickness(0, 0, 4, 0);
        healthTxt.Bind(TextBlock.TextProperty, new Binding("OverallHealth") { Source = _vm });

        Grid.SetColumn(_statusDot, 0);
        Grid.SetColumn(statusTxt, 1);
        Grid.SetColumn(healthTxt, 2);
        row.Children.Add(_statusDot);
        row.Children.Add(statusTxt);
        row.Children.Add(healthTxt);
        border.Child = row;
        return border;
    }

    void OnVmChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsConnected))
            _statusDot.Foreground = _vm.IsConnected ? BrOk : BrTextMuted;
        if (e.PropertyName == nameof(MainViewModel.CurrentPage))
        { RefreshPageVisibility(); UpdateNavHighlight(); }
    }

    void RefreshPageVisibility()
    {
        var pages = new[] { NavPage.Processes, NavPage.Network, NavPage.Services, NavPage.Files, NavPage.KernelModules, NavPage.Handles, NavPage.Startup, NavPage.Audit };
        for (int i = 0; i < _pageHost.Children.Count && i < pages.Length; i++)
            _pageHost.Children[i].IsVisible = pages[i] == _vm.CurrentPage;
        UpdateNavHighlight();
    }
}
