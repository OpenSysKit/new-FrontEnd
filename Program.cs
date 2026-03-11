using Avalonia;
using Avalonia.Win32;
using System;
using System.IO;
using System.Threading.Tasks;

namespace OpenSysKit.UI;

internal sealed class Program
{
    private static readonly string CrashLogDir =
        Path.Combine(AppContext.BaseDirectory, "crash-logs");

    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            WriteCrashLog("Main_Catch", ex);
            throw;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .With(new Win32PlatformOptions
            {
                RenderingMode = [Win32RenderingMode.AngleEgl, Win32RenderingMode.Software],
                CompositionMode = [Win32CompositionMode.WinUIComposition, Win32CompositionMode.RedirectionSurface],
            })
            .LogToTrace();

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        WriteCrashLog("UnhandledException", e.ExceptionObject as Exception);
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        WriteCrashLog("UnobservedTaskException", e.Exception);
        e.SetObserved();
    }

    private static void WriteCrashLog(string source, Exception? ex)
    {
        try
        {
            Directory.CreateDirectory(CrashLogDir);
            var fileName = $"crash_{DateTime.Now:yyyyMMdd_HHmmss}_{source}.log";
            var path = Path.Combine(CrashLogDir, fileName);
            var content = $"""
                === OpenSysKit Crash Report ===
                Time:   {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}
                Source: {source}
                
                {ex}
                """;
            File.WriteAllText(path, content);
        }
        catch
        {
            // last resort: nothing we can do
        }
    }
}
