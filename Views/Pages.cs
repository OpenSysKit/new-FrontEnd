using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Data;
using OpenSysKit.UI.Models;
using OpenSysKit.UI.Converters;

namespace OpenSysKit.UI.Views;

// ══════════════════════════════════════════
// NetworkPage
// ══════════════════════════════════════════
public class NetworkPage : UserControl
{
    public NetworkPage()
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Toolbar
        var toolbar = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#141B22")),
            BorderBrush = new SolidColorBrush(Color.Parse("#2A3F55")),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(16, 8)
        };
        var tbPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        var heading = new TextBlock { Text = "网络连接", Classes = { "heading" }, VerticalAlignment = VerticalAlignment.Center };

        var protoBox = new ComboBox { Width = 100 };
        protoBox.Items.Add("all");
        protoBox.Items.Add("tcp");
        protoBox.Items.Add("udp");
        protoBox.Bind(ComboBox.SelectedItemProperty, new Binding("NetProtocol") { Mode = BindingMode.TwoWay });

        var countLbl = new TextBlock { VerticalAlignment = VerticalAlignment.Center, FontSize = 11 };
        countLbl.Bind(TextBlock.TextProperty, new Binding("Connections.Count") { StringFormat = "{0} 条连接" });
        countLbl.Classes.Add("muted");

        tbPanel.Children.Add(heading);
        tbPanel.Children.Add(protoBox);
        tbPanel.Children.Add(countLbl);
        toolbar.Child = tbPanel;
        Grid.SetRow(toolbar, 0);

        // DataGrid
        var dg = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            CanUserSortColumns = true,
            CanUserResizeColumns = true
        };
        dg.Bind(DataGrid.ItemsSourceProperty, new Binding("Connections"));
        dg.Bind(DataGrid.SelectedItemProperty, new Binding("SelectedConnection") { Mode = BindingMode.TwoWay });

        dg.Columns.Add(new DataGridTextColumn { Header = "协议", Binding = new Binding("Protocol"), Width = new DataGridLength(60) });
        dg.Columns.Add(new DataGridTextColumn { Header = "本地地址", Binding = new Binding("LocalEndpoint"), Width = new DataGridLength(160) });
        dg.Columns.Add(new DataGridTextColumn { Header = "远端地址", Binding = new Binding("RemoteEndpoint"), Width = new DataGridLength(160) });
        dg.Columns.Add(new DataGridTextColumn { Header = "状态", Binding = new Binding("State"), Width = new DataGridLength(100) });
        dg.Columns.Add(new DataGridTextColumn { Header = "PID", Binding = new Binding("ProcessId"), Width = new DataGridLength(70) });
        dg.Columns.Add(new DataGridTextColumn { Header = "进程名", Binding = new Binding("ProcessName"), Width = new DataGridLength(200) });
        Grid.SetRow(dg, 1);

        // Action bar
        var actionBar = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#141B22")),
            BorderBrush = new SolidColorBrush(Color.Parse("#2A3F55")),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(16, 8)
        };
        var abPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };

        var killPortBtn = new Button { Content = "⚡ 结束占用进程", Padding = new Thickness(12, 6) };
        killPortBtn.Classes.Add("danger");
        // Could wire to ResolvePortConflict

        abPanel.Children.Add(killPortBtn);
        actionBar.Child = abPanel;
        Grid.SetRow(actionBar, 2);

        grid.Children.Add(toolbar);
        grid.Children.Add(dg);
        grid.Children.Add(actionBar);
        Content = grid;
    }
}

