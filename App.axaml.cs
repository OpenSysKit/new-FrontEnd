using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace OpenSysKit.UI;

public partial class App : Application
{
    public static bool IsExiting { get; private set; }

    private static MainWindow? _mainWindow;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _mainWindow = new MainWindow();
            desktop.MainWindow = _mainWindow;
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void TrayIcon_OnClicked(object? sender, EventArgs e) => ShowMainWindow();
    private void TrayOpen_OnClick(object? sender, EventArgs e) => ShowMainWindow();

    private void TrayExit_OnClick(object? sender, EventArgs e)
    {
        IsExiting = true;
        _mainWindow?.Close();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    private static void ShowMainWindow()
    {
        if (_mainWindow is null) return;
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    public static void ShowTrayBalloon(string title, string text)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        TrayBalloonHelper.Show(title, text);
    }
}

internal static partial class TrayBalloonHelper
{
    private const int NIF_INFO = 0x00000010;
    private const int NIF_ICON = 0x00000002;
    private const int NIF_TIP = 0x00000004;
    private const int NIF_MESSAGE = 0x00000001;
    private const int NIM_ADD = 0x00000000;
    private const int NIM_MODIFY = 0x00000001;
    private const int NIM_DELETE = 0x00000002;
    private const int NIIF_INFO = 0x00000001;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uID;
        public int uFlags;
        public int uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public int dwInfoFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public int dwState;
        public int dwStateMask;
    }

    [LibraryImport("shell32.dll", EntryPoint = "Shell_NotifyIconW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool Shell_NotifyIcon(int dwMessage, ref NOTIFYICONDATA lpData);

    [LibraryImport("kernel32.dll", EntryPoint = "GetConsoleWindow")]
    private static partial IntPtr GetConsoleWindow();

    private static bool _added;

    public static void Show(string title, string text)
    {
        var nid = new NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = GetConsoleWindow(),
            uID = 1,
            uFlags = NIF_INFO | NIF_TIP,
            szTip = "OpenSysKit",
            szInfo = text,
            szInfoTitle = title,
            dwInfoFlags = NIIF_INFO
        };

        if (!_added)
        {
            nid.uFlags |= NIF_MESSAGE;
            Shell_NotifyIcon(NIM_ADD, ref nid);
            _added = true;
        }
        Shell_NotifyIcon(NIM_MODIFY, ref nid);
    }
}
