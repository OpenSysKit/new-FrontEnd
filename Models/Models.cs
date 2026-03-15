using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace OpenSysKit.UI.Models;

public class ProcessInfo
{
    [JsonPropertyName("process_id")] public uint ProcessId { get; set; }
    [JsonPropertyName("parent_process_id")] public uint ParentProcessId { get; set; }
    [JsonPropertyName("thread_count")] public int ThreadCount { get; set; }
    [JsonPropertyName("working_set_size")] public long WorkingSetSize { get; set; }
    [JsonPropertyName("image_name")] public string ImageName { get; set; } = "";
    public string WorkingSetDisplay => WorkingSetSize > 0
        ? $"{WorkingSetSize / 1024.0:F1} K"
        : "—";
}

public class ProcessTreeNode : ProcessInfo, INotifyPropertyChanged
{
    [JsonPropertyName("children")]
    public ObservableCollection<ProcessTreeNode> Children { get; set; } = [];

    private bool _isExpanded = true;

    [JsonIgnore]
    public bool IsExpanded
    {
        get => _isExpanded;
        set { if (_isExpanded != value) { _isExpanded = value; OnPropertyChanged(); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class ProcessListRow : ProcessInfo
{
    public uint RootProcessId { get; set; }
    public string RootProcessName { get; set; } = "";
    public string ParentImageName { get; set; } = "";
    public int Depth { get; set; }
    public bool IsRootProcess { get; set; }
    public bool HasChildren { get; set; }
    public int ChildCount { get; set; }
    public bool IsGroupExpanded { get; set; } = true;
    public string RelationLabel => IsRootProcess
        ? (HasChildren ? $"父进程 · {ChildCount} 个子进程" : "父进程")
        : $"子进程 · 上级 {ParentImageName}";
}

public class NetworkConnection
{
    [JsonPropertyName("protocol")] public string Protocol { get; set; } = "";
    [JsonPropertyName("local_ip")] public string LocalIp { get; set; } = "";
    [JsonPropertyName("local_port")] public int LocalPort { get; set; }
    [JsonPropertyName("remote_ip")] public string RemoteIp { get; set; } = "";
    [JsonPropertyName("remote_port")] public int RemotePort { get; set; }
    [JsonPropertyName("state")] public string State { get; set; } = "";
    [JsonPropertyName("process_id")] public uint ProcessId { get; set; }
    [JsonPropertyName("process_name")] public string ProcessName { get; set; } = "";
    public string LocalEndpoint => $"{LocalIp}:{LocalPort}";
    public string RemoteEndpoint => RemotePort > 0 ? $"{RemoteIp}:{RemotePort}" : "—";
}

public class ServiceInfo
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("display_name")] public string DisplayName { get; set; } = "";
    [JsonPropertyName("state")] public string State { get; set; } = "";
    [JsonPropertyName("start_type")] public string StartType { get; set; } = "";
    public bool IsRunning => State == "running";
}

public class ModuleInfo
{
    [JsonPropertyName("module_name")] public string ModuleName { get; set; } = "";
    [JsonPropertyName("base_address")] public ulong BaseAddress { get; set; }
    [JsonPropertyName("size")] public long Size { get; set; }
    [JsonPropertyName("path")] public string Path { get; set; } = "";
    public string BaseAddressHex => $"0x{BaseAddress:X16}";
    public string SizeDisplay => $"{Size / 1024} KB";
}

public class ThreadInfo
{
    [JsonPropertyName("thread_id")] public uint ThreadId { get; set; }
    [JsonPropertyName("owner_process_id")] public uint OwnerProcessId { get; set; }
    [JsonPropertyName("base_priority")] public int BasePriority { get; set; }
    [JsonPropertyName("delta_priority")] public int DeltaPriority { get; set; }
    [JsonPropertyName("start_address")] public ulong StartAddress { get; set; }
    [JsonPropertyName("is_terminating")] public bool IsTerminating { get; set; }
    public string StartAddressHex => $"0x{StartAddress:X16}";
}

public class HandleTypeInfo
{
    [JsonPropertyName("type_index")] public int TypeIndex { get; set; }
    [JsonPropertyName("type_name")] public string TypeName { get; set; } = "";
    [JsonPropertyName("count")] public int Count { get; set; }
}

public class HealthComponent
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("status")] public string Status { get; set; } = "";
    [JsonPropertyName("message")] public string Message { get; set; } = "";
}

public class AuditEntry
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("timestamp")] public string Timestamp { get; set; } = "";
    [JsonPropertyName("action")] public string Action { get; set; } = "";
    [JsonPropertyName("success")] public bool Success { get; set; }
}

public class StartupEntry
{
    [JsonPropertyName("source")] public string Source { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("display_name")] public string DisplayName { get; set; } = "";
    [JsonPropertyName("state")] public string State { get; set; } = "";
    [JsonPropertyName("run_as")] public string RunAs { get; set; } = "";
    [JsonPropertyName("command")] public string Command { get; set; } = "";
    [JsonPropertyName("trigger")] public string Trigger { get; set; } = "";
}

public class FileEntry
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("path")] public string Path { get; set; } = "";
    [JsonPropertyName("is_dir")] public bool IsDir { get; set; }
    [JsonPropertyName("size")] public long Size { get; set; }
    [JsonPropertyName("mod_time")] public string ModTime { get; set; } = "";
    public string SizeDisplay => IsDir ? "—" : (Size > 1024 * 1024 ? $"{Size / 1024.0 / 1024.0:F1} MB" : $"{Size / 1024.0:F1} KB");
    public string KindLabel => IsDir ? "文件夹" : "文件";
}

public class KernelModule
{
    [JsonPropertyName("base_address")] public ulong BaseAddress { get; set; }
    [JsonPropertyName("size")] public long Size { get; set; }
    [JsonPropertyName("module_name")] public string ModuleName { get; set; } = "";
    [JsonPropertyName("path")] public string Path { get; set; } = "";
    public string BaseAddressHex => $"0x{BaseAddress:X16}";
    public string SizeDisplay => $"{Size / 1024} KB";
    public string ServiceName => System.IO.Path.GetFileNameWithoutExtension(ModuleName);
}

public class HandleDetailInfo
{
    [JsonPropertyName("process_id")] public uint ProcessId { get; set; }
    [JsonPropertyName("handle")] public ulong Handle { get; set; }
    [JsonPropertyName("object_type_index")] public uint ObjectTypeIndex { get; set; }
    [JsonPropertyName("granted_access")] public uint GrantedAccess { get; set; }
    [JsonPropertyName("object_address")] public ulong ObjectAddress { get; set; }
    [JsonPropertyName("type_name")] public string TypeName { get; set; } = "";
    [JsonPropertyName("object_name")] public string ObjectName { get; set; } = "";
    public string HandleHex => $"0x{Handle:X}";
    public string GrantedAccessHex => $"0x{GrantedAccess:X8}";
    public string ObjectAddressHex => $"0x{ObjectAddress:X16}";
}