// ══════════════════════════════════════════
// ServicesPage
// ══════════════════════════════════════════
public class ServicesPage : UserControl
{
    public ServicesPage()
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Toolbar
        var toolbar = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#141B22")),
            BorderBrush = new SolidColorBrush(Color.Parse("#2A3F55")),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(16, 8)
        };
        var heading = new TextBlock { Text = "系统服务", Classes = { "heading" } };
        toolbar.Child = heading;
        Grid.SetRow(toolbar, 0);

        // DataGrid
        var dg = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            CanUserSortColumns = true,
            CanUserResizeColumns = true
        };
        dg.Bind(DataGrid.ItemsSourceProperty, new Binding("Services"));
        dg.Bind(DataGrid.SelectedItemProperty, new Binding("SelectedService") { Mode = BindingMode.TwoWay });

        dg.Columns.Add(new DataGridTextColumn { Header = "服务名", Binding = new Binding("Name"), Width = new DataGridLength(180) });
        dg.Columns.Add(new DataGridTextColumn { Header = "显示名称", Binding = new Binding("DisplayName"), Width = new DataGridLength(260) });
        dg.Columns.Add(new DataGridTextColumn { Header = "状态", Binding = new Binding("State"), Width = new DataGridLength(90) });
        dg.Columns.Add(new DataGridTextColumn { Header = "启动类型", Binding = new Binding("StartType"), Width = new DataGridLength(100) });
        Grid.SetRow(dg, 1);

        // Action bar
        var actionBar = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#141B22")),
            BorderBrush = new SolidColorBrush(Color.Parse("#2A3F55")),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(16, 8)
        };
        var abPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };

        var startBtn = new Button { Content = "▶ 启动", Padding = new Thickness(12, 6) };
        startBtn.Classes.Add("accent");
        startBtn.Bind(Button.CommandProperty, new Binding("StartServiceCommand"));
        startBtn.Bind(IsEnabledProperty, new Binding("SelectedService") { Converter = Avalonia.Data.Converters.ObjectConverters.IsNotNull });

        var stopBtn = new Button { Content = "■ 停止", Padding = new Thickness(12, 6) };
        stopBtn.Classes.Add("danger");
        stopBtn.Bind(Button.CommandProperty, new Binding("StopServiceCommand"));
        stopBtn.Bind(IsEnabledProperty, new Binding("SelectedService") { Converter = Avalonia.Data.Converters.ObjectConverters.IsNotNull });

        abPanel.Children.Add(startBtn);
        abPanel.Children.Add(stopBtn);
        actionBar.Child = abPanel;
        Grid.SetRow(actionBar, 2);

        grid.Children.Add(toolbar);
        grid.Children.Add(dg);
        grid.Children.Add(actionBar);
        Content = grid;
    }
}

// ══════════════════════════════════════════
// FilesPage
// ══════════════════════════════════════════
public class FilesPage : UserControl
{
    public FilesPage()
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Toolbar
        var toolbar = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#141B22")),
            BorderBrush = new SolidColorBrush(Color.Parse("#2A3F55")),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(16, 8)
        };
        var heading = new TextBlock { Text = "文件浏览", Classes = { "heading" } };
        toolbar.Child = heading;
        Grid.SetRow(toolbar, 0);

        // Path bar
        var pathBar = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#1A2332")),
            BorderBrush = new SolidColorBrush(Color.Parse("#2A3F55")),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(16, 6)
        };
        var pathPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var pathLbl = new TextBlock { Text = "路径：", Classes = { "muted" }, VerticalAlignment = VerticalAlignment.Center, FontSize = 11 };
        var pathTb = new TextBox { Width = 400, FontSize = 12, Padding = new Thickness(8, 4) };
        pathTb.Bind(TextBox.TextProperty, new Binding("CurrentPath") { Mode = BindingMode.TwoWay });
        var goBtn = new Button { Content = "→ 跳转", Padding = new Thickness(10, 4), FontSize = 12 };
        goBtn.Bind(Button.CommandProperty, new Binding("NavigateToCommand"));
        goBtn.Bind(Button.CommandParameterProperty, new Binding("CurrentPath"));
        pathPanel.Children.Add(pathLbl);
        pathPanel.Children.Add(pathTb);
        pathPanel.Children.Add(goBtn);
        pathBar.Child = pathPanel;
        Grid.SetRow(pathBar, 1);

        // DataGrid
        var dg = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            CanUserSortColumns = true,
            CanUserResizeColumns = true
        };
        dg.Bind(DataGrid.ItemsSourceProperty, new Binding("FileEntries"));
        dg.Bind(DataGrid.SelectedItemProperty, new Binding("SelectedFile") { Mode = BindingMode.TwoWay });

        dg.Columns.Add(new DataGridTextColumn { Header = "类型", Binding = new Binding("TypeIcon"), Width = new DataGridLength(45) });
        dg.Columns.Add(new DataGridTextColumn { Header = "名称", Binding = new Binding("Name"), Width = new DataGridLength(280) });
        dg.Columns.Add(new DataGridTextColumn { Header = "大小", Binding = new Binding("SizeDisplay"), Width = new DataGridLength(100) });
        dg.Columns.Add(new DataGridTextColumn { Header = "修改时间", Binding = new Binding("ModTime"), Width = new DataGridLength(180) });
        dg.Columns.Add(new DataGridTextColumn { Header = "路径", Binding = new Binding("Path"), Width = new DataGridLength(400) });
        Grid.SetRow(dg, 2);

        // Action bar
        var actionBar = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#141B22")),
            BorderBrush = new SolidColorBrush(Color.Parse("#2A3F55")),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(16, 8)
        };
        var abPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };

        var deleteBtn = new Button { Content = "🗑 内核删除", Padding = new Thickness(12, 6) };
        deleteBtn.Classes.Add("danger");
        deleteBtn.Bind(Button.CommandProperty, new Binding("DeleteFileCommand"));
        deleteBtn.Bind(IsEnabledProperty, new Binding("SelectedFile") { Converter = Avalonia.Data.Converters.ObjectConverters.IsNotNull });

        abPanel.Children.Add(deleteBtn);
        actionBar.Child = abPanel;
        Grid.SetRow(actionBar, 3);

        grid.Children.Add(toolbar);
        grid.Children.Add(pathBar);
        grid.Children.Add(dg);
        grid.Children.Add(actionBar);
        Content = grid;
    }
}

