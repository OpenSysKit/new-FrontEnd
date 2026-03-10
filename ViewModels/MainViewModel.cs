using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using OpenSysKit.UI.Models;
using OpenSysKit.UI.Services;

namespace OpenSysKit.UI.ViewModels;

public enum NavPage { Processes, Network, Services, Files, KernelModules, Handles, Startup, Audit }

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly RpcClient _rpc = new();
    private readonly DispatcherQueue _dispatcherQueue;
    private CancellationTokenSource? _refreshCts;
    private bool _isConnecting;

    public MainViewModel(DispatcherQueue dispatcherQueue)
    {
        _dispatcherQueue = dispatcherQueue;
    }

    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private string _statusMessage = "未连接";
    [ObservableProperty] private NavPage _currentPage = NavPage.Processes;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _netProtocol = "all";
    [ObservableProperty] private string _currentPath = @"C:\";
    [ObservableProperty] private string _parentPath = "";
    [ObservableProperty] private string _overallHealth = "—";
    [ObservableProperty] private bool _showDetailPanel;

    public ObservableCollection<ProcessInfo> Processes { get; } = [];
    public ObservableCollection<NetworkConnection> Connections { get; } = [];
    public ObservableCollection<ServiceInfo> Services { get; } = [];
    public ObservableCollection<FileEntry> FileEntries { get; } = [];
    public ObservableCollection<KernelModule> KernelModules { get; } = [];
    public ObservableCollection<HandleTypeInfo> HandleTypes { get; } = [];
    public ObservableCollection<StartupEntry> StartupEntries { get; } = [];
    public ObservableCollection<AuditEntry> AuditEntries { get; } = [];
    public ObservableCollection<HealthComponent> HealthComponents { get; } = [];
    public ObservableCollection<ModuleInfo> ProcessModules { get; } = [];
    public ObservableCollection<ThreadInfo> ProcessThreads { get; } = [];

    [ObservableProperty] private ProcessInfo? _selectedProcess;
    [ObservableProperty] private NetworkConnection? _selectedConnection;
    [ObservableProperty] private ServiceInfo? _selectedService;
    [ObservableProperty] private FileEntry? _selectedFile;

    public string CurrentPageTitle => CurrentPage switch
    {
        NavPage.Processes => "进程",
        NavPage.Network => "网络",
        NavPage.Services => "服务",
        NavPage.Files => "文件",
        NavPage.KernelModules => "内核模块",
        NavPage.Handles => "句柄",
        NavPage.Startup => "启动项",
        NavPage.Audit => "审计日志",
        _ => "OpenSysKit"
    };

    public string CurrentPageSubtitle => CurrentPage switch
    {
        NavPage.Processes => "以任务管理器风格集中查看进程、模块和线程活动。",
        NavPage.Network => "用紧凑表格查看连接、状态和归属进程。",
        NavPage.Services => "面向运维操作的服务状态与启停控制台。",
        NavPage.Files => "像 Explorer 一样直接进入目录并查看选中项信息。",
        NavPage.KernelModules => "快速查看已加载驱动与模块映射。",
        NavPage.Handles => "切页即载入当前目标进程的句柄类型统计。",
        NavPage.Startup => "统一审视开机项与其触发来源。",
        NavPage.Audit => "用时间序列方式浏览近期审计事件。",
        _ => ""
    };

    public string SelectedProcessDisplay => SelectedProcess == null
        ? "未选择进程"
        : $"{SelectedProcess.ImageName} (PID {SelectedProcess.ProcessId})";

    public string ProcessesSummary => $"当前共 {Processes.Count} 个进程";
    public string ConnectionsSummary => $"当前共 {Connections.Count} 条连接";
    public string ServicesSummary => $"当前共 {Services.Count} 个服务";
    public string FilesSummary => $"当前目录共 {FileEntries.Count} 项";
    public string KernelModulesSummary => $"当前共 {KernelModules.Count} 个模块";
    public string HandlesSummary => $"当前共 {HandleTypes.Count} 类句柄 / 合计 {HandleTypes.Sum(item => item.Count)}";
    public string StartupSummary => $"当前共 {StartupEntries.Count} 个启动项";
    public string AuditSummary => $"当前共 {AuditEntries.Count} 条审计日志";
    public string SelectedConnectionDisplay => SelectedConnection == null
        ? "未选择连接"
        : $"{SelectedConnection.ProcessName} ({SelectedConnection.LocalEndpoint})";
    public string SelectedServiceDisplay => SelectedService == null
        ? "未选择服务"
        : $"{SelectedService.DisplayName} ({SelectedService.State})";
    public string SelectedFileDisplay => SelectedFile == null ? "未选择项目" : SelectedFile.Name;
    public string SelectedFileKind => SelectedFile == null ? "—" : SelectedFile.KindLabel;
    public string SelectedFilePath => SelectedFile == null ? "—" : SelectedFile.Path;
    public Visibility DetailPanelVisibility => ShowDetailPanel ? Visibility.Visible : Visibility.Collapsed;
    public string AutoRefreshDisplay => IsConnected ? "自动刷新已开启" : "等待后端";
    public string CurrentSelectionDisplay => CurrentPage switch
    {
        NavPage.Processes or NavPage.Handles => SelectedProcessDisplay,
        NavPage.Network => SelectedConnectionDisplay,
        NavPage.Services => SelectedServiceDisplay,
        NavPage.Files => SelectedFileDisplay,
        _ => StatusMessage
    };

    partial void OnCurrentPageChanged(NavPage value)
    {
        OnPropertyChanged(nameof(CurrentPageTitle));
        OnPropertyChanged(nameof(CurrentPageSubtitle));
        OnPropertyChanged(nameof(CurrentSelectionDisplay));
    }

    partial void OnIsConnectedChanged(bool value)
    {
        OnPropertyChanged(nameof(AutoRefreshDisplay));
    }

    partial void OnSelectedProcessChanged(ProcessInfo? value)
    {
        OnPropertyChanged(nameof(SelectedProcessDisplay));
        OnPropertyChanged(nameof(CurrentSelectionDisplay));

        if (CurrentPage == NavPage.Handles && IsConnected)
        {
            _ = LoadHandlesAsync();
        }
    }

    partial void OnSelectedConnectionChanged(NetworkConnection? value)
    {
        OnPropertyChanged(nameof(SelectedConnectionDisplay));
        OnPropertyChanged(nameof(CurrentSelectionDisplay));
    }

    partial void OnSelectedServiceChanged(ServiceInfo? value)
    {
        OnPropertyChanged(nameof(SelectedServiceDisplay));
        OnPropertyChanged(nameof(CurrentSelectionDisplay));
    }

    partial void OnSelectedFileChanged(FileEntry? value)
    {
        OnPropertyChanged(nameof(SelectedFileDisplay));
        OnPropertyChanged(nameof(SelectedFileKind));
        OnPropertyChanged(nameof(SelectedFilePath));
        OnPropertyChanged(nameof(CurrentSelectionDisplay));
    }

    partial void OnShowDetailPanelChanged(bool value)
    {
        OnPropertyChanged(nameof(DetailPanelVisibility));
    }

    public async Task EnsureConnectedAsync()
    {
        if (IsConnected || _isConnecting)
        {
            return;
        }

        await ConnectCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (IsConnected || _isConnecting)
        {
            return;
        }

        try
        {
            _isConnecting = true;
            IsLoading = true;
            StatusMessage = "正在连接...";
            await _rpc.ConnectAsync();
            await _rpc.CallAsync("Toolkit.Ping");
            IsConnected = true;
            StatusMessage = "已连接";
            await LoadCurrentPageAsync();
            StartAutoRefresh();
        }
        catch (Exception ex)
        {
            IsConnected = false;
            StatusMessage = $"连接失败: {ex.Message}";
        }
        finally
        {
            _isConnecting = false;
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task NavigateAsync(NavPage page)
    {
        CurrentPage = page;
        ShowDetailPanel = false;

        if (IsConnected)
        {
            await LoadCurrentPageAsync();
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsConnected)
        {
            await LoadCurrentPageAsync();
        }
    }

    [RelayCommand]
    private async Task NavigateToAsync(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        CurrentPath = path.Trim();
        await LoadFilesAsync();
    }

    [RelayCommand]
    private async Task NavigateUpAsync()
    {
        if (string.IsNullOrWhiteSpace(ParentPath))
        {
            return;
        }

        CurrentPath = ParentPath;
        await LoadFilesAsync();
    }

    [RelayCommand]
    private async Task OpenSelectedFileAsync()
    {
        if (SelectedFile?.IsDir == true)
        {
            CurrentPath = SelectedFile.Path;
            await LoadFilesAsync();
        }
    }

    [RelayCommand]
    private async Task LoadHandlesAsync()
    {
        if (SelectedProcess == null)
        {
            HandleTypes.Clear();
            StatusMessage = "句柄: 未选择进程";
            return;
        }

        var result = await _rpc.CallAsync("Toolkit.EnumHandles", new { process_id = SelectedProcess.ProcessId });
        if (result == null)
        {
            return;
        }

        var types = result["types"]?.Deserialize<List<HandleTypeInfo>>(JsonOptions) ?? [];
        ReplaceCollection(HandleTypes, types);
        OnPropertyChanged(nameof(HandlesSummary));
        StatusMessage = $"句柄: {SelectedProcess.ImageName} 共 {types.Sum(item => item.Count)} 个";
    }

    [RelayCommand]
    private async Task KillProcessAsync()
    {
        if (SelectedProcess == null)
        {
            return;
        }

        try
        {
            await _rpc.CallAsync("Toolkit.KillProcess", new { process_id = SelectedProcess.ProcessId });
            StatusMessage = $"已终止进程 {SelectedProcess.ImageName} (PID {SelectedProcess.ProcessId})";
            await LoadProcessesAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"终止失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task TaskKillProcessAsync()
    {
        if (SelectedProcess == null)
        {
            return;
        }

        try
        {
            var result = await _rpc.CallAsync("Toolkit.TaskKillProcess", new
            {
                process_id = SelectedProcess.ProcessId,
                tree = false
            });
            StatusMessage = $"普通结束: {SelectedProcess.ImageName} {result?["output"]?.GetValue<string>()}".Trim();
            await LoadProcessesAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"普通结束失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ProtectProcessAsync()
    {
        if (SelectedProcess == null)
        {
            return;
        }

        try
        {
            await _rpc.CallAsync("Toolkit.ProtectProcess", new { process_id = SelectedProcess.ProcessId });
            StatusMessage = $"已保护进程 PID {SelectedProcess.ProcessId}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"保护失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task FreezeProcessAsync()
    {
        if (SelectedProcess == null)
        {
            return;
        }

        try
        {
            await _rpc.CallAsync("Toolkit.FreezeProcess", new { process_id = SelectedProcess.ProcessId });
            StatusMessage = $"已冻结进程 PID {SelectedProcess.ProcessId}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"冻结失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task UnfreezeProcessAsync()
    {
        if (SelectedProcess == null)
        {
            return;
        }

        try
        {
            await _rpc.CallAsync("Toolkit.UnfreezeProcess", new { process_id = SelectedProcess.ProcessId });
            StatusMessage = $"已解冻进程 PID {SelectedProcess.ProcessId}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"解冻失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ViewModulesAsync()
    {
        if (SelectedProcess == null)
        {
            return;
        }

        var result = await _rpc.CallAsync("Toolkit.EnumProcessModules", new { process_id = SelectedProcess.ProcessId });
        if (result == null)
        {
            return;
        }

        var modules = result["modules"]?.Deserialize<List<ModuleInfo>>(JsonOptions) ?? [];
        ReplaceCollection(ProcessModules, modules);
        ShowDetailPanel = true;
        StatusMessage = $"模块: {modules.Count} 个";
    }

    [RelayCommand]
    private async Task ViewThreadsAsync()
    {
        if (SelectedProcess == null)
        {
            return;
        }

        var result = await _rpc.CallAsync("Toolkit.EnumThreads", new { process_id = SelectedProcess.ProcessId });
        if (result == null)
        {
            return;
        }

        var threads = result["threads"]?.Deserialize<List<ThreadInfo>>(JsonOptions) ?? [];
        ReplaceCollection(ProcessThreads, threads);
        ShowDetailPanel = true;
        StatusMessage = $"线程: {threads.Count} 个";
    }

    [RelayCommand]
    private async Task StartServiceAsync()
    {
        if (SelectedService == null)
        {
            return;
        }

        try
        {
            await _rpc.CallAsync("Toolkit.StartService", new { name = SelectedService.Name });
            StatusMessage = $"服务 {SelectedService.Name} 已启动";
            await LoadServicesAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"启动失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task StopServiceAsync()
    {
        if (SelectedService == null)
        {
            return;
        }

        try
        {
            await _rpc.CallAsync("Toolkit.StopService", new { name = SelectedService.Name });
            StatusMessage = $"服务 {SelectedService.Name} 已停止";
            await LoadServicesAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"停止失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteFileAsync()
    {
        if (SelectedFile == null || SelectedFile.IsDir)
        {
            return;
        }

        try
        {
            await _rpc.CallAsync("Toolkit.DeleteFileKernel", new { path = SelectedFile.Path });
            StatusMessage = $"已删除 {SelectedFile.Name}";
            await LoadFilesAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"删除失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task KillLockingProcessesAsync()
    {
        if (SelectedFile == null || SelectedFile.IsDir)
        {
            return;
        }

        try
        {
            var result = await _rpc.CallAsync("Toolkit.KillFileLockingProcesses", new { path = SelectedFile.Path });
            var results = result?["results"]?.AsArray();
            var successCount = results?.Count(node => node?["success"]?.GetValue<bool>() == true) ?? 0;
            StatusMessage = $"已处理占用进程 {successCount} 个";
        }
        catch (Exception ex)
        {
            StatusMessage = $"处理占用失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ExportReportAsync()
    {
        try
        {
            var result = await _rpc.CallAsync("Toolkit.ExportReport", new
            {
                path = "",
                include_audit = true,
                audit_limit = 200
            });

            StatusMessage = $"报告已导出: {result?["path"]?.GetValue<string>()}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"导出失败: {ex.Message}";
        }
    }

    private async Task LoadCurrentPageAsync()
    {
        IsLoading = true;

        try
        {
            switch (CurrentPage)
            {
                case NavPage.Processes:
                    await LoadProcessesAsync();
                    break;
                case NavPage.Network:
                    await LoadNetworkAsync();
                    break;
                case NavPage.Services:
                    await LoadServicesAsync();
                    break;
                case NavPage.Files:
                    await LoadFilesAsync();
                    break;
                case NavPage.KernelModules:
                    await LoadKernelModulesAsync();
                    break;
                case NavPage.Handles:
                    await EnsureHandlesAsync();
                    break;
                case NavPage.Startup:
                    await LoadStartupAsync();
                    break;
                case NavPage.Audit:
                    await LoadAuditAsync();
                    break;
            }

            await LoadHealthAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"错误: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task EnsureHandlesAsync()
    {
        if (SelectedProcess == null)
        {
            if (Processes.Count == 0)
            {
                await LoadProcessesAsync();
            }

            SelectedProcess = Processes.FirstOrDefault();
        }

        await LoadHandlesAsync();
    }

    private async Task LoadProcessesAsync()
    {
        var selectedPid = SelectedProcess?.ProcessId;
        var result = await _rpc.CallAsync("Toolkit.EnumProcesses");
        if (result == null)
        {
            return;
        }

        var processes = result["processes"]?.Deserialize<List<ProcessInfo>>(JsonOptions) ?? [];
        ReplaceCollection(Processes, processes);
        SelectedProcess = selectedPid == null
            ? Processes.FirstOrDefault()
            : Processes.FirstOrDefault(item => item.ProcessId == selectedPid);

        OnPropertyChanged(nameof(ProcessesSummary));
        StatusMessage = $"进程: {processes.Count} 个";
    }

    private async Task LoadNetworkAsync()
    {
        var result = await _rpc.CallAsync("Toolkit.EnumNetworkConnections", new { protocol = NetProtocol });
        if (result == null)
        {
            return;
        }

        var connections = result["connections"]?.Deserialize<List<NetworkConnection>>(JsonOptions) ?? [];
        ReplaceCollection(Connections, connections);
        OnPropertyChanged(nameof(ConnectionsSummary));
        StatusMessage = $"网络: {connections.Count} 条连接";
    }

    private async Task LoadServicesAsync()
    {
        var result = await _rpc.CallAsync("Toolkit.ListServices", new { name_like = "" });
        if (result == null)
        {
            return;
        }

        var services = result["services"]?.Deserialize<List<ServiceInfo>>(JsonOptions) ?? [];
        ReplaceCollection(Services, services);
        OnPropertyChanged(nameof(ServicesSummary));
        StatusMessage = $"服务: {services.Count} 个";
    }

    private async Task LoadFilesAsync()
    {
        var selectedPath = SelectedFile?.Path;
        var result = await _rpc.CallAsync("Toolkit.ListDirectory", new { path = CurrentPath });
        if (result == null)
        {
            return;
        }

        CurrentPath = result["current_path"]?.GetValue<string>() ?? CurrentPath;
        ParentPath = result["parent_path"]?.GetValue<string>() ?? "";
        var files = result["entries"]?.Deserialize<List<FileEntry>>(JsonOptions) ?? [];
        ReplaceCollection(FileEntries, files);
        SelectedFile = selectedPath == null
            ? FileEntries.FirstOrDefault()
            : FileEntries.FirstOrDefault(item => string.Equals(item.Path, selectedPath, StringComparison.OrdinalIgnoreCase))
                ?? FileEntries.FirstOrDefault();
        OnPropertyChanged(nameof(FilesSummary));
        StatusMessage = $"目录: {files.Count} 项";
    }

    private async Task LoadKernelModulesAsync()
    {
        var result = await _rpc.CallAsync("Toolkit.EnumKernelModules");
        if (result == null)
        {
            return;
        }

        var modules = result["modules"]?.Deserialize<List<KernelModule>>(JsonOptions) ?? [];
        ReplaceCollection(KernelModules, modules);
        OnPropertyChanged(nameof(KernelModulesSummary));
        StatusMessage = $"内核模块: {modules.Count} 个";
    }

    private async Task LoadStartupAsync()
    {
        var result = await _rpc.CallAsync("Toolkit.ListStartupEntries", new { category = "all", name_like = "" });
        if (result == null)
        {
            return;
        }

        var entries = result["entries"]?.Deserialize<List<StartupEntry>>(JsonOptions) ?? [];
        ReplaceCollection(StartupEntries, entries);
        OnPropertyChanged(nameof(StartupSummary));
        StatusMessage = $"启动项: {entries.Count} 个";
    }

    private async Task LoadAuditAsync()
    {
        var result = await _rpc.CallAsync("Toolkit.GetAuditLogs", new { limit = 200 });
        if (result == null)
        {
            return;
        }

        var entries = result["entries"]?.Deserialize<List<AuditEntry>>(JsonOptions) ?? [];
        ReplaceCollection(AuditEntries, entries);
        OnPropertyChanged(nameof(AuditSummary));
        StatusMessage = $"审计日志: {entries.Count} 条";
    }

    private async Task LoadHealthAsync()
    {
        try
        {
            var result = await _rpc.CallAsync("Toolkit.HealthCheck");
            if (result == null)
            {
                return;
            }

            OverallHealth = result["overall_status"]?.GetValue<string>() ?? "—";
            var components = result["components"]?.Deserialize<List<HealthComponent>>(JsonOptions) ?? [];
            ReplaceCollection(HealthComponents, components);
        }
        catch
        {
        }
    }

    private void StartAutoRefresh()
    {
        _refreshCts?.Cancel();
        _refreshCts = new CancellationTokenSource();
        var token = _refreshCts.Token;

        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), token);

                if (!token.IsCancellationRequested && IsConnected)
                {
                    _dispatcherQueue.TryEnqueue(async () => await LoadCurrentPageAsync());
                }
            }
        }, token);
    }

    private static void ReplaceCollection<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }

    public void Dispose()
    {
        _refreshCts?.Cancel();
        _refreshCts?.Dispose();
        _rpc.Dispose();
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
}
