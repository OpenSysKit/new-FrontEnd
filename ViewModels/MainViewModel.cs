using System;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenSysKit.UI.Models;
using OpenSysKit.UI.Services;

namespace OpenSysKit.UI.ViewModels;

public enum NavPage { Processes, Network, Services, Files, KernelModules, Handles, Startup, Audit }

public partial class MainViewModel : ObservableObject
{
    private readonly RpcClient _rpc = new();
    private CancellationTokenSource? _refreshCts;

    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private string _statusMessage = "未连接";
    [ObservableProperty] private NavPage _currentPage = NavPage.Processes;
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private bool _isLoading;

    // ──── Processes ────
    public ObservableCollection<ProcessInfo> Processes { get; } = [];
    [ObservableProperty] private ProcessInfo? _selectedProcess;

    // ──── Network ────
    public ObservableCollection<NetworkConnection> Connections { get; } = [];
    [ObservableProperty] private NetworkConnection? _selectedConnection;
    [ObservableProperty] private string _netProtocol = "all";

    // ──── Services ────
    public ObservableCollection<ServiceInfo> Services { get; } = [];
    [ObservableProperty] private ServiceInfo? _selectedService;

    // ──── Files ────
    public ObservableCollection<FileEntry> FileEntries { get; } = [];
    [ObservableProperty] private string _currentPath = "C:\\";
    [ObservableProperty] private FileEntry? _selectedFile;

    // ──── Kernel Modules ────
    public ObservableCollection<KernelModule> KernelModules { get; } = [];

    // ──── Handles ────
    public ObservableCollection<HandleTypeInfo> HandleTypes { get; } = [];

    // ──── Startup ────
    public ObservableCollection<StartupEntry> StartupEntries { get; } = [];

    // ──── Audit ────
    public ObservableCollection<AuditEntry> AuditEntries { get; } = [];

    // ──── Health ────
    public ObservableCollection<HealthComponent> HealthComponents { get; } = [];
    [ObservableProperty] private string _overallHealth = "—";

    // ──── Detail panel ────
    public ObservableCollection<ModuleInfo> ProcessModules { get; } = [];
    public ObservableCollection<ThreadInfo> ProcessThreads { get; } = [];
    [ObservableProperty] private bool _showDetailPanel;

