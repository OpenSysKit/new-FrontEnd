using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;

namespace OpenSysKit.UI.Views;

public class HandlesPage : UserControl
{
    public HandlesPage()
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var toolbar = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#141B22")),
            BorderBrush = new SolidColorBrush(Color.Parse("#2A3F55")),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(16, 8)
        };
        var sp = new StackPanel { Orientation = Orientation.Horizontal };
        sp.Children.Add(new TextBlock { Text = "句柄信息", Classes = { "heading" }, VerticalAlignment = VerticalAlignment.Center });
        sp.Children.Add(new TextBlock { Text = "  ← 先在进程页选中进程，再来此页查看句柄", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#4A6580")), VerticalAlignment = VerticalAlignment.Center });
        toolbar.Child = sp;
        Grid.SetRow(toolbar, 0);

        var dg = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            CanUserSortColumns = true,
            CanUserResizeColumns = true
        };
        dg.Bind(DataGrid.ItemsSourceProperty, new Binding("HandleTypes"));
        dg.Columns.Add(new DataGridTextColumn { Header = "类型索引", Binding = new Binding("TypeIndex"), Width = new DataGridLength(90) });
        dg.Columns.Add(new DataGridTextColumn { Header = "类型名称", Binding = new Binding("TypeName"), Width = new DataGridLength(200) });
        dg.Columns.Add(new DataGridTextColumn { Header = "句柄数量", Binding = new Binding("Count"), Width = new DataGridLength(100) });
        Grid.SetRow(dg, 1);

        var actionBar = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#141B22")),
            BorderBrush = new SolidColorBrush(Color.Parse("#2A3F55")),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(16, 8)
        };
        var loadBtn = new Button { Content = "加载选中进程句柄", Padding = new Thickness(12, 6) };
        loadBtn.Classes.Add("accent");
        loadBtn.Bind(Button.CommandProperty, new Binding("LoadHandlesCommand"));
        actionBar.Child = loadBtn;
        Grid.SetRow(actionBar, 2);

        grid.Children.Add(toolbar);
        grid.Children.Add(dg);
        grid.Children.Add(actionBar);
        Content = grid;
    }
}
