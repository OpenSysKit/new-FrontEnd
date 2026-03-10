using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System.Windows.Input;
using OpenSysKit.UI.ViewModels;

namespace OpenSysKit.UI.Views;

public class NavItem : UserControl
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<NavItem, string>(nameof(Label), "");

    public static readonly StyledProperty<string> IconProperty =
        AvaloniaProperty.Register<NavItem, string>(nameof(Icon), "");

    public static readonly StyledProperty<NavPage> PageProperty =
        AvaloniaProperty.Register<NavItem, NavPage>(nameof(Page));

    public static readonly StyledProperty<NavPage> CurrentPageProperty =
        AvaloniaProperty.Register<NavItem, NavPage>(nameof(CurrentPage));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<NavItem, ICommand?>(nameof(Command));

    public string Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public NavPage Page { get => GetValue(PageProperty); set => SetValue(PageProperty, value); }
    public NavPage CurrentPage { get => GetValue(CurrentPageProperty); set => SetValue(CurrentPageProperty, value); }
    public ICommand? Command { get => GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

    private Border? _bg;
    private TextBlock? _label;

    public NavItem()
    {
        PropertyChanged += OnPropChanged;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var root = new Border
        {
            CornerRadius = new CornerRadius(5),
            Margin = new Thickness(0, 1),
            Cursor = new Cursor(StandardCursorType.Hand),
            Padding = new Thickness(10, 7)
        };
        _bg = root;

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var icon = new TextBlock { FontSize = 14, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
        icon.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding(nameof(Icon)) { Source = this });
        Grid.SetColumn(icon, 0);

        _label = new TextBlock
        {
            Margin = new Thickness(8, 0, 0, 0),
            FontSize = 13,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        _label.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding(nameof(Label)) { Source = this });
        Grid.SetColumn(_label, 1);

        grid.Children.Add(icon);
        grid.Children.Add(_label);
        root.Child = grid;
        Content = root;

        root.PointerPressed += (_, e) => { Command?.Execute(Page); };
        root.PointerEntered += (_, _) => UpdateStyle();
        root.PointerExited += (_, _) => UpdateStyle();

        UpdateStyle();
    }

    private void OnPropChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == CurrentPageProperty || e.Property == PageProperty)
            UpdateStyle();
    }

    private void UpdateStyle()
    {
        if (_bg == null || _label == null) return;
        bool active = CurrentPage == Page;
        _bg.Background = active
            ? new SolidColorBrush(Color.Parse("#1A3554"))
            : new SolidColorBrush(Colors.Transparent);
        _label.Foreground = active
            ? new SolidColorBrush(Color.Parse("#4FACDE"))
            : new SolidColorBrush(Color.Parse("#7A9BB5"));
        _label.FontWeight = active ? FontWeight.SemiBold : FontWeight.Normal;
    }
}