    [RelayCommand]
    async Task ConnectAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "正在连接...";
            await _rpc.ConnectAsync();
            var r = await _rpc.CallAsync("Toolkit.Ping");
            IsConnected = true;
            StatusMessage = "已连接";
            await LoadCurrentPageAsync();
            StartAutoRefresh();
        }
        catch (Exception ex)
        {
            StatusMessage = $"连接失败: {ex.Message}";
            IsConnected = false;
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    async Task NavigateAsync(NavPage page)
    {
        CurrentPage = page;
        ShowDetailPanel = false;
        SearchText = "";
        if (IsConnected) await LoadCurrentPageAsync();
    }

    [RelayCommand]
    async Task RefreshAsync()
    {
        if (!IsConnected) return;
        await LoadCurrentPageAsync();
    }

    async Task LoadCurrentPageAsync()
    {
        IsLoading = true;
        try
        {
            switch (CurrentPage)
            {
                case NavPage.Processes: await LoadProcessesAsync(); break;
                case NavPage.Network: await LoadNetworkAsync(); break;
                case NavPage.Services: await LoadServicesAsync(); break;
                case NavPage.Files: await LoadFilesAsync(); break;
                case NavPage.KernelModules: await LoadKernelModulesAsync(); break;
                case NavPage.Handles: /* loaded on demand */ break;
                case NavPage.Startup: await LoadStartupAsync(); break;
                case NavPage.Audit: await LoadAuditAsync(); break;
            }
            await LoadHealthAsync();
        }
        catch (Exception ex) { StatusMessage = $"错误: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    async Task LoadProcessesAsync()
    {
        var r = await _rpc.CallAsync("Toolkit.EnumProcesses");
        if (r == null) return;
        var procs = r["processes"].Deserialize<System.Collections.Generic.List<ProcessInfo>>(JsonOpts) ?? [];
        Processes.Clear();
        foreach (var p in procs) Processes.Add(p);
        StatusMessage = $"进程: {procs.Count} 个";
    }

    async Task LoadNetworkAsync()
    {
        var r = await _rpc.CallAsync("Toolkit.EnumNetworkConnections", new { protocol = NetProtocol });
        if (r == null) return;
        var conns = r["connections"].Deserialize<System.Collections.Generic.List<NetworkConnection>>(JsonOpts) ?? [];
        Connections.Clear();
        foreach (var c in conns) Connections.Add(c);
        StatusMessage = $"连接: {conns.Count} 条";
    }

    async Task LoadServicesAsync()
    {
        var r = await _rpc.CallAsync("Toolkit.ListServices", new { name_like = "" });
        if (r == null) return;
        var svcs = r["services"].Deserialize<System.Collections.Generic.List<ServiceInfo>>(JsonOpts) ?? [];
        Services.Clear();
        foreach (var s in svcs) Services.Add(s);
        StatusMessage = $"服务: {svcs.Count} 个";
    }

    async Task LoadFilesAsync()
    {
        var r = await _rpc.CallAsync("Toolkit.ListDirectory", new { path = CurrentPath });
        if (r == null) return;
        var entries = r["entries"].Deserialize<System.Collections.Generic.List<FileEntry>>(JsonOpts) ?? [];
        CurrentPath = r["current_path"]?.GetValue<string>() ?? CurrentPath;
        FileEntries.Clear();
        foreach (var e in entries) FileEntries.Add(e);
    }

    async Task LoadKernelModulesAsync()
    {
        var r = await _rpc.CallAsync("Toolkit.EnumKernelModules");
        if (r == null) return;
        var mods = r["modules"].Deserialize<System.Collections.Generic.List<KernelModule>>(JsonOpts) ?? [];
        KernelModules.Clear();
        foreach (var m in mods) KernelModules.Add(m);
        StatusMessage = $"内核模块: {mods.Count} 个";
    }

    [RelayCommand]
    async Task LoadHandlesAsync()
    {
        if (SelectedProcess == null) return;
        var r = await _rpc.CallAsync("Toolkit.EnumHandles", new { process_id = SelectedProcess.ProcessId });
        if (r == null) return;
        var types = r["types"].Deserialize<System.Collections.Generic.List<HandleTypeInfo>>(JsonOpts) ?? [];
        HandleTypes.Clear();
        foreach (var t in types) HandleTypes.Add(t);
    }

    async Task LoadStartupAsync()
    {
        var r = await _rpc.CallAsync("Toolkit.ListStartupEntries", new { category = "all", name_like = "" });
        if (r == null) return;
        var entries = r["entries"].Deserialize<System.Collections.Generic.List<StartupEntry>>(JsonOpts) ?? [];
        StartupEntries.Clear();
        foreach (var e in entries) StartupEntries.Add(e);
        StatusMessage = $"启动项: {entries.Count} 个";
    }

    async Task LoadAuditAsync()
    {
        var r = await _rpc.CallAsync("Toolkit.GetAuditLogs", new { limit = 200 });
        if (r == null) return;
        var entries = r["entries"].Deserialize<System.Collections.Generic.List<AuditEntry>>(JsonOpts) ?? [];
        AuditEntries.Clear();
        foreach (var e in entries) AuditEntries.Add(e);
        StatusMessage = $"审计日志: {entries.Count} 条";
    }

    async Task LoadHealthAsync()
    {
        try
        {
            var r = await _rpc.CallAsync("Toolkit.HealthCheck");
            if (r == null) return;
            OverallHealth = r["overall_status"]?.GetValue<string>() ?? "—";
            var comps = r["components"].Deserialize<System.Collections.Generic.List<HealthComponent>>(JsonOpts) ?? [];
            HealthComponents.Clear();
            foreach (var c in comps) HealthComponents.Add(c);
        }
        catch { }
    }

    // ──── Process actions ────
    [RelayCommand]
    async Task KillProcessAsync()
    {
        if (SelectedProcess == null) return;
        try
        {
            await _rpc.CallAsync("Toolkit.KillProcess", new { process_id = SelectedProcess.ProcessId });
            StatusMessage = $"已终止进程 {SelectedProcess.ImageName} (PID {SelectedProcess.ProcessId})";
            await LoadProcessesAsync();
        }
        catch (Exception ex) { StatusMessage = $"终止失败: {ex.Message}"; }
    }

    [RelayCommand]
    async Task ProtectProcessAsync()
    {
        if (SelectedProcess == null) return;
        try
        {
            await _rpc.CallAsync("Toolkit.ProtectProcess", new { process_id = SelectedProcess.ProcessId });
            StatusMessage = $"已保护进程 PID {SelectedProcess.ProcessId}";
        }
        catch (Exception ex) { StatusMessage = $"保护失败: {ex.Message}"; }
    }

    [RelayCommand]
    async Task FreezeProcessAsync()
    {
        if (SelectedProcess == null) return;
        try
        {
            await _rpc.CallAsync("Toolkit.FreezeProcess", new { process_id = SelectedProcess.ProcessId });
            StatusMessage = $"已冻结进程 PID {SelectedProcess.ProcessId}";
        }
        catch (Exception ex) { StatusMessage = $"冻结失败: {ex.Message}"; }
    }

    [RelayCommand]
    async Task ViewModulesAsync()
    {
        if (SelectedProcess == null) return;
        var r = await _rpc.CallAsync("Toolkit.EnumProcessModules", new { process_id = SelectedProcess.ProcessId });
        if (r == null) return;
        var mods = r["modules"].Deserialize<System.Collections.Generic.List<ModuleInfo>>(JsonOpts) ?? [];
        ProcessModules.Clear();
        foreach (var m in mods) ProcessModules.Add(m);
        ShowDetailPanel = true;
    }

    [RelayCommand]
    async Task ViewThreadsAsync()
    {
        if (SelectedProcess == null) return;
        var r = await _rpc.CallAsync("Toolkit.EnumThreads", new { process_id = SelectedProcess.ProcessId });
        if (r == null) return;
        var threads = r["threads"].Deserialize<System.Collections.Generic.List<ThreadInfo>>(JsonOpts) ?? [];
        ProcessThreads.Clear();
        foreach (var t in threads) ProcessThreads.Add(t);
        ShowDetailPanel = true;
    }

    // ──── Service actions ────
    [RelayCommand]
    async Task StartServiceAsync()
    {
        if (SelectedService == null) return;
        try
        {
            await _rpc.CallAsync("Toolkit.StartService", new { name = SelectedService.Name });
            StatusMessage = $"服务 {SelectedService.Name} 已启动";
            await LoadServicesAsync();
        }
        catch (Exception ex) { StatusMessage = $"启动失败: {ex.Message}"; }
    }

    [RelayCommand]
    async Task StopServiceAsync()
    {
        if (SelectedService == null) return;
        try
        {
            await _rpc.CallAsync("Toolkit.StopService", new { name = SelectedService.Name });
            StatusMessage = $"服务 {SelectedService.Name} 已停止";
            await LoadServicesAsync();
        }
        catch (Exception ex) { StatusMessage = $"停止失败: {ex.Message}"; }
    }

    // ──── File actions ────
    [RelayCommand]
    async Task NavigateToAsync(string path)
    {
        CurrentPath = path;
        await LoadFilesAsync();
    }

    [RelayCommand]
    async Task DeleteFileAsync()
    {
        if (SelectedFile == null || SelectedFile.IsDir) return;
        try
        {
            await _rpc.CallAsync("Toolkit.DeleteFileKernel", new { path = SelectedFile.Path });
            StatusMessage = $"已删除 {SelectedFile.Name}";
            await LoadFilesAsync();
        }
        catch (Exception ex) { StatusMessage = $"删除失败: {ex.Message}"; }
    }

    [RelayCommand]
    async Task ExportReportAsync()
    {
        try
        {
            var r = await _rpc.CallAsync("Toolkit.ExportReport", new { path = "", include_audit = true, audit_limit = 200 });
            StatusMessage = $"报告已导出: {r?["path"]?.GetValue<string>()}";
        }
        catch (Exception ex) { StatusMessage = $"导出失败: {ex.Message}"; }
    }

    void StartAutoRefresh()
    {
        _refreshCts?.Cancel();
        _refreshCts = new CancellationTokenSource();
        var ct = _refreshCts.Token;
        Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(5000, ct);
                if (!ct.IsCancellationRequested && IsConnected)
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(LoadCurrentPageAsync);
            }
        }, ct);
    }

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
}
