using Avalonia;

namespace OpenSysKit.UI;

// Main 入口在 App.axaml.cs 的 Program 类里
public static class AppBuilderHelper
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