// ══════════════════════════════════════════
// KernelModulesPage
// ══════════════════════════════════════════
public class KernelModulesPage : UserControl
{
    public KernelModulesPage()
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var toolbar = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#141B22")),
            BorderBrush = new SolidColorBrush(Color.Parse("#2A3F55")),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(16, 8)
        };
        toolbar.Child = new TextBlock { Text = "内核模块", Classes = { "heading" } };
        Grid.SetRow(toolbar, 0);

        var dg = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            CanUserSortColumns = true,
            CanUserResizeColumns = true
        };
        dg.Bind(DataGrid.ItemsSourceProperty, new Binding("KernelModules"));
        dg.Columns.Add(new DataGridTextColumn { Header = "模块名", Binding = new Binding("ModuleName"), Width = new DataGridLength(200) });
        dg.Columns.Add(new DataGridTextColumn { Header = "基地址", Binding = new Binding("BaseAddressHex"), Width = new DataGridLength(180) });
        dg.Columns.Add(new DataGridTextColumn { Header = "大小", Binding = new Binding("SizeDisplay"), Width = new DataGridLength(90) });
        dg.Columns.Add(new DataGridTextColumn { Header = "路径", Binding = new Binding("Path"), Width = new DataGridLength(400) });
        Grid.SetRow(dg, 1);

        grid.Children.Add(toolbar);
        grid.Children.Add(dg);
        Content = grid;
    }
}

// ══════════════════════════════════════════
// StartupPage
// ══════════════════════════════════════════
public class StartupPage : UserControl
{
    public StartupPage()
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var toolbar = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#141B22")),
            BorderBrush = new SolidColorBrush(Color.Parse("#2A3F55")),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(16, 8)
        };
        toolbar.Child = new TextBlock { Text = "启动项管理", Classes = { "heading" } };
        Grid.SetRow(toolbar, 0);

        var dg = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            CanUserSortColumns = true,
            CanUserResizeColumns = true
        };
        dg.Bind(DataGrid.ItemsSourceProperty, new Binding("StartupEntries"));
        dg.Columns.Add(new DataGridTextColumn { Header = "来源", Binding = new Binding("Source"), Width = new DataGridLength(80) });
        dg.Columns.Add(new DataGridTextColumn { Header = "名称", Binding = new Binding("Name"), Width = new DataGridLength(160) });
        dg.Columns.Add(new DataGridTextColumn { Header = "显示名", Binding = new Binding("DisplayName"), Width = new DataGridLength(200) });
        dg.Columns.Add(new DataGridTextColumn { Header = "状态", Binding = new Binding("State"), Width = new DataGridLength(90) });
        dg.Columns.Add(new DataGridTextColumn { Header = "运行账户", Binding = new Binding("RunAs"), Width = new DataGridLength(120) });
        dg.Columns.Add(new DataGridTextColumn { Header = "触发器", Binding = new Binding("Trigger"), Width = new DataGridLength(120) });
        dg.Columns.Add(new DataGridTextColumn { Header = "命令", Binding = new Binding("Command"), Width = new DataGridLength(300) });
        Grid.SetRow(dg, 1);

        grid.Children.Add(toolbar);
        grid.Children.Add(dg);
        Content = grid;
    }
}

// ══════════════════════════════════════════
// AuditPage
// ══════════════════════════════════════════
public class AuditPage : UserControl
{
    public AuditPage()
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var toolbar = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#141B22")),
            BorderBrush = new SolidColorBrush(Color.Parse("#2A3F55")),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(16, 8)
        };
        toolbar.Child = new TextBlock { Text = "操作日志", Classes = { "heading" } };
        Grid.SetRow(toolbar, 0);

        var dg = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            CanUserSortColumns = true,
            CanUserResizeColumns = true
        };
        dg.Bind(DataGrid.ItemsSourceProperty, new Binding("AuditEntries"));
        dg.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new Binding("Id"), Width = new DataGridLength(60) });
        dg.Columns.Add(new DataGridTextColumn { Header = "时间", Binding = new Binding("Timestamp"), Width = new DataGridLength(200) });
        dg.Columns.Add(new DataGridTextColumn { Header = "操作", Binding = new Binding("Action"), Width = new DataGridLength(180) });
        dg.Columns.Add(new DataGridTextColumn { Header = "成功", Binding = new Binding("Success"), Width = new DataGridLength(80) });
        Grid.SetRow(dg, 1);

        grid.Children.Add(toolbar);
        grid.Children.Add(dg);
        Content = grid;
    }
}
