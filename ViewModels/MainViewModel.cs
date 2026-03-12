using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenSysKit.UI.Models;
using OpenSysKit.UI.Services;

namespace OpenSysKit.UI.ViewModels;

public enum NavPage { Processes, Network, Services, Files, KernelModules, Startup, Audit }

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly RpcClient _rpc = new();
    private CancellationTokenSource? _refreshCts;
    private bool _isConnecting;
    private bool _isRefreshing;

    public MainViewModel()
    {
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
    [ObservableProperty] private bool _showHandlesPanel;
    [ObservableProperty] private string _searchText = "";

    public ObservableCollection<ProcessInfo> Processes { get; } = [];
    public ObservableCollection<ProcessTreeNode> ProcessTree { get; } = [];
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
    public ObservableCollection<HandleDetailInfo> HandleDetails { get; } = [];

    private List<ProcessInfo> _allProcesses = [];
    private List<ProcessTreeNode> _allProcessTreeRoots = [];
    private int _totalProcessCount;
    private List<NetworkConnection> _allConnections = [];
    private List<ServiceInfo> _allServices = [];
    private List<KernelModule> _allKernelModules = [];
    private List<StartupEntry> _allStartupEntries = [];

    [ObservableProperty] private ProcessInfo? _selectedProcess;
    [ObservableProperty] private ProcessTreeNode? _selectedTreeNode;
    [ObservableProperty] private NetworkConnection? _selectedConnection;
    [ObservableProperty] private ServiceInfo? _selectedService;
    [ObservableProperty] private FileEntry? _selectedFile;
    [ObservableProperty] private KernelModule? _selectedKernelModule;
    [ObservableProperty] private HandleDetailInfo? _selectedHandle;
    [ObservableProperty] private string _resolvePortText = "";
    [ObservableProperty] private bool _showHandleDetails;

    public string CurrentPageTitle => CurrentPage switch
    {
        NavPage.Processes => "进程",
        NavPage.Network => "网络",
        NavPage.Services => "服务",
        NavPage.Files => "文件",
        NavPage.KernelModules => "内核模块",
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
        NavPage.Startup => "统一审视开机项与其触发来源。",
        NavPage.Audit => "用时间序列方式浏览近期审计事件。",
        _ => ""
    };

    public string SelectedProcessDisplay => SelectedProcess == null
        ? "未选择进程"
        : $"{SelectedProcess.ImageName} (PID {SelectedProcess.ProcessId})";

    public string ProcessesSummary => $"当前共 {_totalProcessCount} 个进程";
    public string ConnectionsSummary => $"当前共 {Connections.Count} 条连接";
    public string ServicesSummary => $"当前共 {Services.Count} 个服务";
    public string FilesSummary => $"当前目录共 {FileEntries.Count} 项";
    public string KernelModulesSummary => $"当前共 {KernelModules.Count} 个模块";
    public string HandlesSummary => $"当前共 {HandleTypes.Count} 类句柄 / 合计 {HandleTypes.Sum(item => item.Count)}";
    public string HandleDetailsSummary => $"共 {HandleDetails.Count} 个句柄";
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
    public string AutoRefreshDisplay => IsConnected ? "自动刷新已开启" : "等待后端";
    public string CurrentSelectionDisplay => CurrentPage switch
    {
        NavPage.Processes => SelectedProcessDisplay,
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
        SearchText = "";
    }

    partial void OnIsConnectedChanged(bool value)
    {
        OnPropertyChanged(nameof(AutoRefreshDisplay));
    }

    partial void OnSelectedProcessChanged(ProcessInfo? value)
    {
        if (_isRefreshing) return;
        OnPropertyChanged(nameof(SelectedProcessDisplay));
        OnPropertyChanged(nameof(CurrentSelectionDisplay));
    }

    partial void OnSelectedTreeNodeChanged(ProcessTreeNode? value)
    {
        if (_isRefreshing) return;
        SelectedProcess = value;
    }

    partial void OnSelectedConnectionChanged(NetworkConnection? value)
    {
        if (_isRefreshing) return;
        OnPropertyChanged(nameof(SelectedConnectionDisplay));
        OnPropertyChanged(nameof(CurrentSelectionDisplay));
    }

    partial void OnSelectedServiceChanged(ServiceInfo? value)
    {
        if (_isRefreshing) return;
        OnPropertyChanged(nameof(SelectedServiceDisplay));
        OnPropertyChanged(nameof(CurrentSelectionDisplay));
    }

    partial void OnSelectedFileChanged(FileEntry? value)
    {
        if (_isRefreshing) return;
        OnPropertyChanged(nameof(SelectedFileDisplay));
        OnPropertyChanged(nameof(SelectedFileKind));
        OnPropertyChanged(nameof(SelectedFilePath));
        OnPropertyChanged(nameof(CurrentSelectionDisplay));
    }

    partial void OnNetProtocolChanged(string value)
    {
        if (IsConnected && CurrentPage == NavPage.Network)
        {
            _ = LoadNetworkAsync();
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplySearchFilter();
    }

    // ── Connection ──────────────────────────────────────────────

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

    // ── Navigation ──────────────────────────────────────────────

    [RelayCommand]
    private async Task NavigateAsync(NavPage page)
    {
        if (CurrentPage == page) return;
        CurrentPage = page;
        ShowDetailPanel = false;
        ShowHandlesPanel = false;

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

    // ── Process operations ──────────────────────────────────────

    [RelayCommand]
    private async Task KillProcessAsync()
    {
        if (SelectedProcess == null) return;
        try
        {
            var result = await _rpc.CallAsync("Toolkit.KillProcess", new { process_id = SelectedProcess.ProcessId });
            var success = result?["success"]?.GetValue<bool>() == true;
            var method = result?["used_method"]?.GetValue<string>() ?? "";
            StatusMessage = success
                ? $"已终止 {SelectedProcess.ImageName} (PID {SelectedProcess.ProcessId}) [{method}]"
                : $"终止未成功 (PID {SelectedProcess.ProcessId}) [{method}] NT={result?["nt_status"]}";
        }
        catch (RpcException ex) when (ex.Message.Contains("驱动未加载"))
        {
            StatusMessage = "终止失败: 内核驱动未加载，请先启动驱动";
        }
        catch (RpcException ex) when (ex.Message.Contains("can't find"))
        {
            StatusMessage = "终止失败: 后端未提供此方法，请更新后端";
        }
        catch (Exception ex) { StatusMessage = $"终止失败: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task KillProcessTreeAsync()
    {
        if (SelectedProcess == null) return;
        try
        {
            var result = await _rpc.CallAsync("Toolkit.KillProcessTree", new
            {
                process_id = SelectedProcess.ProcessId,
                include_root = true,
                leaves_first = true,
                strict_errors = false
            });
            var results = result?["results"]?.AsArray();
            var successCount = results?.Count(n => n?["success"]?.GetValue<bool>() == true) ?? 0;
            StatusMessage = $"进程树: 成功结束 {successCount} 个";
        }
        catch (Exception ex) { StatusMessage = $"结束进程树失败: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task TaskKillProcessAsync()
    {
        if (SelectedProcess == null) return;
        try
        {
            var result = await _rpc.CallAsync("Toolkit.TaskKillProcess", new
            {
                process_id = SelectedProcess.ProcessId,
                tree = false
            });
            var output = result?["output"]?.GetValue<string>() ?? "";
            var success = result?["success"]?.GetValue<bool>() == true;
            StatusMessage = success
                ? $"普通结束: {SelectedProcess.ImageName} {output}".Trim()
                : $"普通结束未成功: {SelectedProcess.ImageName} {output}".Trim();
        }
        catch (RpcException ex) when (ex.Message.Contains("can't find"))
        {
            StatusMessage = $"普通结束失败: 后端未提供此方法，请更新后端版本";
        }
        catch (Exception ex) { StatusMessage = $"普通结束失败: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task FreezeProcessAsync()
    {
        if (SelectedProcess == null) return;
        try
        {
            await _rpc.CallAsync("Toolkit.FreezeProcess", new { process_id = SelectedProcess.ProcessId });
            StatusMessage = $"已冻结 PID {SelectedProcess.ProcessId}";
        }
        catch (Exception ex) { StatusMessage = $"冻结失败: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task UnfreezeProcessAsync()
    {
        if (SelectedProcess == null) return;
        try
        {
            await _rpc.CallAsync("Toolkit.UnfreezeProcess", new { process_id = SelectedProcess.ProcessId });
            StatusMessage = $"已解冻 PID {SelectedProcess.ProcessId}";
        }
        catch (Exception ex) { StatusMessage = $"解冻失败: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task ProtectProcessAsync()
    {
        if (SelectedProcess == null) return;
        try
        {
            await _rpc.CallAsync("Toolkit.ProtectProcess", new { process_id = SelectedProcess.ProcessId });
            StatusMessage = $"已保护 PID {SelectedProcess.ProcessId}";
        }
        catch (Exception ex) { StatusMessage = $"保护失败: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task UnprotectProcessAsync()
    {
        if (SelectedProcess == null) return;
        try
        {
            await _rpc.CallAsync("Toolkit.UnprotectProcess", new { process_id = SelectedProcess.ProcessId });
            StatusMessage = $"已取消保护 PID {SelectedProcess.ProcessId}";
        }
        catch (Exception ex) { StatusMessage = $"取消保护失败: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task HideProcessAsync()
    {
        if (SelectedProcess == null) return;
        try
        {
            await _rpc.CallAsync("Toolkit.HideProcess", new { process_id = SelectedProcess.ProcessId });
            StatusMessage = $"已隐藏 PID {SelectedProcess.ProcessId}";
        }
        catch (Exception ex) { StatusMessage = $"隐藏失败: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task UnhideProcessAsync()
    {
        if (SelectedProcess == null) return;
        try
        {
            await _rpc.CallAsync("Toolkit.UnhideProcess", new { process_id = SelectedProcess.ProcessId });
            StatusMessage = $"已取消隐藏 PID {SelectedProcess.ProcessId}";
        }
        catch (Exception ex) { StatusMessage = $"取消隐藏失败: {ex.Message}"; }
    }

    // ── Process detail ──────────────────────────────────────────

    [RelayCommand]
    private async Task ViewModulesAsync()
    {
        if (SelectedProcess == null) return;
        try
        {
            var result = await _rpc.CallAsync("Toolkit.EnumProcessModules", new { process_id = SelectedProcess.ProcessId });
            if (result == null) return;

            var modules = result["modules"]?.Deserialize<List<ModuleInfo>>(JsonOptions) ?? [];
            ReplaceCollection(ProcessModules, modules);
            ShowDetailPanel = true;
            StatusMessage = $"模块: {modules.Count} 个";
        }
        catch (Exception ex) { StatusMessage = $"枚举模块失败: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task ViewThreadsAsync()
    {
        if (SelectedProcess == null) return;
        try
        {
            var result = await _rpc.CallAsync("Toolkit.EnumThreads", new { process_id = SelectedProcess.ProcessId });
            if (result == null) return;

            var threads = result["threads"]?.Deserialize<List<ThreadInfo>>(JsonOptions) ?? [];
            ReplaceCollection(ProcessThreads, threads);
            ShowDetailPanel = true;
            StatusMessage = $"线程: {threads.Count} 个";
        }
        catch (Exception ex) { StatusMessage = $"枚举线程失败: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task ViewHandlesAsync()
    {
        if (SelectedProcess == null) return;
        try
        {
            await LoadHandlesAsync();
            ShowHandlesPanel = true;
            ShowDetailPanel = false;
        }
        catch (Exception ex) { StatusMessage = $"枚举句柄失败: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task SuspendThreadAsync(uint? threadId)
    {
        if (threadId is null or 0) return;
        try
        {
            await _rpc.CallAsync("Toolkit.SuspendThread", new { thread_id = threadId.Value });
            StatusMessage = $"已挂起线程 {threadId}";
        }
        catch (Exception ex) { StatusMessage = $"挂起失败: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task ResumeThreadAsync(uint? threadId)
    {
        if (threadId is null or 0) return;
        try
        {
            await _rpc.CallAsync("Toolkit.ResumeThread", new { thread_id = threadId.Value });
            StatusMessage = $"已恢复线程 {threadId}";
        }
        catch (Exception ex) { StatusMessage = $"恢复失败: {ex.Message}"; }
    }

    // ── Handles ─────────────────────────────────────────────────

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
        if (result == null) return;

        var types = result["types"]?.Deserialize<List<HandleTypeInfo>>(JsonOptions) ?? [];
        ReplaceCollection(HandleTypes, types);
        OnPropertyChanged(nameof(HandlesSummary));
        StatusMessage = $"句柄: {SelectedProcess.ImageName} 共 {types.Sum(item => item.Count)} 个";
    }

    // ── Service operations ──────────────────────────────────────

    [RelayCommand]
    private async Task StartServiceAsync()
    {
        if (SelectedService == null) return;
        try
        {
            await _rpc.CallAsync("Toolkit.StartService", new { name = SelectedService.Name });
            StatusMessage = $"服务 {SelectedService.Name} 已启动";
        }
        catch (Exception ex) { StatusMessage = $"启动失败: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task StopServiceAsync()
    {
        if (SelectedService == null) return;
        try
        {
            await _rpc.CallAsync("Toolkit.StopService", new { name = SelectedService.Name });
            StatusMessage = $"服务 {SelectedService.Name} 已停止";
        }
        catch (Exception ex) { StatusMessage = $"停止失败: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task SetServiceStartTypeAsync(string? startType)
    {
        if (SelectedService == null || string.IsNullOrEmpty(startType)) return;
        try
        {
            await _rpc.CallAsync("Toolkit.SetServiceStartType", new { name = SelectedService.Name, start_type = startType });
            StatusMessage = $"服务 {SelectedService.Name} 启动类型已设为 {startType}";
        }
        catch (Exception ex) { StatusMessage = $"修改失败: {ex.Message}"; }
    }

    // ── File operations ─────────────────────────────────────────

    [RelayCommand]
    private async Task DeleteFileAsync()
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
    private async Task KillLockingProcessesAsync()
    {
        if (SelectedFile == null || SelectedFile.IsDir) return;
        try
        {
            var result = await _rpc.CallAsync("Toolkit.KillFileLockingProcesses", new { path = SelectedFile.Path });
            var results = result?["results"]?.AsArray();
            var successCount = results?.Count(node => node?["success"]?.GetValue<bool>() == true) ?? 0;
            StatusMessage = $"已处理占用进程 {successCount} 个";
        }
        catch (Exception ex) { StatusMessage = $"处理占用失败: {ex.Message}"; }
    }

    // ── Report ──────────────────────────────────────────────────

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
        catch (Exception ex) { StatusMessage = $"导出失败: {ex.Message}"; }
    }

    // ── Kernel module operations ──────────────────────────────────

    [RelayCommand]
    private async Task UnloadDriverAsync()
    {
        if (SelectedKernelModule == null) return;
        try
        {
            await _rpc.CallAsync("Toolkit.UnloadDriver", new { service_name = SelectedKernelModule.ServiceName });
            StatusMessage = $"已卸载驱动 {SelectedKernelModule.ModuleName}";
            await LoadKernelModulesAsync();
        }
        catch (Exception ex) { StatusMessage = $"卸载驱动失败: {ex.Message}"; }
    }

    // ── Port conflict resolution ────────────────────────────────

    [RelayCommand]
    private async Task ResolvePortKillAsync()
    {
        if (!ushort.TryParse(ResolvePortText?.Trim(), out var port) || port == 0)
        {
            StatusMessage = "请输入有效端口号 (1-65535)";
            return;
        }
        try
        {
            var result = await _rpc.CallAsync("Toolkit.ResolvePortConflict", new
            {
                port = (int)port,
                protocol = NetProtocol,
                action = "kill"
            });
            StatusMessage = result?["summary"]?.GetValue<string>() ?? "端口处置完成";
        }
        catch (Exception ex) { StatusMessage = $"端口处置失败: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task ResolvePortDisconnectAsync()
    {
        if (!ushort.TryParse(ResolvePortText?.Trim(), out var port) || port == 0)
        {
            StatusMessage = "请输入有效端口号 (1-65535)";
            return;
        }
        try
        {
            var result = await _rpc.CallAsync("Toolkit.ResolvePortConflict", new
            {
                port = (int)port,
                protocol = "tcp",
                action = "disconnect"
            });
            StatusMessage = result?["summary"]?.GetValue<string>() ?? "断开连接完成";
        }
        catch (Exception ex) { StatusMessage = $"断开连接失败: {ex.Message}"; }
    }

    // ── Handle details ──────────────────────────────────────────

    [RelayCommand]
    private async Task ViewHandleDetailsAsync()
    {
        if (SelectedProcess == null) return;
        try
        {
            var result = await _rpc.CallAsync("Toolkit.ListHandles", new { process_id = SelectedProcess.ProcessId });
            if (result == null) return;

            var handles = result["handles"]?.Deserialize<List<HandleDetailInfo>>(JsonOptions) ?? [];
            ReplaceCollection(HandleDetails, handles);
            ShowHandleDetails = true;
            ShowHandlesPanel = true;
            OnPropertyChanged(nameof(HandleDetailsSummary));
            StatusMessage = $"句柄详情: {SelectedProcess.ImageName} 共 {handles.Count} 个";
        }
        catch (Exception ex) { StatusMessage = $"枚举句柄详情失败: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task CloseSelectedHandleAsync()
    {
        if (SelectedProcess == null || SelectedHandle == null) return;
        try
        {
            await _rpc.CallAsync("Toolkit.CloseHandle", new
            {
                process_id = SelectedProcess.ProcessId,
                handle = SelectedHandle.Handle
            });
            StatusMessage = $"已关闭句柄 {SelectedHandle.HandleHex} ({SelectedHandle.TypeName})";
            await ViewHandleDetailsAsync();
        }
        catch (Exception ex) { StatusMessage = $"关闭句柄失败: {ex.Message}"; }
    }

    // ── Search filter ───────────────────────────────────────────

    private void ApplySearchFilter()
    {
        var q = SearchText?.Trim() ?? "";
        _isRefreshing = true;
        try
        {
            switch (CurrentPage)
            {
                case NavPage.Processes:
                    var fp = string.IsNullOrEmpty(q)
                        ? _allProcessTreeRoots
                        : FilterProcessTree(_allProcessTreeRoots, q);
                    ReplaceCollection(ProcessTree, fp);
                    OnPropertyChanged(nameof(ProcessesSummary));
                    break;
                case NavPage.Network:
                    var fn = string.IsNullOrEmpty(q)
                        ? _allConnections
                        : _allConnections.Where(c =>
                            c.ProcessName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                            c.LocalEndpoint.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                            c.RemoteEndpoint.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                            c.ProcessId.ToString().Contains(q)).ToList();
                    ReplaceCollection(Connections, fn);
                    OnPropertyChanged(nameof(ConnectionsSummary));
                    break;
                case NavPage.Services:
                    var fs = string.IsNullOrEmpty(q)
                        ? _allServices
                        : _allServices.Where(s =>
                            s.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                            s.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
                    ReplaceCollection(Services, fs);
                    OnPropertyChanged(nameof(ServicesSummary));
                    break;
                case NavPage.KernelModules:
                    var fk = string.IsNullOrEmpty(q)
                        ? _allKernelModules
                        : _allKernelModules.Where(m =>
                            m.ModuleName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                            m.Path.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
                    ReplaceCollection(KernelModules, fk);
                    OnPropertyChanged(nameof(KernelModulesSummary));
                    break;
                case NavPage.Startup:
                    var fst = string.IsNullOrEmpty(q)
                        ? _allStartupEntries
                        : _allStartupEntries.Where(e =>
                            e.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                            e.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                            e.Command.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
                    ReplaceCollection(StartupEntries, fst);
                    OnPropertyChanged(nameof(StartupSummary));
                    break;
            }
        }
        finally { _isRefreshing = false; }
    }

    // ── Data loading ────────────────────────────────────────────

    private async Task LoadCurrentPageAsync()
    {
        IsLoading = true;
        _isRefreshing = true;

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
            IsConnected = _rpc.IsConnected;
        }
        finally
        {
            _isRefreshing = false;
            IsLoading = false;
        }
    }

    private async Task LoadProcessesAsync()
    {
        var selectedPid = SelectedProcess?.ProcessId;

        var result = await _rpc.CallAsync("Toolkit.GetProcessTree");
        if (result == null) return;

        _totalProcessCount = result["total"]?.GetValue<int>() ?? 0;
        _allProcessTreeRoots = result["roots"]?.Deserialize<List<ProcessTreeNode>>(JsonOptions) ?? [];

        var q = SearchText?.Trim() ?? "";
        var filtered = string.IsNullOrEmpty(q)
            ? _allProcessTreeRoots
            : FilterProcessTree(_allProcessTreeRoots, q);

        ReplaceCollection(ProcessTree, filtered);

        if (selectedPid != null)
            SelectedTreeNode = FindNodeByPid(ProcessTree, selectedPid.Value);

        OnPropertyChanged(nameof(ProcessesSummary));
        StatusMessage = $"进程: {_totalProcessCount} 个";
    }

    private static ProcessTreeNode? FindNodeByPid(IEnumerable<ProcessTreeNode> roots, uint pid)
    {
        foreach (var node in roots)
        {
            if (node.ProcessId == pid) return node;
            var found = FindNodeByPid(node.Children, pid);
            if (found != null) return found;
        }
        return null;
    }

    private static List<ProcessTreeNode> FilterProcessTree(List<ProcessTreeNode> roots, string query)
    {
        var result = new List<ProcessTreeNode>();
        foreach (var root in roots)
        {
            var filtered = FilterNode(root, query);
            if (filtered != null)
                result.Add(filtered);
        }
        return result;
    }

    private static ProcessTreeNode? FilterNode(ProcessTreeNode node, string query)
    {
        bool selfMatch = (node.ImageName ?? "").Contains(query, StringComparison.OrdinalIgnoreCase)
                         || node.ProcessId.ToString().Contains(query);

        var filteredChildren = new ObservableCollection<ProcessTreeNode>();
        foreach (var child in node.Children ?? [])
        {
            var fc = FilterNode(child, query);
            if (fc != null) filteredChildren.Add(fc);
        }

        if (!selfMatch && filteredChildren.Count == 0)
            return null;

        return new ProcessTreeNode
        {
            ProcessId = node.ProcessId,
            ParentProcessId = node.ParentProcessId,
            ThreadCount = node.ThreadCount,
            WorkingSetSize = node.WorkingSetSize,
            ImageName = node.ImageName,
            Children = filteredChildren,
            IsExpanded = true
        };
    }

    private async Task LoadNetworkAsync()
    {
        var result = await _rpc.CallAsync("Toolkit.EnumNetworkConnections", new { protocol = NetProtocol });
        if (result == null) return;

        _allConnections = result["connections"]?.Deserialize<List<NetworkConnection>>(JsonOptions) ?? [];

        var q = SearchText?.Trim() ?? "";
        var filtered = string.IsNullOrEmpty(q)
            ? _allConnections
            : _allConnections.Where(c =>
                c.ProcessName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                c.LocalEndpoint.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                c.RemoteEndpoint.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                c.ProcessId.ToString().Contains(q)).ToList();

        ReplaceCollection(Connections, filtered);
        OnPropertyChanged(nameof(ConnectionsSummary));
        StatusMessage = $"网络: {_allConnections.Count} 条连接";
    }

    private async Task LoadServicesAsync()
    {
        var result = await _rpc.CallAsync("Toolkit.ListServices", new { name_like = "" });
        if (result == null) return;

        _allServices = result["services"]?.Deserialize<List<ServiceInfo>>(JsonOptions) ?? [];

        var q = SearchText?.Trim() ?? "";
        var filtered = string.IsNullOrEmpty(q)
            ? _allServices
            : _allServices.Where(s =>
                s.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                s.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();

        ReplaceCollection(Services, filtered);
        OnPropertyChanged(nameof(ServicesSummary));
        StatusMessage = $"服务: {_allServices.Count} 个";
    }

    private async Task LoadFilesAsync()
    {
        var selectedPath = SelectedFile?.Path;
        var result = await _rpc.CallAsync("Toolkit.ListDirectory", new { path = CurrentPath });
        if (result == null) return;

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
        if (result == null) return;

        _allKernelModules = result["modules"]?.Deserialize<List<KernelModule>>(JsonOptions) ?? [];

        var q = SearchText?.Trim() ?? "";
        var filtered = string.IsNullOrEmpty(q)
            ? _allKernelModules
            : _allKernelModules.Where(m =>
                m.ModuleName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                m.Path.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();

        ReplaceCollection(KernelModules, filtered);
        OnPropertyChanged(nameof(KernelModulesSummary));
        StatusMessage = $"内核模块: {_allKernelModules.Count} 个";
    }

    private async Task LoadStartupAsync()
    {
        var result = await _rpc.CallAsync("Toolkit.ListStartupEntries", new { category = "all", name_like = "" });
        if (result == null) return;

        _allStartupEntries = result["entries"]?.Deserialize<List<StartupEntry>>(JsonOptions) ?? [];

        var q = SearchText?.Trim() ?? "";
        var filtered = string.IsNullOrEmpty(q)
            ? _allStartupEntries
            : _allStartupEntries.Where(e =>
                e.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                e.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                e.Command.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();

        ReplaceCollection(StartupEntries, filtered);
        OnPropertyChanged(nameof(StartupSummary));
        StatusMessage = $"启动项: {_allStartupEntries.Count} 个";
    }

    private async Task LoadAuditAsync()
    {
        var result = await _rpc.CallAsync("Toolkit.GetAuditLogs", new { limit = 200 });
        if (result == null) return;

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
            if (result == null) return;

            OverallHealth = result["overall_status"]?.GetValue<string>() ?? "—";
            var components = result["components"]?.Deserialize<List<HealthComponent>>(JsonOptions) ?? [];
            ReplaceCollection(HealthComponents, components);
        }
        catch
        {
        }
    }

    // ── Auto refresh with reconnect ─────────────────────────────

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
                if (token.IsCancellationRequested) break;

                try
                {
                    await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        await LoadCurrentPageAsync();
                        IsConnected = _rpc.IsConnected;
                    }, DispatcherPriority.Background);
                }
                catch
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        IsConnected = false;
                        StatusMessage = "连接已断开，正在重连...";
                    });

                    try
                    {
                        await _rpc.ConnectAsync();
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            IsConnected = true;
                            StatusMessage = "已重新连接";
                        });
                    }
                    catch
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            StatusMessage = "重连失败，稍后重试...";
                        });
                    }
                }
            }
        }, token);
    }

    private static void ReplaceCollection<T>(ObservableCollection<T> target, IList<T> source)
    {
        while (target.Count > source.Count)
        {
            target.RemoveAt(target.Count - 1);
        }

        for (int i = 0; i < target.Count; i++)
        {
            target[i] = source[i];
        }

        for (int i = target.Count; i < source.Count; i++)
        {
            target.Add(source[i]);
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
