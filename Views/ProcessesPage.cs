using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Controls.Templates;
using OpenSysKit.UI.ViewModels;
using OpenSysKit.UI.Models;
using Avalonia.Data;

namespace OpenSysKit.UI.Views;

public class ProcessesPage : UserControl
{
    public ProcessesPage()
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Action toolbar
        var toolbar = BuildToolbar();
        Grid.SetRow(toolbar, 0);

        // DataGrid
        var dg = BuildDataGrid();
        Grid.SetRow(dg, 1);

        // Action bar bottom
        var actionBar = BuildActionBar();
        Grid.SetRow(actionBar, 2);

        grid.Children.Add(toolbar);
        grid.Children.Add(dg);
        grid.Children.Add(actionBar);

        Content = grid;
    }

    private Border BuildToolbar()
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(16, 10, 16, 10),
            Spacing = 8
        };

        var lbl = new TextBlock
        {
            Text = "进程列表",
            Classes = { "heading" },
            VerticalAlignment = VerticalAlignment.Center
        };

        var countLbl = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0),
            FontSize = 11
        };
        countLbl.Bind(TextBlock.TextProperty, new Binding("Processes.Count") { StringFormat = "{0} 个进程" });
        countLbl.Classes.Add("muted");

        panel.Children.Add(lbl);
        panel.Children.Add(countLbl);

        return new Border
        {
            Child = panel,
            BorderBrush = new SolidColorBrush(Color.Parse("#2A3F55")),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Background = new SolidColorBrush(Color.Parse("#141B22"))
        };
    }

    private DataGrid BuildDataGrid()
    {
        var dg = new DataGrid
        {
            AutoGenerateColumns = false,
            CanUserReorderColumns = false,
            CanUserResizeColumns = true,
            CanUserSortColumns = true,
            IsReadOnly = true,
            Margin = new Thickness(0)
        };

        dg.Bind(DataGrid.ItemsSourceProperty, new Binding("Processes"));
        dg.Bind(DataGrid.SelectedItemProperty, new Binding("SelectedProcess") { Mode = BindingMode.TwoWay });

        dg.Columns.Add(new DataGridTextColumn
        {
            Header = "PID",
            Binding = new Binding("ProcessId"),
            Width = new DataGridLength(70)
        });
        dg.Columns.Add(new DataGridTextColumn
        {
            Header = "进程名",
            Binding = new Binding("ImageName"),
            Width = new DataGridLength(200)
        });
        dg.Columns.Add(new DataGridTextColumn
        {
            Header = "内存 (KB)",
            Binding = new Binding("WorkingSetDisplay"),
            Width = new DataGridLength(100)
        });
        dg.Columns.Add(new DataGridTextColumn
        {
            Header = "线程数",
            Binding = new Binding("ThreadCount"),
            Width = new DataGridLength(80)
        });
        dg.Columns.Add(new DataGridTextColumn
        {
            Header = "父 PID",
            Binding = new Binding("ParentProcessId"),
            Width = new DataGridLength(80)
        });

        var sv = new ScrollViewer
        {
            Content = dg,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        return dg;
    }

    private Border BuildActionBar()
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(16, 8),
            Spacing = 8
        };

        var killBtn = new Button { Content = "⚡ 终止进程", Padding = new Thickness(12, 6) };
        killBtn.Classes.Add("danger");
        killBtn.Bind(Button.CommandProperty, new Binding("KillProcessCommand"));
        killBtn.Bind(IsEnabledProperty, new Binding("SelectedProcess") { Converter = Avalonia.Data.Converters.ObjectConverters.IsNotNull });

        var protectBtn = new Button { Content = "🛡 保护", Padding = new Thickness(12, 6) };
        protectBtn.Bind(Button.CommandProperty, new Binding("ProtectProcessCommand"));
        protectBtn.Bind(IsEnabledProperty, new Binding("SelectedProcess") { Converter = Avalonia.Data.Converters.ObjectConverters.IsNotNull });

        var freezeBtn = new Button { Content = "❄ 冻结", Padding = new Thickness(12, 6) };
        freezeBtn.Bind(Button.CommandProperty, new Binding("FreezeProcessCommand"));
        freezeBtn.Bind(IsEnabledProperty, new Binding("SelectedProcess") { Converter = Avalonia.Data.Converters.ObjectConverters.IsNotNull });

        var modulesBtn = new Button { Content = "📦 模块", Padding = new Thickness(12, 6) };
        modulesBtn.Classes.Add("ghost");
        modulesBtn.Bind(Button.CommandProperty, new Binding("ViewModulesCommand"));
        modulesBtn.Bind(IsEnabledProperty, new Binding("SelectedProcess") { Converter = Avalonia.Data.Converters.ObjectConverters.IsNotNull });

        var threadsBtn = new Button { Content = "🧵 线程", Padding = new Thickness(12, 6) };
        threadsBtn.Classes.Add("ghost");
        threadsBtn.Bind(Button.CommandProperty, new Binding("ViewThreadsCommand"));
        threadsBtn.Bind(IsEnabledProperty, new Binding("SelectedProcess") { Converter = Avalonia.Data.Converters.ObjectConverters.IsNotNull });

        panel.Children.Add(killBtn);
        panel.Children.Add(protectBtn);
        panel.Children.Add(freezeBtn);
        panel.Children.Add(new Border { Width = 1, Background = new SolidColorBrush(Color.Parse("#2A3F55")), Margin = new Thickness(4, 0) });
        panel.Children.Add(modulesBtn);
        panel.Children.Add(threadsBtn);

        return new Border
        {
            Child = panel,
            Background = new SolidColorBrush(Color.Parse("#141B22")),
            BorderBrush = new SolidColorBrush(Color.Parse("#2A3F55")),
            BorderThickness = new Thickness(0, 1, 0, 0)
        };
    }
}
